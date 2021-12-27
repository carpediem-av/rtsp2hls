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
using System.Threading.Tasks;

namespace RTSPLiveServer
{
	class HLS
	{
		static string appDirectory;
		static string workingDir;
		static List<HLSWorker> workerProcesses = new List<HLSWorker>();
		static object lckWP = new object();

		const string m3u8EndMarker = "#EXT-X-ENDLIST";
		const string ffmpegArgsTpl = 
			"%rtspTransportTCP% "
			+ "-stimeout %rtspTimeout% "
			+ "-i \"%rtsp_uri%\" " 
			+ "-c copy "
			+ "-t %ffmpegMaxDuration% "
			+ "%enableAudio% "
			+ "-hls_wrap %numchunks% -hls_list_size %numchunks% -hls_time 0 "
			+ "-hls_segment_filename \"%segpath%\" "
			+ "-f hls \"%path%\"";

		static void startProcess(HLSWorker hwp)
		{
			if (hwp.process == null || hwp.process.HasExited) {
				hwp.m3u8filename = Path.Combine(workingDir, hwp.camID.ToString(), "stream.m3u8");

				try {
					prepareStreamDirectory(hwp.m3u8filename);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to prepare stream directory (camera ID {hwp.camID}). {e.Message}");
					return;
				}

				var cam = ConfigManager.Current.findCam(hwp.camID);
				Logger.log($"Start HLS process for camera ID {hwp.camID}");

				try {
					hwp.process = prepareStreamProcess(cam, hwp.m3u8filename);
					hwp.process.Start();
				}
				catch (Exception e) {
					Logger.log($"FFMpeg (HLS) start failed (from {hwp.process.StartInfo.FileName}). {e.Message}");
					hwp.process = null;
					return;
				}
			}
			else Logger.log($"HLS process already started for camera ID {hwp.camID}. Start canceled");
		}

		static void stopProcess(HLSWorker hwp)
		{
			try {
				if (hwp.process != null && !hwp.process.HasExited) {
					Logger.log($"Stop ffmpeg process ID {hwp.process.Id} (camera ID {hwp.camID})");
					hwp.process.Kill();
				}
				else Logger.log($"No process to stop (camera ID {hwp.camID})");
			}
			catch (Exception e) {
				Logger.log($"Exception while stopping ffmpeg process ID {hwp.process.Id} (camera ID {hwp.camID}). {e.Message}");
			}
			finally {
				hwp.process = null;
			}
		}

		static Process prepareStreamProcess(CameraItem cam, string m3u8filename)
		{
			var process = new Process();
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var segmentFilename = Path.Combine(Path.GetDirectoryName(m3u8filename), $"stream%01d_{ConfigManager.Current.key}.ts");

			process.StartInfo.Arguments = ffmpegArgsTpl
				.Replace("%ffmpegMaxDuration%", ConfigManager.Current.hls_ffmpeg_max_duration.ToString())
				.Replace("%enableAudio%", cam.audio_enabled ? "" : "-an")
				.Replace("%rtsp_uri%", cam.rtsp_uri)
				.Replace("%rtspTransportTCP%", cam.transport_tcp ? "-rtsp_transport tcp" : "")
				.Replace("%rtspTimeout%", ConfigManager.Current.rtsp_timeout.ToString())
				.Replace("%path%", m3u8filename)
				.Replace("%numchunks%", ConfigManager.Current.hls_target_chunks.ToString())
				.Replace("%segpath%", segmentFilename);

			process.StartInfo.FileName = Path.Combine(appDirectory, "ffmpeg", isWindows ? "ffmpeg-win64.exe" : "ffmpeg-linux64");
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;

			return process;
		}

		static void prepareStreamDirectory(string m3u8filename)
		{
			var dir = Path.GetDirectoryName(m3u8filename);

			if (ConfigManager.Current.hls_cleanup_before_play && Directory.Exists(dir)) {
				try {
					Directory.Delete(dir, true);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to delete directory ({dir}). {e.Message}");
				}
			}

			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}
			else {
				try {
					removeOldChunks(dir, ConfigManager.Current.hls_target_chunks);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to remove old chunks ({dir}). {e.Message}");
				}

				try {
					addEndMarkerToPlaylist(m3u8filename);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to add END marker to existing playlist ({dir}). {e.Message}");
				}
			}
		}

