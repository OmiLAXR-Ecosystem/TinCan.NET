using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class StatementsQueryTest
    {
        [Test]
        public void TestBooleanParametersUseJsonSpelling()
        {
            var query = new StatementsQuery();
            query.relatedActivities = true;
            query.relatedAgents = false;
            query.ascending = false;
            query.attachments = true;

            var map = query.ToParameterMap(TCAPIVersion.latest());
            Assert.AreEqual("true", map["related_activities"]);
            Assert.AreEqual("false", map["related_agents"]);
            Assert.AreEqual("false", map["ascending"]);
            Assert.AreEqual("true", map["attachments"]);
        }
    }
}
