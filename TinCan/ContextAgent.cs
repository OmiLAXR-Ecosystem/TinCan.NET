using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class ContextAgent : JsonModel
    {
        public Agent agent { get; set; }
        public List<Uri> relevantTypes { get; set; }

        public override JObject ToJObject(TCAPIVersion version)
        {
            var result = new JObject();

            if (agent != null)
            {
                result["agent"] = agent.ToJObject(version);
            }

            if (relevantTypes != null && relevantTypes.Count > 0)
            {
                var typesArray = new JArray();
                foreach (var uri in relevantTypes)
                {
                    if (uri != null)
                    {
                        typesArray.Add(uri.ToString());
                    }
                }
                result["relevantTypes"] = typesArray;
            }

            return result;
        }
    }
}