// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oocx.ReadX509CertificateFromPem;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Hosting;

namespace RTSPLiveServer
{
    class WebServer : IDisposable
    {
        const int FailCountThreshold = 3;
        const int WatchdogInterval = 10000;
        
        HLS _hls;
        Capture _capture;
        Logger _logger;
        Logger _httpAccessLogger;

        bool _disposed = false;
        readonly bool _https;
        readonly int _port;

        readonly string _watchdogCheckedUrl;
        readonly string _certFilename;
        readonly string _pemCertFile;
        readonly string _pemPrivKeyFile;
        readonly string _certPassword;
        readonly string _assetsDir;

        SemaphoreSlim _semaphoreStateControl = new(1, 1);
        KestrelServer _kServer;
        bool _kestrelRunning = false;

        CancellationTokenSource _cancellationWatchdog;
        Task _watchdogTask;
        HttpClientHandler _httpClientHandler;
        HttpClient _checkerHttpClient;

        public WebServer(Logger logger, HLS hls, Capture capture, int port, bool https, string certDir, string certPassword, string assetsDir, Logger httpAccessLogger)
        {
            _httpClientHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
            };

            _checkerHttpClient = new(_httpClientHandler)
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
            _checkerHttpClient.DefaultRequestHeaders.Add(WebUtil.WatchdogHeader, "1");

            _hls = hls;
            _capture = capture;
            _logger = logger;
            _https = https;
            _port = port;
            _certPassword = certPassword;
            _assetsDir = assetsDir;
            _httpAccessLogger = httpAccessLogger;

            _certFilename = Path.Combine(certDir, "cert.pfx");
            _pemCertFile = Path.Combine(certDir, "cert.pem");
            _pemPrivKeyFile = Path.Combine(certDir, "privkey.pem");

            _watchdogCheckedUrl = $"{(https ? "https" : "http")}://localhost:{port}/assets/background.html";
            _watchdogTask = StartWatchdogThread();
        }

        public async void Dispose()
        {
            _disposed = true;

            _cancellationWatchdog.Cancel();
            _watchdogTask.Wait();
            
            await _semaphoreStateControl.WaitAsync();
            
            try
            {
                if (_kestrelRunning) await KestrelStop();
            }
            finally
            {
                _semaphoreStateControl.Release();
            }
                        
            _checkerHttpClient.Dispose();
            _httpClientHandler.Dispose();
        }

        public async Task<bool> Start()
        {
            if (_disposed) throw new ObjectDisposedException(null);
            await _semaphoreStateControl.WaitAsync();

            try
            {
                return await KestrelStart();
            }
            finally
            {
                _semaphoreStateControl.Release();
            }
        }

        async Task<bool> KestrelStart()
        {
            if (_kestrelRunning) throw new InvalidOperationException("Already started");

            KestrelServerOptions serverOptions = new();

            if (_https)
            {
                HttpsConnectionAdapterOptions httpsOptions = ConfigureHttps();
                if (httpsOptions == null) return false;

                serverOptions.ListenAnyIP(_port, options =>
                {
                    options.KestrelServerOptions.ApplicationServices = new AppServices();
                    options.UseHttps(httpsOptions);
                });
            }
            else
            {
                serverOptions.ListenAnyIP(_port);
            }

            var transportOptions = new SocketTransportOptions();
            var loggerFactory = new NullLoggerFactory();
            var transportFactory = new SocketTransportFactory(new OptionsWrapper<SocketTransportOptions>(transportOptions), loggerFactory);
            _kServer = new KestrelServer(new OptionsWrapper<KestrelServerOptions>(serverOptions), transportFactory, loggerFactory);

            try
            {
                await _kServer.StartAsync(new WebApplication(_logger, _httpAccessLogger, _hls, _capture, _assetsDir), CancellationToken.None);
                _kestrelRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message);
            }

            return false;
        }

        public async Task Stop()
        {
            if (_disposed) throw new ObjectDisposedException(null);
            await _semaphoreStateControl.WaitAsync();

            try
            {
                await KestrelStop();
            }
            finally
            {
                _semaphoreStateControl.Release();
            }
        }

        async Task KestrelStop()
        {
            if (!_kestrelRunning) throw new InvalidOperationException("Already stopped");

            await _kServer.StopAsync(CancellationToken.None);
            _kestrelRunning = false;

            _kServer.Dispose();
            _kServer = null;
        }

        HttpsConnectionAdapterOptions ConfigureHttps()
        {
            HttpsConnectionAdapterOptions httpsOptions = new();

            try
            {
                if (File.Exists(_pemCertFile) && File.Exists(_pemPrivKeyFile))
                {
                    var reader = new CertificateFromPemReader();
                    httpsOptions.ServerCertificate = reader.LoadCertificateWithPrivateKey(_pemCertFile, _pemPrivKeyFile);
                }
                else if (File.Exists(_certFilename))
                {
                    httpsOptions.ServerCertificate = new X509Certificate2(_certFilename, _certPassword);
                }
                else
                {
                    _logger.Log("В папке сервера отсутствует сертификат SSL. Для формата PEM: разместите файлы сертификата под именами cert.pem и privkey.pem в подпапке data. Для формата PFX: разместите сертификат под именем cert.pfx в подпапке data.");
                    _logger.Log("There are no SSL certificate in the program folder. For PEM format: place the certificate files named cert.pem and privkey.pem in the 'data' subfolder. For PFX format: place the certificate under the name cert.pfx in the 'data' subfolder.");
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.Log("Проблема с сертификатом SSL. Возможно указан неверный пароль для него или же сам файл сертификата содержит ошибку. " + e.Message);
                _logger.Log("Problem with the SSL certificate. The password may be incorrect, or the certificate file itself may contain an error. " + e.Message);
                return null;
            }

            return httpsOptions;
        }

        Task StartWatchdogThread()
        {
            _cancellationWatchdog = new CancellationTokenSource();

            return Task.Run(async () =>
            {
                var failCount = 0;

                while (true)
                {
                    for (int i = 0, delay = 100; i < WatchdogInterval; i += delay)
                    {
                        await Task.Delay(delay);
                        if (_cancellationWatchdog.Token.IsCancellationRequested) return;
                    }

                    await _semaphoreStateControl.WaitAsync();

                    try
                    {
                        if (!_kestrelRunning) continue;

                        if (!await CheckServerAvailability())
                            failCount++;
                        else
                            failCount = 0;

                        if (failCount >= FailCountThreshold)
                        {
                            _logger.Log("Fatal error: internal HTTP server is down! Restarting HTTP server...");
                         
                            if (await KestrelRestart())
                            {
                                failCount = 0;
                                _logger.Log("HTTP SERVER RESTARTED");
                            }
                        }
                    }
                    finally
                    {
                        _semaphoreStateControl.Release();
                    }
                }
            }, _cancellationWatchdog.Token);
        }

        async Task<bool> KestrelRestart()
        {
            try
            {
                await KestrelStop();
            }
            catch (Exception ex)
            {
                _logger.Log("Stopping current HTTP server instance is failed. " + ex.Message);
            }

            await Task.Delay(1000);

            try
            {
                if (await KestrelStart()) return true;
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message);
            }

            return false;
        }

        async Task<bool> CheckServerAvailability()
        {
            try
            {
                using (var response = await _checkerHttpClient.GetAsync(_watchdogCheckedUrl))
                    return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
