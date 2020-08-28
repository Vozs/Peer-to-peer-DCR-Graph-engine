using DDCR;
using DDCR.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
/*Tests for the 
* Execute 
* TryExecuteExternal
* UpdateMarkingsInternal*/
namespace DDCRTests.UnitTests
{
    class ExecuteTests
    {
        Graph graph;
        EventInternal ev;
        EventInternal evSecond;
        EventInternal evThird;
        EventExternal eventExternal;
        Mock<IConfig> MockConfig;
        IConfig config => MockConfig.Object;

        [SetUp]
        public void Setup()
        {
            MockConfig = new Mock<IConfig>();
               
            var mockNodes = new Dictionary<string, IPAddress>
            {
                {"node1", IPAddress.Parse("127.0.0.1") }
            };
            MockConfig.Setup(x => x.Nodes).Returns(mockNodes);
            MockConfig.Setup(x => x.MainNodes).Returns(new Dictionary<string, IClient>());

            graph = new Graph(new Mock<ILog>().Object, config);
            graph.Load("graph.xml");
            graph.CreateEvent("ev", "label", "node1".Split());
            graph.CreateEvent("evSecond", "label", "node1".Split());
            graph.CreateEvent("evThird", "label", "node1".Split());
            graph.CreateEventExternal("eventExternal", "label", "peer");

            ev = graph.Events["ev"];
            evSecond = graph.Events["evSecond"];
            evThird = graph.Events["evThird"];
            eventExternal = graph.EventsExternal["eventExternal"];
        }

