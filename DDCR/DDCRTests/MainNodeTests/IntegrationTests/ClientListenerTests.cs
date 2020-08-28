using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using DDCR.Interfaces;
using DDCR;
using Moq;
using System.Globalization;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace DDCRTests.IntegrationTests
{

    /*Note, this does use the Parser class - but it does not test against the parser,
     * merely uses a case of the current parser implementation (the simplest case).
     * TLDR; These tests are integrated with the Parser class, but do not test the parser class,
     * and merely use it to check whether the Listener actually replied, since both an unresponsive Listener
     * and an invalid message to the Listener, will result in the same return value from SendAsync - "UNAVAILABLE".
     */
    class ClientListenerTests
    {
        Mock<IConfig> mockConfig;
        string localhost = "127.0.0.1";
        int listenPort = 60000;
        int clientPort = 60001;
        Listener listener;
        Client client;
        char TERMINATOR = '\u0017';
        Guid guid = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            mockConfig = new Mock<IConfig>();
            mockConfig.Setup(x => x.Culture).Returns(new CultureInfo("en-GB"));
            mockConfig.Setup(x => x.ClientTimeoutMs).Returns(5000);
            mockConfig.Setup(x => x.ListenPort).Returns(listenPort);
            mockConfig.Setup(x => x.Name).Returns("listener");


            client = new Client("127.0.0.1", listenPort, mockConfig.Object);
            var listenerClientCopy = new Client("127.0.0.1", clientPort, mockConfig.Object);
            

            mockConfig.Setup(x => x.MainNodes).Returns(new Dictionary<string, IClient> { {"listener", listenerClientCopy } });

            Mock<IGraph> mockGraph = new Mock<IGraph>();

            //LOG IGNORE is the Parser.MainNode case that requires the least setup
            mockGraph.Setup(x => x.GetLogIds).Returns(new List<Guid> { guid });

            Parser parser = new Parser(mockGraph.Object, mockConfig.Object);

            listener = new Listener(parser, mockConfig.Object);

        }

        [Test]
        public void SendReceive_MsgNoTerminator_Replies()
        {
            listenPort = 60000;
            clientPort = 60001;

            var cancellationTokenSource = new CancellationTokenSource();
            var t = Task.Factory.StartNew(() =>
            {
                listener.Listen();
            }, cancellationTokenSource.Token).ContinueWith(task => cancellationTokenSource.Token);

            var result = client.SendAsync("LOG "+guid, false).Result;
            var result2 = client.SendAsync("some invalid message", false).Result;
            cancellationTokenSource.Cancel();

            Assert.AreEqual("LOG IGNORE", result);
            Assert.AreEqual("UNAVAILABLE", result2);
            
        }

        [Test]
        public void SendReceive_MsgWithTerminator_RepliesOnlyMsg()
        {
            listenPort = 60002;
            clientPort = 60003;
            var cancellationTokenSource = new CancellationTokenSource();
            var t = Task.Factory.StartNew(() =>
            {
                listener.Listen();
            }, cancellationTokenSource.Token).ContinueWith(task => cancellationTokenSource.Token);

            var result = client.SendAsync("LOG " + guid, true).Result;
            var result2 = client.SendAsync("some invalid message", true).Result;
            cancellationTokenSource.Cancel();

            Assert.AreEqual("LOG IGNORE", result);
            Assert.AreEqual("UNAVAILABLE", result2);
            
        }

        [Test]
        public void SendReceive_MsgNoListener_UNAVAILABLE()
        {
            var result = client.SendAsync("LOG " + guid, true).Result;
            var result2 = client.SendAsync("some invalid message", true).Result;
            Assert.AreEqual("UNAVAILABLE", result);
            Assert.AreEqual("UNAVAILABLE", result2);
        }
    }
}
