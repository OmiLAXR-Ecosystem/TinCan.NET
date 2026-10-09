using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class AboutTest
    {
        [Test]
        public void TestAcceptsPatchVersions()
        {
            var about = new About("{\"version\":[\"1.0.3\",\"2.0.0\",\"2.0.1\"]}");
            Assert.AreEqual(3, about.version.Count);
            Assert.AreSame(TCAPIVersion.V103, about.version[0]);
            Assert.AreSame(TCAPIVersion.V200, about.version[1]);
            Assert.AreEqual("2.0.1", about.version[2].ToString());
            Assert.IsTrue(about.version[2] > TCAPIVersion.V200);
        }
    }
}
