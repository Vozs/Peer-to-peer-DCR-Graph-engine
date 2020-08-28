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

namespace DDCRTests.UnitTests
{
    class LoadTests
    {
        Graph graph;
        Mock<IConfig> MockConfig;
        [SetUp]
        public void Setup()
        {
            MockConfig = new Mock<IConfig>();
        }

        [Test]
        public void Load_TwoEventsNoRelations_Success()
        {
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1") }
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], set}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph1.xml");

            //Assert
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
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1") }
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], set}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);
            
            graph.Load("TestAssets/integrationtests/TestGraph1Relations.xml");

            //Assert
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
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1") }
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" },
                {"e3" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], set}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2.xml");
            //Assert
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
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1") }
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" },
                {"e3" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], set}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);

            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2ExcutedPending.xml");
            //Assert
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
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1") }
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" },
                {"e3" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], set}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2MilestonePending.xml");
            //Assert
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
            //Arrange

            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1") }
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" },
                {"e3" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], set}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);

            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            //Assert
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
            //Arrange

            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1")},
                {"Node2", IPAddress.Parse("127.0.0.2")},
                {"Node3", IPAddress.Parse("127.0.0.3")}
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };
            HashSet<string> set = new HashSet<string> {
                {"e1"},
                {"e2" },
                {"e3" }
            };
            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], new HashSet<string>{"e1"}},
                {mockNodes["Node2"], new HashSet<string>{"e2"}},
                {mockNodes["Node3"], new HashSet<string>{"e3"}}
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            //Assert
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node1"]].Contains("e1"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node2"]].Contains("e2"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node3"]].Contains("e3"));

        }

        [Test]
        public void Load_MultipleNodesOneNodeHasAllEvents_Success()
        {
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1")},
                {"Node2", IPAddress.Parse("127.0.0.2")},
                {"Node3", IPAddress.Parse("127.0.0.3")}
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };

            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], new HashSet<string>{"e1", "e2", "e3"}},
                {mockNodes["Node2"], new HashSet<string>()},
                {mockNodes["Node3"], new HashSet<string>()},
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);

            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");

            //Assert
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node1"]].Contains("e1"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node1"]].Contains("e2"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node1"]].Contains("e3"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node2"]].IsNullOrEmpty());
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node3"]].IsNullOrEmpty());

        }

        [Test]
        public void Load_MultipleNodesNoEventsForAllNodes_Success()
        {
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1")},
                {"Node2", IPAddress.Parse("127.0.0.2")},
                {"Node3", IPAddress.Parse("127.0.0.3")}
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };

            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], new HashSet<string>()},
                {mockNodes["Node2"], new HashSet<string>()},
                {mockNodes["Node3"], new HashSet<string>()},
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");

            //Assert

            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node1"]].IsNullOrEmpty());
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node2"]].IsNullOrEmpty());
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node3"]].IsNullOrEmpty());

        }

        [Test]
        public void Load_MultipleNodesTwoNodesShareEvent_Success()
        {
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1")},
                {"Node2", IPAddress.Parse("127.0.0.2")},
                {"Node3", IPAddress.Parse("127.0.0.3")}
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };

            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], new HashSet<string>{"e1"} },
                {mockNodes["Node2"], new HashSet<string>{"e1"}},
                {mockNodes["Node3"], new HashSet<string>{"e2", "e3"} },
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph2ConditionExcluded.xml");
            //Assert
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node1"]].Contains("e1"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node2"]].Contains("e1"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node3"]].Contains("e2"));
            Assert.AreEqual(true, MockConfig.Object.NodeEventsMapping[MockConfig.Object.Nodes["Node3"]].Contains("e3"));
        }

        [Test]
        public void Load_ExternalEvents_Success()
        {
            //Arrange

            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1")},
                {"Node2", IPAddress.Parse("127.0.0.2")},
                {"Node3", IPAddress.Parse("127.0.0.3")}
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object}
            };

            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], new HashSet<string>{"e1"}},
                {mockNodes["Node2"], new HashSet<string>{"e2"}},
                {mockNodes["Node3"], new HashSet<string>{"e3"}},
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act
            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph3ExternalEvents.xml");

            //Assert
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(true, MockConfig.Object.MainNodes.ContainsKey("external"));
            Assert.AreEqual(true, graph.Events["e1"].ExcludesExternal.Contains(graph.EventsExternal["x1"]));
            Assert.AreEqual(true, graph.Events["e3"].ConditionsExternal.Contains(graph.EventsExternal["x2"]));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("x1"));
            Assert.AreEqual(true, graph.EventsExternal.ContainsKey("x2"));

        }

        [Test]
        public void Load_ExternalEventsTwoMainNodes_Success()
        {
            //Arrange
            Mock<IClient> mockClient = new Mock<IClient>();
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"Node1", IPAddress.Parse("127.0.0.1")},
                {"Node2", IPAddress.Parse("127.0.0.2")},
                {"Node3", IPAddress.Parse("127.0.0.3")}
            };
            var mockMainNodes = new Dictionary<string, IClient>
            {
                {"external", mockClient.Object},
                {"externalTwo", mockClient.Object}
            };

            var mockNodeEvents = new Dictionary<IPAddress, HashSet<string>> {
                {mockNodes["Node1"], new HashSet<string>{"e1"}},
                {mockNodes["Node2"], new HashSet<string>{"e2"}},
                {mockNodes["Node3"], new HashSet<string>{"e3"}},
            };
            MockConfig.Setup(x => x.Name).Returns("MainNode");
            MockConfig.Setup(x => x.MainNodes).Returns(mockMainNodes);
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.NodeEventsMapping).Returns(mockNodeEvents);
            //Act

            //Initialize graph
            graph = new Graph(new Mock<ILog>().Object, MockConfig.Object);

            graph.Load("TestAssets/integrationtests/TestGraph3ExternalEventsTwoMainNodes.xml");

            //Assert
            Assert.AreEqual(true, graph.Events.ContainsKey("e1"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e2"));
            Assert.AreEqual(true, graph.Events.ContainsKey("e3"));
            Assert.AreEqual(1, graph.Events["e3"].Condition);
            Assert.AreEqual(true, MockConfig.Object.MainNodes.ContainsKey("external"));
            Assert.AreEqual(true, MockConfig.Object.MainNodes.ContainsKey("externalTwo"));
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
