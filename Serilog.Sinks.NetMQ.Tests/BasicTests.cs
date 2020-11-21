using NetMQ;
using NetMQ.Sockets;
using NUnit.Framework;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Serilog.Sinks.NetMQ.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class BasicTests
    {
        private const string InprocAddress = "inproc://44322B99-A6B2-4ABA-B386-95582D8D51E9";
        private const string TcpAddress = "tcp://127.0.0.1:4343";

        private ILogger inProcLog = null;
        private ILogger tcpLog = null;

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Teardown()
        {
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            inProcLog =
                new LoggerConfiguration()
                    .WriteTo.NetMQ(InprocAddress, callNetMqCleanup: false)
                    .CreateLogger();

            tcpLog =
                new LoggerConfiguration()
                    .WriteTo.NetMQ(TcpAddress, callNetMqCleanup: false)
                    .CreateLogger();
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            NetMQConfig.Cleanup(false);
        }

        [Test]
        [Order(1)]
        public void TestReceiveFromLogOverInproc()
        {
            int processed = 0;
            bool done = false;

            using (var subcription1 = new SubscriberSocket(InprocAddress))
            using (var poller = new NetMQPoller { subcription1 })
            {
                subcription1.SubscribeToAnyTopic();
                subcription1.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    processed++;
                    if (processed == 3)
                    {
                        done = true;
                        poller.Stop();
                    }
                };

                poller.RunAsync();

                inProcLog.Information("First");
                inProcLog.Warning("Second");
                inProcLog.Error("Third");

                while (!done)
                    Thread.Sleep(50);

                Assert.AreEqual(3, processed);
            }
        }
        [Test]
        [Order(2)]
        public void TestReceiveFromLogSeperateTopicsOverInproc()
        {

            int infoProcessed = 0;
            int warnProcessed = 0;
            int erorProcessed = 0;
            bool infoDone = false;
            bool warnDone = false;
            bool erorDone = false;

            using (var subcription1 = new SubscriberSocket(InprocAddress))
            using (var subcription2 = new SubscriberSocket(InprocAddress))
            using (var subcription3 = new SubscriberSocket(InprocAddress))
            using (var poller = new NetMQPoller { subcription1, subcription2, subcription3 })
            {
                subcription1.Subscribe(LogEventLevel.Information.ToString());
                subcription1.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    Assert.AreEqual(LogEventLevel.Information.ToString(), msg[0]);

                    infoProcessed++;
                    if (infoProcessed == 3)
                    {
                        infoDone = true;
                    }
                };
                subcription2.Subscribe(LogEventLevel.Warning.ToString());
                subcription2.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    Assert.AreEqual(LogEventLevel.Warning.ToString(), msg[0]);

                    warnProcessed++;
                    if (warnProcessed == 3)
                    {
                        warnDone = true;
                    }
                };
                subcription3.Subscribe(LogEventLevel.Error.ToString());
                subcription3.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    Assert.AreEqual(LogEventLevel.Error.ToString(), msg[0]);

                    erorProcessed++;
                    if (erorProcessed == 2)
                    {
                        erorDone = true;
                    }
                };


                poller.RunAsync();

                inProcLog.Information("First");   // 1st INF
                inProcLog.Warning("Second");      // 1st WARN
                inProcLog.Information("Fifth");   // 2nd INF
                inProcLog.Error("Third");         // 1st ERR
                inProcLog.Error("Sixth");         // 2nd ERR
                inProcLog.Warning("Seventh");     // 2nd WARN
                inProcLog.Information("Eighth");  // 3rd INF
                inProcLog.Warning("Ninth");       // 3nd WARN

                while (!infoDone || !warnDone || !erorDone)
                    Thread.Sleep(50);

                Assert.AreEqual(3, infoProcessed);
                Assert.AreEqual(3, warnProcessed);
                Assert.AreEqual(2, erorProcessed);
            }
        }

        [Test]
        [Order(3)]
        public void TestReceiveFromLogOverTcp()
        {

            int processed = 0;
            bool done = false;

            using (var subcription1 = new SubscriberSocket())
            using (var poller = new NetMQPoller { subcription1 })
            {
                subcription1.Connect(TcpAddress);
                subcription1.SubscribeToAnyTopic();
                subcription1.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    processed++;
                    Console.WriteLine(msg[0], msg[1]);
                    if (processed == 3)
                    {
                        done = true;
                        poller.Stop();
                    }
                };

                var polling = Task.Run(() => poller.Run());


                tcpLog.Information("First");
                tcpLog.Warning("Second");
                tcpLog.Error("Third");

                polling.Wait();

                Assert.AreEqual(3, processed);
            }
        }

        [Test]
        [Order(4)]
        public void TestReceiveFromLogSeperateTopicsOverTcp()
        {

            int infoProcessed = 0;
            int warnProcessed = 0;
            int erorProcessed = 0;
            bool infoDone = false;
            bool warnDone = false;
            bool erorDone = false;
            using (var subcription1 = new SubscriberSocket(TcpAddress))
            using (var subcription2 = new SubscriberSocket(TcpAddress))
            using (var subcription3 = new SubscriberSocket(TcpAddress))
            using (var poller = new NetMQPoller { subcription1, subcription2, subcription3 })
            {
                subcription1.Subscribe(LogEventLevel.Information.ToString());
                subcription1.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    Assert.AreEqual(LogEventLevel.Information.ToString(), msg[0]);

                    infoProcessed++;
                    if (infoProcessed == 3)
                    {
                        infoDone = true;
                        poller.Remove(subcription1);
                    }
                };
                subcription2.Subscribe(LogEventLevel.Warning.ToString());
                subcription2.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    Assert.AreEqual(LogEventLevel.Warning.ToString(), msg[0]);

                    warnProcessed++;
                    if (warnProcessed == 3)
                    {
                        warnDone = true;
                        poller.Remove(subcription2);
                    }
                };
                subcription3.Subscribe(LogEventLevel.Error.ToString());
                subcription3.ReceiveReady += (s, a) =>
                {
                    var msg = a.Socket.ReceiveMultipartStrings(2);
                    Assert.AreEqual(2, msg.Count);
                    Assert.AreEqual(LogEventLevel.Error.ToString(), msg[0]);

                    erorProcessed++;
                    if (erorProcessed == 2)
                    {
                        erorDone = true;
                        poller.Remove(subcription3);
                    }
                };

                var polling = Task.Run(() => poller.Run());                

                tcpLog.Information("First");   // 1st INF
                tcpLog.Warning("Second");      // 1st WARN
                tcpLog.Information("Fifth");   // 2nd INF
                tcpLog.Error("Third");         // 1st ERR
                tcpLog.Error("Sixth");         // 2nd ERR
                tcpLog.Warning("Seventh");     // 2nd WARN
                tcpLog.Information("Eighth");  // 3rd INF
                tcpLog.Warning("Ninth");       // 3nd WARN

                while (!infoDone || !warnDone || !erorDone)
                    Thread.Sleep(50);

                poller.Stop();
                polling.Wait();

                Assert.AreEqual(3, infoProcessed);
                Assert.AreEqual(3, warnProcessed);
                Assert.AreEqual(2, erorProcessed);
            }
        }
    }
}