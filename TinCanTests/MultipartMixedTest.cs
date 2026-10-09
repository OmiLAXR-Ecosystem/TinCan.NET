using System.Collections.Generic;
using NUnit.Framework;
using TinCan;
using TinCan.Http;

namespace TinCanTests
{
    [TestFixture]
    class MultipartMixedTest
    {
        [Test]
        public void TestBuildAndParseRoundTrip()
        {
            var attachment = new Attachment();
            attachment.contentType = "application/octet-stream";
            attachment.content = new byte[] { 0, 1, 2, 255, 13, 10, 45, 45 };
            attachment.EnsureHashAndLength();

            var attachments = new List<Attachment>();
            attachments.Add(attachment);
            attachments.Add(attachment);

            MultipartMixed built = MultipartMixed.Build("{\"x\":1}", attachments);
            MultipartMixed parsed = MultipartMixed.Parse(built.ContentType, built.Body);

            Assert.AreEqual("{\"x\":1}", parsed.Json);
            Assert.AreEqual(1, parsed.Files.Count);
            CollectionAssert.AreEqual(attachment.content, parsed.Files[attachment.sha2]);
        }
    }
}
