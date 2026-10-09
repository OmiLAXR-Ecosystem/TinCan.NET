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
    }
}
