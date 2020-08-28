using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;

namespace DDCRTests.UnitTests
{
    public class TestAssets
    {
        [Test]
        public void TestConfig_PropertiesOnly()
        {
            //Act
            Config config = new Config("TestAssets/configtests/test1.ini");

            //Assert
            Assert.AreEqual("test1", config.Name);
            Assert.AreEqual(1000, config.ListenPort);
            Assert.AreEqual(1234, config.ClientTimeoutMs);
            Assert.AreEqual(1, config.MaxConnectionAttempts);
            Assert.AreEqual("en-US", config.Culture.Name);
            Assert.AreEqual(0, config.NodeEventsMapping.Count);
            Assert.AreEqual(0, config.MainNodes.Count);
            Assert.AreEqual(0, config.Nodes.Count);
        }

        [Test]
        public void TestConfig_MainNodesOnly()
        {
            //Act
            Config config = new Config("TestAssets/configtests/test2.ini");

            //Assert
            Assert.AreEqual(3, config.MainNodes.Count);
            Assert.AreEqual(IPAddress.Parse("127.0.0.5"), config.MainNodes["mNode5"].IP);
            Assert.AreEqual(4001, config.MainNodes["mNode5"].Port);

            Assert.AreEqual(IPAddress.Parse("127.0.0.6"), config.MainNodes["mNode6"].IP);
            Assert.AreEqual(38, config.MainNodes["mNode6"].Port);

            Assert.AreEqual(IPAddress.Parse("127.0.0.7"), config.MainNodes["mNode7"].IP);
            Assert.AreEqual(5556, config.MainNodes["mNode7"].Port);
        }

        [Test]
        public void TestConfig_NodesOnly()
        {
            //Act
            Config config = new Config("TestAssets/configtests/test3.ini");

            //Assert
            Assert.AreEqual(3, config.Nodes.Count);
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), config.Nodes["Node1"]);
            Assert.AreEqual(IPAddress.Parse("127.0.0.2"), config.Nodes["Node2"]);
            Assert.AreEqual(IPAddress.Parse("127.0.0.3"), config.Nodes["Node3"]);
        }

        [Test]
        public void TestConfig_NodeEventsAndNodes()
        {
            //NodeEvents will not work without Nodes.

            //Act
            Config config = new Config("TestAssets/configtests/test4.ini");

            //Assert
            Assert.AreEqual(3, config.NodeEventsMapping.Count);
            Assert.AreEqual(3, config.Nodes.Count);
            Assert.AreEqual(4, config.NodeEventsMapping[IPAddress.Parse("127.0.0.1")].Count);
            Assert.AreEqual(3, config.NodeEventsMapping[IPAddress.Parse("127.0.0.2")].Count);
            Assert.AreEqual(1, config.NodeEventsMapping[IPAddress.Parse("127.0.0.3")].Count);
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.1")].Contains("e1"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.1")].Contains("e2"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.1")].Contains("e3"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.1")].Contains("e4"));

            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.2")].Contains("someEvent"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.2")].Contains("anotherEvent"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.2")].Contains("aThirdEvent"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.0.3")].Contains("oneEvent"));
        }

        [Test]
        public void TestConfig_MainNodesAndNodes()
        {
            //Act
            Config config = new Config("TestAssets/configtests/test5.ini");

            //Assert
            Assert.AreEqual(3, config.MainNodes.Count);
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), config.MainNodes["mNode1"].IP);
            Assert.AreEqual(4000, config.MainNodes["mNode1"].Port);

            Assert.AreEqual(IPAddress.Parse("127.0.0.2"), config.MainNodes["mNode2"].IP);
            Assert.AreEqual(37, config.MainNodes["mNode2"].Port);

            Assert.AreEqual(IPAddress.Parse("127.0.0.3"), config.MainNodes["mNode3"].IP);
            Assert.AreEqual(5555, config.MainNodes["mNode3"].Port);

            Assert.AreEqual(3, config.Nodes.Count);
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), config.Nodes["Node1"]);
            Assert.AreEqual(IPAddress.Parse("127.0.0.2"), config.Nodes["Node2"]);
            Assert.AreEqual(IPAddress.Parse("127.0.0.3"), config.Nodes["Node3"]);
        }

        [Test]
        public void TestConfig_AllCombined()
        {
            //Act
            Config config = new Config("TestAssets/configtests/test6.ini");

            //Assert

            Assert.AreEqual(2, config.NodeEventsMapping[IPAddress.Parse("127.0.4.0")].Count);
            Assert.AreEqual(1, config.NodeEventsMapping[IPAddress.Parse("127.0.5.0")].Count);
            Assert.AreEqual(1, config.NodeEventsMapping[IPAddress.Parse("127.0.6.0")].Count);

            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.4.0")].Contains("e1"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.4.0")].Contains("e2"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.5.0")].Contains("someEvent"));
            Assert.IsTrue(config.NodeEventsMapping[IPAddress.Parse("127.0.6.0")].Contains("e5"));

            Assert.AreEqual(3, config.MainNodes.Count);
            Assert.AreEqual(IPAddress.Parse("127.0.0.8"), config.MainNodes["mNode1"].IP);
            Assert.AreEqual(4300, config.MainNodes["mNode1"].Port);

            Assert.AreEqual(IPAddress.Parse("127.0.0.9"), config.MainNodes["mNode2"].IP);
            Assert.AreEqual(15, config.MainNodes["mNode2"].Port);

            Assert.AreEqual(IPAddress.Parse("127.0.0.10"), config.MainNodes["mNode3"].IP);
            Assert.AreEqual(3453, config.MainNodes["mNode3"].Port);

            Assert.AreEqual(3, config.Nodes.Count);
            Assert.AreEqual(IPAddress.Parse("127.0.4.0"), config.Nodes["Node1"]);
            Assert.AreEqual(IPAddress.Parse("127.0.5.0"), config.Nodes["Node2"]);
            Assert.AreEqual(IPAddress.Parse("127.0.6.0"), config.Nodes["Node3"]);

        }


    }
}
