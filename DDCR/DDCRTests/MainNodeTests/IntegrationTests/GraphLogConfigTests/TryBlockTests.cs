using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;

/*Tests for 
 * TryBlockInternal,
 * TryBlockExternal
 * TryUnblockPeerAsync 
 */

namespace DDCRTests.IntegrationTests.GraphLogConfigTests
{
    class TryBlockTests
    {
        Graph graph;
        Config config;
        EventInternal eventFrom;
        EventInternal eventTo;
        EventInternal thirdEvent;
        Guid id;


        [SetUp]
        public void Setup()
        {
            config = new Config("TestAssets/integrationtests/test1.ini");
            Log log = new Log(config);
            graph = new Graph(log, config);
            graph.Load("TestAssets/integrationtests/TryBlockTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split());
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            graph.CreateEvent("thirdEvent", "label", "Node1".Split());
            eventFrom = graph.Events["eventFrom"];
            eventTo = graph.Events["eventTo"];
            thirdEvent = graph.Events["thirdEvent"];
            id = Guid.NewGuid();
            graph.Blocked.Add(id, new List<EventInternal>());
        }

        [Test]
        public void TryUnblockPeerAsync_TwoUnavailableOneSuccess()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var mockClient = new Mock<IClient>();
            var success = Task.FromResult("SUCCESS");
            var unavailable = Task.FromResult("UNAVAILABLE");
            mockClient.SetupSequence(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(unavailable)
                .Returns(unavailable)
                .Returns(unavailable)
                .Returns(success);
            config.MainNodes.Add("peer1", mockClient.Object);

            //Act
            graph.TryUnblockPeerAsync("peer1", id);

            //Assert
            mockClient.Verify(x => x.SendAsync("UNBLOCK " + id, true), Times.Exactly(4));
        }

        [Test]
        public void TryUnblockPeerAsync_OneSuccess()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var mockClient = new Mock<IClient>();
            var success = Task.FromResult("SUCCESS");
            var unavailable = Task.FromResult("UNAVAILABLE");
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(success);
            config.MainNodes.Add("peer1", mockClient.Object);

            //Act
            graph.TryUnblockPeerAsync("peer1", id);

            //Assert
            mockClient.Verify(x => x.SendAsync("UNBLOCK " + id, true), Times.Once());
        }

        [Test]
        public void TryUnblockPeerAsync_AllUnavailable()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var mockClient = new Mock<IClient>();
            var unavailable = Task.FromResult("UNAVAILABLE");
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(unavailable);
            config.MainNodes.Add("peer1", mockClient.Object);

            //Act
            graph.TryUnblockPeerAsync("peer1", id);
            
            //Assert
            mockClient.Verify(x => x.SendAsync("UNBLOCK " + id, true), Times.Exactly(config.MaxConnectionAttempts));
        }

