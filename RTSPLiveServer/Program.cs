// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.IO;
using System.Threading;

namespace RTSPLiveServer
{
    class RTSPLiveServer
    {
		static readonly string s_appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string s_workingDir = Path.Combine(s_appDirectory, "data", "stream");
        static readonly string s_captureDir = Path.Combine(s_appDirectory, "data", "capture");
        static readonly string s_assetsDir = Path.Combine(s_appDirectory, "assets");
        static readonly string s_ffmpegDir = Path.Combine(s_appDirectory, "ffmpeg");
        static readonly string s_certDir = Path.Combine(s_appDirectory, "data");
        static readonly string s_logDir = Path.Combine(s_appDirectory, "data", "log");
        static readonly string s_commonLogFilename = Path.Combine(s_logDir, "server_log.txt");
        static readonly string s_httpLogfilename = Path.Combine(s_logDir, "http_log.txt");

        static Logger s_logger;
        static Logger s_httpAccessLogger;
        static HLS s_hls;
		static Capture s_capture;
        static WebServer s_webserver;

		static void WaitUser()
		{
			try 
            {
				Console.ReadKey();  //в неинтерактивном режиме бросит исключение
			}
			catch 
            {
				while (true) 
					Thread.Sleep(1000);
			}
		}

		public static void Main(string[] args)
        {
            Console.WriteLine("Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.");
            Console.WriteLine("All Rights Reserved.");
            Console.WriteLine("http://carpediem.0fees.us");
            Console.WriteLine("Version 2.0.");
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine("");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ReleaseAll();

            if (Init())
            {
                SecurityChecks();

                var protocol = ConfigManager.Current.https_mode_on ? "https" : "http";

                s_logger.Log($"Server started. URL: {protocol}://{ConfigManager.Current.server_hostname}:{ConfigManager.Current.port}");
                s_logger.Log($"Stream data directory: {s_workingDir}");
                s_logger.Log($"Capture data directory: {s_captureDir}");
                s_logger.Log($"Log data directory: {s_logDir}");
                s_logger.Log($"App directory: {s_appDirectory}");

                Console.WriteLine("\nPress any key to shutdown application.\n");
            }

            WaitUser();
            Console.WriteLine("\nApplication shutdown requested\n");
            ReleaseAll();
        }

        static bool Init()
        {
            try
            {
                if (!Directory.Exists(s_logDir)) 
                    Directory.CreateDirectory(s_logDir);

                if (!Directory.Exists(s_workingDir)) 
                    Directory.CreateDirectory(s_workingDir);

                if (!Directory.Exists(s_captureDir)) 
                    Directory.CreateDirectory(s_captureDir);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create working directories in the program's directory. " + e.Message);
                return false;
            }

            s_logger = new Logger(s_commonLogFilename, true);
            s_httpAccessLogger = new Logger(s_httpLogfilename, false);

            try
            {
                ConfigManager.Load();
            }
            catch (ConfigException ex)
            {
                s_logger.Log(ex.Message);
                return false;
            }

            s_hls = new HLS(s_workingDir, s_ffmpegDir, s_logger);
            s_capture = new Capture(s_captureDir, s_ffmpegDir, s_logger);

            s_webserver = new WebServer(
                s_logger,
                s_hls,
                s_capture,
                ConfigManager.Current.port,
                ConfigManager.Current.https_mode_on,
                s_certDir,
                ConfigManager.Current.cert_password,
                s_assetsDir,
                s_httpAccessLogger);
            
            var webserverTask = s_webserver.Start();
            webserverTask.Wait();

            if (!webserverTask.Result)
            {
                s_logger.Log($"Internal webserver start failed (using port {ConfigManager.Current.port})");
                return false;
            }

            return true;
        }

        private static void ReleaseAll()
        {
            if (s_webserver != null)
            {
                try
                {
                    s_webserver.Stop().Wait();
                    s_webserver.Dispose();
                    s_webserver = null;
                }
                catch { }
            }

            if (s_hls != null)
			{
				s_hls.Dispose();
				s_hls = null;
			}

            if (s_httpAccessLogger != null)
            {
                s_httpAccessLogger.Dispose();
                s_httpAccessLogger = null;
            }

            if (s_logger != null)
            {
                s_logger.Dispose();
                s_logger = null;
            }
        }

		private static void SecurityChecks()
		{
			if (ConfigManager.Current.key == Config.defaultKey) {
				s_logger.Log("Ключ доступа нужно срочно изменить в конфигураторе! В противном случае любой сможет получить доступ к серверу! Сейчас стоит ключ по умолчанию. Обратите внимание - при изменении ключа изменятся и ссылки на камеры!");
                s_logger.Log("The access key needs to be changed urgently in the configurator! Otherwise, anyone will be able to access the server! The default access key is being used now. Please note that if you change the key, the links to the cameras will also change!");
                Console.WriteLine("");
            }

            if (!ConfigManager.Current.https_mode_on)
            {
                s_logger.Log("Внимание, используется незащищенное соединение (в конфигураторе не включен режим SSL)!");
                s_logger.Log("Attention, an unsecured connection is being used (SSL mode is not enabled in the configurator)!");
                Console.WriteLine("");
            }
        }
    }
}