// Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ceen;
using Ceen.Httpd;
using Ceen.Httpd.Logging;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;
using RTSPLiveServer;

namespace HttpListenerExample
{
	class RTSPLiveServer
	{
		const int assetExpireHours = 24 * 30;

		static string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
		static string workingDir = Path.Combine(appDirectory, "data", "stream");
		static string captureDir = Path.Combine(appDirectory, "data", "capture");
		static string logDir = Path.Combine(appDirectory, "data", "log");

		static string certFilename = Path.Combine(appDirectory, "data", "cert.pfx");
		static string pemCertFile = Path.Combine(appDirectory, "data", "cert.pem");
		static string pemPrivKeyFile = Path.Combine(appDirectory, "data", "privkey.pem");

		static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		static List<string> allowedAssetPathWithoutKey = new List<string>() { "/favicon.ico", "/background.html" };

		public static string getContentType(string extension)
		{
			switch (Path.GetExtension(extension)) {
				case ".m3u8": return "application/x-mpegURL";
				case ".ts":	return "video/MP2T";
				case ".htm":
				case ".html": return "text/html; charset=utf-8";
				case ".js": return "text/javascript";
				case ".css": return "text/css";
				default: return "unknown";
			}
		}

		static string getCamIDFromStreamReq(string absPath)
		{
			var pattern = @"(\/stream\/)(\S+)(\/\S+)";
			var match = Regex.Match(absPath, pattern);

			if (match.Success) {
				if (match.Groups.Count == 4) {
					return match.Groups[2].Value;
				}
			}

			return null;
		}

		static async Task<bool> routeImage(IHttpContext context)
		{
			var reqParamKey = context.Request.QueryString["key"];
			var camID = context.Request.QueryString["cam"];

			if (reqParamKey != ConfigManager.Current.key) {
				context.Response.StatusCode = Ceen.HttpStatusCode.Unauthorized;
			}
			else if (ConfigManager.Current.findCam(camID) == null) {
				context.Response.StatusCode = Ceen.HttpStatusCode.NotFound;
			}
			else {
				var filename = CaptureWorker.capture(camID);
				byte[] data;

				try {
					data = File.ReadAllBytes(filename);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to read file {filename}. " + e.Message);
					context.Response.StatusCode = Ceen.HttpStatusCode.InternalServerError;
					return true;
				}

				context.Response.SetExpires(new TimeSpan(0, 0, ConfigManager.Current.capture_cache_expire), true);
				context.Response.ContentType = "image/jpeg";
				await context.Response.WriteAllAsync(data);
			}

			return true;
		}

		static async Task<bool> routeAsset(IHttpContext context)
		{
			var reqParamKey = context.Request.QueryString["key"];
			
			if (reqParamKey != ConfigManager.Current.key && !allowedAssetPathWithoutKey.Contains(context.Request.Path)) {
				context.Response.StatusCode = Ceen.HttpStatusCode.Unauthorized;
			}
			else {
				var filename = isWindows ? context.Request.Path.Replace('/', '\\') : context.Request.Path;
				filename = Path.Combine(appDirectory, "assets", Path.GetFileName(filename));
				byte[] data;

				if (!File.Exists(filename)) {
					context.Response.StatusCode = Ceen.HttpStatusCode.NotFound;
					return true;
				}

				try {
					data = File.ReadAllBytes(filename);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to read file {filename}. " + e.Message);
					context.Response.StatusCode = Ceen.HttpStatusCode.InternalServerError;
					return true;
				}

				context.Response.SetExpires(new TimeSpan(assetExpireHours, 0, 0), true);
				context.Response.ContentType = getContentType(Path.GetExtension(filename));
				await context.Response.WriteAllAsync(data);
			}

			return true;
		}