        [Test]
        public void TryBlockInternal_ExcludeEventNotBlocked_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Exclude);
            graph.TryBlockInternal(eventFrom, id);

            var expected = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, eventTo.Block);
            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
        }
        [Test]
        public void TryBlockInternal_ExcludeEventBlocksItself_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventFrom.Name, Relation.Exclude);
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var blockedExpected = new Tuple<bool, Guid>(false, Guid.Empty);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(blockedExpected, eventFrom.Block);
        }

        [Test]
        public void TryBlockInternal_ExcludeEventAlreadyBlockedByAnotherID_False()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Exclude);
            Guid anotherID = Guid.NewGuid();
            eventTo.Block = new Tuple<bool, Guid>(true, anotherID);

            var result = graph.TryBlockInternal(eventFrom, id);
            var expected = false;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_ExcludeEventAlreadyBlockedWithSameID_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Exclude);
            eventTo.Block = new Tuple<bool, Guid>(true, id);

            var result = graph.TryBlockInternal(eventFrom, id);
            var expected = true;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_ExcludeEventNotBlockedButHasID_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Exclude);
            eventTo.Block = new Tuple<bool, Guid>(false, id);

            var result = graph.TryBlockInternal(eventFrom, id);
            var expected = true;
            var expectedBlock = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(expectedBlock, eventTo.Block);
        }

        [Test]
        public void TryBlockInternal_ResponseEventNotBlocked_True(){

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.TryBlockInternal(eventFrom, id);

            var expected = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, eventTo.Block);
            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
        }

        [Test]
        public void TryBlockInternal_ResponseToItself_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventFrom.Name, Relation.Response);
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_ResponseNotItselfButBlocked_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            eventTo.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_ResponseSecondEventIncludedAndHasMilestone_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);

            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var expectedSecondEvent = new Tuple<bool, Guid>(true, id);
            var expectedThirdEvent = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expectedSecondEvent, eventTo.Block);
            Assert.AreEqual(expectedThirdEvent, thirdEvent.Block);

            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(true, graph.Blocked[id].Contains(thirdEvent));
        }

        [Test]
        public void TryBlockInternal_ResponseSecondEventExcluded_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            eventTo.Included = false;
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var expectedSecondEvent = new Tuple<bool, Guid>(true, id);
            var expectedThirdEvent = new Tuple<bool, Guid>(false, Guid.Empty);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expectedSecondEvent, eventTo.Block);
            Assert.AreEqual(expectedThirdEvent, thirdEvent.Block);

            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(false, graph.Blocked[id].Contains(thirdEvent));
        }

        [Test]
        public void TryBlockInternal_ResponseThridMileStoneToFirstEvent_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.AddRelationInternal(eventTo.Name, eventFrom.Name, Relation.Milestone);

            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var expectedSecondEvent = new Tuple<bool, Guid>(true, id);
            var expectedThirdEvent = new Tuple<bool, Guid>(false, Guid.Empty);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expectedSecondEvent, eventTo.Block);
            Assert.AreEqual(expectedThirdEvent, thirdEvent.Block);

            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(false, graph.Blocked[id].Contains(thirdEvent));
        }

        [Test]
        public void TryBlockInternal_ResponseThirdMileStoneEventIsAlreadyBlocked_False()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            Guid newId = Guid.NewGuid();
            thirdEvent.Block = new Tuple<bool, Guid>(true, newId);
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = false;
            var expectedSecondEvent = new Tuple<bool, Guid>(true, id);
            var expectedThirdEvent = new Tuple<bool, Guid>(true, newId);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expectedSecondEvent, eventTo.Block);
            Assert.AreEqual(expectedThirdEvent, thirdEvent.Block);

            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(false, graph.Blocked[id].Contains(thirdEvent));
        }

        [Test]
        public void TryBlockInternal_ResponseThirdMileStoneIsAlreadyBlockedByID_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            thirdEvent.Block = new Tuple<bool, Guid>(true, id);
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var expectedSecondEvent = new Tuple<bool, Guid>(true, id);
            var expectedThirdEvent = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expectedSecondEvent, eventTo.Block);
            Assert.AreEqual(expectedThirdEvent, thirdEvent.Block);

            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(false, graph.Blocked[id].Contains(thirdEvent));
        }

        [Test]
        public void TryBlockInternal_ResponseThirdMileStoneIsNotBlockedButHasID_True()
        {

            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Response);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            thirdEvent.Block = new Tuple<bool, Guid>(false, id);
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var expectedSecondEvent = new Tuple<bool, Guid>(true, id);
            var expectedThirdEvent = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expectedSecondEvent, eventTo.Block);
            Assert.AreEqual(expectedThirdEvent, thirdEvent.Block);

            Assert.AreEqual(true, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(true, graph.Blocked[id].Contains(thirdEvent));
        }

        [Test]
        public void TryBlockInternal_IncludeEventNotBlocked_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);

            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var blockedExpected = new Tuple<bool, Guid>(true, id);

            Assert.AreEqual(expected, result);
            Assert.AreEqual(blockedExpected, eventTo.Block);
        }


        [Test]
        public void TryBlockInternal_IncludeThirdEventWithCondition_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Condition);

            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var secondExpected = true;
            var thirdExpected = true;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeIsAlreadyBlockedByAnother_False()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Condition);
            eventTo.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = false;
            var secondExpected = false;
            var thirdExpected = false;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdEventIsAlreadyBlockedByAnother_False()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Condition);
            thirdEvent.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = false;
            var secondExpected = false;
            var thirdExpected = false;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdEventIsTheFirstEvent_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, eventFrom.Name, Relation.Condition);
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var secondExpected = true;
            var thirdExpected = false;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdMileStoneEventSecondIsPending_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            eventTo.Pending = true;
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var secondExpected = true;
            var thirdExpected = true;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdMileStoneEventSecondIsNotPending_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            eventTo.Pending = false;
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var secondExpected = true;
            var thirdExpected = false;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdMileStoneIsFirstEvent_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, eventFrom.Name, Relation.Milestone);
            eventTo.Pending = true;
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var secondExpected = true;
            var thirdExpected = false;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdMileStoneIsAlreadyBlocked_False()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            eventTo.Pending = true;
            thirdEvent.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = false;
            var secondExpected = false;
            var thirdExpected = false;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_IncludeThirdMileStoneHasIDButIsNotBlocked_True()
        {
            graph.AddRelationInternal(eventFrom.Name, eventTo.Name, Relation.Include);
            graph.AddRelationInternal(eventTo.Name, thirdEvent.Name, Relation.Milestone);
            eventTo.Pending = true;
            thirdEvent.Block = new Tuple<bool, Guid>(false, Guid.NewGuid());
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;
            var secondExpected = true;
            var thirdExpected = true;

            Assert.AreEqual(secondExpected, graph.Blocked[id].Contains(eventTo));
            Assert.AreEqual(thirdExpected, graph.Blocked[id].Contains(thirdEvent));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockInternal_NoRelations_True()
        {
            var result = graph.TryBlockInternal(eventFrom, id);

            var expected = true;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockExternal_SuccesfulSendWithAnyString_True()
        {
            var mockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            DateTime time = DateTime.Now;
            config.MainNodes.Add("mainNode", mockClient.Object);
            Dictionary<string, string> peerMessages = new Dictionary<string, string> {
                {"mainNode", "Any string"}
            };

            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            var result = graph.TryBlockExternal(peerMessages, id, time.ToString());

            var expected = true;
            Assert.AreEqual(expected, result);

        }

        [Test]
        public void TryBlockExternal_SuccesfulSendWithSpecificString_True()
        {
            var mockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            DateTime time = DateTime.Now;
            config.MainNodes.Add("mainNode", mockClient.Object);
            Dictionary<string, string> peerMessages = new Dictionary<string, string> {
                {"mainNode", "Any string"}
            };
            var msg = string.Format("BLOCK {0} {1}\n{2}",
                    id, time.ToString(), peerMessages["mainNode"]);
            mockClient.Setup(x => x.SendAsync(msg, false)).ReturnsAsync("SUCCESS").Verifiable();
            var result = graph.TryBlockExternal(peerMessages, id, time.ToString());

            mockClient.Verify(x => x.SendAsync(msg, false), Times.Once());

            var expected = true;
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockExternal_SuccesfulSendWithManyMainNodes_True()
        {
            var mockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            DateTime time = DateTime.Now;
            Dictionary<string, string> peerMessages = new Dictionary<string, string>();
            for (int i = 0; i <= 4; i++)
            {
                config.MainNodes.Add("mainNode"+i, mockClient.Object);
                peerMessages.Add("mainNode" + i, "AnyString");
            }
            List<string> msgs = new List<string>();
            for (int i = 0; i <= 4; i++)
            {
                var msg = string.Format("BLOCK {0} {1}\n{2}",
                    id, time.ToString(), peerMessages["mainNode" + i]);
                mockClient.Setup(x => x.SendAsync(msg, false)).ReturnsAsync("SUCCESS").Verifiable();
                msgs.Add(msg);
            }
            var result = graph.TryBlockExternal(peerMessages, id, time.ToString());
            for (int i = 0; i <= 4; i++)
            {
                mockClient.Verify(x => x.SendAsync(msgs[i], false), Times.Exactly(5));
            }

            var expected = true;
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockExternal_OneMainNodeReturnsUnavailable_False()
        {
            var mockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            DateTime time = DateTime.Now;
            Dictionary<string, string> peerMessages = new Dictionary<string, string>();
            for (int i = 0; i < 4; i++)
            {
                config.MainNodes.Add("mainNode" + i, mockClient.Object);
                peerMessages.Add("mainNode" + i, "AnyString");
            }
            config.MainNodes.Add("mainNode", mockClient.Object);
            peerMessages.Add("mainNode", "AnyString");


            List<string> msgs = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                var msg = string.Format("BLOCK {0} {1}\n{2}",
                    id, time.ToString(), peerMessages["mainNode" + i]);
                mockClient.Setup(x => x.SendAsync(msg, false)).ReturnsAsync("SUCCESS").Verifiable();
                msgs.Add(msg);
            }

            var failmsg =string.Format("BLOCK {0} {1}\n{2}",
                    id, time.ToString(), peerMessages["mainNode"]);
            msgs.Add(failmsg);

            mockClient.Setup(x => x.SendAsync(failmsg, false)).ReturnsAsync("UNAVAILABLE").Verifiable();
            var result = graph.TryBlockExternal(peerMessages, id, time.ToString());

            for (int i = 0; i <= 4; i++)
            {
                mockClient.Verify(x => x.SendAsync(msgs[i], false), Times.Exactly(5));
            }

            var expected = false;
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockExternal_AllMainNodesReturnUnavailable_False()
        {
            var mockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            DateTime time = DateTime.Now;
            Dictionary<string, string> peerMessages = new Dictionary<string, string>();
            for (int i = 0; i <= 4; i++)
            {
                config.MainNodes.Add("mainNode" + i, mockClient.Object);
                peerMessages.Add("mainNode" + i, "AnyString");
            }
            List<string> msgs = new List<string>();
            for (int i = 0; i <= 4; i++)
            {
                var msg = string.Format("BLOCK {0} {1}\n{2}",
                    id, time.ToString(), peerMessages["mainNode" + i]);
                mockClient.Setup(x => x.SendAsync(msg, false)).ReturnsAsync("UNAVAILABLE").Verifiable();
                msgs.Add(msg);
            }
            var result = graph.TryBlockExternal(peerMessages, id, time.ToString());
            for (int i = 0; i <= 4; i++)
            {
                mockClient.Verify(x => x.SendAsync(msgs[i], false), Times.Exactly(5));
            }

            var expected = false;
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryBlockExternal_EmptyPeerMessages_True()
        {
            var mockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            DateTime time = DateTime.Now;
            Dictionary<string, string> peerMessages = new Dictionary<string, string>();
            var result = graph.TryBlockExternal(peerMessages, id, time.ToString());

            var expected = true;
            Assert.AreEqual(expected, result);
        }

    }
}
