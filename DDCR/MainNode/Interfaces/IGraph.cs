using DDCR.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DDCR
{
    /// <summary>
    /// The local graph for the MainNode
    /// </summary>
    public interface IGraph
    {
        /// <summary>
        /// Mapping of execution Ids to a list of EventInternal references. Used to easily unblock events after finishing an execution.
        /// </summary>
        Dictionary<Guid, List<EventInternal>> Blocked { get; }

        /// <summary>
        /// Mapping of string EventInternal names to references of their instantiations, for fast access.
        /// </summary>
        Dictionary<string, EventInternal> Events { get; }

        /// <summary>
        /// Mapping of execution ID to temporary events, which are of type EventForeign. Required so that these foreign events are not lost between ingoing requests.
        /// </summary>
        Dictionary<Guid, EventForeign> EventsForeign { get; }

        /// <summary>
        /// Contains a list of Guids from previously processed Accepting requests. Used in order to prevent duplicate messaging.
        /// </summary>
        List<Guid> GetAcceptingIds { get; }

        /// <summary>
        /// Contains a list of Guids from previously processed Log requests. Used in order to prevent duplicate messaging.
        /// </summary>
        List<Guid> GetLogIds { get; }

        /// <summary>
        /// The log for the local graph.
        /// </summary>
        ILog Log { get; }

        /// <summary>
        /// NodeEvents is a mapping of Nodes, identified by their IP addresses - to a HashSet which contains references to what events they are allowed to execute.
        /// </summary>
        Dictionary<IPAddress, HashSet<EventInternal>> NodeEvents { get; }

        /// <summary>
        /// Loads the graph.xml file located in the same folder as the executable, and constructs a DCR local DCR graph based on the properties of the Config
        /// </summary>
        /// <param name="path">The path to the XMP graph file.</param>
        public void Load(string path);
        /// <summary>
        /// Adds a relation from an already existing EventInternal, to an already existing EventExternal.
        /// </summary>
        /// <param name="eNameFrom">The name of the EventInternal, used to index Graph.Events.</param>
        /// <param name="eNameTo">The name of the EventExternal, used to index Graph.EventsExternal.</param>
        /// <param name="relation">The type of relation to be added.</param>
        void AddRelationExternal(string eNameFrom, string eNameTo, Relation relation);

        /// <summary>
        /// Adds a relation from one EventInternal to another.
        /// </summary>
        /// <param name="eNameFrom">The name of the EventInternal the relation goes from, used to index Graph.Events.</param>
        /// <param name="eNameTo">The name of the EventInternal the relation goes to, used to index Graph.Events.</param>
        /// <param name="relation">The type of relation to be added.</param>
        void AddRelationInternal(string eNameFrom, string eNameTo, Relation relation);

        /// <summary>
        /// Changes the markings of local events based of the information in "ev".
        /// </summary>
        /// <param name="ev">A foreign event containing the information for marking changes.</param>
        /// <param name="revert">Indicates if the markings should be reverted. If true - revert markings.</param>
        void ChangeMarkings(EventForeign ev, bool revert);

        /// <summary>
        /// Used for to respond to incoming requests from other MainNodes, to check whether an event is currently blocked.
        /// </summary>
        /// <param name="evs">The names of the events to check.</param>
        /// <returns>False if none of the events are blocked, true if atleast one is blocked.</returns>
        bool CheckBlocked(string[] evs);

        /// <summary>
        /// Checks if a peer replied "UNAVAILABLE" by looking at the value of the input dictionary.
        /// </summary>
        /// <param name="tasks"> A dictionary containing Main Nodes as keys and their reply message as values.</param>
        /// <returns>Returns a bool indicating whether or not a reply was "UNAVAILABLE".</returns>
        bool CheckPeerReplies(Dictionary<string, Task<string>> tasks);

        /// <summary>
        /// Creates an EventInternal, adds it to the Graph, and sets up the permissions of which node(s) are allowed to execute the event.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="label">The label of the event. Currently unused.</param>
        /// <param name="owners">One or more owners of the event.</param>
        /// <param name="included">Optional, the Included marking of the event. Default is true.</param>
        /// <param name="pending">Optional, the Pending marking of the event. Default is false.</param>
        void CreateEvent(string name, string label, string[] owners, bool included = true, bool pending = false, bool executed = false);

        /// <summary>
        /// Creates an EventExternal, and adds it to the graph.
        /// </summary>
        /// <param name="name">The name of the EventExternal. Must match the name on the external MainNode.</param>
        /// <param name="label">The label of the EventExternal. Currently unused.</param>
        /// <param name="peer">The name of the MainNode, which this event is located on.</param>
        void CreateEventExternal(string name, string label, string peer);

        /// <summary>
        /// Execute method. Blocks internal and external events before exevuting the local event.
        /// </summary>
        /// <param name="eventName">The event to be executed.</param>
        /// <returns>Returns a bool indicating whether or not the executing was a success.</returns>
        bool Execute(string eventName);

        /// <summary>
        /// Queries other peers to determine whether their local graph is in a Accepting.
        /// </summary>
        /// <param name="peers">Peers to propagate the request to</param>
        /// <param name="prevPeers">Peers that have already received a request, and thus should be ignored</param>
        /// <param name="id">Guid for the current Accepting request</param>
        /// <returns>true if all local graphs are Accepting, otherwise false</returns>
        bool GetAcceptingExternal(Dictionary<string, IClient> peers, string prevPeers, Guid id);

        /// <summary>
        /// Iterates through all EventInternal instantiations in Graph.Events to check whether the local graph is Accepting.
        /// </summary>
        /// <returns>true for Accepting, otherwise false</returns>
        bool GetAcceptingInternal();

        /// <summary>
        /// Gets and converts the marking of an EventInternal to an easily readable string, which is returned.
        /// </summary>
        /// <param name="e">The EventInternal containing the markings</param>
        /// <returns>A string with the markings of the input event</returns>
        string GetMarking(EventInternal e);

        /// <summary>
        /// Gets and converts the markings of all of the graphs EventInternal to an easily readable string, which is returned.
        /// </summary>
        /// <returns>All the Graph.Events markings as a single string</returns>
        string GetAllMarkings();

        /// <summary>
        /// Builds the "actions" list in a foreign event based on the relations of the foreign event.
        /// </summary>
        /// <param name="ev">The foreign event.</param>
        /// <returns>Returns a list of "actions" indicating which markings should be changed and how.</returns>
        List<(EventInternal, Relation, bool)> GetNewMarkingsReversible(EventForeign ev);

        /// <summary>
        /// Finds and the events that a Node, identified by its IP, has permissions to execute, and returns them in string format.
        /// </summary>
        /// <param name="ip">The IP of the Node that sent the request, used to index the Graph.NodeEvents property.</param>
        /// <returns>The InternalEvents that a Node has permissions to execute, in string format.</returns>
        string GetNodeEvents(IPAddress ip);

        /// <summary>
        /// Creates messages to be forwarded to each affected MainNode, based on how their markings will change by executing ev.
        /// </summary>
        /// <param name="ev">A currently executing event, whose marking and relations are used in order to create the peer messages.</param>
        /// <returns></returns>
        Dictionary<string, string> GetPeerMessages(Event ev);

        /// <summary>
        /// Sends a message to a Main Node. The messages contains the input "command" string and the input "executeId" Guid.
        /// A message is only sent to a Main node if it has not previously replies with "UNAVAILABLE".
        /// </summary>
        /// <param name="command">The keyword of the message, i.e "BLOCK", "EXECUTE" etc.</param>
        /// <param name="executeId">A ID associated with the execution.</param>
        /// <param name="tasksPrev">A dictionary containing previously sent to Main nodes. 
        /// Key is the Main node name and value is the reply the Main node got.</param>
        /// <returns>Returns </returns>
        Dictionary<string, Task<string>> MessagePeers(string command, Guid executeId, Dictionary<string, Task<string>> tasksPrev, bool lastMsg);

        /// <summary>
        /// Sends a BLOCK message to Main nodes requesting them to block event(s).
        /// If one of the main nodes can not block the event(s), it will unblock the events in other main nodes.
        /// </summary>
        /// <param name="peerMessages">A dictionary containing main node names a keys and the message as value.</param>
        /// <param name="executeId">The ID associated with the execution.</param>
        /// <param name="executeTime">The time of the execution.</param>
        /// <param name="lastMsg">Indicates whether this is the last message in a sequence.</param>
        /// <returns>Returns a bool indicating whether or not all the blocks were a success.</returns>
        bool TryBlockExternal(Dictionary<string, string> peerMessages, Guid executeId, string executeTime);

        /// <summary>
        /// Tries to block internal events based off an events relations. 
        /// </summary>
        /// <param name="ev">The event whoms relations is used for blocking.</param>
        /// <param name="execId">The ID associated with the execution.</param>
        /// <returns>Returns a bool indicating whether or not all the blocks were a success.</returns>
        bool TryBlockInternal(Event ev, Guid execId);

        /// <summary>
        /// Communicate to a external event in another Main Node to change its markings. 
        /// If one of the Main Nodes fail to reply the method will attempt to revert.
        /// </summary>
        /// <param name="tasks">tasks contains which Main Nodes to send the the execute message to.</param>
        /// <param name="execId">The ID associated with the execution.</param>
        /// <returns>Returns a bool indicating whether or not the marking changes was a success.</returns>
        bool TryExecuteExternal(Dictionary<string, Task<string>> tasks, Guid execId);

        /// <summary>
        /// Repeatedly try to re-connect with a Main Node up to the Config.MaxConnectionAttempts amount of tries.
        /// </summary>
        /// <param name="peerName">The Main node name</param>
        /// <param name="execId">The execution ID</param>
        void TryUnblockPeerAsync(string peerName, string execId);

        /// <summary>
        /// Unblocks all EventInternal instantiations which have been blocked by the execution, as indicated by the execution ID parameter.
        /// </summary>
        /// <param name="execId">The execution ID, used to find what EventInternal's to unblock by indexing Graph.Blocked.</param>
        void UnblockEvents(Guid execId);
    }
}