using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using System.Globalization;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using DDCR;
using DDCR.Interfaces;
namespace DDCRTests.SystemTests
{
    class SystemTests
    {
        Config config1;
        Config config2;
        Config config3;
        Log log1;
        Log log2;
        Log log3;
        Graph graph1;
        Graph graph2;
        Graph graph3;
        Parser parser1;
        Parser parser2;
        Parser parser3;
        Listener listener1;
        Listener listener2;
        Listener listener3;



        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void SystemTest1_TwoMainNodes_Success()
        {
            //Arrange (although some logic is done here, e.g. config reading method)
            config1 = new Config("TestAssets/systemtests/SystemTest1/MainNode1.ini");
            config2 = new Config("TestAssets/systemtests/SystemTest1/MainNode2.ini");

            log1 = new Log(config1);
            graph1 = new Graph(log1, config1);
            parser1 = new Parser(graph1, config1);
            listener1 = new Listener(parser1, config1);
            var cancellationTokenSource = new CancellationTokenSource();
            var t = Task.Factory.StartNew(() =>
            {
                listener1.Listen();
            }, cancellationTokenSource.Token).ContinueWith(task => cancellationTokenSource.Token);

            log2 = new Log(config2);
            graph2 = new Graph(log2, config2);
            parser2 = new Parser(graph2, config2);
            listener2 = new Listener(parser2, config2);

            var t2 = Task.Factory.StartNew(() =>
            {
                listener2.Listen();
            }, cancellationTokenSource.Token).ContinueWith(task => cancellationTokenSource.Token);


            graph1.Load("TestAssets/systemtests/SystemTest1/SystemTest1.xml");
            graph2.Load("TestAssets/systemtests/SystemTest1/SystemTest1.xml");



            Node.Config nodeConfig1 = new Node.Config("TestAssets/systemtests/SystemTest1/Node1.ini");
            Node.Config nodeConfig2 = new Node.Config("TestAssets/systemtests/SystemTest1/Node2.ini");

            Node.Node node1 = new Node.Node(nodeConfig1);
            Node.Node node2 = new Node.Node(nodeConfig2);

            //Act
            var executableEventsN1 = node1.HandleInput("PERMISSIONS");
            var executableEventsN2 = node2.HandleInput("PERMISSIONS");


            //Assert
            Assert.AreEqual("request approve reject", executableEventsN1);
            Assert.AreEqual("document pay", executableEventsN2);

            //Assert all markings after initial setup.
            Assert.AreEqual(true, graph1.Events["request"].Included);
            Assert.AreEqual(true, graph1.Events["approve"].Included);
            Assert.AreEqual(true, graph1.Events["reject"].Included);
            Assert.AreEqual(true, graph2.Events["document"].Included);
            Assert.AreEqual(true, graph2.Events["pay"].Included);

            Assert.AreEqual(0, graph1.Events["request"].Condition);
            Assert.AreEqual(1, graph1.Events["approve"].Condition);
            Assert.AreEqual(1, graph1.Events["reject"].Condition);
            Assert.AreEqual(1, graph2.Events["document"].Condition);
            Assert.AreEqual(1, graph2.Events["pay"].Condition);

            Assert.AreEqual(0, graph1.Events["request"].Milestone);
            Assert.AreEqual(0, graph1.Events["approve"].Milestone);
            Assert.AreEqual(0, graph1.Events["reject"].Milestone);
            Assert.AreEqual(0, graph2.Events["document"].Milestone);
            Assert.AreEqual(0, graph2.Events["pay"].Milestone);

            // Attempt to execute Request unemployment benefit
            var executeRequest = node1.HandleInput("EXECUTE request");

            Task wait = Task.Delay(1000); //Create delay for next event execution, so that the log dates aren't the same.
            Assert.AreEqual("True", executeRequest);
            Assert.AreEqual(true, graph1.Events["request"].Executed);
            Assert.AreEqual(0, graph1.Events["approve"].Condition);
            Assert.AreEqual(0, graph1.Events["reject"].Condition);

            // Attempt to execute Pay benefits
            var executePay = node2.HandleInput("EXECUTE pay");
            Assert.AreEqual("False", executePay);

            wait.Wait(); //Wait the created delay (this is also done, two more times)
            // Attempt to execute Approve
            var executeApprove = node1.HandleInput("EXECUTE approve");
            wait = Task.Delay(1000);
            Assert.AreEqual("True", executeApprove);
            Assert.AreEqual(true, graph1.Events["approve"].Executed);
            Assert.AreEqual(false, graph1.Events["approve"].Included);
            Assert.AreEqual(false, graph1.Events["reject"].Included);
            Assert.AreEqual(false, graph1.Events["request"].Included);
            Assert.AreEqual(0, graph2.Events["document"].Condition);

            wait.Wait();
            // Attempt to execute Document monthly job-search & activation
            var executedocument = node2.HandleInput("EXECUTE document");
            wait = Task.Delay(1000);
            Assert.AreEqual("True", executedocument);
            Assert.AreEqual(true, graph2.Events["document"].Executed);
            Assert.AreEqual(0, graph2.Events["pay"].Condition);
            Assert.AreEqual(true, graph2.Events["pay"].Included);
            Assert.AreEqual(true, graph2.Events["pay"].Pending);

            // Get accepting state
            var acceptingV1 = node1.HandleInput("accepting");
            Assert.AreEqual("False", acceptingV1);

            wait.Wait();
            // Attempt to execute the last event - Pay benefits
            var executePayV2 = node2.HandleInput("EXECUTE pay");
            Assert.AreEqual("True", executePayV2);
            Assert.AreEqual(true, graph2.Events["pay"].Executed);
            Assert.AreEqual(false, graph2.Events["pay"].Pending);
            Assert.AreEqual(false, graph2.Events["pay"].Included);

            // Get accepting state
            var acceptingV2 = node1.HandleInput("accepting");
            Assert.AreEqual("True", acceptingV2);

            // Get log of executed events
            var actualNode1Log = node1.HandleInput("log");
            var logLines = actualNode1Log.Split('\n');
            var expLog1 = "request";
            var expLog2 = "approve";
            var expLog3 = "document";
            var expLog4 = "pay";

            var actLog1 = logLines[0].Substring(logLines[0].Length - expLog1.Length);
            var actLog2 = logLines[1].Substring(logLines[1].Length - expLog2.Length);
            var actLog3 = logLines[2].Substring(logLines[2].Length - expLog3.Length);
            var actLog4 = logLines[3].Substring(logLines[3].Length - expLog4.Length);

            Assert.AreEqual(expLog1, actLog1);
            Assert.AreEqual(expLog2, actLog2);
            Assert.AreEqual(expLog3, actLog3);
            Assert.AreEqual(expLog4, actLog4);

            //Cancel listener threads
            cancellationTokenSource.Cancel();
        }

