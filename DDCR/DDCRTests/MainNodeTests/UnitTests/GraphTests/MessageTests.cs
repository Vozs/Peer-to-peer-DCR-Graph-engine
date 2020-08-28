using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

/*Contains tests for 
 * GetPeerMessages, 
 * MessagePeers, 
 * CheckPeerReplies, 
*/
namespace DDCRTests.UnitTests
{
    class MessageTests
    {
        Graph graph;
        IConfig config;
        [SetUp]
        public void Setup()
        {
            var MockConfig = new Mock<IConfig>();
            config = MockConfig.Object;
            var mockNodes = new Dictionary<string, System.Net.IPAddress>
            {
                {"node1", IPAddress.Parse("127.0.0.1") }
            };
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.MainNodes).Returns(new Dictionary<string, IClient>());
            Mock<Log> mockLog = new Mock<Log>(MockConfig.Object);
            graph = new Graph(mockLog.Object, MockConfig.Object);

        }

        [Test]
        public void GetPeerMessages_InternalEvent_x1Exclude()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.ExcludesExternal.Add(new EventExternal("x1", "l1", "peer1"));

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 exclude");
        }
        [Test]
        public void GetPeerMessages_InternalEvent_x1ExcludeConditionMinus()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            var extEvent = new EventExternal("x1", "l1", "peer2");
            mockEvent.ExcludesExternal.Add(extEvent);
            mockEvent.ConditionsExternal.Add(extEvent);
            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer2"));
            Assert.IsTrue(result["peer2"] == "x1 exclude condition-");
        }

        [Test]
        public void GetPeerMessages_InternalEvent_x1Excludex2ConditionMinus()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.ExcludesExternal.Add(new EventExternal("x1", "l1", "peer2"));
            mockEvent.ConditionsExternal.Add(new EventExternal("x2", "l2", "peer2"));
            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer2"));
            Assert.IsTrue(result["peer2"] == "x1 exclude\n" +
                                            "x2 condition-");
        }

        [Test]
        public void GetPeerMessages_InternalEvent_Exclude()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.Executed = true; //Should not send condition- now, because it has been previously executed
            mockEvent.ExcludesExternal.Add(new EventExternal("x1", "l1", "peer1"));
            mockEvent.ConditionsExternal.Add(new EventExternal("x2", "l2", "peer1"));
            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 exclude");
        }

        [Test]
        public void GetPeerMessages_InternalEvent_TwoPeers()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.ExcludesExternal.Add(new EventExternal("x1", "l1", "peer1"));
            mockEvent.ConditionsExternal.Add(new EventExternal("x2", "l2", "peer2"));
            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result.ContainsKey("peer2"));
            Assert.IsTrue(result["peer1"] == "x1 exclude");
            Assert.IsTrue(result["peer2"] == "x2 condition-");
        }
        [Test]
        public void GetPeerMessages_InternalEvent_MilestoneMinus()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, true);
            mockEvent.MilestonesExternal.Add(new EventExternal("x1", "l1", "peer1"));

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 milestone-");
        }

        [Test]
        public void GetPeerMessages_InternalEvent_NoMilestone()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.MilestonesExternal.Add(new EventExternal("x1", "l1", "peer1"));

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsFalse(result.ContainsKey("peer1"));
        }

        [Test]
        public void GetPeerMessages_InternalEvent_Include()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.IncludesExternal.Add(new EventExternal("x1", "l1", "peer1"));

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 include");
        }
        [Test]
        public void GetPeerMessages_InternalEvent_Response()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);
            mockEvent.ResponsesExternal.Add(new EventExternal("x1", "l1", "peer1"));

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 response");
        }
        [Test]
        public void GetPeerMessages_InternalEvent_MilestonePlus()
        {
            //Arrange
            var mockEvent = new EventInternal("ev", "evl", true, false);

            //Makes itself pending
            mockEvent.Responses.Add(mockEvent);

            //If it is not pending, but becomes pending after execution, we should send a milestone+ if its also included
            mockEvent.MilestonesExternal.Add(new EventExternal("x1", "l1", "peer1"));

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 milestone+");
        }
        [Test]
        public void GetPeerMessages_EventForeignIncludes_ConditionPlus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", false, false);
            var extEvent = new EventExternal("x1", "l1", "peer1");

            //Include a local event that isn't already included
            mockEvent.Includes.Add(mockInternal);

            mockInternal.ConditionsExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 condition+");
        }

        [Test]
        public void GetPeerMessages_EventForeignIncludes_NoConditionPlus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", true, false);
            var extEvent = new EventExternal("x1", "l1", "peer1");

            mockEvent.Includes.Add(mockInternal);

            mockInternal.ConditionsExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsFalse(result.ContainsKey("peer1"));
        }
        [Test]
        public void GetPeerMessages_EventForeignIncludes_MilestonePlus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", false, true);
            var extEvent = new EventExternal("x1", "l1", "peer1");

            //Include a local event that isn't already included
            mockEvent.Includes.Add(mockInternal);

            mockInternal.MilestonesExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.IsTrue(result["peer1"] == "x1 milestone+");
        }

        [Test]
        public void GetPeerMessages_EventForeign_MilestoneMinus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", true, true);
            var extEvent = new EventExternal("x1", "l1", "peer1");

            //Exclude an event
            mockEvent.Excludes.Add(mockInternal);

            mockInternal.MilestonesExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.AreEqual("x1 milestone-", result["peer1"]);
        }

        [Test]
        public void GetPeerMessages_EventForeign_NoMilestoneMinus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", true, false);
            var extEvent = new EventExternal("x1", "l1", "peer1");

            //Exclude an event
            mockEvent.Excludes.Add(mockInternal);

            mockInternal.MilestonesExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsFalse(result.ContainsKey("peer1"));
        }

        [Test]
        public void GetPeerMessages_EventForeign_ConditionMinus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", true, false);
            var extEvent = new EventExternal("x1", "l1", "peer1");

            //Exclude an event
            mockEvent.Excludes.Add(mockInternal);

            mockInternal.ConditionsExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsTrue(result.ContainsKey("peer1"));
            Assert.AreEqual("x1 condition-", result["peer1"]);
        }

        [Test]
        public void GetPeerMessages_EventForeign_NoConditionMinus()
        {
            //Arrange
            var mockEvent = new EventForeign();
            var mockInternal = new EventInternal("ev", "evl", true, false);
            mockInternal.Executed = true;
            var extEvent = new EventExternal("x1", "l1", "peer1");

            //Exclude an event
            mockEvent.Excludes.Add(mockInternal);

            mockInternal.MilestonesExternal.Add(extEvent);

            //Act
            var result = graph.GetPeerMessages(mockEvent);

            //Assert
            Assert.IsFalse(result.ContainsKey("peer1"));
        }

        [Test]
        public void MessagePeers_OnePeer_peer1SendResult()
        {
            //Arrange
            var prevTasks = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") } };
            var id = Guid.NewGuid();
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("SUCCESS")).Verifiable();
            config.MainNodes.Add("peer1", mockClient.Object);

            //Act
            var result = graph.MessagePeers("COMMAND", id, prevTasks, true);

            //Assert
            mockClient.Verify(x => x.SendAsync("COMMAND " + id.ToString(), It.IsAny<bool>()));

        }

        [Test]
        public void MessagePeers_OnePeer_peer1SendResult2()
        {
            //Arrange
            var prevTasks = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") }, { "peer2", Task.FromResult("UNAVAILABLE") } };
            var id = Guid.NewGuid();
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("SUCCESS")).Verifiable();
            config.MainNodes.Add("peer1", mockClient.Object);

            //Act
            var result = graph.MessagePeers("COMMAND", id, prevTasks, true);

            //Assert
            mockClient.Verify(x => x.SendAsync("COMMAND " + id.ToString(), It.IsAny<bool>()));
            Assert.AreEqual("SUCCESS", result["peer1"].Result);
            Assert.IsFalse(result.ContainsKey("peer2"));
        }

        [Test]
        public void MessagePeers_OnePeer_peer1And2SendResult()
        {
            //Arrange
            var prevTasks = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("SUCCESS") }, { "peer2", Task.FromResult("SUCCESS") } };
            var id = Guid.NewGuid();
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("SUCCESS")).Verifiable();
            config.MainNodes.Add("peer1", mockClient.Object);
            config.MainNodes.Add("peer2", mockClient.Object);
            //Act
            var result = graph.MessagePeers("COMMAND", id, prevTasks, true);

            //Assert
            mockClient.Verify(x => x.SendAsync("COMMAND " + id.ToString(), It.IsAny<bool>()));
            Assert.AreEqual("SUCCESS", result["peer1"].Result);
            Assert.AreEqual("SUCCESS", result["peer2"].Result);
        }

        [Test]
        public void MessagePeers_OnePeer_peer1SendResult3()
        {
            //Arrange
            var prevTasks = new Dictionary<string, Task<string>> { { "peer1", Task.FromResult("UNAVAILABLE") } };
            var id = Guid.NewGuid();
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("SUCCESS")).Verifiable();
            config.MainNodes.Add("peer1", mockClient.Object);

            //Act
            var result = graph.MessagePeers("COMMAND", id, prevTasks, true);

            //Assert
            Assert.IsFalse(result.ContainsKey("peer1"));

        }
        [Test]
        public void CheckPeerReplies_OneTask_false()
        {
            //Arrange
            var tasks = new Dictionary<string, Task<string>> {
                { "peer1", Task.FromResult("SUCCESS") } };

            //Act
            var result = graph.CheckPeerReplies(tasks);

            //Assert
            Assert.IsFalse(result);
        }
        [Test]
        public void CheckPeerReplies_OneTask_true()
        {
            //Arrange
            var tasks = new Dictionary<string, Task<string>> {
                { "peer1", Task.FromResult("UNAVAILABLE") } };

            //Act
            var result = graph.CheckPeerReplies(tasks);

            //Assert
            Assert.IsTrue(result);
        }


        [Test]
        public void CheckPeerReplies_3Tasks_true()
        {
            //Assert
            var tasks = new Dictionary<string, Task<string>> {
                { "peer1", Task.FromResult("SUCCESS") },
                { "peer2", Task.FromResult("UNAVAILABLE") },
                { "peer3", Task.FromResult("SUCCESS") },
            };

            //Act
            var result = graph.CheckPeerReplies(tasks);

            //Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CheckPeerReplies_3Tasks_false()
        {
            //Assert
            var tasks = new Dictionary<string, Task<string>> {
                { "peer1", Task.FromResult("SUCCESS") },
                { "peer2", Task.FromResult("SUCCESS") },
                { "peer3", Task.FromResult("SUCCESS") },
            };

            //Act
            var result = graph.CheckPeerReplies(tasks);

            //Assert
            Assert.IsFalse(result);
        }
        [Test]
        public void CheckPeerReplies_EmptyTasks_false()
        {
            //Assert
            var tasks = new Dictionary<string, Task<string>>();

            //Act
            var result = graph.CheckPeerReplies(tasks);

            //Assert
            Assert.IsFalse(result);
        }

    }
}