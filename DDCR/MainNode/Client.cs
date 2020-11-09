using DDCR.Interfaces;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DDCR
{
    public class Client : IClient
    {
        public IPAddress IP => ip;
        public int Port => port;
        private readonly IPAddress ip;
        private readonly int port;
        private TcpClient client;
        public Semaphore Semaphore = new Semaphore(1, 1);
        public IPAddress IpAddress => ip;
        private IConfig config;
        private char Terminator;

        public Client(string ip, int port, IConfig config)
        {
            this.config = config;
            this.ip = IPAddress.Parse(ip);
            this.port = port;
            client = new TcpClient()
            {
                ReceiveTimeout = config.ClientTimeoutMs,
                SendTimeout = config.ClientTimeoutMs
            };
            Terminator = config.Terminator;
        }

        public async Task<string> SendAsync(string msg, bool endConnection)
        {
            string reply = string.Empty;
            Semaphore.WaitOne();
            Console.WriteLine("C:REQUEST: {0}", msg);

            try
            {
                //If NOT already connected, establish a connection
                if (!client.Client.Connected && !IsConnected())
                {
                    await client.ConnectAsync(ip, port);
                }
                NetworkStream stream = client.GetStream();
                //Prepend M to indicate it is a main node request.
                //Append terminator symbol if it is the last message to be sent in a sequence.
                if (endConnection)
                    msg += Terminator;

                byte[] buffer = Encoding.UTF8.GetBytes("M:"+msg);
                await stream.WriteAsync(buffer, 0, buffer.Length);

                buffer = new byte[client.ReceiveBufferSize];
                int replySize = await stream.ReadAsync(buffer, 0, buffer.Length);

                reply = Parser.DecodeMessage(buffer);

                if (reply[reply.Length - 1] == Terminator)
                {
                    endConnection = true;
                    reply = reply.Substring(0, reply.Length - 1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("C:Exception occured: {0}", e.Message);
                endConnection = true;
                reply = "UNAVAILABLE";
            }
            if (endConnection)
            {
                try
                {
                    client.GetStream().Close();
                    client.Close();
                    Console.WriteLine("C:Closed Socket {0}:{0}", ip, port);
                } catch { }
                client = new TcpClient()
                {
                    ReceiveTimeout = config.ClientTimeoutMs,
                    SendTimeout = config.ClientTimeoutMs
                };

            }
            Semaphore.Release();
            Console.WriteLine("C:REPLY = {0}",  reply);
            return reply;
        }

        /// <summary>
        /// Checks if a client and the endpoint is connected.
        /// </summary>
        /// <returns>Returns a bool indicating whether or not they are connected.</returns>
        private bool IsConnected()
        {
            //Note: This is not very efficient since it loops through all active TCP connections, but it is faster than closing and reconnecting every time. 
            IPGlobalProperties ipProp = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation connection = ipProp.GetActiveTcpConnections()
                .FirstOrDefault(x =>
                    x.LocalEndPoint.Equals(client.Client.LocalEndPoint) //default value is null
                    && x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint));

            if (connection != null && connection.State == TcpState.Established)
                return true;

            return false;
        }
    }
}