using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
/*Contains tests for 
 * GetAcceptingInternal, 
 * GetAcceptingExternal, 
 * UnblockEvents, 
 * CheckBlocked*/
namespace DDCRTests.IntegrationTests.GraphLogConfigTests
{
    class StateTests
    {
        Graph graph;
        Config config = new Config("TestAssets/integrationtests/test1.ini");
        [SetUp]
        public void Setup()
        {
            Log log = new Log(config);
            graph = new Graph(log, config);
            graph.Load("TestAssets/integrationtests/StateTests.xml");
        }

        [Test]
        public void GetAcceptingInternalTest_OneEventNotPending_True()
        {
            graph.CreateEvent("event", "label", "Node1".Split());

            var expected = true;

            var actual = graph.GetAcceptingInternal();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetAcceptingInternalTest_OneEventIsPending_False()
        {
            graph.CreateEvent("event", "label", "Node1".Split(), true, true);

            var expected = false;

            var actual = graph.GetAcceptingInternal();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetAcceptingInternalTest_FiveEventsNonePending_True()
        {
            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEvent("event"+i, "label", "Node1".Split());
            }

            var expected = true;

            var actual = graph.GetAcceptingInternal();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetAcceptingInternalTest_FiveEventsOnePending_False()
        {
            for (int i = 0; i <= 3; i++)
            {
                graph.CreateEvent("event" + i, "label", "Node1".Split());
            }
            graph.CreateEvent("event", "label", "Node1".Split(), true, true);

            var expected = false;

            var actual = graph.GetAcceptingInternal();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetAcceptingInternalTest_FiveEventsOnePendingAndExcluded_True()
        {
            for (int i = 0; i <= 3; i++)
            {
                graph.CreateEvent("event" + i, "label", "Node1".Split());
            }
            graph.CreateEvent("event", "label", "Node1".Split(), false, true);

            var expected = true;

            var actual = graph.GetAcceptingInternal();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetAcceptingExternalTest_ThreePeersAllAcceptingSpecificString_True()
        {
            Mock<IClient> MockClient = new Mock<IClient>();
            string peermessage = "thisPeer peerOne peerTwo peerThree";
            Guid id = Guid.NewGuid();
            string msg = $"ACCEPTING {id}\n{peermessage}";
            MockClient.Setup(x => x.SendAsync(msg, It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            Dictionary<string, IClient> peers = new Dictionary<string, IClient>()
            {
                {"peerOne", MockClient.Object },
                {"peerTwo", MockClient.Object },
                {"peerThree", MockClient.Object }
            };

            

            var expected = true;

            var result = graph.GetAcceptingExternal(peers, "thispeer", id);
            Assert.AreEqual(3, peers.Count);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetAcceptingExternalTest_ThreePeersAllAcceptingAnyString_True()
        {
            Mock<IClient> MockClient = new Mock<IClient>();
            Guid id = Guid.NewGuid();
            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            Dictionary<string, IClient> peers = new Dictionary<string, IClient>()
            {
                {"peerOne", MockClient.Object },
                {"peerTwo", MockClient.Object },
                {"peerThree", MockClient.Object }
            };

            

            var expected = true;

            var result = graph.GetAcceptingExternal(peers, "thispeer", id);
            Assert.AreEqual(3, peers.Count);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetAcceptingExternalTest_ThreePeersOneNotAcceptingSpecificString_False()
        {
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientFalse = new Mock<IClient>();
            string peermessage = "thisPeer peerOne peerTwo peerThree";
            Guid id = Guid.NewGuid();
            string msg = $"ACCEPTING {id}\n{peermessage}";

            MockClient.Setup(x => x.SendAsync(msg, It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            MockClientFalse.Setup(x => x.SendAsync(msg, It.IsAny<bool>())).ReturnsAsync("FALSE");
            Dictionary<string, IClient> peers = new Dictionary<string, IClient>()
            {
                {"peerOne", MockClient.Object },
                {"peerTwo", MockClient.Object },
                {"peerThree", MockClientFalse.Object }
            };

            

            var expected = false;

            var result = graph.GetAcceptingExternal(peers, "thisPeer", id);
            Assert.AreEqual(3, peers.Count);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetAcceptingExternalTest_ThreePeersOneNotAcceptingAnyString_False()
        {
            Mock<IClient> MockClient = new Mock<IClient>();
            Mock<IClient> MockClientFalse = new Mock<IClient>();
            Guid id = Guid.NewGuid();

            MockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS");
            MockClientFalse.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("FALSE");
            Dictionary<string, IClient> peers = new Dictionary<string, IClient>()
            {
                {"peerOne", MockClient.Object },
                {"peerTwo", MockClient.Object },
                {"peerThree", MockClientFalse.Object }
            };


            var expected = false;

            var result = graph.GetAcceptingExternal(peers, "thisPeer", id);
            Assert.AreEqual(3, peers.Count);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void UnblockEventTest_SimpleRemove_EventIsUnblocked()
        {
            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            Guid execid = Guid.NewGuid();
            ev.Block = new Tuple<bool, Guid>(true, execid);

            graph.Blocked.Add(execid, new List<EventInternal>());
            graph.Blocked[execid].Add(ev);

            var result = graph.Blocked.ContainsKey(execid);
            
            graph.UnblockEvents(execid);

            var expectedBoolResult = false;
            var expectedGuidResult = Guid.Empty;
            var expectedContainsResult = false;

            var boolresult = ev.Block.Item1;
            var guidresult = ev.Block.Item2;
            var containsresult = graph.Blocked.ContainsKey(execid);
            Assert.AreEqual(true, result);
            Assert.AreEqual(expectedBoolResult, boolresult);
            Assert.AreEqual(expectedGuidResult, guidresult);
            Assert.AreEqual(expectedContainsResult, containsresult);
        }

        [Test]
        public void UnblockEventTest_BlockedDoesNotContainKey_NothingChanges()
        {
            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            Guid execid = Guid.NewGuid();

            var result = graph.Blocked.ContainsKey(execid);
            
            graph.UnblockEvents(execid);

            var expectedBoolResult = false;
            var expectedGuidResult = Guid.Empty;
            var expectedContainsResult = false;

            var boolresult = ev.Block.Item1;
            var guidresult = ev.Block.Item2;
            var containsresult = graph.Blocked.ContainsKey(execid);
            Assert.AreEqual(false, result);
            Assert.AreEqual(expectedBoolResult, boolresult);
            Assert.AreEqual(expectedGuidResult, guidresult);
            Assert.AreEqual(expectedContainsResult, containsresult);
        }

        [Test]
        public void UnblockEventTest_EventBlocedUnblockWithDifferentId_EventNotUnblocked()
        {
            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            Guid execid = Guid.NewGuid();
            Guid differentId = Guid.NewGuid();
            ev.Block = new Tuple<bool, Guid>(true, execid);

            graph.Blocked.Add(execid, new List<EventInternal>());
            graph.Blocked[execid].Add(ev);

            var result = graph.Blocked.ContainsKey(execid);
            
            graph.UnblockEvents(differentId);

            var expectedBoolResult = true;
            var expectedContainsResult = true;

            var boolresult = ev.Block.Item1;
            var guidresult = ev.Block.Item2;
            var containsresult = graph.Blocked.ContainsKey(execid);
            Assert.AreEqual(true, result);
            Assert.AreEqual(expectedBoolResult, boolresult);
            Assert.AreEqual(execid, guidresult);
            Assert.AreEqual(expectedContainsResult, containsresult);
        }
        [Test]
        public void CheckBlocked_InputIsEmpty_False()
        {
            string[] evs = new string[] {"event"};

            var expected = false;

            var actual = graph.CheckBlocked(evs);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CheckBlocked_InputIsNotBlocked_False()
        {
            string[] evs = new string[] {"event"};

            var expected = false;

            var actual = graph.CheckBlocked(evs);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CheckBlocked_InputIsBlocked_True()
        {
            string[] evs = new string[] { "event" };
            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            ev.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());

            var expected = true;

            var actual = graph.CheckBlocked(evs);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CheckBlocked_ManyEventsAllBlocked_True()
        {
            string[] evs = new string[5];
            graph.CreateEvent("event", "label", "Node1".Split());

            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEvent("event"+i, "label", "Node1".Split());
                var ev = graph.Events["event"+i];
                evs[i] = ev.Name;
                ev.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            }

            var expected = true;

            var actual = graph.CheckBlocked(evs);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CheckBlocked_ManyEventsOneBlocked_True()
        {
            string[] evs = new string[6];

            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEvent("event" + i, "label", "Node1".Split());
                var ev = graph.Events["event" + i];
                evs[i] = ev.Name;
            }

            graph.CreateEvent("event", "label", "Node1".Split());
            var evBlocked = graph.Events["event"];
            evBlocked.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            evs[5] = evBlocked.Name;

            var expected = true;

            var actual = graph.CheckBlocked(evs);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CheckBlocked_ManyEventNoneBlocked_False()
        {
            string[] evs = new string[5];
            graph.CreateEvent("event", "label", "Node1".Split());

            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEvent("event" + i, "label", "Node1".Split());
                var ev = graph.Events["event" + i];
                evs[i] = ev.Name;
            }

            var expected = false;

            var actual = graph.CheckBlocked(evs);

            Assert.AreEqual(expected, actual);
        }
    }
}
