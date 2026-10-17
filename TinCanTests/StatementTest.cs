/*
    Copyright 2014 Rustici Software

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
namespace TinCanTests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Newtonsoft.Json.Linq;
    using TinCan;
    using TinCan.Json;

    [TestFixture]
    class StatementTest
    {
        [SetUp]
        public void Init()
        {
            Console.WriteLine("Running " + TestContext.CurrentContext.Test.FullName);
        }

        [Test]
        public void TestEmptyCtr()
        {
            Statement obj = new Statement();
            Assert.IsInstanceOf<Statement>(obj);
            Assert.IsNull(obj.id);
            Assert.IsNull(obj.actor);
            Assert.IsNull(obj.verb);
            Assert.IsNull(obj.target);
            Assert.IsNull(obj.result);
            Assert.IsNull(obj.context);
            Assert.IsNull(obj.version);
            Assert.IsNull(obj.timestamp);
            Assert.IsNull(obj.stored);

            StringAssert.AreEqualIgnoringCase("{}", obj.ToJSON());
        }

        [Test]
        public void TestTimestampRoundTripIsUtc()
        {
            Statement obj = new Statement(new StringOfJSON("{\"timestamp\":\"2020-01-02T04:04:05.006+01:00\"}"));
            Assert.AreEqual(new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), obj.timestamp);

            JObject serialized = obj.ToJObject(TCAPIVersion.V200);
            Assert.AreEqual("2020-01-02T03:04:05.006Z", (string)serialized["timestamp"]);
        }

        [Test]
        public void TestJObjectCtrSubStatement()
        {
            JObject cfg = new JObject();
            cfg.Add("actor", Support.agent.ToJObject());
            cfg.Add("verb", Support.verb.ToJObject());
            cfg.Add("object", Support.subStatement.ToJObject());

            Statement obj = new Statement(cfg);
            Assert.IsInstanceOf<Statement>(obj);
            Assert.IsInstanceOf<SubStatement>(obj.target);
        }
    }
}
