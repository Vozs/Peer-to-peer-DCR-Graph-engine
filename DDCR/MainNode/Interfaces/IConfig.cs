using DDCR.Interfaces;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace DDCR
{
    public interface IConfig
    {
        /// <summary>
        /// The amount of time in milliseconds to wait before timing out a client, after a connection has ended and/or after initating a connection and not receiving a reply.
        /// </summary>
        int ClientTimeoutMs { get; set; }
        /// <summary>
        /// The culture which determines what TimeDate formatting, e.g. in the Log
        /// </summary>
        CultureInfo Culture { get; set; }
        /// <summary>
        /// The port that the MainNode should listen for ingoing requests on
        /// </summary>
        int ListenPort { get; set; }
        /// <summary>
        /// The amount of connection attempts that should be made until giving up, when attempting to unblock a non responsive MainNode.
        /// </summary>
        int MaxConnectionAttempts { get; set; }
        /// <summary>
        /// The name of the MainNode, used primarily in both the communication protocol when propagating requests, as well as when building a graph from XML.
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Mapping of main nodes to clients. Other main nodes should only be known if there is an outgoing relation to them.
        /// </summary>
        public Dictionary<string, IClient> MainNodes { get; }
        /// <summary>
        /// Mapping of node names to clients (Non port-forwarded peers)
        /// </summary>
        public Dictionary<string, IPAddress> Nodes { get; }
        /// <summary>
        /// Mapping of nodes to the events that they may execute.
        /// </summary>
        public Dictionary<IPAddress, HashSet<string>> NodeEventsMapping { get; }
    }
}