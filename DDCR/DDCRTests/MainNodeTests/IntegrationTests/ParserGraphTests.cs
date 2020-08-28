using NUnit.Framework;
using DDCR;
using System;
using Moq;
using DDCR.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;

namespace DDCRTests.IntegrationTests
{
    public class ParserGraphTests
    {
        Graph graph;
        IConfig config;
        ILog log;
        string Terminator = "\u0017";
        string Success = "SUCCESS";
        string Unavailable = "UNAVAILABLE";
        [SetUp]
        public void Setup()
        {
            config = new Config("DDCR.ini");
            log = new Log(config);
            graph = new Graph(log, config);
        }

        [Test]
        public void DecodeMessage1_stringResult()
        {
            //Arrange
            string stringMsg = "result";
            var msg = Encoding.UTF8.GetBytes(stringMsg);

            //Act
            var result = Parser.DecodeMessage(msg);

            //Assert
            Assert.AreEqual("result", result);
        }

        [Test]
        public void DecodeMessage2_stringResult()
        {
            //Arrange
            string stringMsg = "result";
            byte[] msg = new byte[200];
            msg = Encoding.UTF8.GetBytes(stringMsg);

            //Act
            var result = Parser.DecodeMessage(msg);

            //Assert
            Assert.AreEqual("result", result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseInvalidEvent_UNAVAILABLE()
        {
            //Arrange
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;
            string eventToBlock = "e6 include";

            //Act
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseValidEvent_SUCCESS()
        {
            //Arrange
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e2", "l2", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l3", "Node1".Split(), true, false);

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            //Act
            string eventToBlock = "e1 include";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            Assert.IsTrue(graph.Blocked.ContainsKey(id));
            Assert.AreEqual("SUCCESS", result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEvents_SUCCESS()
        {
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e2", "l2", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l3", "Node1".Split(), true, false);
            graph.CreateEvent("e4", "l4", "Node1".Split(), false, true);
            graph.CreateEvent("e5", "l5", "Node1".Split(), true, false);

            //Arrange

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 milestone+";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert

            Assert.IsTrue(graph.Blocked.ContainsKey(id));
            Assert.AreEqual("SUCCESS", result);
            Console.WriteLine(graph.EventsForeign[id]);
            var actionsActual = new List<(EventInternal, Relation, bool)>();

            foreach (var item in graph.EventsForeign[id].actions)
            {
                actionsActual.Add(item);
            }

            var actionsExpected = new List<(EventInternal, Relation, bool)> {
            (graph.Events["e3"], Relation.Milestone, true),
            (graph.Events["e2"], Relation.Exclude, true)};

            Assert.AreEqual(actionsExpected, actionsActual);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEventsOneBlocked_UNAVAILABLE()
        {
            //Arrange
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e2", "l2", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l3", "Node1".Split(), true, false);
            graph.Events["e1"].Block = new Tuple<bool, Guid>(true, Guid.NewGuid());

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 condition-";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert

            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEventsCannotBlockInternal_UNAVAILABLE()
        {
            //Arrange
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e2", "l2", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l3", "Node1".Split(), true, false);
            graph.Events["e1"].Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            graph.Events["e2"].Block = new Tuple<bool, Guid>(true, Guid.NewGuid());

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 condition-";

            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEventsCannotBlockExternal_UNAVAILABLE()
        {
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE");
            //Arrange
            graph.CreateEvent("e1", "l1", "Node1".Split(), false, true);
            graph.CreateEvent("e2", "l2", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l3", "Node1".Split(), true, false);
            graph.CreateEvent("e4", "l4", "Node1".Split(), false, true);
            graph.CreateEvent("e5", "l5", "Node1".Split(), false, false);
            graph.CreateEventExternal("ex", "label","peer");
            config.MainNodes.Add("peer", MockClient.Object);
            graph.AddRelationExternal("e1","ex", Relation.Milestone);

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 condition+";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseTwoValidCanBlockExternal_SUCCESS()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            config.MainNodes.Add("peer", MockClient.Object);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e2", "l2", "Node1".Split(), true, false);
            graph.CreateEventExternal("ex", "label", "peer");
            graph.Events["e1"].MilestonesExternal.Add((graph.EventsExternal["ex"]));
            graph.Events["e2"].MilestonesExternal.Add((graph.EventsExternal["ex"]));

            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            //Act
            string eventToBlock = "e1 milestone-\n"
                                + "e2 response";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            Assert.AreEqual("SUCCESS", result);
            Console.WriteLine(graph.EventsForeign[id].peersPropagate.Count);
            Console.WriteLine(graph.EventsForeign.Count);
            foreach (var item in graph.EventsForeign)
            {
                Console.WriteLine(item.Key);
                Console.WriteLine(item.Value.peersPropagate.Count);
            }
            Assert.IsTrue(graph.EventsForeign[id].peersPropagate.ContainsKey("peer"));
            Assert.AreEqual(1, graph.EventsForeign[id].peersPropagate.Count);
        }

        [Test]
        public void ParseMainNode_EXECUTECaseInvalidExecutionId_UNAVAILABLE()
        {
            //Arrange
            EventForeign foreignEv = new EventForeign();
            graph.EventsForeign.Add(Guid.NewGuid(), foreignEv);
            Parser parser = new Parser(graph, config);
            Guid id = Guid.NewGuid();

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("EXECUTE " + id.ToString() + "\n" + eventToExecute);

            //Assert

            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_EXECUTECaseExternalEventsBlocked_UNAVAILABLE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE");
            config.MainNodes.Add("peer", MockClient.Object);
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("") } }
            };
            graph.EventsForeign.Add(id, foreignEv);


            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("EXECUTE " + id.ToString() + "\n" + eventToExecute);

            //Assert

            Assert.AreEqual(Unavailable + Terminator, result);
            Assert.IsFalse(graph.EventsForeign.ContainsKey(id));
        }

        [Test]
        public void ParseMainNode_EXECUTECaseExternalEventsExecuted_SUCCESS()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            config.MainNodes.Add("peer", MockClient.Object);
            graph.CreateEvent("e1", "l1", "Node1".Split());
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("") } }
                
            };
            graph.EventsForeign.Add(id, foreignEv);
            foreignEv.actions = new List<(EventInternal, Relation, bool)>();
            foreignEv.actions.Append((graph.Events["e1"], Relation.Include, false));

            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("EXECUTE " + id.ToString() + "\n" + eventToExecute);

            //Assert
            Assert.AreEqual("SUCCESS", result);
        }

        [Test]
        public void ParseMainNode_REVERTCaseInvalidExecutionId_UNAVAILABLE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            config.MainNodes.Add("peer", MockClient.Object);
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("SUCCESS") } }
            };
            graph.EventsForeign.Add(Guid.NewGuid(), foreignEv);

            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("REVERT " + id.ToString() + "\n" + eventToExecute);

            //Assert
            Assert.AreEqual(Unavailable + Terminator, result);
        }
        [Test]
        public void ParseMainNode_REVERTCaseValidExecutionId_SUCCESS()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            config.MainNodes.Add("peer", MockClient.Object);
            graph.CreateEvent("e1", "l1", "Node1".Split());
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("SUCCESS") } }
            };
            graph.EventsForeign.Add(id, foreignEv);
            foreignEv.actions = new List<(EventInternal, Relation, bool)>();
            foreignEv.actions.Append((graph.Events["e1"], Relation.Include, true));

            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("REVERT " + id.ToString() + "\n" + eventToExecute);
            Assert.AreEqual(Success + Terminator, result);
            Assert.IsFalse(graph.EventsForeign.ContainsKey(id));
        }

        [Test]
        public void ParseMainNode_UNBLOCKCaseInvalidExecutionId_UNAVAILABLE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            config.MainNodes.Add("peer", MockClient.Object);
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("SUCCESS") } }
            };
            graph.EventsForeign.Add(Guid.NewGuid(), foreignEv);

            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("UNBLOCK " + id.ToString() + "\n" + eventToExecute);

            //Assert
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_UNBLOCKCaseValidExecutionIdNoDeadPeers_SUCCESS()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            config.MainNodes.Add("peer", MockClient.Object);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            Guid id = Guid.NewGuid();
            graph.Blocked.Add(id, new List<EventInternal> { graph.Events["e1"] });
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("SUCCESS") } }
            };
            graph.EventsForeign.Add(id, foreignEv);

            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("UNBLOCK " + id.ToString() + "\n" + eventToExecute);

            //Assert

            Assert.AreEqual(Success + Terminator, result);
            Assert.IsFalse(graph.EventsForeign.ContainsKey(id));
        }
        [Test]
        public void ParseMainNode_UNBLOCKCaseValidExecutionIdOneDeadPeer_SUCCESS()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientTwo = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            MockClientTwo.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE");
            config.MainNodes.Add("peer", MockClient.Object);
            config.MainNodes.Add("peer1", MockClientTwo.Object);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            Guid id = Guid.NewGuid();
            graph.Blocked.Add(id, new List<EventInternal> { graph.Events["e1"] });
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer", Task.FromResult("SUCCESS") }, { "peer1", Task.FromResult("SUCCESS") } }
            }; 
            graph.EventsForeign.Add(id, foreignEv);

            Parser parser = new Parser(graph, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("UNBLOCK " + id.ToString() + "\n" + eventToExecute);

            //Assert
            Assert.AreEqual(Success + Terminator, result);
            Assert.IsFalse(graph.EventsForeign.ContainsKey(id));
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseInternalNotAccepting_FALSE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, true);

            string nodeName = "Node1";
            //Act
            string result = parser.ParseMainNode("ACCEPTING " + nodeName + "\n" + id.ToString());

            //Assert
            Assert.AreEqual("FALSE" + Terminator, result);
        }
        [Test]
        public void ParseMainNodeACCEPTINGCaseDuplicateRequest_ACCEPTING_IGNORE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            graph.GetAcceptingIds.Add(id);

            string nodeName = "Node1";
            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.AreEqual("ACCEPTING IGNORE" + Terminator, result);
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseExternalNotAccepting_FALSE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientTwo = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("FALSE");
            MockClientTwo.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("FALSE");
            config.MainNodes.Add("peer", MockClient.Object);
            config.MainNodes.Add("peer1", MockClientTwo.Object);

            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            string nodeName = "Node1";


            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.IsTrue(graph.GetAcceptingIds.Contains(id));
            Assert.AreEqual("FALSE" + Terminator, result);
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseExternalNotUnique_TRUE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientTwo = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("TRUE");
            MockClientTwo.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("TRUE");
            config.MainNodes.Add("peer", MockClient.Object);
            config.MainNodes.Add("peer1", MockClientTwo.Object);
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            string nodeNames = "m1 m2";

            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeNames);

            //Assert
            Assert.IsTrue(graph.GetAcceptingIds.Contains(id));
            Assert.AreEqual("TRUE" + Terminator, result);
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseExternalAccepting_TRUE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientTwo = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("TRUE");
            config.MainNodes.Add("peer", MockClient.Object);
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);

            string nodeName = "peer";

            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.IsTrue(graph.GetAcceptingIds.Contains(id));
            Assert.AreEqual("TRUE" + Terminator, result);
        }
        [Test]
        public void ParseMainNodeLOGCaseDuplicateRequest_LOG_IGNORE()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("TRUE");
            config.MainNodes.Add("peer", MockClient.Object);
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            graph.GetLogIds.Add(id);

            string nodeName = "peer";
            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.AreEqual("LOG IGNORE" +Terminator, result);
        }

        [Test]
        public void ParseMainNodeLOGCaseExternal_InternalLog()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Log newLog = new Log(config);
            newLog.Add(DateTime.Now, "e1");
            var msg = newLog.ToString();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(msg);
            config.MainNodes.Add("peer", MockClient.Object);

            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            string nodeName = "peer";

            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.IsTrue(graph.GetLogIds.Contains(id));
            Assert.AreNotEqual("UNAVAILABLE", result);

        }

        [Test]
        public void ParseMainNodeLOGCaseNoExternalMainNodes_InternalLog()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);
            string nodeName = "m1";

            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.IsTrue(graph.GetLogIds.Contains(id));
            Assert.AreNotEqual("UNAVAILABLE", result);
        }

        [Test]
        public void ParseMainNodeLOGCaseExternalDuplicate_InternalLog()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientTwo = new Mock<IClient>();
            Log newLog = new Log(config);
            newLog.Add(DateTime.Now, "e89");
            var msg = newLog.ToString();
            Log secondNewLog = new Log(config);
            secondNewLog.Add(DateTime.Now, "e87");
            var secondMsg = secondNewLog.ToString();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(msg);
            MockClientTwo.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(secondMsg);
            config.MainNodes.Add("peer", MockClient.Object);
            config.MainNodes.Add("peer1", MockClientTwo.Object);
            Guid id = Guid.NewGuid();
            Parser parser = new Parser(graph, config);

            string nodeName = "peer peer1";

            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            Assert.IsTrue(graph.GetLogIds.Contains(id));
            Assert.AreNotEqual("UNAVAILABLE", result);
        }
        [Test]
        public void ParseNodePERMISSIONSCase_NodeEvents()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            //Act
            var result = parser.ParseNode("PERMISSIONS", ip);
            //Assert
            Assert.AreEqual("NONE", result);
        }
        [Test]
        public void ParseNodeEXECUTECase_ExecuteResult()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e2", "l1", "Node1".Split(), true, true);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l1", "Node1".Split(), true, false);

            //Act
            var result = parser.ParseNode("EXECUTE e2", ip);

            //Assert
            Assert.AreEqual(true.ToString(), result);
        }

        [Test]
        public void ParseNodeEXECUTECase_InvalidEvent()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e2", "l1", "Node1".Split(), true, true);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l1", "Node1".Split(), true, false);

            //Act
            var result = parser.ParseNode("EXECUTE ex", ip);

            //Assert
            Assert.AreEqual("SPECIFIED EVENT INVALID", result);
        }
        [Test]
        public void ParseNodeEXECUTECase_InvalidPermission()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);

            //graph.CreateNode("Node2", "127.0.0.2");
            config.Nodes.Add("Node2", IPAddress.Parse("127.0.0.2"));
            graph.NodeEvents.Add(IPAddress.Parse("127.0.0.2"), new HashSet<EventInternal>());
            graph.CreateEvent("e2", "l1", "Node2".Split(), true, true);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l1", "Node1".Split(), true, false);

            //Act
            var result = parser.ParseNode("EXECUTE e2", ip);

            //Assert
            Assert.AreEqual("INVALID PERMISSION", result);
        }

        [Test]
        public void ParseNodeACCEPTINGCaseNoneAccepting_False()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e2", "l1", "Node1".Split(), true, true);

            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("False", result);
        }
        [Test]
        public void ParseNodeACCEPTINGCaseInternalAccepting_False()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("FALSE");
            config.MainNodes.Add("peer", MockClient.Object);
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);

            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Assert.AreEqual("False", result);
        }
        [Test]
        public void ParseNodeACCEPTINGCaseExternalAccepting_False()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("TRUE");
            config.MainNodes.Add("peer", MockClient.Object);
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, true);

            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Assert.AreEqual("False", result);
        }

        [Test]
        public void ParseNodeACCEPTINGCaseBothAccepting_True()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IClient> MockClient = new Mock<IClient>();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("TRUE");
            config.MainNodes.Add("peer", MockClient.Object);
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);

            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Assert.AreEqual("True", result);
        }
        //TODO: This case is extremely simple, but there is a problem in that Moq is not able to mock GetGlobal because It.IsAny<Guid> does not work.
        [Test]
        public void ParseNodeLOGCase_GlobalLog()
        {
            //Arrange
            Mock<IClient> MockClient = new Mock<IClient>();
            Log newLog = new Log(config);
            newLog.Add(DateTime.Now, "e89");
            var msg = newLog.ToString();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(msg);
            config.MainNodes.Add("peer", MockClient.Object);
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);

            //Act
            var result = parser.ParseNode("LOG", ip);

            //Assert
            Assert.AreEqual(msg, result);
        }

        [Test]
        public void ParseNodeMARKINGCase_InvalidEvent()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e2", "l1", "Node1".Split(), true, true);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l1", "Node1".Split(), true, false);


            //Act
            var result = parser.ParseNode("MARKING ex", ip);

            //Assert
            Assert.AreEqual("SPECIFIED EVENT INVALID", result);
        }
        [Test]
        public void ParseNodeMARKINGCase_resultingMarking()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            graph.CreateEvent("e2", "l1", "Node1".Split(), true, true);
            graph.CreateEvent("e1", "l1", "Node1".Split(), true, false);
            graph.CreateEvent("e3", "l1", "Node1".Split(), true, false);
            //Act
            var result = parser.ParseNode("MARKING e2", ip);
            var expected = graph.GetMarking(graph.Events["e2"]);

            //Assert

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ParseNodeEmptyCommand_UNKNOWN_COMMAND()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);

            //Act
            var result = parser.ParseNode(string.Empty, ip);

            //Assert
            Assert.AreEqual("UNKNOWN COMMAND", result);
        }

        [Test]
        public void ParseNodeInvalidCommand_UNKNOWN_COMMAND()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);

            //Act
            var result = parser.ParseNode("12345", ip);

            //Assert
            Assert.AreEqual("UNKNOWN COMMAND", result);
        }

        [Test]
        public void ParseNodeInvalidCommand2_UNKNOWN_COMMAND()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);

            //Act
            var result = parser.ParseNode("MARKING421 5212 awrysaeryd", ip);

            //Assert
            Assert.AreEqual("UNKNOWN COMMAND", result);
        }

        // Fuzzing to test ParseMainNode with many many different input strings 
        [Test]
        public void ParseMainNodeRandomInvalidCommands_UNAVAILABLE()
        {
            //Arrange
            Parser parser = new Parser(graph, config);
            int rndSeed = 42;
            Random random = new Random(rndSeed);
            List<string> rndArgs = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                byte[] rndMessage = new byte[random.Next(0, 50)];
                random.NextBytes(rndMessage);
                string strMsg = Encoding.UTF8.GetString(rndMessage);
                rndArgs.Add(strMsg);
            }

            //Act
            List<string> results = new List<string>();
            foreach (string arg in rndArgs)
            {
                results.Add(parser.ParseMainNode(arg));
            }

            //Assert
            foreach (string result in results)
            {
                Assert.AreEqual(Unavailable + Terminator, result);
            }
        }
        // Fuzzing to test ParseNode with many many different input strings 
        [Test]
        public void ParseNodeRandomInvalidCommands_UNKNOWN_COMMAND()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Parser parser = new Parser(graph, config);
            int rndSeed = 42;
            Random random = new Random(rndSeed);
            List<string> rndArgs = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                byte[] rndMessage = new byte[random.Next(0, 50)];
                random.NextBytes(rndMessage);
                string strMsg = Encoding.UTF8.GetString(rndMessage);
                rndArgs.Add(strMsg);
            }

            //Act
            List<string> results = new List<string>();
            foreach (string arg in rndArgs)
            {
                results.Add(parser.ParseNode(arg, ip));
            }

            //Assert
            foreach (string result in results)
            {
                Assert.AreEqual("UNKNOWN COMMAND", result);
            }
        }
    }
}
