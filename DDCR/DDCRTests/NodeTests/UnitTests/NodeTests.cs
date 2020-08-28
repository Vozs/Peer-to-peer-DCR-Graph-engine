using NUnit.Framework;
using Moq;
using Node;
using Node.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace NodeTests.UnitTests
{
    class NodeTests
    {
        Mock<IConfig> mockConfig;
        Mock<IClient> mockClient;
        private readonly string[,] commands = new string[,]
{
            {"HELP", "Lists available commands and their usage" },
            {"PERMISSIONS", "Sends a TCP request to the main node for Events which are executable by this node. These events are then added to a local list of known events." },
            {"ACCEPTING", "Sends a TCP request to the main node, to check whether the global graph is in an accepting state. Prints a True or False." },
            {"LOG", "Sends a TCP request to the main node, to receive the global log of all executed events and their timestamps." },
            {"MARKING <Event Name>", "Sends a TCP request to the main node to reply with the current marking of a given event." },
            {"ALLMARKINGS", "Sends a TCP request to the main node to reply with the markings of all its local events" },
            {"EXECUTE <Event Name>", "Sends a TCP request to the main node to execute the provided event. This only works if the event has already been added to the list of local events, using the \"PERMISSION\" command." }
};
        [SetUp]
        public void Setuo()
        {
            mockConfig = new Mock<IConfig>();
            mockClient = new Mock<IClient>();
            Node.Node node = new Node.Node(mockConfig.Object); 
        }

        [Test]
        public void GetEvents_MainNodeReturnsEvents_PrintsEvents()
        {
            // Arrange
            mockClient.Setup(x => x.Send("PERMISSIONS")).Returns("e1 e2");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            // Act
            var result = node.GetEvents();

            // Assert
            var expected = "e1 e2";
            Assert.AreEqual(expected, result);
            Assert.AreEqual(2, node.Events.Length);
            Assert.AreEqual("e1", node.Events[0]);
            Assert.AreEqual("e2", node.Events[1]);

        }

        [Test]
        public void GetEvents_MainNodeReturnsNone_PrintsNoneEventsEmpty()
        {
            // Arrange
            mockClient.Setup(x => x.Send("PERMISSIONS")).Returns("NONE");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);
            // Act
            var result = node.GetEvents();

            // Assert
            var expected = "NONE";
            Assert.AreEqual(expected, result);
            Assert.AreEqual("", node.Events[0]);
        }

        [Test]
        public void Command_PERMISSIONS_ReturnsEvents()
        {
            // Arrange

            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            string expected = "";
            for (int i = 0; i < commands.Length/2; i++)
            {
                expected += string.Format("{0,-10}: {1}\n\n", commands[i, 0], commands[i, 1]);
            }
            var result = node.Command("HELP");

            //Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Command_PERMISSIONS_Stoutput()
        {
            // Arrange
            mockClient.Setup(x => x.Send("PERMISSIONS")).Returns("e1 e2 e3 e4 e5");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.Command("PERMISSIONS");

            //Assert
            var expected = "e1 e2 e3 e4 e5";
            Assert.AreEqual(5, node.Events.Length);
            mockClient.Verify(x => x.Send("PERMISSIONS"), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Command_Log_ReturnsLogWithOneEntry()
        {
            // Arrange
            DateTime time = DateTime.Now;
            mockClient.Setup(x => x.Send("LOG")).Returns(time.ToString() + " e1");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.Command("LOG");

            // Assert
            var expected = time.ToString() + " e1";
            mockClient.Verify(x => x.Send("LOG"), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Command_Accepting_True()
        {
            // Arrange
            mockClient.Setup(x => x.Send("Accepting")).Returns("True");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.Command("Accepting");

            // Assert
            var expected = "True";
            mockClient.Verify(x => x.Send("Accepting"), Times.Once);
            Assert.AreEqual(expected, result);

        }

        [Test]
        public void Command_ValidEvent_True()
        {
            // Arrange
            mockClient.Setup(x => x.Send(It.IsAny<string>())).Returns("True");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);
            node.Events = new string[3] {"e1","e2","e3" };

            //Act
            var result = node.Command("Execute", "e1");

            // Assert
            var expected = "True";
            mockClient.Verify(x => x.Send(It.IsAny<string>()), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Command_InvalidEvent_ReturnsInvalidEvent()
        {
            // Arrange
            mockClient.Setup(x => x.Send(It.IsAny<string>())).Returns("true");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);
            node.Events = new string[3] { "e1", "e2", "e3" };

            //Act
            var result = node.Command("Execute", "e4");

            // Assert
            var expected = "Invalid event (local).";
            mockClient.Verify(x => x.Send(It.IsAny<string>()), Times.Never);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Command_EventsEmpty_ReturnsInvalidEvent()
        {
            // Arrange
            mockClient.Setup(x => x.Send(It.IsAny<string>())).Returns("true");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.Command("Execute", "e4");

            // Assert
            var expected = "Invalid event (local).";
            mockClient.Verify(x => x.Send(It.IsAny<string>()), Times.Never);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void InputTest_InvalidInput_ReturnsInvalidCommand()
        {
            // Arrange
            mockClient.Setup(x => x.Send(It.IsAny<string>())).Returns("true");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);
            //Act
            var input = "gibberish";
            var result = node.HandleInput(input);

            // Assert
            string expected = string.Format("Invalid command: {0} - Enter \"HELP\" to view commands.", input);
            mockClient.Verify(x => x.Send(It.IsAny<string>()), Times.Never);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void InputTest_HelpInput_ReturnHelpList()
        {
            // Arrange
            mockClient.Setup(x => x.Send(It.IsAny<string>())).Returns("true");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.HandleInput("HELP");

            //Assert
            var expected = "";
            for (int i = 0; i < commands.Length/2; i++)
            {
                expected += string.Format("{0,-10}: {1}\n\n", commands[i, 0], commands[i, 1]);
            }
            mockClient.Verify(x => x.Send(It.IsAny<string>()), Times.Never);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void InputTest_PermissionsInput_ReturnsEventName()
        {
            // Arrange
            mockClient.Setup(x => x.Send("PERMISSIONS")).Returns("e1");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.HandleInput("PERMISSIONS");

            //Assert
            var expected = "e1";
            mockClient.Verify(x => x.Send("PERMISSIONS"), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void InputTest_MARKINGSInput_ReturnsMarkingOfEvent()
        {
            // Arrange
            var returnString = string.Format(
                "Included: true\nPending: false\nCondition: 0\nMilestone: 1\nExecuted: False");
            mockClient.Setup(x => x.Send("MARKING")).Returns(returnString);
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.HandleInput("MARKING");

            //Assert
            mockClient.Verify(x => x.Send("MARKING"), Times.Once);
            Assert.AreEqual(returnString, result);
        }

        [Test]
        public void InputTest_ACCEPTINGInput_ReturnsGlobalState()
        {
            // Arrange
            mockClient.Setup(x => x.Send("ACCEPTING")).Returns("True");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.HandleInput("ACCEPTING");

            //Assert
            var expected = "True";
            mockClient.Verify(x => x.Send("ACCEPTING"), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void InputTest_LOGInput_ReturnsLog()
        {
            // Arrange
            mockClient.Setup(x => x.Send("LOG")).Returns("True");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.HandleInput("LOG");

            //Assert
            var expected = "True";
            mockClient.Verify(x => x.Send("LOG"), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void InputTest_ALLMARKINGSInput_ReturnsAllMarkingsForAllEvents()
        {
            // Arrange
            string EventName = "e1";
            string firstReturnString = string.Format("{0," + -EventName.Length + "}\t{1,-5}\t{2,-5}\t{3,-5}\t{4,-5}\t{5,-5}",
                 "Event", "Incl", "Pend", "Cond", "Mile", "Exec");
            string SecondReturnString = string.Format("\n{0," + EventName.Length + "}\t{1,-5}\t{2,-5}\t{3,-5}\t{4,-5}\t{5,-5}",
                "e1", "true", "false", "0", "0", "false");
            mockClient.Setup(x => x.Send("ALLMARKINGS")).Returns(firstReturnString + SecondReturnString);
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);

            //Act
            var result = node.HandleInput("ALLMARKINGS");

            //Assert
            var expected = firstReturnString + SecondReturnString;
            mockClient.Verify(x => x.Send("ALLMARKINGS"), Times.Once);
            Assert.AreEqual(expected, result);
        }


        [Test]
        public void InputTest_ExecuteInput_True()
        {
            // Arrange
            mockClient.Setup(x => x.Send("EXECUTE e1")).Returns("True");
            mockConfig.Setup(x => x.MainNode).Returns(mockClient.Object);
            Node.Node node = new Node.Node(mockConfig.Object);
            node.Events = new string[] { "e1" };

            //Act
            var result = node.HandleInput("EXECUTE e1");

            //Assert
            var expected = "True";
            mockClient.Verify(x => x.Send("EXECUTE e1"), Times.Once);
            Assert.AreEqual(expected, result);
        }
    }
}

