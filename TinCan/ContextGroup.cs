using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class ContextGroup : JsonModel
    {
        public static readonly String OBJECT_TYPE = "contextGroup";

        public Group group { get; set; }
        public List<Uri> relevantTypes { get; set; }

        public ContextGroup() { }

        public ContextGroup(StringOfJSON json) : this(json.toJObject()) { }

        public ContextGroup(JObject jobj)
        {
            if (jobj["group"] != null)
            {
                group = new Group((JObject)jobj["group"]);
            }
            if (jobj["relevantTypes"] is JArray rtArr)
            {
                relevantTypes = ContextAgent.ReadRelevantTypes(rtArr);
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            var result = new JObject();
            result.Add("objectType", OBJECT_TYPE);

            if (group != null)
            {
                result.Add("group", group.ToJObject(version));
            }

            if (relevantTypes != null && relevantTypes.Count > 0)
            {
                result.Add("relevantTypes", ContextAgent.WriteRelevantTypes(relevantTypes));
            }

            return result;
        }

        public static explicit operator ContextGroup(JObject jobj)
        {
            return new ContextGroup(jobj);
        }
    }
}
