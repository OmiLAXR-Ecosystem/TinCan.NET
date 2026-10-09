using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class TCAPIVersionTest
    {
        [Test]
        public void TestOrdering()
        {
            Assert.IsTrue(TCAPIVersion.V200 > TCAPIVersion.V103);
            Assert.IsTrue(TCAPIVersion.V103 < TCAPIVersion.V200);
            Assert.IsTrue(TCAPIVersion.V200 >= TCAPIVersion.V103);
            Assert.AreEqual(0, TCAPIVersion.V200.CompareTo(TCAPIVersion.V200));
            Assert.IsTrue(TCAPIVersion.V095 > TCAPIVersion.V090);
            Assert.IsTrue(TCAPIVersion.V200.IsAtLeast(TCAPIVersion.V103));
            Assert.IsFalse(TCAPIVersion.V103.IsAtLeast(TCAPIVersion.V200));
        }

        [Test]
        public void TestLenientParse()
        {
            Assert.AreSame(TCAPIVersion.V200, (TCAPIVersion)"2.0");
            Assert.AreSame(TCAPIVersion.V200, (TCAPIVersion)"2.0.0");
            Assert.AreSame(TCAPIVersion.V100, (TCAPIVersion)"1.0");

            TCAPIVersion patch = (TCAPIVersion)"2.0.1";
            Assert.AreEqual("2.0.1", patch.ToString());
            Assert.IsTrue(patch > TCAPIVersion.V200);
            Assert.AreEqual(0, patch.CompareTo((TCAPIVersion)"2.0.1"));
        }

        [Test]
        public void TestRejectsGarbage()
        {
            Assert.Throws<System.ArgumentException>(delegate
            {
                TCAPIVersion ignored = (TCAPIVersion)"not-a-version";
            });
        }

        [Test]
        public void TestNegotiationPrefersTwoPointZero()
        {
            var about = AboutFor("1.0.3", "2.0.0");
            Assert.AreSame(TCAPIVersion.V200, RemoteLRS.SelectVersion(about));

            about = AboutFor("2.0");
            Assert.AreSame(TCAPIVersion.V200, RemoteLRS.SelectVersion(about));
        }

        [Test]
        public void TestNegotiationFallsBackToHighestLegacy()
        {
            var about = AboutFor("1.0.0", "1.0.2", "1.0.1");
            Assert.AreSame(TCAPIVersion.V102, RemoteLRS.SelectVersion(about));
        }

        [Test]
        public void TestNegotiationIgnoresUnsupportedAndFailures()
        {
            Assert.IsNull(RemoteLRS.SelectVersion(AboutFor("0.9", "0.95")));
            Assert.IsNull(RemoteLRS.SelectVersion(AboutFor()));
            Assert.IsNull(RemoteLRS.SelectVersion(new TinCan.LRSResponses.AboutLRSResponse { success = false }));
            Assert.IsNull(RemoteLRS.SelectVersion(null));
        }

        static TinCan.LRSResponses.AboutLRSResponse AboutFor(params string[] versions)
        {
            var json = new Newtonsoft.Json.Linq.JObject();
            json["version"] = new Newtonsoft.Json.Linq.JArray(versions);
            var about = new About(json);

            return new TinCan.LRSResponses.AboutLRSResponse { success = true, content = about };
        }
    }
}
