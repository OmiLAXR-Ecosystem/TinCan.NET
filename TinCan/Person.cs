using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class Person : JsonModel
    {
        public static readonly String OBJECT_TYPE = "Person";

        public List<String> name { get; set; }
        public List<String> mbox { get; set; }
        public List<String> mbox_sha1sum { get; set; }
        public List<String> openid { get; set; }
        public List<AgentAccount> account { get; set; }

        public Person() { }

        public Person(StringOfJSON json) : this(json.toJObject()) { }

        public Person(JObject jobj)
        {
            name = ReadStrings(jobj["name"] as JArray);
            mbox = ReadStrings(jobj["mbox"] as JArray);
            mbox_sha1sum = ReadStrings(jobj["mbox_sha1sum"] as JArray);
            openid = ReadStrings(jobj["openid"] as JArray);

            if (jobj["account"] is JArray accounts)
            {
                account = new List<AgentAccount>();
                foreach (var item in accounts)
                {
                    if (item is JObject obj)
                    {
                        account.Add(new AgentAccount(obj));
                    }
                }
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            var result = new JObject();
            result.Add("objectType", OBJECT_TYPE);
            WriteStrings(result, "name", name);
            WriteStrings(result, "mbox", mbox);
            WriteStrings(result, "mbox_sha1sum", mbox_sha1sum);
            WriteStrings(result, "openid", openid);

            if (account != null)
            {
                var accounts = new JArray();
                foreach (AgentAccount item in account)
                {
                    if (item != null)
                    {
                        accounts.Add(item.ToJObject(version));
                    }
                }
                result.Add("account", accounts);
            }

            return result;
        }

        static List<String> ReadStrings(JArray array)
        {
            if (array == null)
            {
                return null;
            }

            var values = new List<String>();
            foreach (var item in array)
            {
                if (item != null && item.Type != JTokenType.Null)
                {
                    values.Add(item.Value<String>());
                }
            }
            return values;
        }

        static void WriteStrings(JObject result, string name, List<String> values)
        {
            if (values == null)
            {
                return;
            }

            var array = new JArray();
            foreach (String value in values)
            {
                array.Add(value);
            }
            result.Add(name, array);
        }

        public static explicit operator Person(JObject jobj)
        {
            return new Person(jobj);
        }
    }
}