        [Test]
        public void SystemTest2_ThreeMainNodesContinuousExecutionOfEvents_Success()
        {
            //Arrange (although some logic is done here, e.g. config reading method)
            config1 = new Config("TestAssets/systemtests/SystemTest2/MainNode1.ini");
            config2 = new Config("TestAssets/systemtests/SystemTest2/MainNode2.ini");
            config3 = new Config("TestAssets/systemtests/SystemTest2/MainNode3.ini");

            log1 = new Log(config1);
            graph1 = new Graph(log1, config1);
            parser1 = new Parser(graph1, config1);
            listener1 = new Listener(parser1, config1);
            var cancellationTokenSource2 = new CancellationTokenSource();
            var t = Task.Factory.StartNew(() =>
            {
                listener1.Listen();
            }, cancellationTokenSource2.Token).ContinueWith(task => cancellationTokenSource2.Token);

            log2 = new Log(config2);
            graph2 = new Graph(log2, config2);
            parser2 = new Parser(graph2, config2);
            listener2 = new Listener(parser2, config2);
            var t2 = Task.Factory.StartNew(() =>
            {
                listener2.Listen();
            }, cancellationTokenSource2.Token).ContinueWith(task => cancellationTokenSource2.Token);

            log3 = new Log(config3);
            graph3 = new Graph(log3, config3);
            parser3 = new Parser(graph3, config3);
            listener3 = new Listener(parser3, config3);
            var t3 = Task.Factory.StartNew(() =>
            {
                listener3.Listen();
            }, cancellationTokenSource2.Token).ContinueWith(task => cancellationTokenSource2.Token);

            graph1.Load("TestAssets/systemtests/SystemTest2/SystemTest2.xml");
            graph2.Load("TestAssets/systemtests/SystemTest2/SystemTest2.xml");
            graph3.Load("TestAssets/systemtests/SystemTest2/SystemTest2.xml");


            Node.Config nodeConfig1 = new Node.Config("TestAssets/systemtests/SystemTest2/Node1.ini");
            Node.Config nodeConfig2 = new Node.Config("TestAssets/systemtests/SystemTest2/Node2.ini");
            Node.Config nodeConfig3 = new Node.Config("TestAssets/systemtests/SystemTest2/Node3.ini");

            Node.Node node1 = new Node.Node(nodeConfig1);
            Node.Node node2 = new Node.Node(nodeConfig2);
            Node.Node node3 = new Node.Node(nodeConfig3);

            //Act
            var executableEventsN1 = node1.HandleInput("PERMISSIONS");
            var executableEventsN2 = node2.HandleInput("PERMISSIONS");
            var executableEventsN3 = node3.HandleInput("PERMISSIONS");


            //Assert
            Assert.AreEqual("gdpr sign first", executableEventsN1);
            Assert.AreEqual("need", executableEventsN2);
            Assert.AreEqual("order receive", executableEventsN3);
            // Assert Markings before execution
            Assert.AreEqual(true, graph1.Events["gdpr"].Included);
            Assert.AreEqual(true, graph1.Events["sign"].Included);
            Assert.AreEqual(true, graph1.Events["first"].Included);
            Assert.AreEqual(true, graph2.Events["need"].Included);
            Assert.AreEqual(true, graph3.Events["order"].Included);
            Assert.AreEqual(true, graph3.Events["receive"].Included);
            Assert.AreEqual(1, graph1.Events["sign"].Condition);
            Assert.AreEqual(1, graph1.Events["first"].Condition);
            Assert.AreEqual(0, graph1.Events["sign"].Milestone);
            Assert.AreEqual(1, graph3.Events["order"].Condition);

            // Execute GDPR Consent and assert markings has changed as expected.
            var executegdpr = node1.HandleInput("EXECUTE gdpr");

            Assert.AreEqual("True", executegdpr);
            Assert.AreEqual(true, graph1.Events["gdpr"].Executed);
            Assert.AreEqual(0, graph1.Events["sign"].Condition);

            // Attempt to execute First day at work, although it has Condition = 1.
            var executeFirst = node1.HandleInput("EXECUTE first");
            Assert.AreEqual("False", executeFirst);

            // Attempt to execute Sign Contract.
            var executeSign = node1.HandleInput("EXECUTE sign");
            Assert.AreEqual("True", executeSign);
            Assert.AreEqual(true, graph1.Events["sign"].Executed);
            Assert.AreEqual(0, graph3.Events["order"].Condition);
            Assert.AreEqual(0, graph1.Events["first"].Condition);

            // Attempt to execute Need for PC.
            var executeNeed = node2.HandleInput("EXECUTE need");
            Assert.AreEqual("True", executeNeed);
            Assert.AreEqual(true, graph2.Events["need"].Executed);
            Assert.AreEqual(true, graph3.Events["order"].Pending);
            Assert.AreEqual(1, graph1.Events["first"].Milestone);
            Assert.AreEqual(true, graph3.Events["order"].Included);


            // Attempt to execute Order PC.
            var executeOrder = node3.HandleInput("EXECUTE order");
            Assert.AreEqual("True", executeOrder);
            Assert.AreEqual(true, graph3.Events["order"].Executed);
            Assert.AreEqual(false, graph3.Events["order"].Pending);

            // Attempt to execute Receive PC.
            var executeReceive = node3.HandleInput("EXECUTE receive");
            Assert.AreEqual("True", executeReceive);
            Assert.AreEqual(true, graph3.Events["receive"].Executed);
            Assert.AreEqual(false, graph3.Events["order"].Included);

            // Attempt to execute First day at work.
            var executeFirstV2 = node1.HandleInput("EXECUTE first");
            Assert.AreEqual("True", executeFirstV2);
            Assert.AreEqual(true, graph1.Events["first"].Executed);
            Assert.AreEqual(false, graph1.Events["first"].Included);

            // Query if the graph is accepting
            var accepting = node1.HandleInput("accepting");
            Assert.AreEqual("True", accepting);

            //Cancel listener threads
            cancellationTokenSource2.Cancel();
        }