        [Test]
        public void TryExecuteExternal_SuccessfulExecute_True()
        {
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS").Verifiable();
            config.MainNodes.Add("mainNode", mockClient.Object);
            Guid id = Guid.NewGuid();
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>> {
                {"mainNode", Task.FromResult("")}
            };
            var result = graph.TryExecuteExternal(tasks, id);
            var expected = true;

            mockClient.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryExecuteExternal_PeerReturnsUNAVAVILABLE_False()
        {
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE").Verifiable();
            config.MainNodes.Add("mainNode", mockClient.Object);
            Guid id = Guid.NewGuid();

            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>> {
                {"mainNode", Task.FromResult("")}
            };

            var result = graph.TryExecuteExternal(tasks, id);
            var expected = false;

            mockClient.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryExecuteExternal_SuccessfulExecuteManyPeers_True()
        {
            Guid id = Guid.NewGuid();
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS").Verifiable();
            for (int i = 0; i <= 4; i++)
            {
                config.MainNodes.Add("mainNode" + i, mockClient.Object);
                tasks.Add("mainNode" + i, Task.FromResult(""));
            }

            var result = graph.TryExecuteExternal(tasks, id);
            var expected = true;

            mockClient.VerifyAll();
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryExecuteExternal_ExecuteManyPeersOneFails_False()
        {
            Guid id = Guid.NewGuid();
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();
            var mockClient = new Mock<IClient>();
            var mockClientFail = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("SUCCESS").Verifiable();
            mockClientFail.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE").Verifiable();
            for (int i = 0; i <= 4; i++)
            {
                if (i == 4)
                {
                    config.MainNodes.Add("mainNode" + i, mockClientFail.Object);
                    tasks.Add("mainNode" + i, Task.FromResult(""));
                }
                else
                {
                    config.MainNodes.Add("mainNode" + i, mockClient.Object);
                    tasks.Add("mainNode" + i, Task.FromResult(""));
                }
            }

            var result = graph.TryExecuteExternal(tasks, id);
            var expected = false;

            mockClient.VerifyAll();
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryExecuteExternal_ExecuteManyPeersAllFail_False()
        {
            Guid id = Guid.NewGuid();
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();
            var mockClient = new Mock<IClient>();
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE").Verifiable();
            for (int i = 0; i <= 4; i++)
            {
                config.MainNodes.Add("mainNode" + i, mockClient.Object);
                tasks.Add("mainNode" + i, Task.FromResult(""));
            }

            var result = graph.TryExecuteExternal(tasks, id);
            var expected = false;

            mockClient.VerifyAll();
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TryExecuteExternal_ExecuteNoPeers_True()
        {
            Guid id = Guid.NewGuid();
            Dictionary<string, Task<string>> tasks = new Dictionary<string, Task<string>>();

            var result = graph.TryExecuteExternal(tasks, id);
            var expected = true;

            Assert.AreEqual(expected, result);
        }
        [Test]
        public void Execute_EventIsAlreadyBlocked_False()
        {
            Guid id = Guid.NewGuid();
            ev.Block = new Tuple<bool, Guid>(true, id);

            var result = graph.Execute(ev.Name);

            var expected = false;
            var executed = false;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
            Assert.AreEqual(ev.Block.Item2, id);
        }

        [Test]
        public void Execute_EventIsNotEnabled_False()
        {
            ev.Included = false;

            var result = graph.Execute(ev.Name);

            var expected = false;
            var executed = false;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventInternalEventIsAlreadyBlockedExclude_False()
        {
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Exclude);
            evSecond.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.Execute(ev.Name);

            var expected = false;
            var executed = false;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventInternalEventIsAlreadyBlockedCondition_True()
        {
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Condition);
            evSecond.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.Execute(ev.Name);

            var expected = true;
            var executed = true;
            Assert.AreEqual(executed, ev.Executed);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventInternalEventIsAlreadyBlockedInclude_False()
        {
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Include);
            evSecond.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.Execute(ev.Name);

            var expected = false;
            var executed = false;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventInternalEventIsAlreadyBlockedMilestone_True()
        {
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Milestone);
            evSecond.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.Execute(ev.Name);

            var expected = true;
            var executed = true;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventInternalEventIsAlreadyBlockedResponse_True()
        {
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Response);
            evSecond.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());
            var result = graph.Execute(ev.Name);

            var expected = true;
            var executed = true;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventIsNotEnabledAndInternalEventIsBlocked_False()
        {
            ev.Included = false;
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Exclude);
            evSecond.Block = new Tuple<bool, Guid>(true, Guid.NewGuid());

            var result = graph.Execute(ev.Name);

            var expected = false;
            var executed = false;

            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventNoExternalRelationsPeerMessagesIsZero_True()
        {
            var mockClient = new Mock<IClient>();
            config.MainNodes.Add("peer", mockClient.Object);
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync("UNAVAILABLE").Verifiable();

            var result = graph.Execute(ev.Name);

            var expected = true;
            var executed = true;
            mockClient.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_EventOneExternalRelationExternalBlockFails_False()
        {
            var mockClient = new Mock<IClient>();
            config.MainNodes.Add("peer", mockClient.Object);
            ev.IncludesExternal.Add(eventExternal);
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("UNAVAILABLE")).Verifiable();

            var result = graph.Execute(ev.Name);

            var expected = false;
            var executed = false;

            mockClient.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_SuccessfulWithExternalEvent_False()
        {
            var mockClient = new Mock<IClient>();
            config.MainNodes.Add("peer", mockClient.Object);
            ev.IncludesExternal.Add(eventExternal);
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("SUCCESS")).Verifiable();


            var result = graph.Execute(ev.Name);

            var expected = true;
            var executed = true;

            mockClient.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(3));
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Execute_SuccessfulWithExternalEventAndInternalEvent_False()
        {
            var mockClient = new Mock<IClient>();
            config.MainNodes.Add("peer", mockClient.Object);
            ev.IncludesExternal.Add(eventExternal);
            graph.AddRelationInternal(ev.Name, evSecond.Name, Relation.Response);
            mockClient.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult("SUCCESS")).Verifiable();


            var result = graph.Execute(ev.Name);

            var expected = true;
            var executed = true;

            mockClient.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(3));
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(new Tuple<bool, Guid>(false, Guid.Empty), evSecond.Block);
            Assert.AreEqual(expected, result);
        }
        //TODO: Test executeexternal if statement

        [Test]
        public void UpdateMarkingsInternalTest_Response_Success()
        {
            ev.Responses.Add(evSecond);
            evSecond.Conditions.Add(evThird);


            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(executed, ev.Executed);
        }

        [Test]
        public void UpdateMarkingsInternalTest_SecondResponseThirdMilestone_Success()
        {
            ev.Responses.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = true;
            evSecond.Pending = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(1, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_SecondResponseThirdMilestonePending_Success()
        {
            ev.Responses.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = true;
            evSecond.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_SecondResponseThirdMilestoneExcluded_Success()
        {
            ev.Responses.Add(evSecond);
            evSecond.Milestones.Add(evThird);
            evSecond.Included = true;
            evSecond.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_Includes_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Excludes.Add(evThird);

            evSecond.Included = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesMilestonePending_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = false;
            evSecond.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(1, evThird.Milestone);
            Assert.AreEqual(true, evSecond.Included);

        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesMilestoneNotPending_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Milestones.Add(evThird);
            evSecond.Included = false;
            evSecond.Pending = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesMilestoneIncludedNotPending_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Milestones.Add(evThird);
            evSecond.Included = true;
            evSecond.Pending = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesMilestoneIncludedPending_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = true;
            evSecond.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Pending);
            Assert.AreEqual(0, evThird.Milestone);
            Assert.AreEqual(true, evSecond.Included);

        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesConditionNotExecuted_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = false;
            evSecond.Executed = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
            Assert.AreEqual(1, evThird.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesConditionExecuted_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = false;
            evSecond.Executed = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
            Assert.AreEqual(0, evThird.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesConditionIncludedExecuted_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = true;
            evSecond.Executed = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
            Assert.AreEqual(0, evThird.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_IncludesConditionIncludedNotExecuted_Success()
        {
            ev.Includes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = true;
            evSecond.Executed = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(true, evSecond.Included);
            Assert.AreEqual(0, evThird.Condition);
        }
        [Test]
        public void UpdateMarkingsInternalTest_Excludes_Success()
        {
            ev.Excludes.Add(evSecond);

            evSecond.Included = true;
            evSecond.Executed = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesConditionIncludedExecuted_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = true;
            evSecond.Executed = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(0, evThird.Condition);
        }
        [Test]
        public void UpdateMarkingsInternalTest_ExcludesConditionNotIncludedExecuted_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = false;
            evSecond.Executed = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(0, evThird.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesConditionNotIncludedNotExecuted_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = false;
            evSecond.Executed = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(0, evThird.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesConditionIncludedNotExecuted_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Conditions.Add(evThird);

            evSecond.Included = true;
            evSecond.Executed = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(-1, evThird.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesMilstoneIncludedNotPending_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = true;
            evSecond.Pending = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesMilstoneNotIncludedNotPending_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = false;
            evSecond.Pending = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesMilstoneNotIncludedPending_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = false;
            evSecond.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(0, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ExcludesMilstoneIncludedPending_Success()
        {
            ev.Excludes.Add(evSecond);
            evSecond.Milestones.Add(evThird);

            evSecond.Included = true;
            evSecond.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(false, evSecond.Included);
            Assert.AreEqual(-1, evThird.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ConditionExecuted_Success()
        {
            ev.Conditions.Add(evSecond);

            ev.Executed = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(0, evSecond.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_ConditionNotExecuted_Success()
        {
            ev.Conditions.Add(evSecond);

            ev.Executed = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(-1, evSecond.Condition);
        }

        [Test]
        public void UpdateMarkingsInternalTest_MilestoneNotPending_Success()
        {
            ev.Milestones.Add(evSecond);

            ev.Pending = false;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(0, evSecond.Milestone);
        }

        [Test]
        public void UpdateMarkingsInternalTest_MilestonePending_Success()
        {
            ev.Milestones.Add(evSecond);

            ev.Pending = true;

            var result = graph.Execute(ev.Name);
            var expected = true;
            var executed = true;

            Assert.AreEqual(expected, result);
            Assert.AreEqual(executed, ev.Executed);
            Assert.AreEqual(-1, evSecond.Milestone);
        }



    }
}
