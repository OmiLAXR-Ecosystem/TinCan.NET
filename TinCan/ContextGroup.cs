using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class ContextGroup : JsonModel
    {
        public Group group { get; set; }
        public List<Uri> relevantTypes { get; set; }
        public override JObject ToJObject(TCAPIVersion version)
        {
            var result = new JObject();

            if (group != null)
            {
                result["group"] = group.ToJObject(version);
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