using NetMQ;
using NetMQ.Sockets;
using Serilog.Events;
using System;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            object lockObj = new object();

            using (var subcription1 = new SubscriberSocket("tcp://127.0.0.1:4343"))
            using (var subcription2 = new SubscriberSocket("tcp://127.0.0.1:4343"))
            using (var subcription3 = new SubscriberSocket("tcp://127.0.0.1:4343"))
            using (var poller = new NetMQPoller { subcription1, subcription2, subcription3 })
            {
                subcription1.Subscribe(LogEventLevel.Information.ToString());
                subcription1.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    lock (lockObj)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{msg[0]} : {msg[1]}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                };
                subcription2.Subscribe(LogEventLevel.Warning.ToString());
                subcription2.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    lock (lockObj)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{msg[0]} : {msg[1]}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                };
                subcription3.Subscribe(LogEventLevel.Error.ToString());
                subcription3.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    lock (lockObj)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{msg[0]} : {msg[1]}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                };

                poller.Run();
            }
        }
    }
}
