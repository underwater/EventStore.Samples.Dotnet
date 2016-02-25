using System;
using System.Net;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace PersistentSubscription
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
        const string STREAM = "a_test_stream";
        const string GROUP = "a_test_group";
        const int DEFAULTPORT = 1113;

        static void Main(string[] args)
        {
            
            //uncommet to enable verbose logging in client.
            var settings = ConnectionSettings.Create();//.EnableVerboseLogging().UseConsoleLogger();
            using (var conn = EventStoreConnection.Create(settings, new IPEndPoint(IPAddress.Loopback, DEFAULTPORT)))
            {
                conn.ConnectAsync().Wait();
                
                //Normally the creating of the subscription group is not done in your general executable code. 
                //Instead it is normally done as a step during an install or as an admin task when setting 
                //things up. You should assume the subscription exists in your code.
                CreateSubscription(conn);
               
                conn.ConnectToPersistentSubscription(STREAM, GROUP, (_, x) =>
                {
                    var data = Encoding.ASCII.GetString(x.Event.Data);
                    Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
                    Console.WriteLine(data);
                });

                Console.WriteLine("waiting for events. press enter to exit");
                Console.ReadLine();
            }

        }

        private static void CreateSubscription(IEventStoreConnection conn)
        {
            PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
                .DoNotResolveLinkTos()
                .StartFromCurrent();

            try
            {
                conn.CreatePersistentSubscriptionAsync(STREAM, GROUP, settings, new UserCredentials("admin", "changeit")).Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException.GetType() != typeof(InvalidOperationException)
                    && ex.InnerException?.Message != $"Subscription group {GROUP} on stream {STREAM} already exists")
                {
                    throw;
                }
            }
        }
    }
}
