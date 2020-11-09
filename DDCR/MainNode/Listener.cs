using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DDCR.Interfaces;

namespace DDCR
{
    /// <summary>
    /// Responsible for listening on the port for any incoming messages from either other Main Nodes and
    /// sub-net Peers.When a message is received, then it is passed on to be parsed.
    /// </summary>
    public class Listener
    {
        private readonly IConfig config;
        private readonly int port;
        private readonly Parser parser;
        private char Terminator;

        public Listener(Parser parser, IConfig config)
        {
            this.parser = parser;
            this.port = config.ListenPort;
            this.config = config;
            this.Terminator = config.Terminator;
        }
        /// <summary>
        /// A listen method waiting for incoming TCP requests.
        /// </summary>
        public void Listen()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Server is listening on port: " + port);
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Task.Run(() => HandleConnection(client));
            }
        }
        /// <summary>
        /// A method for handling TCP requests. Reads the incoming messages and passes it on to the parser.
        /// The string returned from the peer is sent to the client.
        /// </summary>
        /// <param name="client">TCP client whoms connection needs to be handled.</param>
        private void HandleConnection(TcpClient client)
        {
            client.ReceiveTimeout = config.ClientTimeoutMs;
            client.SendTimeout = config.ClientTimeoutMs;

            IPEndPoint clientInfo = (IPEndPoint)client.Client.RemoteEndPoint;
            Console.WriteLine("L:Client {0}:{1} connected", clientInfo.Address, clientInfo.Port);
            NetworkStream stream = client.GetStream();
            char peerType = ' ';
            bool endConnection = false;
            while (true)
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        byte[] buffer = new byte[client.ReceiveBufferSize];
                        int size = stream.Read(buffer);

                        string msg = Parser.DecodeMessage(buffer);
                        peerType = Convert.ToChar(msg.Substring(0, 1));
                        if (msg[msg.Length-1] == Terminator)
                        {
                            endConnection = true;
                            msg = msg.Substring(0, msg.Length - 1);
                        }
                        string actualMsg = msg.Substring(2);
                        string reply = "INVALID";
                        Console.WriteLine("L:RECEIVED: " + msg);

                        if (peerType == 'M')
                            reply = parser.ParseMainNode(actualMsg);

                        else if (peerType == 'N')
                            reply = parser.ParseNode(actualMsg, clientInfo.Address);
                        
                        stream.Write(Encoding.UTF8.GetBytes(reply), 0, reply.Length);
                        Console.WriteLine("L:REPLY: " + reply);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"L:Connection closed with message: {e.Message}");
                    endConnection = true;
                }

                if (endConnection || !client.Connected)
                {
                    stream.Close();
                    client.Close();
                    Console.WriteLine("L:CLOSED SOCKET {0}:{1}", clientInfo.Address, clientInfo.Port);
                    return;
                }

                Task.Delay(15).Wait();
            }
        }
    }
}