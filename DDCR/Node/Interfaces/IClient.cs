
using System.Net;

namespace Node.Interfaces
{
    /// <summary>
    /// Client is responsible for outgoing communication to the Main Node.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// An synchronous method for sending messages to a client.
        /// Converts the message to bytes before sending.
        /// </summary>
        /// <param name="msg">Message to be sent</param>
        /// <returns>Returns a string representing the reply from the client.</returns>
        public string Send(string msg);
    }
}
