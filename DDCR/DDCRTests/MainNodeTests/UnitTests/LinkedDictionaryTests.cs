using DDCR;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DDCRTests.UnitTests
{
    public class LinkedDictionaryTests
    {
        [Test]
        public void Add_One_OneEntry()
        {
            //Arrange
            LinkedDictionary ld = new LinkedDictionary();

            //Assert
            ld.Add("peer1", "event1", "include");

            //Act
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer1"));
            Assert.IsTrue(ld.Dictionary["peer1"].Contains("event1"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event1"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("include"));
        }

        [Test]
        public void Add_TwoIdentical_NoDuplicates()
        {
            //Arrange
            LinkedDictionary ld = new LinkedDictionary();

            //Assert
            ld.Add("peer1", "event1", "include");
            ld.Add("peer1", "event1", "include");
            //Act
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer1"));
            Assert.IsTrue(ld.Dictionary["peer1"].Contains("event1"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event1"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("include"));
            Assert.AreEqual(1, ld.SubDictionary["event1"].Count);
        }

        [Test]
        public void Add_TwoSameKeys_TwoSubEntries()
        {
            //Arrange
            LinkedDictionary ld = new LinkedDictionary();

            //Act
            ld.Add("peer1", "event1", "include");
            ld.Add("peer1", "event1", "condition");
            //Assert
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer1"));
            Assert.IsTrue(ld.Dictionary["peer1"].Contains("event1"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event1"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("include"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("condition"));
            Assert.AreEqual(2, ld.SubDictionary["event1"].Count);
        }

        [Test]
        public void Add_TwoUnique_TwoDictEntries()
        {
            //Arrange
            LinkedDictionary ld = new LinkedDictionary();

            //Act
            ld.Add("peer1", "event1", "include");
            ld.Add("peer2", "event2", "condition");
            //Assert
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer1"));
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer2"));
            Assert.IsTrue(ld.Dictionary["peer1"].Contains("event1"));
            Assert.IsTrue(ld.Dictionary["peer2"].Contains("event2"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event1"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event2"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("include"));
            Assert.IsTrue(ld.SubDictionary["event2"].Contains("condition"));
            Assert.AreEqual(1, ld.SubDictionary["event1"].Count);
            Assert.AreEqual(1, ld.SubDictionary["event2"].Count);
            Assert.AreEqual(2, ld.Dictionary.Count);
        }

        [Test]
        public void Add_TwoUniqueOneIdentical_TwoDictEntries()
        {
            //Arrange
            LinkedDictionary ld = new LinkedDictionary();

            //Act
            ld.Add("peer1", "event1", "include");
            ld.Add("peer1", "event1", "milestone");
            ld.Add("peer2", "event2", "condition");
            //Assert
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer1"));
            Assert.IsTrue(ld.Dictionary.ContainsKey("peer2"));
            Assert.IsTrue(ld.Dictionary["peer1"].Contains("event1"));
            Assert.IsTrue(ld.Dictionary["peer2"].Contains("event2"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event1"));
            Assert.IsTrue(ld.SubDictionary.ContainsKey("event2"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("include"));
            Assert.IsTrue(ld.SubDictionary["event1"].Contains("milestone"));
            Assert.IsTrue(ld.SubDictionary["event2"].Contains("condition"));
            Assert.AreEqual(2, ld.SubDictionary["event1"].Count);
            Assert.AreEqual(1, ld.SubDictionary["event2"].Count);
        }
        [Test]
        public void Clear_TwoEntries_EmptyLD()
        {
            //Arrange
            LinkedDictionary ld = new LinkedDictionary();
            ld.Add("peer1", "event1", "include");
            ld.Add("peer1", "event1", "milestone");
            ld.Add("peer2", "event2", "condition");

            //Act

            ld.Clear();

            //Assert
            Assert.AreEqual(0, ld.Dictionary.Count);
            Assert.AreEqual(0, ld.SubDictionary.Count);
        }

    }
}
