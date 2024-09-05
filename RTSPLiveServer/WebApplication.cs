// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RTSPLiveServer
{
    class WebApplication : IHttpApplication<Context>
    {
        readonly static List<string> AllowedAssetPathWithoutKey = new() { "favicon.ico", "background.html" };
        const int AssetExpire = 24 * 30 * 3600;     // seconds
        
        HLS _hls;
        Capture _capture;
        Logger _logger;
        Logger _httpAccessLogger;

        readonly string _assetsDir;
        readonly string _errorImageFilename;
        readonly string _playerTemplateFilename;

        public WebApplication(Logger logger, Logger httpAccessLogger, HLS hls, Capture capture, string assetsDir)
        {
            _logger = logger;
            _httpAccessLogger = httpAccessLogger;
            _hls = hls;
            _capture = capture;
            _assetsDir = assetsDir;
            _errorImageFilename = Path.Combine(assetsDir, "error_image.jpg");
            _playerTemplateFilename = Path.Combine(assetsDir, "player.html");
        }

        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            return new Context(contextFeatures);
        }

        public void DisposeContext(Context context, Exception exception)
        {
        }

        public async Task ProcessRequestAsync(Context context)
        {
            DefaultHttpContext httpContext = new(context.features);
            var path = httpContext.Request.Path.Value;

            if (path == null)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                var pathSegments = path.TrimStart('/').Split('/');

                if (pathSegments.Length < 1)
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                else
                {
                    switch (pathSegments[0])
                    {
                        case "stream":
                            await RouteStream(httpContext);
                            break;
                        case "player":
                            await RoutePlayer(httpContext);
                            break;
                        case "image":
                            await RouteImage(httpContext);
                            break;
                        case "assets":
                            await RouteAsset(httpContext);
                            break;
                        case "favicon.ico":
                            await RouteAsset(httpContext);
                            break;
                        default:
                            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            break;
                    }
                }
            }

            LogConnection(httpContext);
        }

        void LogConnection(DefaultHttpContext httpContext)
        {
            if (!httpContext.Request.Headers.ContainsKey(WebUtil.WatchdogHeader))
            {
                var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                var path = httpContext.Request.Path;
                var query = httpContext.Request.QueryString;

                _httpAccessLogger.Log($"[{remoteIpAddress}] | {httpContext.Response.StatusCode} | {path}{query}");
            }
        }

        static string GetCamIDFromStreamRequest(string requestPath)
        {
            var pattern = @"(?:\/stream\/)(\S+)(?:\/\S+)";
            var match = Regex.Match(requestPath, pattern);

            if (match.Success && match.Groups.Count >= 2)
                return match.Groups[1].Value;
            else
                return null;
        }

        async Task RouteAsset(HttpContext context)
        {
            var reqParamKey = context.Request.Query["key"];
            var filename = Path.GetFileName(context.Request.Path);
            
            if (reqParamKey != ConfigManager.Current.key && !AllowedAssetPathWithoutKey.Contains(filename))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                filename = Path.Combine(_assetsDir, filename);
                byte[] data;

                if (!File.Exists(filename))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                try
                {
                    data = File.ReadAllBytes(filename);
                }
                catch (Exception e)
                {
                    _logger.Log($"Exception while try to read file \"{filename}\". " + e.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return;
                }

                WebUtil.SetCacheControl(context.Response, AssetExpire);
                context.Response.ContentType = WebUtil.GetFileMIMEContentType(filename);
                await context.Response.BodyWriter.AsStream().WriteAsync(data);
            }
        }

        async Task RouteImage(HttpContext context)
        {
            var reqParamKey = context.Request.Query["key"];
            var camID = context.Request.Query["cam"];

            if (reqParamKey != ConfigManager.Current.key)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (ConfigManager.Current.FindCam(camID) == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                var filename = _capture.Run(camID);

                if (filename == null)
                {
                    await SendErrorImage(context);
                }
                else
                {
                    byte[] data = null;
                    int attemptRemain = 3;
                    Exception readException = null;

                    while (attemptRemain-- > 0)
                    {
                        try
                        {
                            data = File.ReadAllBytes(filename);
                            break;
                        }
                        catch (Exception e)
                        {
                            _logger.Log($"Trying to read \"{filename}\" again (remaining attempts: {attemptRemain})");
                            readException = e;
                            await Task.Delay(30);
                        }
                    }

                    if (readException != null)
                    {
                        _logger.Log($"Exception while try to read file \"{filename}\". " + readException.Message);
                        await SendErrorImage(context);
                    }
                    else
                    {
                        WebUtil.SetCacheControl(context.Response, ConfigManager.Current.capture_cache_expire);
                        context.Response.ContentType = WebUtil.GetFileMIMEContentType(filename);
                        await context.Response.BodyWriter.AsStream().WriteAsync(data);
                    }
                }
            }
        }

        async Task SendErrorImage(HttpContext context)
        {
            try
            {
                var data = File.ReadAllBytes(_errorImageFilename); 
                WebUtil.SetCacheControl(context.Response, 0);
                context.Response.ContentType = WebUtil.GetFileMIMEContentType(_errorImageFilename);
                await context.Response.BodyWriter.AsStream().WriteAsync(data);
            }
            catch (Exception e)
            {
                _logger.Log($"Exception while try to read file \"{_errorImageFilename}\". " + e.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        async Task RouteStream(HttpContext context)
        {
            var reqParamKey = context.Request.Query["key"];
            var camID = GetCamIDFromStreamRequest(context.Request.Path);
            var isPlaylistReq = context.Request.Path.ToString().EndsWith(".m3u8");

            if (isPlaylistReq && reqParamKey != ConfigManager.Current.key)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (ConfigManager.Current.FindCam(camID) == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                if (isPlaylistReq) _hls.Canary(camID);

                var filename = Path.GetFileName(context.Request.Path);
                filename = Path.Combine(_hls.WorkingDir, camID.ToString(), filename);
                byte[] data;

                try
                {
                    if (!File.Exists(filename))
                    {
                        if (isPlaylistReq)
                        {
                            filename = Path.Combine(_assetsDir, "loading.m3u8");
                        }
                        else if (context.Request.Path.ToString().EndsWith(".ts"))
                        {
                            filename = Path.Combine(_assetsDir, "loading.ts");
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            return;
                        }
                    }

                    data = File.ReadAllBytes(filename);
                }
                catch (Exception e)
                {
                    _logger.Log($"Exception while try to read file \"{filename}\". " + e.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return;
                }

                WebUtil.SetCacheControl(context.Response, 0);
                context.Response.ContentType = WebUtil.GetFileMIMEContentType(filename);
                await context.Response.BodyWriter.AsStream().WriteAsync(data);
            }
        }

        async Task RoutePlayer(HttpContext context)
        {
            var reqParamKey = context.Request.Query["key"];
            var camID = context.Request.Query["cam"];

            if (reqParamKey != ConfigManager.Current.key)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (ConfigManager.Current.FindCam(camID) == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                var m3u8filename = _hls.Run(camID);

                if (m3u8filename == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                else
                {
                    var wwwPath = $"/stream/{camID}/{Path.GetFileName(m3u8filename)}";
                    string html;

                    try
                    {
                        html = File.ReadAllText(_playerTemplateFilename);
                    }
                    catch (Exception e)
                    {
                        _logger.Log($"Exception while try to read file \"{_playerTemplateFilename}\". " + e.Message);
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        return;
                    }

                    html = html.Replace("%key%", ConfigManager.Current.key)
                        .Replace("%redirect_url_if_background%", ConfigManager.Current.redirect_url_if_background)
                        .Replace("%hls_allow_video_seek_back%", ConfigManager.Current.hls_allow_video_seek_back ? "true" : "false")
                        .Replace("%path%", wwwPath)
                        .Replace("%cam%", camID.ToString());

                    byte[] data = Encoding.UTF8.GetBytes(html);

                    WebUtil.SetCacheControl(context.Response, 0);
                    context.Response.ContentType = WebUtil.GetFileMIMEContentType(_playerTemplateFilename);
                    await context.Response.BodyWriter.AsStream().WriteAsync(data);
                }
            }
        }
    }


    class AppServices : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ILoggerFactory))
                return new NullLoggerFactory();

            return null;
        }
    }


    class Context
    {
        public IFeatureCollection features;

        public Context(IFeatureCollection features)
        {
            this.features = features;
        }
    }
}
