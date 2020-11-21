using System;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;

using NetMQ;
using NetMQ.Sockets;

namespace Serilog.Sinks.NetMQ
{
    public class NetMQSink : ILogEventSink, IDisposable
    {
        private bool disposedValue;
        private bool cleanUpNetMq;
        private readonly IFormatProvider fmtProvider;
        private readonly PublisherSocket publisherSocket;

        internal NetMQSink(string bindingAddress, IFormatProvider formatProvider, bool callNetMqCleanup)
        {
            publisherSocket = new PublisherSocket();
            publisherSocket.Bind(bindingAddress);
            fmtProvider = formatProvider;
            cleanUpNetMq = callNetMqCleanup;

            publisherSocket.Options.Linger = new TimeSpan(0, 0, 5);
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(fmtProvider);
            var level = logEvent.Level.ToString();

            publisherSocket.SendMoreFrame(level).SendFrame(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    publisherSocket.Dispose();
                    if (cleanUpNetMq)
                        NetMQConfig.Cleanup(true);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public static class NetMQSinkExtensions
    {
        public static LoggerConfiguration NetMQ(this LoggerSinkConfiguration loggerSinkConfiguration, string bindingAddress, IFormatProvider formatProvider = null, bool callNetMqCleanup = true)
        {
            return loggerSinkConfiguration.Sink(new NetMQSink(bindingAddress, formatProvider, callNetMqCleanup));
        }
    }
}
