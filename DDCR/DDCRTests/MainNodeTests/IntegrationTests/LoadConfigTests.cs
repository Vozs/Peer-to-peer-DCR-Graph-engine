using NUnit.Framework;
using DDCR;
using System;
using DDCR.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using Castle.Core.Internal;

namespace DDCRTests.IntegrationTests
{
    class LoadConfigTests
    {
        Graph graph;
        [Test]
        public void Load_TwoEventsNoRelations_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig1.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph1.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            HashSet<EventInternal> expected = new HashSet<EventInternal> {
                {graph.Events["e1"]},
                {graph.Events["e2"]}
            };
            Assert.AreEqual(true, graph.NodeEvents.ContainsKey(IPAddress.Parse("127.0.0.1")));
            Assert.AreEqual(graph.NodeEvents[IPAddress.Parse("127.0.0.1")], expected);
        }

        [Test]
        public void Load_TwoEventsWithRelations_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig2.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph1Relations.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            HashSet<EventInternal> expected = new HashSet<EventInternal> {
                {graph.Events["e1"]},
                {graph.Events["e2"]}
            };
            Assert.AreEqual(true, graph.Events["e1"].Conditions.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e1"].Excludes.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e1"].Includes.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e1"].Responses.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e1"].Milestones.Contains(graph.Events["e2"]));
            Assert.AreEqual(1, graph.Events["e2"].Condition);
            Assert.AreEqual(true, graph.NodeEvents.ContainsKey(IPAddress.Parse("127.0.0.1")));
            Assert.AreEqual(graph.NodeEvents[IPAddress.Parse("127.0.0.1")], expected);
        }

        [Test]
        public void Load_ThreeEventsConditionResponse_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig3.ini");
            graph = new Graph(new Mock<ILog>().Object, config);
            Mock<IClient> mockClient = new Mock<IClient>();

            graph.Load("TestAssets/integrationtests/TestGraph2.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            HashSet<EventInternal> expected = new HashSet<EventInternal> {
                {graph.Events["e1"]},
                {graph.Events["e2"]},
                {graph.Events["e3"]}
            };
            Assert.AreEqual(1, graph.Events["e2"].Condition);
            Assert.AreEqual(true, graph.Events["e1"].Conditions.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e2"].Responses.Contains(graph.Events["e3"]));
            Assert.AreEqual(true, graph.NodeEvents.ContainsKey(IPAddress.Parse("127.0.0.1")));
            Assert.AreEqual(graph.NodeEvents[IPAddress.Parse("127.0.0.1")], expected);
        }

        [Test]
        public void Load_ThreeEventsConditionResponseFirstExecutedSecondPending_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig3.ini");
            graph = new Graph(new Mock<ILog>().Object, config);
            Mock<IClient> mockClient = new Mock<IClient>();

            graph.Load("TestAssets/integrationtests/TestGraph2ExcutedPending.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            HashSet<EventInternal> expected = new HashSet<EventInternal> {
                {graph.Events["e1"]},
                {graph.Events["e2"]},
                {graph.Events["e3"]}
            };
            Assert.AreEqual(0, graph.Events["e2"].Condition);
            Assert.AreEqual(true, graph.Events["e2"].Pending);
            Assert.AreEqual(true, graph.Events["e1"].Executed);
            Assert.AreEqual(true, graph.Events["e1"].Conditions.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e2"].Responses.Contains(graph.Events["e3"]));
            Assert.AreEqual(true, graph.NodeEvents.ContainsKey(IPAddress.Parse("127.0.0.1")));
            Assert.AreEqual(graph.NodeEvents[IPAddress.Parse("127.0.0.1")], expected);
        }


        [Test]
        public void Load_ThreeEventsMilestonePending_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig3.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph2MilestonePending.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            HashSet<EventInternal> expected = new HashSet<EventInternal> {
                {graph.Events["e1"]},
                {graph.Events["e2"]},
                {graph.Events["e3"]}
            };
            Assert.AreEqual(1, graph.Events["e3"].Condition);
            Assert.AreEqual(true, graph.Events["e1"].Pending);
            Assert.AreEqual(1, graph.Events["e2"].Milestone);
            Assert.AreEqual(true, graph.Events["e1"].Milestones.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e2"].Conditions.Contains(graph.Events["e3"]));
            Assert.AreEqual(true, graph.NodeEvents.ContainsKey(IPAddress.Parse("127.0.0.1")));
            Assert.AreEqual(graph.NodeEvents[IPAddress.Parse("127.0.0.1")], expected);
        }

        [Test]
        public void Load_ThreeEventsConditionExcluded_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig3.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            HashSet<EventInternal> expected = new HashSet<EventInternal> {
                {graph.Events["e1"]},
                {graph.Events["e2"]},
                {graph.Events["e3"]}
            };
            Assert.AreEqual(0, graph.Events["e2"].Condition);
            Assert.AreEqual(true, graph.Events["e1"].Conditions.Contains(graph.Events["e2"]));
            Assert.AreEqual(true, graph.Events["e2"].Includes.Contains(graph.Events["e3"]));
            Assert.AreEqual(true, graph.NodeEvents.ContainsKey(IPAddress.Parse("127.0.0.1")));
            Assert.AreEqual(graph.NodeEvents[IPAddress.Parse("127.0.0.1")], expected);
        }

        [Test]
        public void Load_MultipleNodesOneEventOnEachNode_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig4.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node1"]].Contains("e1"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node2"]].Contains("e2"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node3"]].Contains("e3"));

        }

        [Test]
        public void Load_MultipleNodesOneNodeHasAllEvents_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig5.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node1"]].Contains("e1"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node1"]].Contains("e2"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node1"]].Contains("e3"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node2"]].IsNullOrEmpty());
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node3"]].IsNullOrEmpty());
        }

        [Test]
        public void Load_MultipleNodesNoEventsForAllNodes_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig6.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node1"]].IsNullOrEmpty());
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node2"]].IsNullOrEmpty());
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node3"]].IsNullOrEmpty());

        }

        [Test]
        public void Load_MultipleNodesTwoNodesShareEvent_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig7.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node1"]].Contains("e1"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node2"]].Contains("e1"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node3"]].Contains("e2"));
            Assert.AreEqual(true, config.NodeEventsMapping[config.Nodes["Node3"]].Contains("e3"));
        }

        [Test]
        public void Load_ExternalEvents_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig8.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph3ExternalEvents.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(true, config.MainNodes.ContainsKey("external"));
            Assert.AreEqual(true, graph.Events["e1"].ExcludesExternal.Contains(graph.EventsExternal["x1"]));
            Assert.AreEqual(true, graph.Events["e3"].ConditionsExternal.Contains(graph.EventsExternal["x2"]));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("x1"));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("x2"));

        }

        [Test]
        public void Load_ExternalEventsTwoMainNodes_Success()
        {
            Config config = new Config("TestAssets/configtests/loadConfig9.ini");
            graph = new Graph(new Mock<ILog>().Object, config);

            graph.Load("TestAssets/integrationtests/TestGraph3ExternalEventsTwoMainNodes.xml");
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(1, graph.Events["e3"].Condition);
            Assert.AreEqual(true, config.MainNodes.ContainsKey("external"));
            Assert.AreEqual(true, config.MainNodes.ContainsKey("externalTwo"));
            Assert.AreEqual(true, graph.Events["e1"].ExcludesExternal.Contains(graph.EventsExternal["x1"]));
            Assert.AreEqual(true, graph.Events["e3"].ConditionsExternal.Contains(graph.EventsExternal["x2"]));
            Assert.AreEqual(true, graph.Events["e2"].ResponsesExternal.Contains(graph.EventsExternal["xx1"]));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("x1"));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("x2"));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("xx1"));
            Assert.AreEqual(false, graph.EventsExternal.ContainsKey("xx2")); // No relation to it, thus not known

        }
    }
}
