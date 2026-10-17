using System;
using System.Collections.Generic;
using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class StatementsQueryTest
    {
        [Test]
        public void TestParameterMap()
        {
            var query = new StatementsQuery();
            query.relatedActivities = true;
            query.relatedAgents = false;
            query.ascending = false;
            query.attachments = true;
            query.since = new DateTime(2020, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            query.until = new DateTime(2020, 6, 2, 0, 0, 0, DateTimeKind.Unspecified);

            Dictionary<string, string> map = query.ToParameterMap(TCAPIVersion.V200);
            Assert.AreEqual("true", map["related_activities"]);
            Assert.AreEqual("false", map["related_agents"]);
            Assert.AreEqual("false", map["ascending"]);
            Assert.AreEqual("true", map["attachments"]);
            Assert.AreEqual("2020-06-01T12:00:00.000Z", map["since"]);
            Assert.AreEqual("2020-06-02T00:00:00.000Z", map["until"]);
        }
    }
}
