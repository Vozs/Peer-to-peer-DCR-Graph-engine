using Node;
using NUnit.Framework;
using System;

namespace NodeTests.UnitTests
{
    class ConfigTests
    {
        [Test]
        public void Config_EverythingSet_Success()
        {
            // Arrange & Act
            Node.Config config = new Node.Config("TestAssets/configtests/nodetest1.ini");

            // Assert
            Assert.AreEqual("node1", config.Name);
            Assert.AreEqual("127.0.0.1", config.MainNodeIP);
            Assert.AreEqual(5005, config.MainNodePort);
            Assert.AreEqual(10000, config.ClientTimeoutMs);
        }

        [Test]
        public void Config_NameEmpty_Success()
        {
            // Arrange & Act
            Node.Config config = new Node.Config("TestAssets/configtests/nodetest2.ini");

            // Assert
            Assert.AreEqual("", config.Name);
            Assert.AreEqual("127.0.0.1", config.MainNodeIP);
            Assert.AreEqual(5005, config.MainNodePort);
            Assert.AreEqual(10000, config.ClientTimeoutMs);
        }

        [Test]
        public void Config_NoMainNode_ThrowsFormatException()
        {
            // Arrange & Act
            Node.Config config;

            // Assert
            Assert.Throws<FormatException>(delegate { config = new Node.Config("TestAssets/configtests/nodetest3.ini"); }) ;
        }

        [Test]
        public void Config_WrongFormatProducesWrongName_Success()
        {
            // Arrange & Act
            Node.Config config = config = new Node.Config("TestAssets/configtests/nodetest4.ini");

            // Assert
            Assert.AreEqual(" node1", config.Name);
        }
    }
}