		static async Task<bool> routeStream(IHttpContext context)
		{
			var reqParamKey = context.Request.QueryString["key"];
			var camID = getCamIDFromStreamReq(context.Request.Path);
			var isPlaylistReq = context.Request.Path.EndsWith(".m3u8");

			if (isPlaylistReq && reqParamKey != ConfigManager.Current.key) {
				context.Response.StatusCode = Ceen.HttpStatusCode.Unauthorized;
			}
			else if (ConfigManager.Current.findCam(camID) == null) {
				context.Response.StatusCode = Ceen.HttpStatusCode.NotFound;
			}
			else {
				if (isPlaylistReq) HLS.canary(camID);

				var filename = isWindows ? context.Request.Path.Replace('/', '\\') : context.Request.Path;
				filename = Path.Combine(workingDir, camID.ToString(), Path.GetFileName(filename));
				byte[] data;

				try {
					if (!File.Exists(filename)) {
						if (isPlaylistReq) {
							filename = Path.Combine(appDirectory, "assets", "loading.m3u8");
						}
						else if (context.Request.Path.EndsWith(".ts")) {
							filename = Path.Combine(appDirectory, "assets", "loading.ts");
						}
						else {
							context.Response.StatusCode = Ceen.HttpStatusCode.NotFound;
							return true;
						}
					}

					data = File.ReadAllBytes(filename);
				}
				catch (Exception e) {
					Logger.log($"Exception while try to read file {filename}. " + e.Message);
					context.Response.StatusCode = Ceen.HttpStatusCode.InternalServerError;
					return true;
				}

				context.Response.SetNonCacheable();
				context.Response.ContentType = getContentType(Path.GetExtension(filename));
				await context.Response.WriteAllAsync(data);
			}

			return true;
		}
		
		static async Task<bool> routePlayer(IHttpContext context)
		{
			var reqParamKey = context.Request.QueryString["key"];
			var camID = context.Request.QueryString["cam"];

			if (ConfigManager.Current.findCam(camID) == null) {
				context.Response.StatusCode = Ceen.HttpStatusCode.NotFound;
			}
			else if (reqParamKey != ConfigManager.Current.key) {
				context.Response.StatusCode = Ceen.HttpStatusCode.Unauthorized;
			}
			else {
				var m3u8filename = HLS.run(camID);

				if (m3u8filename == null) {
					context.Response.StatusCode = Ceen.HttpStatusCode.InternalServerError;
					return true;
				}

				var wwwPath = $"/stream/{camID}/{Path.GetFileName(m3u8filename)}";
				var html = File.ReadAllText(Path.Combine(appDirectory, "assets", "player.html"));

				html = html.Replace("%key%", ConfigManager.Current.key)
					.Replace("%redirect_url_if_background%", ConfigManager.Current.redirect_url_if_background)
					.Replace("%hls_allow_video_seek_back%", ConfigManager.Current.hls_allow_video_seek_back ? "true" : "false")
					.Replace("%path%", wwwPath)
					.Replace("%cam%", camID.ToString());

				byte[] data = Encoding.UTF8.GetBytes(html);

				context.Response.SetNonCacheable();
				context.Response.ContentType = "text/html; charset=utf-8";
				await context.Response.WriteAllAsync(data);
			}

			return true;
		}

		static void waitUser()
		{
			try {
				Console.ReadKey();
			}
			catch (Exception) {
				while (true) {
					Thread.Sleep(3000);
				}
			}
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty.");
			Console.WriteLine("All Rights Reserved.");
			Console.WriteLine("http://carpediem.0fees.us");
			Console.WriteLine("-------------------------------------------------------------------");
			Console.WriteLine("");

			try {
				if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
				if (!Directory.Exists(workingDir)) Directory.CreateDirectory(workingDir);
				if (!Directory.Exists(captureDir)) Directory.CreateDirectory(captureDir);
			}
			catch (Exception e) {
				Console.WriteLine("Не удалось создать рабочие папки в папке данной программы. " + e.Message);
				waitUser();
				return;
			}

			Logger.init(Path.Combine(logDir, "server_log.txt"), true);
			ConfigManager.load();
			securityChecks();
			HLS.init(workingDir, appDirectory);
			CaptureWorker.init(captureDir, appDirectory);

			if (!startHttpServer()) {
				waitUser();
				return;
			}

			var protocol = ConfigManager.Current.https_mode_on ? "https" : "http";

			Logger.log($"Server started. URL: {protocol}://{ConfigManager.Current.server_hostname}:{ConfigManager.Current.port}{Environment.NewLine}"
				+ $"Stream data directory: {workingDir}{Environment.NewLine}"
				+ $"Capture data directory: {captureDir}{Environment.NewLine}"
				+ $"Log data directory: {logDir}{Environment.NewLine}"
				+ $"App directory: {appDirectory}{Environment.NewLine}");
			Console.WriteLine("Press any key to shutdown application.");

			waitUser();
		}

