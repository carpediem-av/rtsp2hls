// Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.IO;

namespace RTSPLiveServer
{
	class Logger
	{
		static string filename;
		static bool consoleOut;
		static object lockObj = new object();

		public static void init(string filename, bool consoleOut)
		{
			Logger.filename = filename;
			Logger.consoleOut = consoleOut;
			File.WriteAllText(filename, "Система логирования инициализирована.");
		}

		public static void log(string msg)
		{
			lock (lockObj) {
				var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				var msgWithDate = date + "\t" + msg;
				if (consoleOut) Console.WriteLine(msgWithDate);

				try {
					File.AppendAllText(filename, Environment.NewLine + msgWithDate);
				}
				catch (Exception e) {
					Console.WriteLine("Write to log file failed! " + e.Message);
				}
			}
		}
	}
}
