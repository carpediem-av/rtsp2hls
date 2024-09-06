// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RTSPLiveServer
{
    //Thread-Safe
    class Capture
    {
		readonly string _imageCacheDir;
        readonly string _ffmpegBinary;
        Logger _logger;

        List<CaptureJob> _jobs = new();
        object _sync = new();

        public Capture(string imageCacheDir, string ffmpegDir, Logger logger)
        {
            _imageCacheDir = imageCacheDir;
            _logger = logger;

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _ffmpegBinary = Path.Combine(ffmpegDir, isWindows ? "ffmpeg-win64.exe" : "ffmpeg-linux64");
        }

		// return: filesystem path to image or null if failed
		public string Run(string camID)
		{
			var cam = ConfigManager.Current.FindCam(camID);

			if (cam == null)
			{
				_logger.Log("Capture failed! Camera not found: ID " + camID);
				return null;
			}

			var imageFilename = Path.Combine(_imageCacheDir, camID + ".jpg");

			if (File.Exists(imageFilename))
			{
				if (!IsCachedImageExpired(imageFilename)) return imageFilename;
				else DeleteCachedImage(imageFilename);
			}

			var job = FindOrCreateJob(cam, imageFilename);
			job.WaitForComplete();
			return job.Error == null ? imageFilename : null;
		}

        void DeleteCachedImage(string imageFilename)
        {
            try
            {
                File.Delete(imageFilename);
            }
            catch (Exception e)
            {
                _logger.Log($"Exception while try to delete file \"{imageFilename}\". {e.Message}");
            }
        }

        bool IsCachedImageExpired(string filename)
        {
			try
			{
				var lastModified = File.GetLastWriteTime(filename);
				return (DateTime.Now - lastModified).TotalSeconds > ConfigManager.Current.capture_cache_expire;
			}
			catch (Exception e)
            {
                _logger.Log($"Exception while try to get last write time of file \"{filename}\". {e.Message}"); 
				return true; 
			}
        }

		CaptureJob FindOrCreateJob(CameraItem cam, string outputFilename)
		{
			lock (_sync)
			{
                var job = _jobs.Where(x => x.CamID == cam.id).FirstOrDefault();
				
				if (job != null)
				{
                    _logger.Log($"Capture process for camera ID {cam.id} already started");
                    return job;
				}
				else
                {
                    _logger.Log($"Start capture process for camera ID {cam.id}");

                    job = new CaptureJob(cam, outputFilename, _ffmpegBinary);
                    _jobs.Add(job);
                    RemoveJobAfterComplete(cam, job);

                    return job;
                }
            }
        }

        void RemoveJobAfterComplete(CameraItem cam, CaptureJob job)
        {
            Task.Run(() =>
            {
                job.WaitForComplete();

                string result = job.Error == null ? "completed" : $"failed. {job.Error}";
                _logger.Log($"Capture process for camera ID {cam.id} {result}");

                lock (_sync) _jobs.Remove(job);
            });
        }
    }

	//Thread-Safe
	class CaptureJob
    {
        readonly string _camID;
        readonly string _outputFilename;
        readonly string _ffmpegBinary;
		readonly int _maxExecutionTime = ConfigManager.Current.capture_timeout * 1000;	//milliseconds

        bool _completed;
		string _error;
        object _sync = new();

		public bool IsCompleted
		{
			get
			{
				lock (_sync) return _completed;
			}
		}
		
		public string Error
		{
			get
			{
				lock (_sync) return _error;
			}
		}
        
		public string CamID => _camID;

        const string ffmpegArgsTemplate =
            "-hide_banner -loglevel error -nostats " +
            "-y %rtspTransportTCP% " +
            "-stimeout %rtspTimeout% " +
            "-i \"%rtsp_uri%\" " +
            "-vframes 1 -q:v %jpeg_quality% -f singlejpeg \"%path%\"";
				
        public CaptureJob(CameraItem cam, string outputFilename, string ffmpegBinary)
		{
			_camID = cam.id;
			_outputFilename = outputFilename;
			_ffmpegBinary = ffmpegBinary;
			Run(PrepareProcess(cam));
		}

        Process PrepareProcess(CameraItem cam)
        {
            var ffmpegProcess = new Process();

            ffmpegProcess.StartInfo.Arguments = ffmpegArgsTemplate
                .Replace("%rtsp_uri%", cam.rtsp_uri)
                .Replace("%rtspTransportTCP%", cam.transport_tcp ? "-rtsp_transport tcp" : "")
                .Replace("%rtspTimeout%", ConfigManager.Current.rtsp_timeout.ToString())
                .Replace("%path%", _outputFilename)
                .Replace("%jpeg_quality%", ConfigManager.Current.capture_jpeg_quality.ToString());

            ffmpegProcess.StartInfo.FileName = _ffmpegBinary;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.StartInfo.UseShellExecute = false;
			
			return ffmpegProcess;
        }

        void Run(Process ffmpegProcess)
		{
			Task.Run(() => {
				try
				{
					ffmpegProcess.Start();

					if (!ffmpegProcess.WaitForExit(_maxExecutionTime))
					{
						lock (_sync) _error = "Timeout exceeded waiting for ffmpeg process exit.";

						try { ffmpegProcess.Kill(); }
						catch { }
					}
					else if (!File.Exists(_outputFilename))
					{
						lock (_sync) _error = "Unknown FFMpeg error, missing output image file (camera may be turned off or faulty).";
					}
                }
				catch (Exception e)
				{
					lock (_sync) _error = e.Message;
				}
                        
				lock (_sync) _completed = true;
			});
		}

		public void WaitForComplete()
		{
			while (true)
			{
				lock (_sync) 
					if (_completed) break;
				
				Task.Delay(50).Wait();
			}
		}
    }
}