		private static void securityChecks()
		{
			if (ConfigManager.Current.key == Config.defaultKey) {
				Logger.log("Ключ доступа нужно срочно изменить в конфигураторе! В противном случае любой сможет получить доступ к серверу! Сейчас стоит ключ по умолчанию. Обратите внимание - при изменении ключа изменятся и ссылки на камеры!");
				Console.WriteLine("");
			}
		}

		private static bool startHttpServer()
		{
			string httpLogFilename = Path.Combine(logDir, "http_log.txt");
			if (File.Exists(httpLogFilename)) File.Delete(httpLogFilename);

			var config = new ServerConfig()
				.AddLogger(new CLFLogger(httpLogFilename))
				.AddRoute("/image/*", routeImage)
				.AddRoute("/stream/*", routeStream)
				.AddRoute("/player/*", routePlayer)
				.AddRoute("/assets/*", routeAsset)
				.AddRoute("/favicon.ico", routeAsset)
				.AddRoute("/background.html", routeAsset);

			if (ConfigManager.Current.https_mode_on) {
				try {
					if (File.Exists(pemCertFile) && File.Exists(pemPrivKeyFile)) {
						using (var rsa = readPemPrivateKey(pemPrivKeyFile)) {
							using (X509Certificate2 cert = readPemCert(pemCertFile)) {
								config.SSLCertificate = cert.CopyWithPrivateKey(rsa);
							}
						}
					}
					else if (File.Exists(certFilename)) {
						config.LoadCertificate(certFilename, ConfigManager.Current.cert_password);
					}
					else {
						Logger.log("В папке сервера отсутствует сертификат SSL. Для формата PEM: разместите файлы сертификата под именами cert.pem и privkey.pem в подпапке data. Для формата PFX: разместите сертификат под именем cert.pfx в подпапке data.");
						return false;
					}
				}
				catch (Exception e) {
					Logger.log("Проблема с сертификатом SSL. Возможно указан неверный пароль для него или же сам файл сертификата содержит ошибку. " + e.Message);
					return false;
				}
			}
			else {
				Logger.log("Внимание, используется незащищенное соединение (в конфигураторе не включен режим SSL)!");
			}

			var tcs = new CancellationTokenSource();
			var task = HttpServer.ListenAsync(
				new IPEndPoint(IPAddress.Any, ConfigManager.Current.port),
				ConfigManager.Current.https_mode_on,
				config,
				tcs.Token
			);

			return true;
		}

		public static X509Certificate2 readPemCert(string pathToPemFile)
		{
			X509CertificateParser x509CertificateParser = new X509CertificateParser();
			var cert = x509CertificateParser.ReadCertificate(File.ReadAllBytes(pathToPemFile));
			return new X509Certificate2(cert.GetEncoded());
		}

		public static RSA readPemPrivateKey(string filename)
		{
			using (StreamReader streamReader = File.OpenText(filename)) {
				PemReader pemReader = new PemReader(streamReader);
				PemObject pemObject = pemReader.ReadPemObject();
				PrivateKeyInfo privateKeyInfo = PrivateKeyInfo.GetInstance(pemObject.Content);
				AsymmetricKeyParameter privateKeyBC = PrivateKeyFactory.CreateKey(privateKeyInfo);

				if (isWindows) {
					//this don't working on Linux
					return DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)privateKeyBC);
				}
				else {
					//this don't working on Windows
					var parms = DotNetUtilities.ToRSAParameters(privateKeyBC as RsaPrivateCrtKeyParameters);
					var rsa = RSA.Create();
					rsa.ImportParameters(parms);
					return rsa;
				}
			}
		}
	}
}