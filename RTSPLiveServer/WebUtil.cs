// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using Microsoft.AspNetCore.Http;
using System.IO;

namespace RTSPLiveServer
{
    class WebUtil
    {
        public const string WatchdogHeader = "JUSTWATCHDOG";

        public static string GetFileMIMEContentType(string filename, string encoding = "charset=utf-8")
        {
            return Path.GetExtension(filename).ToLower() switch
            {
                ".m3u8" => "application/x-mpegURL",
                ".ts" => "video/MP2T",
                ".htm" or ".html" => $"text/html; {encoding}",
                ".js" => "text/javascript",
                ".css" => "text/css",
                ".ico" => "image/x-icon",
                ".jpg" or ".jpeg" or ".jpe" => "image/jpeg",
                _ => null,
            };
        }

        public static void SetCacheControl(HttpResponse resp, int seconds)
        {
            if (seconds <= 0) 
                resp.Headers.CacheControl = "no-cache";
            else 
                resp.Headers.CacheControl = "max-age=" + seconds.ToString();
        }
    }
}
