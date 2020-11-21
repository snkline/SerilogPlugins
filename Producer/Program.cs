using Serilog;
using Serilog.Sinks.NetMQ;
using System;
using System.Threading;

namespace Producer
{
    class Program
    {
        static void Main(string[] args)
        {
            long bigNum = 0;

            var tcpLog =
                new LoggerConfiguration()
                    .WriteTo.NetMQ("tcp://127.0.0.1:4343", callNetMqCleanup: false)
                    .CreateLogger();

            while(true)
            {
                bigNum++;
                var x = new Random().Next(1, 4);
                if (x == 1)
                {
                    tcpLog.Information(bigNum.ToString());
                }
                else if (x == 2)
                {
                    tcpLog.Warning(bigNum.ToString());
                }
                else if (x == 3)
                {
                    tcpLog.Error(bigNum.ToString());
                }
                Thread.Sleep(200);
            }
        }
    }
}
