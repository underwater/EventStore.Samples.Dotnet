using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;

namespace AccountBalance
{
    public class Controller
    {
        private readonly ConsoleView _view;
        private readonly BalanceReadModel _rm;
        private readonly string _streamName;
        private readonly string _localFile;
        public Controller(ConsoleView view, BalanceReadModel rm, string streamName, string localFile)
        {
            _view = view;
            _rm = rm;
            _streamName = streamName;
            _localFile = localFile;
        }
        public void StartCommandLoop()
        {
            do //Command loop
            {
                var cmd = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    _view.Redraw();
                    continue;
                }
                //Single token commands
                if (cmd.Equals("clean", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Shutting down EventStore and Cleaning data");
                    EventStoreLoader.TeardownEventStore(false, true);
                    File.Delete(_localFile);
                    break;
                }
                if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Disconnecting EventStore");
                    EventStoreLoader.TeardownEventStore();
                    break;
                }
                if (cmd.Equals("repeat", StringComparison.OrdinalIgnoreCase))
                {
                    RepeatLast();
                    continue;
                }
                if (cmd.Equals("undo", StringComparison.OrdinalIgnoreCase))
                {
                    ReverseLastTransaction();
                    continue;
                }
                if (cmd.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    ListOperations();
                    continue;
                }
                if (cmd.Equals("rlist", StringComparison.OrdinalIgnoreCase))
                {
                    ListOperations(true);
                    continue;
                }
                //2 token commands
                var tokens = cmd.Split(' ');
                if (tokens.Length != 2)
                {
                    _view.ErrorMsg = "Unknown command or Invalid number of parameters.";
                    continue;
                }
                int parameter;
                if (!int.TryParse(tokens[1], out parameter))
                {
                    _view.ErrorMsg = "Command parameter not type int.";
                    continue;
                }
                switch (tokens[0].ToUpperInvariant())
                {
                    case "CREDIT":
                        EventStoreLoader.Connection.AppendToStreamAsync(
                            _streamName,
                            _rm.Checkpoint ?? ExpectedVersion.EmptyStream,
                            new EventData(
                                Guid.NewGuid(),
                                "CREDIT",
                                true,
                                Encoding.UTF8.GetBytes("{amount:" + parameter.ToString() + "}"),
                                new byte[] { }
                                )
                            );
                        break;
                    case "DEBIT":
                        EventStoreLoader.Connection.AppendToStreamAsync(
                            _streamName,
                            _rm.Checkpoint ?? ExpectedVersion.EmptyStream,
                            new EventData(
                                Guid.NewGuid(),
                                "DEBIT",
                                true,
                                Encoding.UTF8.GetBytes("{amount:" + parameter.ToString() + "}"),
                                new byte[] { }
                                )
                            );
                        break;
                    case "REPEAT":
                        Repeat(parameter);
                        break;
                    default:
                        _view.ErrorMsg = "Unknown Command";
                        break;
                }

            } while (true);
        }
        //Read Backwards 1 event to get last event
        private void ReverseLastTransaction()
        {
            StreamEventsSlice slice = EventStoreLoader.Connection.ReadStreamEventsBackwardAsync(
                _streamName,
                StreamPosition.End,
                1,
                false).Result;
            if (!slice.Events.Any() || !_rm.Checkpoint.HasValue)
            {
                _view.ErrorMsg = "Event not found to undo";
                return;
            }
            var evt = slice.Events[0].Event;
            var amount = int.Parse((string)JObject.Parse(Encoding.UTF8.GetString(evt.Data))["amount"]);
            var reversedAmount = amount * -1;

            EventStoreLoader.Connection.AppendToStreamAsync(
                evt.EventStreamId,
                _rm.Checkpoint.Value,
                new EventData(
                    Guid.NewGuid(),
                    evt.EventType,
                    evt.IsJson,
                    Encoding.UTF8.GetBytes("{amount:" + reversedAmount + "}"),
                    evt.Metadata));

        }

        private void RepeatLast()
        {
            StreamEventsSlice slice = EventStoreLoader.Connection.ReadStreamEventsBackwardAsync(
                _streamName,
                StreamPosition.End,
                1,
                false).Result;
            if (!slice.Events.Any())
            {
                _view.ErrorMsg = "Event not found to repeat";
                return;
            }
            var evt = slice.Events[0].Event;
            EventStoreLoader.Connection.AppendToStreamAsync(
                evt.EventStreamId,
                evt.EventNumber,
                new EventData(
                    Guid.NewGuid(),
                    evt.EventType,
                    evt.IsJson,
                    evt.Data,
                    evt.Metadata));

        }
        private void Repeat(int position)
        {

            EventReadResult result = EventStoreLoader.Connection.ReadEventAsync(
                _streamName,
                position,
                false).Result;


            if (result.Status != EventReadStatus.Success || result.Event == null)
            {
                _view.ErrorMsg = "Event not found to repeat";
                return;
            }
            var evt = result.Event.Value.Event;
            EventStoreLoader.Connection.AppendToStreamAsync(
                evt.EventStreamId,
                ExpectedVersion.Any,
                new EventData(
                    Guid.NewGuid(),
                    evt.EventType,
                    evt.IsJson,
                    evt.Data,
                    evt.Metadata));

        }

        private void ListOperations(bool reversed = false)
        {
            var streamEvents = new List<ResolvedEvent>();

            StreamEventsSlice currentSlice;
            var nextSliceStart = reversed ? StreamPosition.End : StreamPosition.Start;
            do
            {
                currentSlice =
                    reversed ?
                        EventStoreLoader.Connection.ReadStreamEventsBackwardAsync(
                            _streamName,
                            nextSliceStart,
                            20,
                            false).Result

                        :
                        EventStoreLoader.Connection.ReadStreamEventsForwardAsync(
                            _streamName,
                            nextSliceStart,
                            20,
                            false).Result;
                nextSliceStart = currentSlice.NextEventNumber;
                streamEvents.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);

            _view.EventList = streamEvents;
        }


    }
}