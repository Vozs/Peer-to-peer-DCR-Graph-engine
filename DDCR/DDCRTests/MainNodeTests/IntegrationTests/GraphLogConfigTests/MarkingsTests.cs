using DDCR;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
/*Tests for 
 * GetNewMarkingsReversible, 
 * ChangeMarkings*/
namespace DDCRTests.IntegrationTests.GraphLogConfigTests
{
    class MarkingsTests
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
    }
}
