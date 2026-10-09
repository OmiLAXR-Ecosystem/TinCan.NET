using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class ContextAgent : JsonModel
    {
        public static readonly String OBJECT_TYPE = "contextAgent";

        public Agent agent { get; set; }
        public List<Uri> relevantTypes { get; set; }

        public ContextAgent() { }

        public ContextAgent(StringOfJSON json) : this(json.toJObject()) { }

        public ContextAgent(JObject jobj)
        {
            if (jobj["agent"] != null)
            {
                agent = new Agent((JObject)jobj["agent"]);
            }
            if (jobj["relevantTypes"] is JArray rtArr)
            {
                relevantTypes = ReadRelevantTypes(rtArr);
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            var result = new JObject();
            result.Add("objectType", OBJECT_TYPE);

            if (agent != null)
            {
                result.Add("agent", agent.ToJObject(version));
            }

            if (relevantTypes != null && relevantTypes.Count > 0)
            {
                result.Add("relevantTypes", WriteRelevantTypes(relevantTypes));
            }

            return result;
        }

        internal static List<Uri> ReadRelevantTypes(JArray rtArr)
        {
            var types = new List<Uri>();
            foreach (var item in rtArr)
            {
                var value = item.Type == JTokenType.String ? item.Value<string>() : null;
                if (value != null)
                {
                    types.Add(new Uri(value));
                }
            }
            return types;
        }

        internal static JArray WriteRelevantTypes(List<Uri> types)
        {
            var typesArray = new JArray();
            foreach (var uri in types)
            {
                if (uri != null)
                {
                    typesArray.Add(uri.ToString());
                }
            }
            return typesArray;
        }

        public static explicit operator ContextAgent(JObject jobj)
        {
            return new ContextAgent(jobj);
        }
    }
}