		static void addEndMarkerToPlaylist(string path)
		{
			if (File.Exists(path)) {
				var m3u8 = File.ReadAllText(path);

				if (!m3u8.Contains(m3u8EndMarker)) {
					m3u8 += $"\n{m3u8EndMarker}";
					File.WriteAllText(path, m3u8);
				}
			}
		}

		static void removeOldChunks(string dir, int numChunksToKeep)
		{
			DirectoryInfo info = new DirectoryInfo(dir);
			FileInfo[] fileList = info.GetFiles("*.ts", SearchOption.AllDirectories);
			var query = fileList.OrderByDescending(file => file.CreationTime);

			var i = 1;

			foreach (var file in query) {
				if (i++ > numChunksToKeep) {
					file.Delete();
				}
			}
		}

		static void stopIrrelevantWorkers()
		{
			var listToStop = new List<HLSWorker>();

			lock (lckWP) {
				foreach (var x in workerProcesses) {
					if ((DateTime.Now - x.lastCanary).TotalSeconds > ConfigManager.Current.hls_no_canary_time_before_stop) {
						if (x.process != null) {
							Logger.log($"Found no canary for ffmpeg process ID {x.process.Id} (camera ID {x.camID})");
						}
						else {
							Logger.log($"Found no canary for camera ID {x.camID} (related ffmpeg process not exists)");
						}
						listToStop.Add(x);
					}
				}

				foreach (var x in listToStop) {
					workerProcesses.Remove(x);
				}
			}

			foreach (var x in listToStop) {
				lock (x) {
					stopProcess(x);
				}
			}
		}

		static void resumeBrokenProcesses()
		{
			var listToStart = new List<HLSWorker>();

			lock (lckWP) {
				foreach (var hwp in workerProcesses) {
					try {
						lock (hwp) {
							if (hwp.process == null) {
								listToStart.Add(hwp);
							}
							else if (hwp.process.HasExited) {
								listToStart.Add(hwp);
								Logger.log($"Found broken ffmpeg process ID {hwp.process.Id} (camera ID {hwp.camID})");
							}
						}
					}
					catch (Exception e) {
						Logger.log($"Exception while try to get process status (camera ID {hwp.camID}). {e.Message}");
					}
				}
			}

			foreach (var x in listToStart) {
				lock (x) {
					startProcess(x);
				}
			}
		}

		static void watchdog()
		{
			while (true) {
				stopIrrelevantWorkers();
				resumeBrokenProcesses();
				Thread.Sleep(ConfigManager.Current.hls_process_check_interval * 1000);
			}
		}

		public static void init(string workingDir, string appDirectory)
		{
			HLS.workingDir = workingDir;
			HLS.appDirectory = appDirectory;
			Task.Run(() => watchdog());
		}

		public static string run(string camID)
		{
			var cam = ConfigManager.Current.findCam(camID);

			if (cam == null) {
				Logger.log("Run HLS worker failed! Camera not found: ID " + camID);
				return null;
			}

			HLSWorker hwp = null;

			lock (lckWP) {
				hwp = workerProcesses.Where(x => x.camID == camID).SingleOrDefault();

				if (hwp != null) {
					lock (hwp) {
						return hwp.m3u8filename;
					}
				}
				else {
					hwp = new HLSWorker();
					hwp.camID = camID;
					workerProcesses.Add(hwp);
				}
			}

			lock (hwp) {
				startProcess(hwp);
				return hwp.m3u8filename;
			}
		}

		public static void canary(string camID)
		{
			var cam = ConfigManager.Current.findCam(camID);

			if (cam == null) {
				Logger.log("Canary: camera not found: ID " + camID);
			}

			HLSWorker hwp = null;

			lock (lckWP) {
				hwp = workerProcesses.Where(x => x.camID == camID).SingleOrDefault();

				if (hwp != null) {
					lock (hwp) {
						hwp.lastCanary = DateTime.Now;
						return;
					}
				}
				else {
					hwp = new HLSWorker();
					hwp.camID = camID;
					workerProcesses.Add(hwp);
				}
			}

			lock (hwp) {
				startProcess(hwp);
			}
		}
	}

	class HLSWorker
	{
		public string camID;
		public Process process = null;
		public DateTime lastCanary = DateTime.Now;
		public string m3u8filename = null;
	}
}
