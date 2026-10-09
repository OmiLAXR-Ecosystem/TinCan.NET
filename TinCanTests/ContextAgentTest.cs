using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class ContextAgentTest
    {
        [Test]
        public void TestObjectTypeAndRoundTrip()
        {
            var agent = new ContextAgent();
            agent.agent = new Agent();
            agent.agent.mbox = "mailto:learner@example.com";
            agent.relevantTypes = new List<Uri>();
            agent.relevantTypes.Add(new Uri("http://id.tincanapi.com/activitytype/instructor"));

            JObject json = agent.ToJObject(TCAPIVersion.V200);
            Assert.AreEqual(ContextAgent.OBJECT_TYPE, (string)json["objectType"]);

            var restored = new ContextAgent(json);
            Assert.AreEqual("mailto:learner@example.com", restored.agent.mbox);
            Assert.AreEqual(agent.relevantTypes[0], restored.relevantTypes[0]);
        }

        [Test]
        public void TestDroppedForVersion103()
        {
            var context = new Context();
            context.contextAgents = new List<ContextAgent>();
            context.contextAgents.Add(new ContextAgent
            {
                agent = new Agent { mbox = "mailto:learner@example.com" }
            });

            JObject v1 = context.ToJObject(TCAPIVersion.V103);
            Assert.IsFalse(v1.ContainsKey("contextAgents"));

            JObject v2 = context.ToJObject(TCAPIVersion.V200);
            Assert.AreEqual("contextAgent", (string)v2["contextAgents"][0]["objectType"]);
        }
    }
}
