// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.IO;
using System.Threading;

namespace RTSPLiveServer
{
	// Thread-Safe
	class Logger : IDisposable
	{
        readonly string _filename;
		readonly bool _consoleOut;
        readonly bool _skipDuplicates;
        readonly int _flushInterval;
        
		string _buffer;
		string _lastFlushedBuffer;
        object _sync = new object();

        CancellationTokenSource _tokenSource;
        CancellationToken _cancellationToken;
        bool _disposed = false;

        public Logger(string filename, bool consoleOut, int flushInterval = 1200, bool skipDuplicates = true)
		{
			_filename = filename;
			_consoleOut = consoleOut;
            _flushInterval = flushInterval;
            _skipDuplicates = skipDuplicates;
            _lastFlushedBuffer = _buffer = "";

            _tokenSource = new CancellationTokenSource();
			_cancellationToken = _tokenSource.Token;
			new Thread(FlushLoop).Start();

            File.WriteAllText(filename, "Система логирования инициализирована.");
        }

		~Logger()
		{
			if (!_disposed) Dispose();
		}

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("Logger");
			_disposed = true;
            _tokenSource.Cancel();
            Flush();
        }

        private void FlushLoop()
        {
            while (true)
            {
                if (_cancellationToken.IsCancellationRequested) return;
                Flush();
                Thread.Sleep(_flushInterval);
            }
        }

        private void Flush()
        {
            string bufferCopy;

            lock (_sync)
            {
                if (_buffer.Length == 0) return;
                bufferCopy = _buffer;
                
                if (_skipDuplicates) 
                    _lastFlushedBuffer = _buffer;
                
                _buffer = "";
            }

            try
            {
                File.AppendAllText(_filename, bufferCopy);
            }
            catch (Exception e)
            {
                Console.WriteLine("Write to log file failed! " + e.Message);
            }
        }

		public void Log(string msg)
        {
            if (_disposed) throw new ObjectDisposedException("Logger");
            
			var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var msgWithDate = $"{date}\t{msg}";
			
            lock (_sync) 
            {
                if (_skipDuplicates) 
                    if (_buffer.Contains(msgWithDate) || _lastFlushedBuffer.Contains(msgWithDate)) return;

                _buffer += Environment.NewLine + msgWithDate;

                if (_consoleOut) 
                    Console.WriteLine(msgWithDate);
			}
		}
	}
}
