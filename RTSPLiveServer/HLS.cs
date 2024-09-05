// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace RTSPLiveServer
{
    class HLSWorkerItem
    {
        HLSWorker _worker;
        DateTime _lastCanary;

        public HLSWorker Worker => _worker;

        public DateTime LastCanary
        {
            get => _lastCanary;
            set => _lastCanary = value;
        }

        public HLSWorkerItem(HLSWorker worker)
        {
            _worker = worker;
            _lastCanary = DateTime.Now;
        }
    }

    // Thread-Safe
    class HLS
	{
        readonly string _workingDir;

		Logger _logger;
        bool _disposed = false;

		List<HLSWorkerItem> _workers = new();
		object _sync = new();

        CancellationTokenSource _watchdogTokenSource;
        CancellationToken _watchdogCancellationToken;
        Thread _watchdogThread;

        public string WorkingDir => _workingDir;

        public HLS(string workingDir, string ffmpegDir, Logger logger)
        {
            _workingDir = workingDir;
			_logger = logger;

            foreach (var cam in ConfigManager.Current.cameraList)
            {
                HLSWorker worker = new(logger, ffmpegDir, workingDir, cam);
                HLSWorkerItem workerItem = new(worker);
                lock (_sync) _workers.Add(workerItem);
            }

            _watchdogTokenSource = new CancellationTokenSource();
            _watchdogCancellationToken = _watchdogTokenSource.Token;

            _watchdogThread = new Thread(WorkerUpdater);
            _watchdogThread.Start();
        }

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("HLS");
            _disposed = true;
            
            _watchdogTokenSource.Cancel();
            _watchdogThread.Join();

            lock (_sync)
            {
                foreach (var workerItem in _workers)
                    workerItem.Worker.Dispose();

                _workers.Clear();
            }
        }

        ~HLS()
        {
            if (!_disposed) Dispose();
        }

        void StopIrrelevantWorkers()
        {
            lock (_sync)
                foreach (var workerItem in _workers)
                {
                    if (workerItem.Worker.TurnedOn && (DateTime.Now - workerItem.LastCanary).TotalSeconds > ConfigManager.Current.hls_no_canary_time_before_stop)
                    {
                        _logger.Log($"Found no canary for camera ID {workerItem.Worker.CamID}");
                        workerItem.Worker.Stop();
                    }
                }
        }

        void WorkerUpdater()
        {
            List<HLSWorker> workersCopy = new List<HLSWorker>();

            lock (_sync) 
                foreach (var wi in _workers) 
                    workersCopy.Add(wi.Worker);

            while (true)
            {
                try
                {
                    StopIrrelevantWorkers();

                    foreach (var worker in workersCopy)
                        worker.Update();
                }
                catch (Exception e)
                {
                    _logger.Log($"Watchdog exception. {e.Message}");
                }

                Thread.Sleep(ConfigManager.Current.hls_process_check_interval * 10);
                if (_watchdogCancellationToken.IsCancellationRequested) return;
            }
        }

        public string Run(string camID)
        {
            if (_disposed) throw new ObjectDisposedException("HLS");
                        
            lock (_sync)
            {
                var workerItem = _workers.Where(x => x.Worker.CamID == camID).SingleOrDefault();

                if (workerItem != null)
                {
                    if (!workerItem.Worker.TurnedOn) 
                    {
                        workerItem.LastCanary = DateTime.Now;
                        workerItem.Worker.Start(); 
                    }

                    return workerItem.Worker.PlaylistFilename;
                }
                else
                {
                    _logger.Log($"Run HLS worker failed! Camera ID {camID} not found");
                    return null;
                }
            }
        }

        public void Canary(string camID)
        {
            if (_disposed) throw new ObjectDisposedException("HLS");
            
            lock (_sync)
            {
               var workerItem = _workers.Where(x => x.Worker.CamID == camID).SingleOrDefault();

                if (workerItem != null)
                {
                    workerItem.LastCanary = DateTime.Now;

                    if (!workerItem.Worker.TurnedOn) 
                        workerItem.Worker.Start();
                }
                else
                {
                    _logger.Log($"Canary failed! Camera ID {camID} not found");
                }
            }
		}
	}

    //Thread-safe
	class HLSWorker : IDisposable
	{
        readonly string _ffmpegDir;
        readonly string _workingDir;
        readonly string _camID; 
        readonly string _playlistFilename;

        const string M3u8EndMarker = "#EXT-X-ENDLIST";
        const string FfmpegArgsTpl = "-hide_banner -loglevel error -nostats "
            + "%rtspTransportTCP% "
            + "-stimeout %rtspTimeout% "
            + "-i \"%rtsp_uri%\" "
            + "-c copy "
            + "-t %ffmpegMaxDuration% "
            + "%enableAudio% "
            + "-hls_wrap %numchunks% -hls_list_size %numchunks% -hls_time 0 "
            + "-hls_segment_filename \"%segpath%\" "
            + "-f hls \"%path%\"";

		Process _process;
        Logger _logger;
        bool _disposed = false;

        enum Actions { start, stop, none }

        bool _turnedOn = false;
        object _sync = new();
        
        public string CamID => _camID;
        public string PlaylistFilename => _playlistFilename;

        public bool TurnedOn
        {
            get
            {
                lock (_sync) return _turnedOn;
            }
        }

        public HLSWorker(Logger logger, string ffmpegDir, string workingDir, CameraItem cam)
        {
            _workingDir = workingDir;
            _ffmpegDir = ffmpegDir;
            _logger = logger;
            
            _camID = cam.id;
            _playlistFilename = Path.Combine(workingDir, CamID.ToString(), "stream.m3u8");

            _process = PrepareStreamProcess(cam);
        }

        ~HLSWorker()
        {
            if (!_disposed) Dispose();
        }

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("HLSWorker");
            _disposed = true;

            lock (_sync)
                if (_turnedOn)
                    StopProcess();

            try { _process.Dispose(); }
            catch { }
        }

        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException("HLSWorker");

            lock (_sync)
                if (_turnedOn)
                {
                    _logger.Log($"HLS process already started for camera ID {CamID}");
                }
                else
                {
                    _logger.Log($"Start HLS process for camera ID {CamID}");
                    _turnedOn = true;
                }
        }

        public void Stop()
        {
            if (_disposed) throw new ObjectDisposedException("HLSWorker");

            lock (_sync)
                if (!_turnedOn)
                {
                    _logger.Log($"HLS process is not started for camera ID {CamID}");
                }
                else
                {
                    _logger.Log($"Stop HLS process for camera ID {CamID}");
                    _turnedOn = false;
                }
        }

        void StopProcess()
        {
            try
            {
                _logger.Log($"Stop ffmpeg process ID {_process.Id} (camera ID {CamID})");
                _process.Kill();
            }
            catch (Exception e)
            {
                try
                {
                    _logger.Log($"Exception while stopping ffmpeg process ID {_process.Id} (camera ID {CamID}). {e.Message}");
                }
                catch (InvalidOperationException)
                {
                    _logger.Log($"Exception while stopping ffmpeg process without ID (camera ID {CamID}). {e.Message}");
                }
            }
        }

        void StartProcess()
        {
            try
            {
                PrepareStreamDirectory();
            }
            catch (Exception e)
            {
                _logger.Log($"Exception while try to prepare stream directory (camera ID {CamID}). {e.Message}");
                throw new Exception("HLS process: prepare stream directory failed");
            }

            try
            {
                _logger.Log($"Start new ffmpeg process (camera ID {CamID})");
                _process.Start();
            }
            catch (Exception e)
            {
                _logger.Log($"HLS process: FFMpeg start failed (from {_process.StartInfo.FileName}). {e.Message}");
            }
        }

        public void Update()
        {
            if (_disposed) throw new ObjectDisposedException("HLSWorker");

            bool turnedOn;

            lock (_sync)
                turnedOn = _turnedOn;

            bool procRunning = false;

            try
            {
                procRunning = !_process.HasExited;
            }
            catch (InvalidOperationException)
            {
                procRunning = false;
            }
            catch (NotSupportedException)
            {
                _logger.Log($"CRITICAL ERROR: on this System it is not possible to determine whether the ffmpeg process has exited!");
            }
            catch (Exception e)
            {
                _logger.Log($"Exception when getting ffmpeg process status (camera ID {CamID}). {e.Message}");
                return;
            }

            if (turnedOn)
            {
                if (!procRunning)
                    StartProcess();
            }
            else
            {
                if (procRunning)
                StopProcess();
            }
        }

        Process PrepareStreamProcess(CameraItem cam)
        {
            var process = new Process();
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string playlistDir = Path.GetDirectoryName(PlaylistFilename);
            var segmentFilename = Path.Combine(playlistDir, $"stream%01d_{ConfigManager.Current.key}.ts");

            process.StartInfo.Arguments = FfmpegArgsTpl
                .Replace("%ffmpegMaxDuration%", ConfigManager.Current.hls_ffmpeg_max_duration.ToString())
                .Replace("%enableAudio%", cam.audio_enabled ? "-map 0:v:0 -map 0:a:0 -c:a aac -b:a 16k" : "-an")
                .Replace("%rtsp_uri%", cam.rtsp_uri)
                .Replace("%rtspTransportTCP%", cam.transport_tcp ? "-rtsp_transport tcp" : "")
                .Replace("%rtspTimeout%", ConfigManager.Current.rtsp_timeout.ToString())
                .Replace("%path%", PlaylistFilename)
                .Replace("%numchunks%", ConfigManager.Current.hls_target_chunks.ToString())
                .Replace("%segpath%", segmentFilename);

            process.StartInfo.FileName = Path.Combine(_ffmpegDir, isWindows ? "ffmpeg-win64.exe" : "ffmpeg-linux64");
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            return process;
        }

        void PrepareStreamDirectory()
        {
            var dir = Path.GetDirectoryName(PlaylistFilename);

            if (ConfigManager.Current.hls_cleanup_before_play && Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    _logger.Log($"Exception while try to delete directory \"{dir}\". {e.Message}");
                }
            }

            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception e)
                {
                    _logger.Log($"Exception while try to create directory \"{dir}\". {e.Message}");
                }
            }
            else
            {
                RemoveOldChunks(dir, ConfigManager.Current.hls_target_chunks);
                FinalizePlaylist(PlaylistFilename);
            }
        }

        void FinalizePlaylist(string playlistFilename)
        {
            if (!File.Exists(playlistFilename)) return;

            var content = File.ReadAllText(playlistFilename);

            if (!content.Contains(M3u8EndMarker))
            {
                content += $"\n{M3u8EndMarker}";

                try
                {
                    File.WriteAllText(playlistFilename, content);
                }
                catch (Exception e)
                {
                    _logger.Log($"Exception while try to add END marker to existing playlist \"{playlistFilename}\". {e.Message}");
                }
            }
        }

        void RemoveOldChunks(string dir, int numChunksToKeep)
        {
            DirectoryInfo dirInfo = new(dir);
            FileInfo[] fileList = dirInfo.GetFiles("*.ts", SearchOption.AllDirectories);
            var fileListSorted = fileList.OrderByDescending(file => file.CreationTime);

            var i = 1;

            try
            {
                foreach (var file in fileListSorted)
                    if (i++ > numChunksToKeep) 
                        file.Delete();
            }
            catch (Exception e)
            {
                _logger.Log($"Exception while try to remove old chunks ({dir}). {e.Message}");
            }
        }
    }
}
