// Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty.
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
	class CaptureItem
	{
		public string camID;
		public Process process;
	}

	class CaptureWorker
	{
		static string workingDir;
		static string appDirectory;
		static List<CaptureItem> currentCamCapture = new List<CaptureItem>();
		static object lck = new object();

		const string ffmpegArgsTpl =
			"-y %rtspTransportTCP% "
			+ "-stimeout %rtspTimeout% "
			+ "-i \"%rtsp_uri%\" "
			+ "-vframes 1 -q:v %jpeg_quality% -f singlejpeg  \"%path%\"";
		
		// multithreading support
		// return: filesystem path to image
		public static string capture(string camID)
		{
			var cam = ConfigManager.Current.findCam(camID);

			if (cam == null) {
				Logger.log("Capture failed! Camera not found: ID " + camID);
				return getErrorImage();
			}

			var filename = Path.Combine(workingDir, camID + ".jpg");

			if (File.Exists(filename)) {
				var lastModified = File.GetLastWriteTime(filename);

				if ((DateTime.Now - lastModified).TotalSeconds <= ConfigManager.Current.capture_cache_expire) {
					return filename;
				}
				else {
					try {
						File.Delete(filename);
					}
					catch (Exception e) {
						Logger.log($"Exception while try to delete file {filename}). {e.Message}");
					}
				}
			}

			Process process = prepareProcess(cam, filename);
			
			try {
				CaptureItem current;
				lock (lck) {
					current = currentCamCapture.Where(x => x.camID == camID).FirstOrDefault();
					if (current == null) currentCamCapture.Add(new CaptureItem() { camID = camID, process = process });
				}

				if (current == null) {
					Logger.log("Start capture process for camera ID " + camID);
					process.Start();
					if (!waitProcessExit(process) || !File.Exists(filename)) return getErrorImage();
				}
				else {
					if (!waitProcessExit(current.process) || !File.Exists(filename)) return getErrorImage();
				}

				return filename;
			}
			catch (Exception e) {
				Logger.log($"FFMpeg start failed (from {process.StartInfo.FileName}). {e.Message}");
				return getErrorImage();
			}
			finally {
				lock (lck) {
					var item = currentCamCapture.Where(x => x.camID == camID).FirstOrDefault();
					if (item != null) currentCamCapture.Remove(item);
				}
			}
		}

		//return false if ffmpegWaitTimeout exceeded
		static bool waitProcessExit(Process process)
		{
			double timeout = ConfigManager.Current.capture_timeout;

			while (true) {
				lock (lck) {
					if (process.HasExited) return true;
				}
				Thread.Sleep(200);
				timeout -= 0.2;
				if (timeout <= 0) return false;
			}
		}

		static Process prepareProcess(CameraItem cam, string outFilename)
		{
			var process = new Process();
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			process.StartInfo.Arguments = ffmpegArgsTpl
				.Replace("%rtsp_uri%", cam.rtsp_uri)
				.Replace("%rtspTransportTCP%", cam.transport_tcp ? "-rtsp_transport tcp" : "")
				.Replace("%rtspTimeout%", ConfigManager.Current.rtsp_timeout.ToString())
				.Replace("%path%", outFilename)
				.Replace("%jpeg_quality%", ConfigManager.Current.capture_jpeg_quality.ToString());

			process.StartInfo.FileName = Path.Combine(appDirectory, "ffmpeg", isWindows ? "ffmpeg-win64.exe" : "ffmpeg-linux64");
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;

			return process;
		}

		private static string getErrorImage()
		{
			return Path.Combine(appDirectory, "assets", "error_image.jpg");
		}

		public static void init(string workingDir, string appDirectory)
		{
			CaptureWorker.workingDir = workingDir;
			CaptureWorker.appDirectory = appDirectory;
		}
	}
}
