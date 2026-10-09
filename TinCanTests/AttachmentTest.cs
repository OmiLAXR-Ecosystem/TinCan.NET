using System;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TinCan;

namespace TinCanTests
{
    [TestFixture]
    class AttachmentTest
    {
        [Test]
        public void TestOmitsNullsAndFillsHash()
        {
            var attachment = new Attachment();
            attachment.usageType = new Uri("http://example.com/attachments/test");
            attachment.display = new LanguageMap();
            attachment.display.Add("en", "note");
            attachment.contentType = "text/plain";
            attachment.content = Encoding.UTF8.GetBytes("hi");

            JObject json = attachment.ToJObject(TCAPIVersion.latest());
            Assert.IsFalse(json.ContainsKey("description"));
            Assert.IsFalse(json.ContainsKey("fileUrl"));
            Assert.AreEqual(2, (int)json["length"]);
            Assert.AreEqual("8f434346648f6b96df89dda901c5176b10a6d83961dd3c1ac88b59b2dc327aa4", (string)json["sha2"]);
        }

        [Test]
        public void TestEmptyAttachmentHasNoNullProperties()
        {
            JObject json = new Attachment().ToJObject(TCAPIVersion.latest());
            Assert.AreEqual(0, json.Count);
        }
    }
}
