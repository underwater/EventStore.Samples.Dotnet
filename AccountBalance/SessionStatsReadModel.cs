using System;
using EventStore.ClientAPI;

namespace AccountBalance
{
    public class SessionStatsReadModel
    {
        private const string CreditStream = "$et-CREDIT";
        private const string DebitStream = "$et-DEBIT";
        private readonly ConsoleView _view;
        private int _creditCount;
        private int _debitCount;
        
        public SessionStatsReadModel(ConsoleView view)
        {

            _view = view;            
            EnsureEventTypeStreamsExist();
            EventStoreLoader.Connection.SubscribeToStreamAsync(CreditStream, true, GotCredit);
            EventStoreLoader.Connection.SubscribeToStreamAsync(DebitStream, true, GotDebit);
        }

        private static void EnsureEventTypeStreamsExist()
        {
            var tempStreamId = "Temp-" + Guid.NewGuid();
            var triggerStreamCreation = false;

            try
            {
                var creditEvent = EventStoreLoader.Connection.ReadEventAsync(CreditStream, 0, false).Result;
                var debitEvent = EventStoreLoader.Connection.ReadEventAsync(DebitStream, 0, false).Result;
                if (creditEvent.Status != EventReadStatus.Success || debitEvent.Status != EventReadStatus.Success)
                    triggerStreamCreation = true;
            }
            catch
            {
                triggerStreamCreation = true;
            }
            if (!triggerStreamCreation) return;
            //We'll just drop some of the correct event types into a temp stream to trigger the projection

            EventStoreLoader.Connection.AppendToStreamAsync(
                tempStreamId,
                ExpectedVersion.Any,
                new EventData(
                    Guid.NewGuid(),
                    "CREDIT",
                    false,
                    new byte[] { },
                    new byte[] { })
                ).Wait();
            EventStoreLoader.Connection.AppendToStreamAsync(
                tempStreamId,
                ExpectedVersion.Any,
                new EventData(
                    Guid.NewGuid(),
                    "DEBIT",
                    false,
                    new byte[] { },
                    new byte[] { })
                ).Wait();
            EventStoreLoader.Connection.DeleteStreamAsync(tempStreamId, ExpectedVersion.Any).Wait();
        }

        private void GotCredit(EventStoreSubscription sub, ResolvedEvent resolvedEvent)
        {
            _creditCount ++;
            _view.Credits = _creditCount;
            _view.Transactions = _creditCount + _debitCount;
        }
        private void GotDebit(EventStoreSubscription sub, ResolvedEvent resolvedEvent)
        {
            _debitCount ++;
            _view.Debits = _debitCount;
            _view.Transactions = _creditCount + _debitCount;
        }
    }
}