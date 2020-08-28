using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace Node.Interfaces
{
    /// <summary>
    /// A configuration, containing the name of the Node, an instance of Client with information relating to the MainNode, and how long the client should wait for a reply before timing out.
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// The amount of time in milliseconds to wait before timing out a client, after a connection has ended and/or after initating a connection and not receiving a reply.
        /// </summary>
        int ClientTimeoutMs { get; set; }

        /// <summary>
        /// The name of the Node
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// The Client used to communicate with the associated MainNode
        /// </summary>
        public IClient MainNode { get; set; }

        /// <summary>
        /// The IP address of the client
        /// </summary>
        public string MainNodeIP { get; }

        /// <summary>
        /// The port that the MainNode is listening for requests on
        /// </summary>
        public int MainNodePort { get; }
    }
}