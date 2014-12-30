using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace CatchupSubscription
{
    /*
     * This example sets up a volatile subscription to a test stream.
     * 
     * As written it will use the default ipaddress (loopback) and the default tcp port 1113 of the event
     * store. In order to run the application bring up the event store in another window (you can use
     * default arguments eg EventStore.ClusterNode.exe) then you can run this application with it. Once 
     * this program is running you can run the WritingEvents sample to write some events to the stream
     * and they will appear over the catch up subscription. You can also run many concurrent instances of this
     * program and each will receive the events over the subscription.
     * 
     */
    class Program
    {
        static void Main(string[] args)
        {
            const string STREAM = "a_test_stream";
            const int DEFAULTPORT = 1113;
            //uncommet to enable verbose logging in client.
            var settings = ConnectionSettings.Create();//.EnableVerboseLogging().UseConsoleLogger();
            using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, DEFAULTPORT)))
            {
                conn.ConnectAsync().Wait();
                //Note the subscription is subscribing from the beginning every time. You could also save
                //your checkpoint of the last seen event and subscribe to that checkpoint at the beginning.
                //If stored atomically with the processing of the event this will also provide simulated
                //transactional messaging.
                var sub = conn.SubscribeToStreamFrom(STREAM, StreamPosition.Start,true,
                    (_, x) =>
                    {
                        var data = Encoding.ASCII.GetString(x.Event.Data);
                        Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
                        Console.WriteLine(data);
                    });
                Console.WriteLine("waiting for events. press enter to exit");
                Console.ReadLine();
            }

        }
    }
}