        [Test]
        public void SystemTest3_DeadLockGraph_ExecutionsFalse()
        {
            //Arrange (although some logic is done here, e.g. config reading method)
            config1 = new Config("TestAssets/systemtests/SystemTest3/deadlock1.ini");
            config2 = new Config("TestAssets/systemtests/SystemTest3/deadlock2.ini");

            log1 = new Log(config1);
            graph1 = new Graph(log1, config1);
            parser1 = new Parser(graph1, config1);
            listener1 = new Listener(parser1, config1);
            var cancellationTokenSource3 = new CancellationTokenSource();
            var t = Task.Factory.StartNew(() =>
            {
                listener1.Listen();
            }, cancellationTokenSource3.Token).ContinueWith(task => cancellationTokenSource3.Token);

            log2 = new Log(config2);
            graph2 = new Graph(log2, config2);
            parser2 = new Parser(graph2, config2);
            listener2 = new Listener(parser2, config2);
            var t2 = Task.Factory.StartNew(() =>
            {
                listener2.Listen();
            }, cancellationTokenSource3.Token).ContinueWith(task => cancellationTokenSource3.Token);

            graph1.Load("TestAssets/systemtests/SystemTest3/deadlock.xml");
            graph2.Load("TestAssets/systemtests/SystemTest3/deadlock.xml");


            Node.Config nodeConfig1 = new Node.Config("TestAssets/systemtests/SystemTest3/deadlocknode1.ini");
            Node.Config nodeConfig2 = new Node.Config("TestAssets/systemtests/SystemTest3/deadlocknode2.ini");

            Node.Node node1 = new Node.Node(nodeConfig1);
            Node.Node node2 = new Node.Node(nodeConfig2);

            //Act
            var executableEventsN1 = node1.HandleInput("PERMISSIONS");
            var executableEventsN2 = node2.HandleInput("PERMISSIONS");

            //Assert
            Assert.AreEqual("e1", executableEventsN1);
            Assert.AreEqual("x2 x1", executableEventsN2);

            //Act (Check all markings are as expected)
            string expectede1Markings = "Included: True\nPending: True\nCondition: 1\nMilestone: 0\nExecuted: False";
            string expectedx1Markings = "Included: True\nPending: False\nCondition: 1\nMilestone: 0\nExecuted: False";
            string expectedx2Markings = "Included: True\nPending: False\nCondition: 1\nMilestone: 0\nExecuted: False";
            string actuale1Markings = node1.HandleInput("MARKING e1");
            string actualx1Markings = node2.HandleInput("MARKING x1");
            string actualx2Markings = node2.HandleInput("MARKING x2");

            //Assert
            Assert.AreEqual(expectede1Markings, actuale1Markings);
            Assert.AreEqual(expectedx1Markings, actualx1Markings);
            Assert.AreEqual(expectedx2Markings, actualx2Markings);

            //Act (Check all events cannot be executed)
            string actuale1Execute = node1.HandleInput("EXECUTE e1");
            string actualx1Execute = node2.HandleInput("EXECUTE x1");
            string actualx2Execute = node2.HandleInput("EXECUTE x2");

            //Assert
            Assert.AreEqual("False", actuale1Execute);
            Assert.AreEqual("False", actualx1Execute);
            Assert.AreEqual("False", actualx2Execute);

            //Act (Check that graph is accepting, no matter which node requests it).
            string actualNode1Accepting = node1.HandleInput("Accepting");
            string actualNode2Accepting = node2.HandleInput("Accepting");

            //Assert
            Assert.AreEqual(actualNode1Accepting, actualNode2Accepting);
            Assert.AreEqual("False", actualNode1Accepting);

            //Act try to get log (should be empty)
            string actualNode1Log = node1.HandleInput("LOG");
            string actualNode2Log = node2.HandleInput("LOG");

            //Assert
            Assert.AreEqual(actualNode1Log, actualNode2Log);
            Assert.AreEqual("LOG EMPTY", actualNode1Log);

            //Act (Check ALLMARKINGS works properly)
            string expectedGetAllMarkingsNode2 =
                "Event	Incl 	Pend 	Cond 	Mile 	Exec \n" +
                "x2   	True 	False	1    	0    	False\n" +
                "x1   	True 	False	1    	0    	False";
            string actualGetAllMarkingsNode2 = node2.HandleInput("ALLMARKINGS");

            //Assert
            Assert.AreEqual(expectedGetAllMarkingsNode2, actualGetAllMarkingsNode2);

            //Cancel listener threads
            cancellationTokenSource3.Cancel();
        }

