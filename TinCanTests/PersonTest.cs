using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TinCan;
using TinCan.Json;

namespace TinCanTests
{
    [TestFixture]
    class PersonTest
    {
        [Test]
        public void TestRoundTrip()
        {
            var json = new StringOfJSON("{\"objectType\":\"Person\",\"name\":[\"Alice\"],\"mbox\":[\"mailto:alice@example.com\"],\"account\":[{\"homePage\":\"http://example.com\",\"name\":\"alice\"}]}");
            var person = new Person(json);

            Assert.AreEqual("Alice", person.name[0]);
            Assert.AreEqual("mailto:alice@example.com", person.mbox[0]);
            Assert.AreEqual("alice", person.account[0].name);

            JObject serialized = person.ToJObject(TCAPIVersion.latest());
            Assert.AreEqual(Person.OBJECT_TYPE, (string)serialized["objectType"]);
            Assert.AreEqual("Alice", (string)serialized["name"][0]);
            Assert.AreEqual("http://example.com/", (string)serialized["account"][0]["homePage"]);
        }
    }
}
