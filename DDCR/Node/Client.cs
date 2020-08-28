using System;
using System.Text;
using System.Net.Sockets;
using Node.Interfaces;

namespace Node
{

    public class Client : IClient
    {
        IConfig config;
        private TcpClient client;

        public Client(IConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// An synchronous method for sending messages to a client.
        /// Converts the message to bytes before sending.
        /// </summary>
        /// <param name="msg">Message to be sent</param>
        /// <returns>Returns a string representing the reply from the client.</returns>
        public string Send(string msg) 
        {
            try
            {
                client = new TcpClient(config.MainNodeIP, config.MainNodePort)
                {
                    ReceiveTimeout = config.ClientTimeoutMs,
                    SendTimeout = config.ClientTimeoutMs
                };
                Console.WriteLine("REQUEST: " + msg);
                var stream = client.GetStream();

                //send msg
                byte[] buffer = Encoding.UTF8.GetBytes("N:"+msg + '\u0017');

                stream.Write(buffer, 0, buffer.Length);
                buffer = new byte[client.ReceiveBufferSize];
                int replySize = stream.Read(buffer, 0, buffer.Length);
                client.Close();
                return DecodeMessage(buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "UNAVAILABLE";
            }
        }

        /// <summary>
        /// Converts a Byte array to a string and replaces the tailing whitespace with empty strings.
        /// </summary>
        /// <param name="msg">The incoming message</param>
        /// <returns>Returns the incoming message as a string.</returns>
        public static string DecodeMessage(byte[] input)
        {
            var decoded = Encoding.ASCII.GetString(input);
            return decoded.Replace("\0", string.Empty);
        }
    }
}