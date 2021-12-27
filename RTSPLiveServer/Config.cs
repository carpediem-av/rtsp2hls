// Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CDSD.Data;

namespace RTSPLiveServer
{
	public class CameraItem
	{
		public string id { get; set; } = Guid.NewGuid().ToString();
		public string rtsp_uri { get; set; }
		public string name { get; set; }
		public bool transport_tcp { get; set; }
		public bool audio_enabled { get; set; }
	}

	public class Config
	{
		public const string defaultKey = "78Hj967sdrF481";

		public List<CameraItem> cameraList { get; set; } = new List<CameraItem>();

		public string key { get; set; } = defaultKey;
		public string cert_password { get; set; } = "";
		public ushort port { get; set; } = 8000;

		public int rtsp_timeout { get; set; } = 5 * 1000 * 1000;		//microseconds

		public int capture_timeout { get; set; } = 10;					//seconds
		public int capture_cache_expire { get; set; } = 60;				//seconds
		public int capture_jpeg_quality { get; set; } = 5;				//typical use = 3-10. See ffmpeg documentation

		public int hls_no_canary_time_before_stop { get; set; } = 60;	//seconds
		public int hls_process_check_interval { get; set; } = 5;		//seconds
		public int hls_ffmpeg_max_duration { get; set; } = 600;			//seconds
		public int hls_target_chunks { get; set; } = 3;                 //max .ts file count in playlist
		public bool hls_cleanup_before_play { get; set; } = false;		//remove .ts and .m3u8 from storage before start ffmpeg

		public string redirect_url_if_background { get; set; } = "/background.html";
		public string server_hostname { get; set; } = "localhost";

		public bool hls_allow_video_seek_back { get; set; } = false;

		public bool https_mode_on { get; set; } = false;

		public CameraItem findCam(string camID)
		{
			return cameraList.Where(x => x.id == camID).SingleOrDefault();
		}

		public Config clone()
		{
			return (Config) MemberwiseClone();
		}
	}

	class ConfigManager
	{
		static string configFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "config.xml");
		public static Config Current { get; set; }

		public static void load()
		{
			try {
				var xml = File.ReadAllText(configFilename);
				Current = Serialization.XmlDeserialize<Config>(xml);
			}
			catch (Exception ex) {
				Logger.log("Не удалось прочитать конфигурационный файл. " + ex.Message);
				Current = new Config();
			}
		}

		public static void save()
		{
			if (Current == null) throw new InvalidOperationException("Load config first");
			var clone = Current.clone();
			clone.cameraList = Current.cameraList.OrderBy(x => x.name).ToList();

			try {
				var xml = Serialization.XmlSerialize(clone);
				File.WriteAllText(configFilename, xml);
			}
			catch (Exception ex) {
				Logger.log("Не удалось сохранить конфигурационный файл. " + ex.Message);
			}
		}
	}
}
