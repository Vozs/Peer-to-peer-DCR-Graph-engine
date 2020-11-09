using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DDCRTests.UnitTests
{
    public class ParserTests
    {
        char Terminator = '\u0017';
        string Success = "SUCCESS";
        string Unavailable = "UNAVAILABLE";
        //Some tests use the Log class, because the function of the parser method is to reconstruct and return a Log object.
        Mock<IConfig> MockConfig;
        IConfig config;

        [SetUp]
        public void Setup()
        {
            MockConfig = new Mock<IConfig>();
            MockConfig.SetupGet(x => x.Terminator).Returns(this.Terminator);
            config = MockConfig.Object;
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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));

            Parser parser = new Parser(mockGraph.Object, config);
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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            var mockBlocked = new Dictionary<Guid, List<EventInternal>>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign);
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e2", new EventInternal("e2", "l2", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            var correctCheckBlockParam = new string[1] { "e1" };

            mockGraph.Setup(x => x.CheckBlocked(correctCheckBlockParam)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.TryBlockInternal(It.IsAny<EventForeign>(), id)).Returns(true);

            mockGraph.Setup(x => x.GetPeerMessages(It.IsAny<EventForeign>())).Returns(new Dictionary<string, string>()).Verifiable();


            var mockMarkingChange = new List<(EventInternal, Relation, bool)>();

            mockMarkingChange.Add((mockEvents["e1"], Relation.Include, true));
            mockGraph.Setup(x => x.GetNewMarkingsReversible(It.IsAny<EventForeign>())).Returns(mockMarkingChange).Verifiable();

            //Act
            string eventToBlock = "e1 include";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            mockGraph.Verify(x => x.CheckBlocked(correctCheckBlockParam));
            Assert.IsTrue(mockBlocked.ContainsKey(id));
            Assert.AreEqual("SUCCESS", result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEvents_SUCCESS()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            var mockBlocked = new Dictionary<Guid, List<EventInternal>>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign);
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e2", new EventInternal("e2", "l2", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            mockEvents.Add("e4", new EventInternal("e4", "l4", false, true));
            mockEvents.Add("e5", new EventInternal("e5", "l5", false, false));
            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            var correctCheckBlockParam = new string[3] { "e1", "e2", "e3" };

            mockGraph.Setup(x => x.CheckBlocked(correctCheckBlockParam)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.TryBlockInternal(It.IsAny<EventForeign>(), id)).Returns(true);
            mockGraph.Setup(x => x.GetPeerMessages(It.IsAny<EventForeign>())).Returns(new Dictionary<string, string>()).Verifiable();

            var mockMarkingChange = new List<(EventInternal, Relation, bool)>();

            mockMarkingChange.Add((mockEvents["e1"], Relation.Include, true));
            mockMarkingChange.Add((mockEvents["e2"], Relation.Exclude, true));

            mockGraph.Setup(x => x.GetNewMarkingsReversible(It.IsAny<EventForeign>())).Returns(mockMarkingChange).Verifiable();


            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 milestone+";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            mockGraph.Verify(x => x.CheckBlocked(correctCheckBlockParam));
            Mock.VerifyAll();
            Assert.IsTrue(mockBlocked.ContainsKey(id));
            Assert.AreEqual("SUCCESS", result);

            var actionsActual = new List<(EventInternal, Relation, bool)>();
            foreach (var item in mockEventsForeign[id].actions)
            {
                actionsActual.Add(item);

            }

            var actionsExpected = new List<(EventInternal, Relation, bool)> {
                (mockEvents["e3"], Relation.Milestone, true),
                (mockEvents["e1"], Relation.Include, true),
                (mockEvents["e2"], Relation.Exclude, true)};

            Assert.AreEqual(actionsExpected, actionsActual);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEventsOneBlocked_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            var mockBlocked = new Dictionary<Guid, List<EventInternal>>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign);
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e2", new EventInternal("e2", "l2", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            var correctCheckBlockParam = new string[3] { "e1", "e2", "e3" };

            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();
            mockGraph.Setup(x => x.CheckBlocked(correctCheckBlockParam)).Returns(true).Verifiable();

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 condition-";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            mockGraph.Verify(x => x.CheckBlocked(correctCheckBlockParam));
            mockGraph.Verify(x => x.UnblockEvents(id));
            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEventsCannotInternal_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            var mockBlocked = new Dictionary<Guid, List<EventInternal>>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign);
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e2", new EventInternal("e2", "l2", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            var correctCheckBlockParam = new string[3] { "e1", "e2", "e3" };

            mockGraph.Setup(x => x.CheckBlocked(correctCheckBlockParam)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.TryBlockInternal(It.IsAny<EventForeign>(), id)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();
            //Empty because no external relations

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 condition-";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            mockGraph.Verify(x => x.CheckBlocked(correctCheckBlockParam));
            mockGraph.Verify(x => x.UnblockEvents(id));
            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_BLOCKCaseThreeValidEventsCannotBlockExternal_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            var mockBlocked = new Dictionary<Guid, List<EventInternal>>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign);
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e2", new EventInternal("e2", "l2", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            mockEvents.Add("e4", new EventInternal("e4", "l4", false, true));
            mockEvents.Add("e5", new EventInternal("e5", "l5", false, false));
            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            var correctCheckBlockParam = new string[3] { "e1", "e2", "e3" };

            mockGraph.Setup(x => x.CheckBlocked(correctCheckBlockParam)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.TryBlockInternal(It.IsAny<EventForeign>(), id)).Returns(true).Verifiable();
            var peerMessagesReturnPlaceholder = new Dictionary<string, string> { { "x", "x" } }; //Simply needs to have a .Count != 0 for this test.
            mockGraph.Setup(x => x.GetPeerMessages(It.IsAny<EventForeign>())).Returns(peerMessagesReturnPlaceholder).Verifiable();
            mockGraph.Setup(x => x.TryBlockExternal(peerMessagesReturnPlaceholder, id, date.ToString())).Returns(false).Verifiable();
            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();

            //Act
            string eventToBlock = "e1 include\n"
                                + "e2 exclude\n"
                                + "e3 condition+";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            mockGraph.Verify(x => x.CheckBlocked(correctCheckBlockParam));
            mockGraph.Verify(x => x.UnblockEvents(id));
            mockGraph.Verify(x => x.TryBlockExternal(peerMessagesReturnPlaceholder, id, date.ToString()));
            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
        }
        [Test]
        public void ParseMainNode_BLOCKCaseTwoValidCanBlockExternal_SUCCESS()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEvents = new Dictionary<string, EventInternal>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            var mockBlocked = new Dictionary<Guid, List<EventInternal>>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign);
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e2", new EventInternal("e2", "l2", true, false));

            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();
            DateTime date = DateTime.Now;

            var correctCheckBlockParam = new string[2] { "e1", "e2" };

            mockGraph.Setup(x => x.CheckBlocked(correctCheckBlockParam)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.TryBlockInternal(It.IsAny<EventForeign>(), id)).Returns(true).Verifiable();
            var peerMessagesReturnPlaceholder = new Dictionary<string, string> { { "peer1", "msg1" }, { "peer2", "msg2" } };
            mockGraph.Setup(x => x.GetPeerMessages(It.IsAny<EventForeign>())).Returns(peerMessagesReturnPlaceholder).Verifiable();
            mockGraph.Setup(x => x.TryBlockExternal(peerMessagesReturnPlaceholder, id, date.ToString())).Returns(true).Verifiable();

            var mockMarkingChange = new List<(EventInternal, Relation, bool)> {
                { (mockEvents["e2"], Relation.Response, true) } };
            mockGraph.Setup(x => x.GetNewMarkingsReversible(It.IsAny<EventForeign>())).Returns(mockMarkingChange).Verifiable();

            //Act
            string eventToBlock = "e1 milestone-\n"
                                + "e2 response";
            string result = parser.ParseMainNode("BLOCK " + id.ToString() + " " + date + "\n" + eventToBlock);

            //Assert
            mockGraph.Verify(x => x.CheckBlocked(correctCheckBlockParam));
            mockGraph.Verify(x => x.TryBlockExternal(peerMessagesReturnPlaceholder, id, date.ToString()));
            Mock.VerifyAll();
            Assert.AreEqual("SUCCESS", result);

            Assert.IsTrue(mockEventsForeign[id].peersPropagate.ContainsKey("peer1"));
            Assert.IsTrue(mockEventsForeign[id].peersPropagate.ContainsKey("peer2"));
            Assert.AreEqual(2, mockEventsForeign[id].peersPropagate.Count);
        }

        [Test]
        public void ParseMainNode_EXECUTECaseInvalidExecutionId_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            var mockEventsForeign = new Dictionary<Guid, EventForeign>();
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            Parser parser = new Parser(mockGraph.Object, config);
            Guid id = Guid.NewGuid();

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("EXECUTE " + id.ToString() + "\n" + eventToExecute);

            //Assert

            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_EXECUTECaseExternalEventsBlocked_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("UNAVAILABLE") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { id, foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            mockGraph.Setup(x => x.TryExecuteExternal(foreignEv.peersPropagate, id)).Returns(false).Verifiable();
            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();

            Parser parser = new Parser(mockGraph.Object, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("EXECUTE " + id.ToString() + "\n" + eventToExecute);

            //Assert
            mockGraph.Verify(x => x.TryExecuteExternal(foreignEv.peersPropagate, id));
            mockGraph.Verify(x => x.UnblockEvents(id));
            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
            Assert.IsFalse(mockEventsForeign.ContainsKey(id));
        }

        [Test]
        public void ParseMainNode_EXECUTECaseExternalEventsExecuted_SUCCESS()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { id, foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            mockGraph.Setup(x => x.TryExecuteExternal(mockEventsForeign[id].peersPropagate, id)).Returns(true).Verifiable();
            mockGraph.Setup(x => x.ChangeMarkings(foreignEv, false)).Verifiable();

            Parser parser = new Parser(mockGraph.Object, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("EXECUTE " + id.ToString() + "\n" + eventToExecute);

            //Assert
            mockGraph.Verify(x => x.TryExecuteExternal(mockEventsForeign[id].peersPropagate, id));
            mockGraph.Verify(x => x.ChangeMarkings(foreignEv, false));
            Mock.VerifyAll();
            Assert.AreEqual("SUCCESS", result);
        }

        [Test]
        public void ParseMainNode_REVERTCaseInvalidExecutionId_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { Guid.NewGuid(), foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            Parser parser = new Parser(mockGraph.Object, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("REVERT " + id.ToString() + "\n" + eventToExecute);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
        }
        [Test]
        public void ParseMainNode_REVERTCaseValidExecutionId_SUCCESS()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { id, foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            mockGraph.Setup(x => x.MessagePeers("REVERT", id, foreignEv.peersPropagate, true)).Returns(new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } });
            mockGraph.Setup(x => x.ChangeMarkings(foreignEv, true)).Verifiable();
            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();

            Parser parser = new Parser(mockGraph.Object, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("REVERT " + id.ToString() + "\n" + eventToExecute);

            //Assert
            mockGraph.Verify(x => x.MessagePeers("REVERT", id, foreignEv.peersPropagate, true));
            mockGraph.Verify(x => x.ChangeMarkings(foreignEv, true));
            mockGraph.Verify(x => x.UnblockEvents(id));
            Mock.VerifyAll();
            Assert.AreEqual(Success + Terminator, result);
            Assert.IsFalse(mockEventsForeign.ContainsKey(id));
        }

        [Test]
        public void ParseMainNode_UNBLOCKCaseInvalidExecutionId_UNAVAILABLE()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { Guid.NewGuid(), foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            Parser parser = new Parser(mockGraph.Object, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("UNBLOCK " + id.ToString() + "\n" + eventToExecute);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual(Unavailable + Terminator, result);
        }

        [Test]
        public void ParseMainNode_UNBLOCKCaseValidExecutionIdNoDeadPeers_SUCCESS()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { id, foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            mockGraph.Setup(x => x.MessagePeers("UNBLOCK", id, foreignEv.peersPropagate, true)).Returns(new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } });
            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();
            var mockEvents = new Dictionary<string, EventInternal>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            var mockBlocked = new Dictionary<Guid, List<EventInternal>> {
                {id, new List<EventInternal> { mockEvents["e1"] } } };
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked).Verifiable();

            Parser parser = new Parser(mockGraph.Object, config);

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("UNBLOCK " + id.ToString() + "\n" + eventToExecute);

            //Assert
            mockGraph.Verify(x => x.MessagePeers("UNBLOCK", id, foreignEv.peersPropagate, true));
            mockGraph.Verify(x => x.UnblockEvents(id));
            Mock.VerifyAll();
            Assert.AreEqual(Success + Terminator, result);
            Assert.IsFalse(mockEventsForeign.ContainsKey(id));
        }
        public void ParseMainNode_UNBLOCKCaseValidExecutionIdOneDeadPeer_SUCCESS()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Guid id = Guid.NewGuid();
            var foreignEv = new EventForeign
            {
                peersPropagate = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") }, { "peer2", Task.FromResult("SUCCESS") } }
            };
            var mockEventsForeign = new Dictionary<Guid, EventForeign> { { id, foreignEv } };
            mockGraph.Setup(x => x.EventsForeign).Returns(mockEventsForeign).Verifiable();
            mockGraph.Setup(x => x.MessagePeers("UNBLOCK", id, foreignEv.peersPropagate, true)).Returns(new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } });
            mockGraph.Setup(x => x.UnblockEvents(id)).Verifiable();
            var mockEvents = new Dictionary<string, EventInternal>();
            mockGraph.Setup(x => x.Events).Returns(mockEvents);
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            var mockBlocked = new Dictionary<Guid, List<EventInternal>> {
                {id, new List<EventInternal> { mockEvents["e1"] } } };
            mockGraph.Setup(x => x.Blocked).Returns(mockBlocked).Verifiable();

            Parser parser = new Parser(mockGraph.Object, config);

            mockGraph.Setup(x => x.TryUnblockPeerAsync("peer2", id.ToString())).Verifiable();

            //Act
            string eventToExecute = "e1 include";
            string result = parser.ParseMainNode("UNBLOCK " + id.ToString() + "\n" + eventToExecute);

            //Assert
            mockGraph.Verify(x => x.MessagePeers("UNBLOCK", id, foreignEv.peersPropagate, true));
            mockGraph.Verify(x => x.UnblockEvents(id));
            Mock.VerifyAll();
            Assert.AreEqual("SUCCESS", result);
            Assert.IsFalse(mockEventsForeign.ContainsKey(id));
        }


        [Test]
        public void ParseMainNodeACCEPTINGCaseInternalNotAccepting_FALSE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(false).Verifiable();

            string nodeName = "m1";
            //Act
            string result = parser.ParseMainNode("ACCEPTING " + nodeName + "\n" + id.ToString());

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("FALSE" +Terminator, result);
        }
        [Test]
        public void ParseMainNodeACCEPTINGCaseDuplicateRequest_ACCEPTING_IGNORE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(true).Verifiable();
            List<Guid> mockAcceptingIds = new List<Guid> { id };
            mockGraph.Setup(x => x.GetAcceptingIds).Returns(mockAcceptingIds).Verifiable();

            string nodeName = "m1";
            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeName);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("ACCEPTING IGNORE" + Terminator, result);
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseExternalNotAccepting_FALSE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(true).Verifiable();
            var mockMainNodes = new Dictionary<string, IClient> { { "m1", null }, { "m2", null } };
            string nodeName = "m1";
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), nodeName, id)).Returns(false).Verifiable();

            List<Guid> mockAcceptingIds = new List<Guid>();
            mockGraph.Setup(x => x.GetAcceptingIds).Returns(mockAcceptingIds).Verifiable();

            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeName);

            //Assert
            mockGraph.Verify(x => x.GetAcceptingExternal(new Dictionary<string, IClient> { { "m2", null } }, nodeName, id));
            Mock.VerifyAll();
            Assert.IsTrue(mockAcceptingIds.Contains(id));
            Assert.AreEqual("FALSE" + Terminator, result);
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseExternalNotUnique_TRUE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(true).Verifiable();
            var mockMainNodes = new Dictionary<string, IClient> { { "m1", null }, { "m2", null } };
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            string nodeNames = "m1 m2";
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), nodeNames, id)).Returns(true).Verifiable();

            List<Guid> mockAcceptingIds = new List<Guid>();
            mockGraph.Setup(x => x.GetAcceptingIds).Returns(mockAcceptingIds).Verifiable();

            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeNames);

            //Assert
            mockGraph.Verify(x => x.GetAcceptingExternal(new Dictionary<string, IClient>(), nodeNames, id));
            Mock.VerifyAll();
            Assert.IsTrue(mockAcceptingIds.Contains(id));
            Assert.AreEqual("TRUE" +Terminator, result);
        }

        [Test]
        public void ParseMainNodeACCEPTINGCaseExternalAccepting_TRUE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(true).Verifiable();
            var mockMainNodes = new Dictionary<string, IClient> { { "m1", null }, { "m2", null } };
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            string nodeName = "m1";
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), nodeName, id)).Returns(true).Verifiable();

            List<Guid> mockAcceptingIds = new List<Guid>();
            mockGraph.Setup(x => x.GetAcceptingIds).Returns(mockAcceptingIds).Verifiable();

            //Act
            string result = parser.ParseMainNode("ACCEPTING " + id.ToString() + "\n" + nodeName);

            //Assert
            mockGraph.Verify(x => x.GetAcceptingExternal(new Dictionary<string, IClient> { { "m2", null } }, nodeName, id));
            Mock.VerifyAll();
            Assert.IsTrue(mockAcceptingIds.Contains(id));
            Assert.AreEqual("TRUE" + Terminator, result);
        }
        [Test]
        public void ParseMainNodeLOGCaseDuplicateRequest_LOG_IGNORE()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            List<Guid> mockLogIds = new List<Guid> { id };
            mockGraph.Setup(x => x.GetLogIds).Returns(mockLogIds).Verifiable();

            string nodeName = "m1";
            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("LOG IGNORE" + Terminator, result);
        }

        [Test]
        public void ParseMainNodeLOGCaseExternal_InternalLog()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            List<Guid> mockLogIds = new List<Guid>();
            mockGraph.Setup(x => x.GetLogIds).Returns(mockLogIds).Verifiable();
            var mockMainNodes = new Dictionary<string, IClient> { { "m1", null }, { "m2", null } };
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            string nodeName = "m1";


            mockGraph.Setup(x => x.Log.GetGlobal(It.IsAny<Dictionary<string, IClient>>(), nodeName, id))
                .Returns(new Mock<ILog>().Object)
                .Verifiable();

            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            mockGraph.Verify(x => x.Log.GetGlobal(new Dictionary<string, IClient> { { "m2", null } }, nodeName, id));
            Mock.VerifyAll();
            Assert.IsTrue(mockLogIds.Contains(id));
            Assert.AreNotEqual("UNAVAILABLE", result);

        }

        [Test]
        public void ParseMainNodeLOGCaseNoExternalMainNodes_InternalLog()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            List<Guid> mockLogIds = new List<Guid>();
            mockGraph.Setup(x => x.GetLogIds).Returns(mockLogIds).Verifiable();
            var mockMainNodes = new Dictionary<string, IClient>();
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            string nodeName = "m1";

            mockGraph.Setup(x => x.Log.GetGlobal(It.IsAny<Dictionary<string, IClient>>(), nodeName, id))
                .Returns(new Log(config))
                .Verifiable();

            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            mockGraph.Verify(x => x.Log.GetGlobal(new Dictionary<string, IClient>(), nodeName, id));
            Mock.VerifyAll();
            Assert.IsTrue(mockLogIds.Contains(id));
            Assert.AreNotEqual("UNAVAILABLE", result);
        }

        [Test]
        public void ParseMainNodeLOGCaseExternalDuplicate_InternalLog()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            List<Guid> mockLogIds = new List<Guid>();
            mockGraph.Setup(x => x.GetLogIds).Returns(mockLogIds).Verifiable();
            var mockMainNodes = new Dictionary<string, IClient> { { "m1", null }, { "m2", null } };
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            string nodeName = "m1 m2";

            mockGraph.Setup(x => x.Log.GetGlobal(It.IsAny<Dictionary<string, IClient>>(), nodeName, id))
                .Returns(new Log(config))
                .Verifiable();

            //Act
            string result = parser.ParseMainNode("LOG " + id.ToString() + "\n" + nodeName);

            //Assert
            mockGraph.Verify(x => x.Log.GetGlobal(new Dictionary<string, IClient>(), nodeName, id));
            Mock.VerifyAll();
            Assert.IsTrue(mockLogIds.Contains(id));
            Assert.AreNotEqual("UNAVAILABLE", result);
        }
        [Test]
        public void ParseNodePERMISSIONSCase_NodeEvents()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetNodeEvents(It.IsAny<IPAddress>())).Returns("NONE").Verifiable();
            //Act
            var result = parser.ParseNode("PERMISSIONS", ip);
            //Assert
            Mock.VerifyAll();
            mockGraph.Verify(x => x.GetNodeEvents(ip));
            Assert.AreEqual("NONE", result);
        }
        [Test]
        public void ParseNodeEXECUTECase_ExecuteResult()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            var eventToExecute = new EventInternal("e2", "l2", true, true);
            var mockEventSet = new HashSet<EventInternal>
            {
                new EventInternal("e1", "l1", true, false),
                eventToExecute,
            };
            var mockEvents = new Dictionary<string, EventInternal>();
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            mockEvents.Add(eventToExecute.Name, eventToExecute);
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<EventInternal>>
                                { {ip, mockEventSet } };
            mockGraph.Setup(x => x.NodeEvents).Returns(mockNodeEvents).Verifiable();
            mockGraph.Setup(x => x.Events).Returns(mockEvents).Verifiable();
            mockGraph.Setup(x => x.Execute(eventToExecute.Name)).Returns(true).Verifiable();
            //Act
            var result = parser.ParseNode("EXECUTE e2", ip);

            //Assert
            Mock.VerifyAll();
            mockGraph.Verify(x => x.Execute(eventToExecute.Name));
            Assert.AreEqual(true.ToString(), result);
        }

        [Test]
        public void ParseNodeEXECUTECase_InvalidEvent()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            var eventToExecute = new EventInternal("e2", "l2", true, true);
            var mockEventSet = new HashSet<EventInternal>
            {
                new EventInternal("e1", "l1", true, false),
                eventToExecute,
            };
            var mockEvents = new Dictionary<string, EventInternal>();
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            mockEvents.Add(eventToExecute.Name, eventToExecute);
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<EventInternal>>
                                { {ip, mockEventSet } };
            mockGraph.Setup(x => x.Events).Returns(mockEvents).Verifiable();

            //Act
            var result = parser.ParseNode("EXECUTE ex", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("SPECIFIED EVENT INVALID", result);
        }
        [Test]
        public void ParseNodeEXECUTECase_InvalidPermission()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            var eventToExecute = new EventInternal("e2", "l2", true, true);
            var mockEventSet = new HashSet<EventInternal>
            {
                new EventInternal("e1", "l1", true, false),
            };
            var mockEvents = new Dictionary<string, EventInternal>();
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            mockEvents.Add(eventToExecute.Name, eventToExecute);
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<EventInternal>>
                                { {ip, mockEventSet } };
            mockGraph.Setup(x => x.NodeEvents).Returns(mockNodeEvents).Verifiable();
            mockGraph.Setup(x => x.Events).Returns(mockEvents).Verifiable();

            //Act
            var result = parser.ParseNode("EXECUTE e2", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("INVALID PERMISSION", result);
        }

        [Test]
        public void ParseNodeACCEPTINGCaseNoneAccepting_False()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(false).Verifiable();
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), It.IsAny<string>(), It.IsAny<Guid>())).Returns(false);
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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(true).Verifiable();
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), It.IsAny<string>(), It.IsAny<Guid>())).Returns(false).Verifiable();
            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("False", result);
        }
        [Test]
        public void ParseNodeACCEPTINGCaseExternalAccepting_False()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(false).Verifiable();
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), It.IsAny<string>(), It.IsAny<Guid>())).Returns(true).Verifiable();
            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("False", result);
        }

        [Test]
        public void ParseNodeACCEPTINGCaseBothAccepting_True()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

            mockGraph.Setup(x => x.GetAcceptingInternal()).Returns(true).Verifiable();
            mockGraph.Setup(x => x.GetAcceptingExternal(It.IsAny<Dictionary<string, IClient>>(), It.IsAny<string>(), It.IsAny<Guid>())).Returns(true).Verifiable();
            //Act
            var result = parser.ParseNode("ACCEPTING", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("True", result);
        }

        [Test]
        public void ParseNodeLOGCase_GlobalLog()
        {
            //Arrange
            Guid id = Guid.NewGuid();
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IClient> mockClient = new Mock<IClient>();
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Mock<ILog> mockLog = new Mock<ILog>();
            Parser parser = new Parser(mockGraph.Object, config);
            DateTime time = new DateTime(2020, 07, 04, 13, 45, 10);
            DateTime timeTwo = new DateTime(2020, 07, 03, 13, 45, 10);
            var mockMainNodes = new Dictionary<string, IClient> { { "MainNode1", mockClient.Object } };

            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Name).Returns("this");
            mockGraph.Setup(x => x.GetLogIds).Returns(new List<Guid>());
            mockGraph.Setup(x => x.Log).Returns(mockLog.Object);
            mockLog.Setup(x => x.GetGlobal(config.MainNodes, config.Name, It.IsAny<Guid>())).Returns(new Log(config)
            {
                { time, "e2" },
                { timeTwo, "e1" }
            });

            // Act
            var result = parser.ParseNode("LOG " , ip);


            //Assert
            var expected = timeTwo + " e1" + "\n" + time + " e2";
            Mock.VerifyAll();
            mockGraph.Verify(x => x.Log.GetGlobal(config.MainNodes, config.Name, It.IsAny<Guid>()));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ParseNodeMARKINGCase_InvalidEvent()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            var mockEvents = new Dictionary<string, EventInternal>();
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e3", new EventInternal("e2", "l2", true, false));
            mockEvents.Add("e2", new EventInternal("e3", "l3", true, true));
            mockGraph.Setup(x => x.Events).Returns(mockEvents).Verifiable();

            //Act
            var result = parser.ParseNode("MARKING ex", ip);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("SPECIFIED EVENT INVALID", result);
        }
        [Test]
        public void ParseNodeMARKINGCase_resultingMarking()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            var eventM = new EventInternal("e2", "l2", true, true);
            var mockEvents = new Dictionary<string, EventInternal>();
            mockEvents.Add("e1", new EventInternal("e1", "l1", true, false));
            mockEvents.Add("e3", new EventInternal("e3", "l3", true, false));
            mockEvents.Add(eventM.Name, eventM);
            mockGraph.Setup(x => x.Events).Returns(mockEvents).Verifiable();
            mockGraph.Setup(x => x.GetMarking(eventM)).Returns("GetMarking(e2) RESULT").Verifiable();
            //Act
            var result = parser.ParseNode("MARKING e2", ip);

            //Assert
            Mock.VerifyAll();
            mockGraph.Verify(x => x.GetMarking(eventM));
            Assert.AreEqual("GetMarking(e2) RESULT", result);
        }

        [Test]
        public void ParseNodeALLMARKINGSCase_resultingMarkings()
        {
            //Arrange
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
            mockGraph.Setup(x => x.GetAllMarkings()).Returns("resultingMarkings").Verifiable();
            //Act
            var result = parser.ParseNode("ALLMARKINGS", IPAddress.Any);

            //Assert
            Mock.VerifyAll();
            Assert.AreEqual("resultingMarkings", result);
        }

        [Test]
        public void ParseNodeEmptyCommand_UNKNOWN_COMMAND()
        {
            //Arrange
            var ip = IPAddress.Parse("127.0.0.1");
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);

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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
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
            Mock<IGraph> mockGraph = new Mock<IGraph>();
            Parser parser = new Parser(mockGraph.Object, config);
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