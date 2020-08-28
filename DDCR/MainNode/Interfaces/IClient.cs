using System.Net;
using System.Threading.Tasks;

namespace DDCR.Interfaces
{
    /// <summary>
    /// Client is responsible for outgoing communication to other Main Nodes.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// The IP address of the client
        /// </summary>
        public IPAddress IP { get; }

        /// <summary>
        /// The port of the client
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// An asynchronous method for sending messages to a client.
        /// Converts the message to bytes before sending.
        /// </summary>
        /// <param name="msg">Message to be sent</param>
        /// /// <param name="endConnection">Whether the socket should be closed after receiving a reply to the sent message.</param>
        /// <returns>Returns a Task<string> representing the reply from the client.</returns>
        public Task<string> SendAsync(string msg, bool endConnection);
    }
}