        [Test]
        public void SystemTest2_MainNode3Dies_NothingHappens()
        {
            //Arrange (although some logic is done here, e.g. config reading method)
            config1 = new Config("TestAssets/systemtests/SystemTest2/MainNode1-2.ini");
            config2 = new Config("TestAssets/systemtests/SystemTest2/MainNode2-2.ini");
            config3 = new Config("TestAssets/systemtests/SystemTest2/MainNode3-2.ini");


            log1 = new Log(config1);
            graph1 = new Graph(log1, config1);
            parser1 = new Parser(graph1, config1);
            listener1 = new Listener(parser1, config1);
            var cancellationTokenSource4 = new CancellationTokenSource();
            
            var t = Task.Factory.StartNew(() =>
            {
                listener1.Listen();
            }, cancellationTokenSource4.Token).ContinueWith(task => cancellationTokenSource4.Token);

            log2 = new Log(config2);
            graph2 = new Graph(log2, config2);
            parser2 = new Parser(graph2, config2);
            listener2 = new Listener(parser2, config2);
            var t2 = Task.Factory.StartNew(() =>
            {
                listener2.Listen();
            }, cancellationTokenSource4.Token).ContinueWith(task => cancellationTokenSource4.Token);

            log3 = new Log(config3);
            graph3 = new Graph(log3, config3);
            parser3 = new Parser(graph3, config3);
            listener3 = new Listener(parser3, config3);


            graph1.Load("TestAssets/systemtests/SystemTest2/SystemTest2.xml");
            graph2.Load("TestAssets/systemtests/SystemTest2/SystemTest2.xml");
            graph3.Load("TestAssets/systemtests/SystemTest2/SystemTest2.xml");

            Node.Config nodeConfig1 = new Node.Config("TestAssets/systemtests/SystemTest2/Node1-2.ini");
            Node.Config nodeConfig2 = new Node.Config("TestAssets/systemtests/SystemTest2/Node2-2.ini");
            Node.Config nodeConfig3 = new Node.Config("TestAssets/systemtests/SystemTest2/Node3-2.ini");

            Node.Node node1 = new Node.Node(nodeConfig1);
            Node.Node node2 = new Node.Node(nodeConfig2);
            Node.Node node3 = new Node.Node(nodeConfig3);

            //Act
            var executableEventsN1 = node1.HandleInput("PERMISSIONS");
            var executableEventsN2 = node2.HandleInput("PERMISSIONS");
            var executableEventsN3 = node3.HandleInput("PERMISSIONS");


            //Assert
            Assert.AreEqual("gdpr sign first", executableEventsN1);
            Assert.AreEqual("need", executableEventsN2);
            Assert.AreEqual("UNAVAILABLE", executableEventsN3);

            //Act (execute gdpr)
            var gdprExecute = node1.HandleInput("execute gdpr");

            //Assert
            Assert.AreEqual("True", gdprExecute);

            //Act (check that Sign contract markings are correct and by extension that is should be executable)
            var actualSignMarkings = node1.HandleInput("marking sign");
            //This event has markings that need to be changed (condition decremented) if Sign Contract is executed.
            var expectedSignMarkings = "Included: True\nPending: False\nCondition: 0\nMilestone: 0\nExecuted: False";

            //Assert it is enabled - it should be executable.
            Assert.AreEqual(expectedSignMarkings, actualSignMarkings);
            Assert.AreEqual(true, graph1.Events["sign"].Enabled());

            //Act - Try to execute Sign Contract. Should fail
            var signExecute = node1.HandleInput("execute sign");
            var MainNode3RequestTest = node3.HandleInput("LOG");
            //Assert that MainNode1 replies UNAVAILABLE, since it cannot propagate execution/markings.
            Assert.AreEqual("False", signExecute);
            //Assert that Node3 gets no reply
            Assert.AreEqual("UNAVAILABLE", MainNode3RequestTest);
            
            //Act - Start Listener. "sign" should executable now, as new markings can be propagated.
            var t3 = Task.Factory.StartNew(() =>
            {
                listener3.Listen();
            }, cancellationTokenSource4.Token).ContinueWith(task => cancellationTokenSource4.Token);

            var signExecute2 = node1.HandleInput("execute sign");

            //Assert that MainNode1 replies true now,
            Assert.AreEqual("True", signExecute2);
            
            cancellationTokenSource4.Cancel();
        }

    }
}
