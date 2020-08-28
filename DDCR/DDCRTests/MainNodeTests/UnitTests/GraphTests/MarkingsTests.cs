using DDCR;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
/*Tests for 
 * GetNewMarkingsReversible, 
 * ChangeMarkings*/
namespace DDCRTests.UnitTests
{
    class MarkingsTests
    {
        Graph graph;
        [SetUp]
        public void Setup()
        {
            var config = new Mock<IConfig>();
            Mock<Log> mockLog = new Mock<Log>(config.Object);
            config.Setup(x => x.Nodes).Returns(new Dictionary<string, IPAddress>());
            graph = new Graph(mockLog.Object,config.Object);
        }


        [Test]
        public void GetNewMarkingsReversibe_ResponseMilestone_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", false, true);
            EventInternal e3 = new EventInternal("e3", "l3", true, true);
            ev.Responses.Add(e2);
            e2.Milestones.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_ResponseMilestone_Nothing2()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", false, true);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Responses.Add(e2);
            e2.Milestones.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_ResponseMilestone_TwoActions()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Responses.Add(e2);
            e2.Milestones.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e3, Relation.Milestone, true));
            resultExpected.Add((e2, Relation.Response, true));

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_ResponseMilestone_PendingAction()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            ev.Responses.Add(e2);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e2, Relation.Response, true));

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_IncludeAlreadyIncluded_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Includes.Add(e2);
            e2.Conditions.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }
        [Test]
        public void GetNewMarkingsReversibe_IncludeCondition_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            e2.Executed = true;
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Includes.Add(e2);
            e2.Conditions.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_IncludeCondition_ConditionAction()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", false, false);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Includes.Add(e2);
            e2.Conditions.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e3, Relation.Condition, true));
            resultExpected.Add((e2, Relation.Include, true));
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_IncludeCondition_MilestoneAction()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", false, true);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Includes.Add(e2);
            e2.Milestones.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e3, Relation.Milestone, true));
            resultExpected.Add((e2, Relation.Include, true));
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }


        [Test]
        public void GetNewMarkingsReversibe_ExcludeCondition_TwoActions()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Excludes.Add(e2);
            e2.Conditions.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e3, Relation.Condition, false));
            resultExpected.Add((e2, Relation.Exclude, true));
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_ExcludeAlreadyExecutedCondition_ExludeAction()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            e2.Executed = true;
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Excludes.Add(e2);
            e2.Conditions.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e2, Relation.Exclude, true));
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_ExcludeAlreadyExcludedCondition_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", false, false);
            EventInternal e3 = new EventInternal("e3", "l3", true, false);
            ev.Excludes.Add(e2);
            e2.Conditions.Add(e3);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_ConditionIncluded_DecrementConditionAction()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            ev.Conditions.Add(e2);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e2, Relation.Condition, false));
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }
        [Test]
        public void GetNewMarkingsReversibe_ConditionNotIncluded_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            ev.Included = false;
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            ev.Conditions.Add(e2);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }
        [Test]
        public void GetNewMarkingsReversibe_MilestoneIncluded_DecrementMilestoneAction()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            ev.Pending = true;

            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            ev.Milestones.Add(e2);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            resultExpected.Add((e2, Relation.Milestone, false));
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }
        [Test]
        public void GetNewMarkingsReversibe_MilestoneNotPending_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            ev.Pending = false;

            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            ev.Milestones.Add(e2);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();

            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void GetNewMarkingsReversibe_MilestoneNotIncluded_Nothing()
        {
            //Arrange
            //- Arrage test event and relations
            EventForeign ev = new EventForeign();
            ev.Pending = true;
            ev.Included = false;
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            ev.Milestones.Add(e2);

            // -Arrange expected result
            List<(EventInternal, Relation, bool)> resultExpected =
                new List<(EventInternal, Relation, bool)>();
            //Act
            var result = graph.GetNewMarkingsReversible(ev);

            //Assert
            Assert.AreEqual(resultExpected, result);
        }

        [Test]
        public void ChangeMarkings_Condition_CondPlus()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Condition, true)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(1, e2.Condition);
        }

        [Test]
        public void ChangeMarkings_Condition_CondMinus()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Condition, false)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(-1, e2.Condition);
        }

        [Test]
        public void ChangeMarkings_Condition_CondMinus2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Condition, true)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(-1, e2.Condition);
        }

        [Test]
        public void ChangeMarkings_Condition_CondPlus2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Condition, false)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(1, e2.Condition);
        }

        [Test]
        public void ChangeMarkings_Response_PendingTrue()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Response, true)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(true, e2.Pending);
        }

        [Test]
        public void ChangeMarkings_Response_PendingTrue2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Response, false)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(true, e2.Pending);
        }

        [Test]
        public void ChangeMarkings_Response_PendingFalse()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Response, true)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(false, e2.Pending);
        }

        [Test]
        public void ChangeMarkings_Response_PendingFalse2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Response, false)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(false, e2.Pending);
        }

        [Test]
        public void ChangeMarkings_Milestone_MilestonePlus()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Milestone, true)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(1, e2.Milestone);
        }

        [Test]
        public void ChangeMarkings_Milestone_MilestoneMinus()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Milestone, false)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(-1, e2.Milestone);
        }

        [Test]
        public void ChangeMarkings_Milestone_MilestoneMinus2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Milestone, true)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(-1, e2.Milestone);
        }

        [Test]
        public void ChangeMarkings_Milestone_MilestonePlus2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Milestone, false)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(1, e2.Milestone);
        }

        [Test]
        public void ChangeMarkings_Include_IncludedTrue()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Include, true)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(true, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Include_IncludedTrue2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Include, false)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(true, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Include_IncludedFalse()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Include, true)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(false, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Include_IncludedFalse2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Include, false)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(false, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Exclude_IncludedTrue()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Exclude, false)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(true, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Exclude_IncludedTrue2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Exclude, true)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(true, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Exclude_IncludedFalse()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Exclude, false)},
            };

            //Act
            graph.ChangeMarkings(ev, true);

            //Assert
            Assert.AreEqual(false, e2.Included);
        }

        [Test]
        public void ChangeMarkings_Exclude_IncludedFalse2()
        {
            //Arrange
            EventForeign ev = new EventForeign();
            EventInternal e2 = new EventInternal("e2", "l2", true, false);
            graph.Events.Add(e2.Name, e2);
            ev.actions = new List<(EventInternal, Relation, bool)>
            {
                {(e2, Relation.Exclude, true)},
            };

            //Act
            graph.ChangeMarkings(ev, false);

            //Assert
            Assert.AreEqual(false, e2.Included);
        }


        [Test]
        public void GetMarking_AllTrueOr1_ResultingMarking()
        {
            //Arrange
            EventInternal e1 = new EventInternal("e1", "l1", true, true, true);
            e1.Condition++;
            e1.Milestone++;
            graph.Events.Add(e1.Name, e1);
            //Act
            var result = graph.GetMarking(e1);
            //Assert
            var sResult = result.Split('\n');
            Assert.AreEqual("Included: True", sResult[0]);
            Assert.AreEqual("Pending: True", sResult[1]);
            Assert.AreEqual("Condition: 1", sResult[2]);
            Assert.AreEqual("Milestone: 1", sResult[3]);
            Assert.AreEqual("Executed: True", sResult[4]);
        }

        [Test]
        public void GetMarking_AllFalseOr0_ResultingMarking()
        {
            //Arrange
            EventInternal e2 = new EventInternal("e2", "l2", false, false, false);
            graph.Events.Add(e2.Name, e2);
            //Act
            var result = graph.GetMarking(e2);
            //Assert
            var sResult = result.Split('\n');
            Assert.AreEqual("Included: False", sResult[0]);
            Assert.AreEqual("Pending: False", sResult[1]);
            Assert.AreEqual("Condition: 0", sResult[2]);
            Assert.AreEqual("Milestone: 0", sResult[3]);
            Assert.AreEqual("Executed: False", sResult[4]);
        }

        [Test]
        public void GetAllMarkings_1Event_ResultingMarkings()
        {
            //Arrange
            EventInternal e1 = new EventInternal("e1", "l1", false, true, false);
            e1.Condition = 2;
            e1.Milestone = 3;
            graph.Events.Add(e1.Name, e1);
            //Act
            var result = graph.GetAllMarkings();
            //Assert
            string[] sResult = result.Split('\n');
            string[] expected = new string[]
            {
                "Event\tIncl \tPend \tCond \tMile \tExec ",
                "e1   \tFalse\tTrue \t2    \t3    \tFalse",
            };
            Assert.AreEqual(expected[0], sResult[0]);
            Assert.AreEqual(expected[1], sResult[1]);
        }

        [Test]
        public void GetAllMarkings_2Events_ResultingMarkings()
        {
            //Arrange
            EventInternal e1 = new EventInternal("e1", "l1", true, true, true);
            e1.Condition++;
            e1.Milestone++;
            EventInternal e2 = new EventInternal("e2", "l2", false, false, false);
            graph.Events.Add(e1.Name, e1);
            graph.Events.Add(e2.Name, e2);
            //Act
            var result = graph.GetAllMarkings();
            //Assert
            string[] sResult = result.Split('\n');
            string[] expected = new string[]
            {
                "Event	Incl 	Pend 	Cond 	Mile 	Exec ",
                "e1   	True 	True 	1    	1    	True ",
                "e2   	False	False	0    	0    	False"
            };
            Assert.AreEqual(expected[0], sResult[0]);
            Assert.AreEqual(expected[1], sResult[1]);
            Assert.AreEqual(expected[2], sResult[2]);
        }

        [Test]
        public void GetAllMarkings_VeryLargeName_IndentationMatchesLargeEventName()
        {
            //Arrange
            EventInternal e1 = new EventInternal("e1", "l1", true, true, true);
            e1.Condition++;
            e1.Milestone++;
            EventInternal e2 = new EventInternal("e2", "l2", false, false, false);
            EventInternal e3 = new EventInternal("aVeryLargeEventName", "l2", true, false, false);
            graph.Events.Add(e1.Name, e1);
            graph.Events.Add(e2.Name, e2);
            graph.Events.Add(e3.Name, e3);
            //Act
            var result = graph.GetAllMarkings();
            //Assert
            string[] sResult = result.Split('\n');
            string[] expected = new string[]
            {
                "Event              	Incl 	Pend 	Cond 	Mile 	Exec ",
                "e1                 	True 	True 	1    	1    	True ",
                "e2                 	False	False	0    	0    	False",
                "aVeryLargeEventName	True 	False	0    	0    	False"
            };
            
            Assert.AreEqual(expected[0], sResult[0]);
            Assert.AreEqual(expected[1], sResult[1]);
            Assert.AreEqual(expected[2], sResult[2]);
            
        }

    }
}
