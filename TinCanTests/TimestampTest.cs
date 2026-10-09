using System;
using NUnit.Framework;
using TinCan.Json;

namespace TinCanTests
{
    [TestFixture]
    class TimestampTest
    {
        [Test]
        public void TestUnspecifiedIsTreatedAsUtc()
        {
            var unspecified = new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified);
            Assert.AreEqual("2020-01-02T03:04:05.006Z", Timestamp.Format(unspecified));
        }

        [Test]
        public void TestLocalIsConvertedToUtc()
        {
            var local = new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Local);
            string formatted = Timestamp.Format(local);
            Assert.IsTrue(formatted.EndsWith("Z"));
            Assert.IsFalse(formatted.Contains("+"));
            Assert.AreEqual(local.ToUniversalTime(), Timestamp.Parse(formatted));
        }

        [Test]
        public void TestParseKeepsTheInstant()
        {
            DateTime parsed = Timestamp.Parse("2020-01-02T04:04:05.006+01:00");
            Assert.AreEqual(DateTimeKind.Utc, parsed.Kind);
            Assert.AreEqual(new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), parsed);
        }
    }
}
