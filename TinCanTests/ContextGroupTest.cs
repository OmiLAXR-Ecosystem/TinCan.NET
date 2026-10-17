using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class ContextGroupTest
    {
        [Test]
        public void TestObjectTypeAndRoundTrip()
        {
            var group = new Group();
            group.name = "Team A";
            group.mbox = "mailto:team@example.com";

            var contextGroup = new ContextGroup();
            contextGroup.group = group;
            contextGroup.relevantTypes = new List<Uri>();
            contextGroup.relevantTypes.Add(new Uri("http://id.tincanapi.com/activitytype/team"));

            JObject json = contextGroup.ToJObject(TCAPIVersion.V200);
            Assert.AreEqual(ContextGroup.OBJECT_TYPE, (string)json["objectType"]);
            Assert.AreEqual("Group", (string)json["group"]["objectType"]);

            var restored = new ContextGroup(json);
            Assert.AreEqual("mailto:team@example.com", restored.group.mbox);
            Assert.AreEqual(contextGroup.relevantTypes[0], restored.relevantTypes[0]);
        }

        [Test]
        public void TestDroppedForVersion103()
        {
            var context = new Context();
            context.contextGroups = new List<ContextGroup>();
            context.contextGroups.Add(new ContextGroup
            {
                group = new Group { mbox = "mailto:team@example.com" }
            });

            JObject v1 = context.ToJObject(TCAPIVersion.V103);
            Assert.IsFalse(v1.ContainsKey("contextGroups"));

            JObject v2 = context.ToJObject(TCAPIVersion.V200);
            Assert.IsTrue(v2.ContainsKey("contextGroups"));
        }
    }
}
