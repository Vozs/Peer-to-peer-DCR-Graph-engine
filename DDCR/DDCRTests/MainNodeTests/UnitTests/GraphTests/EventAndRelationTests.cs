using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;

namespace DDCRTests.UnitTests
{/*Tests for
    GetNodeEvents, 
    CreateEvent, 
    CreateEventExternal, 
    CreateMainNode, 
    CreateNode, 
    Initialize,
    AddRelationExternal, 
    AddRelationExternal
    */
    public class EventAndRelationTests
    {
        Graph graph;
        Mock<IConfig> MockConfig;
        IConfig config;

        [SetUp]
        public void Setup()
        {
            MockConfig = new Mock<IConfig>();
            config = MockConfig.Object;
            MockConfig.Setup(x => x.ClientTimeoutMs).Returns(1000);
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"node1", IPAddress.Parse("127.0.0.1") }
            };
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.MainNodes).Returns(new Dictionary<string, IClient>());
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);
            graph.Load("graph.xml");
        }

        [Test]
        public void CreateNodeTest_CheckContainersAfterNodeIsAdded_True()
        {
            IPAddress expected = IPAddress.Parse("127.0.0.1");
            var expectedNodesContains = true;
            var expectedNodeEventsContains = true;

            Assert.AreEqual(expectedNodesContains, config.Nodes.ContainsKey("node1"));
            Assert.AreEqual(expected, config.Nodes["node1"]);

            Assert.AreEqual(expectedNodeEventsContains, graph.NodeEvents.ContainsKey(expected));
        }

        [Test]
        public void CreateEventTest_CreateEventCheckMarkings_CorrectMarkings()
        {
            //Arrange
            var expectedIncluded = true;
            var expectedExecuted = false;
            var expectedPending = false;
            var expectedEventsContains = true;
            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expectedContains = true;

            //Act
            graph.CreateEvent("testEvent", "label", "node1".Split());
            
            
            //Assert
            EventInternal ev = graph.Events["testEvent"];
            Assert.AreEqual(expectedEventsContains, graph.Events.ContainsKey("testEvent"));
            Assert.AreEqual(expectedIncluded, ev.Included);
            Assert.AreEqual(expectedExecuted, ev.Executed);
            Assert.AreEqual(expectedPending, ev.Pending);
            

            Assert.AreEqual(expectedContains, graph.NodeEvents[expectedIP].Contains(ev));
        }

        [Test]
        public void CreateEventTest_CreateEventNotIncluded_FalseMarkings()
        {
            //Arrange
            var expectedIncluded = false;
            var expectedExecuted = false;
            var expectedPending = true;
            var expectedEventContains = true;
            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expectedContains = true;

            //Act
            graph.CreateEvent("testEvent", "label", "node1".Split(), false, true);

            //Assert
            EventInternal ev = graph.Events["testEvent"];
            Assert.AreEqual(expectedEventContains, graph.Events.ContainsKey("testEvent"));

            Assert.AreEqual(expectedIncluded, ev.Included);
            Assert.AreEqual(expectedExecuted, ev.Executed);
            Assert.AreEqual(expectedPending, ev.Pending);
            Assert.AreEqual(expectedContains, graph.NodeEvents[expectedIP].Contains(ev));
        }

        [Test]
        public void GetNodeEventsTest_NoEventsForInput_None()
        {
            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expected = "NONE";

            string result = graph.GetNodeEvents(expectedIP);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetNodeEventsTest_OneEventForNode_StringOfEventName()
        {
            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expected = "testEvent";
            graph.CreateEvent("testEvent", "label", "node1".Split());

            
            string result = graph.GetNodeEvents(expectedIP);

            Assert.AreEqual(expected, result);
        }


        [Test]
        public void GetNodeEventsTest_FiveEventForNode_StringOfEventNames()
        {
            var expectedIP = IPAddress.Parse("127.0.0.1");
            string expectedString = "testEvent0 testEvent1 testEvent2 testEvent3 testEvent4";
            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEvent("testEvent" + i, "label", "node1".Split());
            }

            string result = graph.GetNodeEvents(expectedIP);
            
            Assert.AreEqual(expectedString, result);
        }

        [Test]
        public void CreateEventExternalTest_CreateOneEventForOnePeer_True()
        {
            graph.CreateEventExternal("externalEventTest", "label", "peer");

            var expected = true;

            Assert.AreEqual(expected, graph.EventsExternal.ContainsKey("externalEventTest"));
        }

        [Test]
        public void CreateEventExternalTest_CreateFiveEventForOnePeer_True()
        {
            var expected = true;

            //Act
            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEventExternal("externalEventTest" + i, "label", "peer");
            }
            
            //Assert
            foreach (var ev in graph.EventsExternal)
            {
                Assert.AreEqual(expected, graph.EventsExternal.ContainsKey(ev.Key));
            }
        }

        [Test]
        public void AddRelationExternalTest_ConditionCase_True()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Condition);

            //Assert
            Assert.IsTrue(ev.ConditionsExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_ConditionCase_NotInInit()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split(), false, false);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Condition);

            //Assert
            Assert.IsTrue(ev.ConditionsExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_Response()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];
            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Response);
            //Assert
            Assert.IsTrue(ev.ResponsesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestonePendingIncluded_True()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split(), true, true);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];
            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);
            //Assert
            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestoneExluded_False()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split(), false, false);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];
            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);
            //Assert
            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestoneExludedPending_False()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split(), false, true);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];
            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);

            //Assert
            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestoneIncluded_False()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split(), true, false);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];
            //Act

            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);

            //Assert
            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_Include()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Include);
            //Assert
            Assert.IsTrue(ev.IncludesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_Exclude()
        {
            //Arrange
            graph.CreateEvent("event", "label", "node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];
            //Act
            graph.AddRelationExternal("event", "externalEvent", Relation.Exclude);
            //Assert
            Assert.IsTrue(ev.ExcludesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationInternalTest_ResponseRelation()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split());
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            Assert.IsTrue(graph.Events.Count == 2);
            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Response);
            //Assert
            Assert.IsTrue(eFrom.Responses.Contains(eTo));
        }

        [Test]
        public void AddRelationInternalTest_IncludeRelation()
        {
            //Arrange

            graph.CreateEvent("eventFrom", "label", "node1".Split());
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Include);
            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Includes.Contains(eTo));
        }

        [Test]
        public void AddRelationInternalTest_ExcludeRelation()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split());
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            
            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Exclude);
            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Excludes.Contains(eTo));
        }

        [Test]
        public void AddRelationInternalTest_ConditionRelation_Excluded()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split(), false, false);
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Condition);
            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Conditions.Contains(eTo));
            Assert.IsTrue(eTo.Condition == 0);
        }

        [Test]
        public void AddRelationInternalTest_ConditionRelation_Included()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split(), true, false);
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];
            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Condition);
            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Conditions.Contains(eTo));
            Assert.IsTrue(eTo.Condition == 1);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_Excluded()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split(), false, false);
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];
            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);


            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 0);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_Included()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split(), true, false);
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];
            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);

            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 0);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_Pending()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split(), false, true);
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];
            //Act
            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);
            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 0);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_IncludedPending()
        {
            //Arrange
            graph.CreateEvent("eventFrom", "label", "node1".Split(), true, true);
            graph.CreateEvent("eventTo", "label", "node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            //Act

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);
            //Assert
            Assert.IsTrue(graph.Events.Count == 2);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 1);
        }
    }
}
