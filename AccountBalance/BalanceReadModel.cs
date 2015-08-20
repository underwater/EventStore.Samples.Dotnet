using System;
using System.IO;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;

namespace AccountBalance
{
    /// <summary>
    /// An in-memory read model that persists a checkpoint and current state on disk 
    /// for recovery between runs. 
    /// If the checkpoint file does no exist we replay from the beginning 
    /// </summary>
    public class BalanceReadModel
    {
        private int _total;
        public int? Checkpoint { get; private set; }
        private readonly string _streamName;
        private readonly string _localFile;
        private readonly ConsoleView _view;


        public BalanceReadModel(ConsoleView view, string streamName, string localFile)
        {
            _view = view;
            _streamName = streamName;
            _localFile = localFile;
            int? checkpoint = null;
            //See if we have a local checkpoint file
            if (File.Exists(_localFile))
            {
                try
                {
                    var text = File.ReadAllText(_localFile);
                    var tokens = text.Split(',');
                    if (tokens.Length == 2)
                    {
                        checkpoint = int.Parse(tokens[0]);
                        _total = int.Parse(tokens[1]);
                    }
                }
                catch
                {
                    //error loading file
                    checkpoint = null;
                    _total = 0;
                }
            }


            _view.Total = _total;
            //n.b. if there is no checkpoint we use null
            Checkpoint = checkpoint;
            Subscribe();
            
        }

        private void Subscribe()
        {
            EventStoreLoader.Connection.SubscribeToStreamFrom(_streamName, Checkpoint, false, GotEvent, subscriptionDropped: Dropped  );
        }

        private void Dropped(EventStoreCatchUpSubscription sub,SubscriptionDropReason reason,Exception ex)
        {
            //Reconnect if we drop
            //TODO: check the reason and handle it appropriately
            _view.ErrorMsg = "Subscription Dropped, press Enter to reconnect";
            Subscribe();
        }

        private void GotEvent(EventStoreCatchUpSubscription sub, ResolvedEvent evt)
        {
            try
            {
                //create local copies of state variables
                var total = _total;
                var checkpoint = evt.Event.EventNumber;

                var amount = (string)JObject.Parse(Encoding.UTF8.GetString(evt.Event.Data))["amount"];
                switch (evt.Event.EventType.ToUpperInvariant())
                {
                    case "CREDIT":
                        total += int.Parse(amount);
                        break;
                    case "DEBIT":
                        total -= int.Parse(amount);
                        break;
                    default:
                        throw new Exception("Unknown Event Type");
                }
                File.WriteAllText(_localFile, checkpoint + "," + total);
                //Update the common state after commit to disk
                _total = total;
                Checkpoint = checkpoint;
            }
            catch (Exception ex)
            {
                _view.ErrorMsg = "Event Exception: " + ex.Message;
            }
            //repaint screen
            _view.Total = _total;
        }
    }
}