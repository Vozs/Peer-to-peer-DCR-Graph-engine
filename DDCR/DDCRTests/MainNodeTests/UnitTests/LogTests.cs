using NUnit.Framework;
using DDCR;
using System;
using DDCR.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace DDCRTests.UnitTests
{
    public class LogTests
    {
        Log log;
        IConfig config;

        [SetUp]
        public void Setup()
        {
            var mockConfig = new Mock<IConfig>();
            mockConfig.Setup(x => x.Name).Returns("name");
            mockConfig.Setup(x => x.ClientTimeoutMs).Returns(10000);
            mockConfig.Setup(x => x.ListenPort).Returns(5006);
            mockConfig.Setup(x => x.Culture).Returns(CultureInfo.CreateSpecificCulture("en-GB"));
            config = mockConfig.Object;
            log = new Log(config);

        }

        [Test]
        public void AddTest()
        {
            log.Add(new DateTime(2020, 6, 8, 12, 13, 0), "e1");
            log.Add(new DateTime(2020, 6, 8, 12, 11, 1), "e2");
            log.Add(new DateTime(2020, 6, 8, 12, 12, 2), "e3");

            Assert.AreEqual(log[0].Item2, "e1");
            Assert.AreEqual(log[1].Item2, "e2");
            Assert.AreEqual(log[2].Item2, "e3");
        }

        [Test]
        public void SortTest()
        {
            log.Add(new DateTime(2020, 6, 8, 12, 13, 0), "e1");
            log.Add(new DateTime(2020, 6, 8, 12, 11, 1), "e2");
            log.Add(new DateTime(2020, 6, 8, 12, 12, 2), "e3");

            log.SortLog();
            Assert.AreEqual(log[0].Item2, "e2");
            Assert.AreEqual(log[1].Item2, "e3");
            Assert.AreEqual(log[2].Item2, "e1");
        }

        [Test]
        public void SortTestByDateTime()
        {
            log.Add(new DateTime(2020, 6, 8, 12, 13, 0), "e1");
            log.Add(new DateTime(2020, 6, 8, 12, 11, 1), "e2");
            log.Add(new DateTime(2020, 6, 8, 12, 12, 2), "e3");

            log.SortLog();
            Assert.LessOrEqual(log[0].Item1, log[1].Item1);
            Assert.LessOrEqual(log[1].Item1, log[2].Item1);
        }

        [Test]
        public void SortEmptyLogTest()
        {
            log.SortLog();
            Assert.IsEmpty(log);
        }

        [Test]
        public void ToStringTest()
        {
            log.Add(new DateTime(2020, 6, 8, 12, 13, 0), "e1");
            log.Add(new DateTime(2020, 6, 8, 12, 11, 1), "e2");
            log.Add(new DateTime(2020, 6, 8, 12, 12, 2), "e3");

            var expected = "08/06/2020 12:13:00 e1\n" +
                "08/06/2020 12:11:01 e2\n" +
                "08/06/2020 12:12:02 e3";

            Assert.AreEqual(expected, log.ToString());
        }
        [Test]
        public void ToStringTestEmptyLog()
        {
            Log log = new Log(config);
            string expected = "LOG EMPTY";

            Assert.AreEqual(expected, log.ToString());
        }

        [Test]
        public void GetGlobalTest()
        {
            //Arrange
            Dictionary<string, IClient> MockDict = new Dictionary<string, IClient>();
            Guid id = Guid.NewGuid();
            string peer = "peer";
            Log expected = new Log(config);

            string msg = string.Format("LOG {0}\n {1}", id.ToString(), peer);
            string returns = "08/06/2020 12:10:00 e4";

            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(msg, true)).ReturnsAsync(returns);

            MockDict.Add(peer, mockClient.Object);

            var mockLog = new Mock<Log>(config);
            log.Add(new DateTime(2020, 6, 8, 12, 10, 0), "e4");

            //Act
            var actual = mockLog.Object.GetGlobal(MockDict, string.Empty, id);


            //Assert
            Assert.AreEqual(log.Count, actual.Count);
            for (int i = 0; i < log.Count; i++)
            {
                Assert.AreEqual(log[i].Item2, actual[i].Item2);
            };
        }

        [Test]
        public void GetGlobal_FourPeers_CombinedLog() //MethodName_Params_ExpectedResult
        {
            //Arrange
            string[] sendResult = new string[4];
            sendResult[0] = "08/05/2020 12:13:00 e1";
            sendResult[1] = "04/03/2020 11:11:11 e4";
            sendResult[2] = "09/06/2020 12:14:40 e2";
            sendResult[3] = "09/06/2020 12:14:33 e3";

            Log correctLog = new Log(config);
            correctLog.Add(new DateTime(2020, 5, 8, 12, 13, 00), "e1");
            correctLog.Add(new DateTime(2020, 3, 4, 11, 11, 11), "e4");
            correctLog.Add(new DateTime(2020, 6, 9, 12, 14, 40), "e2");
            correctLog.Add(new DateTime(2020, 6, 9, 12, 14, 33), "e3");

            Guid id = Guid.NewGuid();
            Dictionary<string, IClient> mockClients = new Dictionary<string, IClient>();
            for (int i = 0; i < 4; i++)
            {
                var mockClient = new Mock<IClient>();
                mockClient.Setup(x => x.SendAsync(string.Format("LOG {0}\n{1}", id.ToString(), "none 0 1 2 3"), true))
                    .Returns(Task.FromResult(sendResult[i]));
                mockClients[i.ToString()] = mockClient.Object;
            }
            Log log = new Log(config);

            //Act
            var globalLog = log.GetGlobal(mockClients, "none", id);

            //Assert
            Assert.AreEqual(globalLog, correctLog);
        }

        [Test]
        public void GetGlobal_FourPeersOneEmpty_IgnoreEmpty() 
        {
            //Arrange
            string[] sendResult = new string[4];
            sendResult[0] = "08/05/2020 12:13:00 e1";
            sendResult[1] = "LOG EMPTY";
            sendResult[2] = "09/06/2020 12:14:40 e2";
            sendResult[3] = "09/06/2020 12:14:33 e3";

            Log correctLog = new Log(config);
            correctLog.Add(new DateTime(2020, 5, 8, 12, 13, 00), "e1");
            correctLog.Add(new DateTime(2020, 6, 9, 12, 14, 40), "e2");
            correctLog.Add(new DateTime(2020, 6, 9, 12, 14, 33), "e3");

            Guid id = Guid.NewGuid();
            Dictionary<string, IClient> mockClients = new Dictionary<string, IClient>();
            for (int i = 0; i < 4; i++)
            {
                var mockClient = new Mock<IClient>();
                mockClient.Setup(x => x.SendAsync(string.Format("LOG {0}\n{1}", id.ToString(), "none 0 1 2 3"), true))
                    .Returns(Task.FromResult(sendResult[i]));
                mockClients[i.ToString()] = mockClient.Object;
            }
            Log log = new Log(config);

            //Act
            var globalLog = log.GetGlobal(mockClients, "none", id);

            //Assert
            Assert.AreEqual(globalLog, correctLog);
        }

        [Test]
        public void GetGlobal_TwoEmptyTwoIgnore_EmptyLog() 
        {
            //Arrange
            string[] sendResult = new string[4];
            sendResult[0] = "LOG EMPTY";
            sendResult[1] = "LOG EMPTY";
            sendResult[2] = "LOG IGNORE";
            sendResult[3] = "LOG IGNORE";

            Log correctLog = new Log(config);

            Guid id = Guid.NewGuid();
            Dictionary<string, IClient> mockClients = new Dictionary<string, IClient>();
            for (int i = 0; i < 4; i++)
            {
                var mockClient = new Mock<IClient>();
                mockClient.Setup(x => x.SendAsync(string.Format("LOG {0}\n{1}", id.ToString(), "none 0 1 2 3"), true))
                    .Returns(Task.FromResult(sendResult[i]));
                mockClients[i.ToString()] = mockClient.Object;
            }
            Log log = new Log(config);

            //Act
            var globalLog = log.GetGlobal(mockClients, "none", id);

            //Assert
            Assert.AreEqual(globalLog, correctLog);
        }

        [Test]
        public void GetGlobal_OnePeerUnavailable_nullLog()
        {
            //Arrange
            string[] sendResult = new string[4];
            sendResult[0] = "08/05/2020 12:13:00 e1";
            sendResult[1] = "LOG EMPTY";
            sendResult[2] = "UNAVAILABLE";
            sendResult[3] = "09/06/2020 12:14:33 e3";

            Guid id = Guid.NewGuid();
            Dictionary<string, IClient> mockClients = new Dictionary<string, IClient>();
            for (int i = 0; i < 4; i++)
            {
                var mockClient = new Mock<IClient>();
                mockClient.Setup(x => x.SendAsync(string.Format("LOG {0}\n{1}", id.ToString(), "none 0 1 2 3"), true))
                    .Returns(Task.FromResult(sendResult[i]));
                mockClients[i.ToString()] = mockClient.Object;
            }
            Log log = new Log(config);

            //Act
            var globalLog = log.GetGlobal(mockClients, "none", id);

            //Assert
            Assert.AreEqual(globalLog, null);
        }

        [Test]
        public void GetGlobal_NoPeers_LocalLog()
        {
            //Arrange

            Guid id = Guid.NewGuid();
            Dictionary<string, IClient> mockClients = new Dictionary<string, IClient>();
            Log log = new Log(config);
            Log correctLog = new Log(config);

            string execEventName = "ex";
            DateTime execEventDate = new DateTime(2020, 6, 9, 0, 0, 1);
            log.Add(execEventDate, execEventName);
            correctLog.Add(execEventDate, execEventName);
            //Act
            var globalLog = log.GetGlobal(mockClients, "none", id);

            //Assert
            Assert.AreEqual(globalLog, correctLog);
        }
    }
}