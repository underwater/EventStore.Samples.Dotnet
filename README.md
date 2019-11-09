 EventStore.Samples.Dotnet
=========================

Samples with the dotnetapi

# Account Balance Sample App#
A simple CQRS MVC Console application that demonstrates many of the features of the EventStore .net Client API

**BalanceReadModel** uses a catchup subscription with a checkpoint saved on disk

**SessionStatsReadModel** uses a live subscription to count events seen during the current session

**Controller** runs the command loop

-     The controller posts updates to the event store
-     Screen updates are triggered by changes in the data in the event store
-     Multiple concurrent instances can be running and all will seamlessly post and get updates
-     Other UIs can be built and attached at will
   
Commands

- **credit** & **debit** post events to a stream with the current checkpoint or NoStream as the expected position
- **repeat** reads the last event posted to a stream and posts a new copy back to the stream using EventNumber as the expected position
- **repeat #** reads a single event from a known position in a stream and posts a new copy back to the stream using ExpectedEvent.Any as the expected position
- **list** reads all events in a stream forwards
- **rlist** reads all events in a stream backwards
- **undo** creates a reversal event to "erase" a mistake in an append only system
- **exit** shuts down while leaving the EventStore running
- **clean** shuts down, stops the EventStore, Deletes the EventStore Data, and deletes the Checkpoint file

**EventStoreLoader** 

-      connects to or starts the event store as a separate process using the config file     
-      Enables Projections via config file
-      Starts the system projections using the projection manager
-      The currently embedded version of the EventStore is 3.1.0
     
**N.B.**: To launch the EventStore the Application needs to be run as an administrator,
this means for interactive debugging your development environment needs to be running as an administrator.
If you are connecting to an already running event store Admin rights are not required. 


   
