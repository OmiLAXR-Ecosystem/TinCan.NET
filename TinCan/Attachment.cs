using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class Attachment : JsonModel
    {
        public Uri usageType { get; set; }
        public LanguageMap display { get; set; }
        public string contentType { get; set; }
        public int length { get; set; }
        public string sha2 { get; set; }

        public LanguageMap description { get; set; }
        public Uri fileUrl { get; set; }

        // Not serialized: this is only used during sending
        public byte[] content { get; set; }

        public Attachment() {}

        public Attachment(JObject jobj)
        {
            if (jobj["usageType"] != null) usageType = new Uri(jobj.Value<string>("usageType"));
            if (jobj["display"] != null) display = (LanguageMap)jobj.Value<JObject>("display");
            if (jobj["contentType"] != null) contentType = jobj.Value<string>("contentType");
            if (jobj["length"] != null) length = jobj.Value<int>("length");
            if (jobj["sha2"] != null) sha2 = jobj.Value<string>("sha2");
            if (jobj["description"] != null) description = (LanguageMap)jobj.Value<JObject>("description");
            if (jobj["fileUrl"] != null) fileUrl = new Uri(jobj.Value<string>("fileUrl"));
        }

        public void EnsureHashAndLength()
        {
            if (content == null)
            {
                return;
            }

            if (length == 0)
            {
                length = content.Length;
            }

            if (string.IsNullOrEmpty(sha2))
            {
                using (var sha = new SHA256Managed())
                {
                    byte[] hash = sha.ComputeHash(content);
                    var builder = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                    {
                        builder.Append(b.ToString("x2"));
                    }
                    sha2 = builder.ToString();
                }
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            EnsureHashAndLength();

            var result = new JObject();

            if (usageType != null)
            {
                result.Add("usageType", usageType.ToString());
            }
            if (display != null)
            {
                result.Add("display", display.ToJObject(version));
            }
            if (contentType != null)
            {
                result.Add("contentType", contentType);
            }
            if (length != 0 || content != null)
            {
                result.Add("length", length);
            }
            if (sha2 != null)
            {
                result.Add("sha2", sha2);
            }
            if (description != null)
            {
                result.Add("description", description.ToJObject(version));
            }
            if (fileUrl != null)
            {
                result.Add("fileUrl", fileUrl.ToString());
            }

            return result;
        }

        public static explicit operator Attachment(JObject jobj)
        {
            return new Attachment(jobj);
        }
    }
}