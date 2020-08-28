using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;

namespace DDCRTests.IntegrationTests.GraphLogConfigTests
{/*Tests for
    GetNodeEvents, 
    CreateEvent, 
    CreateEventExternal, 
    AddRelationExternal, 
    AddRelationExternal
    */
    [TestFixture]
    public class EventAndRelationTests
    {
        Graph graph;
        Config config = new Config("TestAssets/integrationtests/test1.ini");

        [SetUp]
        public void Setup()
        {
            Log log = new Log(config);
            graph = new Graph(log, config);
        }

        [Test]
        public void CreateEventTest_CreateEventCheckMarkings_CorrectMarkings()
        {
            //Arrange
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");
            //Act
            graph.CreateEvent("testEvent2", "label", "Node1".Split());

            //Assert
            var expectedEventsContains = true;
            Assert.AreEqual(expectedEventsContains, graph.Events.ContainsKey("testEvent2"));
            EventInternal ev = graph.Events["testEvent2"];

            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expectedIncluded = true;
            var expectedExecuted = false;
            var expectedPending = false;
            Assert.AreEqual(expectedIncluded, ev.Included);
            Assert.AreEqual(expectedExecuted, ev.Executed);
            Assert.AreEqual(expectedPending, ev.Pending);
            var expectedContains = true;

            Assert.AreEqual(expectedContains, graph.NodeEvents[expectedIP].Contains(ev));
        }

        [Test]
        public void CreateEventTest_CreateEventNotIncluded_FalseMarkings()
        {
            //Arrange
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");
            //Act
            graph.CreateEvent("testEvent2", "label", "Node1".Split(), false, true);
            //Assert
            var expectedEventContains = true;
            Assert.AreEqual(expectedEventContains, graph.Events.ContainsKey("testEvent2"));
            EventInternal ev = graph.Events["testEvent2"];

            var expectedIncluded = false;
            var expectedExecuted = false;
            var expectedPending = true;

            Assert.AreEqual(expectedIncluded, ev.Included);
            Assert.AreEqual(expectedExecuted, ev.Executed);
            Assert.AreEqual(expectedPending, ev.Pending);

            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expectedContains = true;

            Assert.AreEqual(expectedContains, graph.NodeEvents[expectedIP].Contains(ev));
        }

        [Test]
        public void GetNodeEventsTest_NoEventsForInput_testEventFromConfig()
        {
            //Arrange
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            var expectedIP = IPAddress.Parse("127.0.0.1");
            var expected = "testEvent";
            //Act
            string result = graph.GetNodeEvents(expectedIP);
            //Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetNodeEventsTest_OneEventForNode_StringOfEventName()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            var expectedIP = IPAddress.Parse("127.0.0.1");
            graph.CreateEvent("testEvent2", "label", "Node1".Split());

            var expected = "testEvent testEvent2";
            string result = graph.GetNodeEvents(expectedIP);
            Assert.AreEqual(expected, result);
        }


        [Test]
        public void GetNodeEventsTest_FiveEventForNode_StringOfEventNames()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            var expectedIP = IPAddress.Parse("127.0.0.1");
            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEvent("testEvent" + i, "label", "Node1".Split());
            }

            string result = graph.GetNodeEvents(expectedIP);
            //testEvent is from the .xml file that is loaded at every test
            string expectedString = "testEvent testEvent0 testEvent1 testEvent2 testEvent3 testEvent4";
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
            for (int i = 0; i <= 4; i++)
            {
                graph.CreateEventExternal("externalEventTest" + i, "label", "peer");
            }

            var expected = true;

            foreach (var ev in graph.EventsExternal)
            {
                Assert.AreEqual(expected, graph.EventsExternal.ContainsKey(ev.Key));
            }
        }

        [Test]
        public void AddRelationExternalTest_ConditionCase_True()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Condition);

            Assert.IsTrue(ev.ConditionsExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_ConditionCase_NotInInit()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split(), false, false);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Condition);

            Assert.IsTrue(ev.ConditionsExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_Response()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Response);

            Assert.IsTrue(ev.ResponsesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestonePendingIncluded_True()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split(), true, true);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);

            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestoneExluded_False()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split(), false, false);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);


            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestoneExludedPending_False()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split(), false, true);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);


            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_MilestoneIncluded_False()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split(), true, false);
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Milestone);

            Assert.IsTrue(ev.MilestonesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_Include()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Include);

            Assert.IsTrue(ev.IncludesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationExternalTest_Exclude()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("event", "label", "Node1".Split());
            var ev = graph.Events["event"];
            graph.CreateEventExternal("externalEvent", "label", "peer");
            var evx = graph.EventsExternal["externalEvent"];

            graph.AddRelationExternal("event", "externalEvent", Relation.Exclude);

            Assert.IsTrue(ev.ExcludesExternal.Contains(evx));
        }

        [Test]
        public void AddRelationInternalTest_ResponseRelation()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split());
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Response);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Responses.Contains(eTo));
        }

        [Test]
        public void AddRelationInternalTest_IncludeRelation()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split());
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Include);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Includes.Contains(eTo));
        }

        [Test]
        public void AddRelationInternalTest_ExcludeRelation()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split());
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Exclude);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Excludes.Contains(eTo));
        }

        [Test]
        public void AddRelationInternalTest_ConditionRelation_Excluded()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split(), false, false);
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Condition);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Conditions.Contains(eTo));
            Assert.IsTrue(eTo.Condition == 0);
        }

        [Test]
        public void AddRelationInternalTest_ConditionRelation_Included()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split(), true, false);
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Condition);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Conditions.Contains(eTo));
            Assert.IsTrue(eTo.Condition == 1);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_Excluded()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split(), false, false);
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 0);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_Included()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");

            graph.CreateEvent("eventFrom", "label", "Node1".Split(), true, false);
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 0);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_Pending()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");
            graph.CreateEvent("eventFrom", "label", "Node1".Split(), false, true);
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 0);
        }

        [Test]
        public void AddRelationInternalTest_MilestoneRelation_IncludedPending()
        {
            graph.Load("TestAssets/integrationtests/EventAndRelationTests.xml");
            graph.CreateEvent("eventFrom", "label", "Node1".Split(), true, true);
            graph.CreateEvent("eventTo", "label", "Node1".Split());
            var eFrom = graph.Events["eventFrom"];
            var eTo = graph.Events["eventTo"];

            

            graph.AddRelationInternal("eventFrom", "eventTo", Relation.Milestone);
            //3 because one is loaded by Graph.Load
            Assert.IsTrue(graph.Events.Count == 3);
            Assert.IsTrue(eFrom.Milestones.Contains(eTo));
            Assert.IsTrue(eTo.Milestone == 1);
        }
    }
}
