using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace AccountBalance
{
    public static class EventStoreLoader

    {
        public enum StartConflictOption
        {
            Kill,
            Connect,
            Error
        }

        private const string Path = @".\EventStore\EventStore.ClusterNode.exe";

        private const string Args = "--config=./EventStore/config.yaml";

        private static Process _process;

        public static IEventStoreConnection Connection { get; private set; }
        public static void SetupEventStore(StartConflictOption opt = StartConflictOption.Connect)
        {

            //TODO: Convert to Embedded when I can figure out loading the miniWeb component
            var runningEventStores = Process.GetProcessesByName("EventStore.ClusterNode");
            if (runningEventStores.Length != 0)
            {
                switch (opt)
                {
                    case StartConflictOption.Connect:
                        _process = runningEventStores[0];
                        break;
                    case StartConflictOption.Kill:
                        foreach (var es in runningEventStores)
                        {
                            es.Kill();
                        }
                        break;
                    case StartConflictOption.Error:
                        throw new Exception("Conflicting EventStore running.");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(opt), opt, null);
                }
            }
            if (_process == null)
            {
                _process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false, CreateNoWindow = true, FileName = Path, Arguments = Args, Verb = "runas"
                    }
                };
                _process.Start();
            }
            var tcp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113);
            var http = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);
            Connection = EventStoreConnection.Create(tcp);
            Connection.ConnectAsync().Wait();
            var pManager = new ProjectionsManager(new NullLogger(), http, TimeSpan.FromSeconds(5));
            var creds = new UserCredentials("admin", "changeit");
            bool ready = false;
            int retry = 0;
            while (!ready)
            {
                try
                {
                    pManager.EnableAsync("$streams", creds).Wait();
                    pManager.EnableAsync("$by_event_type", creds).Wait();
                    pManager.EnableAsync("$by_category", creds).Wait();
                    pManager.EnableAsync("$stream_by_category", creds).Wait();
                    ready = true;
                }
                catch
                {
                    retry++;
                    if (retry > 8)
                        throw new Exception("EventStore Projection Start Error.");
                    System.Threading.Thread.Sleep(250);
                }
            }
        }

        public static void TeardownEventStore(bool leaveRunning = true, bool dropData = false)
        {
            Connection.Close();
            if (
                leaveRunning ||
                _process == null ||
                _process.HasExited
                ) return;

            _process.Kill();
            _process.WaitForExit();
            if (dropData)
            {
                Directory.Delete(@".\ESData", true);
            }
        }
    }

    public class NullLogger : ILogger
    {
        public void Debug(string format, params object[] args)
        {
        }

        public void Debug(Exception ex, string format, params object[] args)
        {
        }

        public void Error(string format, params object[] args)
        {
        }

        public void Error(Exception ex, string format, params object[] args)
        {
        }

        public void Info(string format, params object[] args)
        {
        }

        public void Info(Exception ex, string format, params object[] args)
        {
        }
    }
}