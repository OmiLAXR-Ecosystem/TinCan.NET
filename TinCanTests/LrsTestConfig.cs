/*
    Copyright 2026 Rustici Software

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
    using System.IO;
    using TinCan;
    using TinCan.LRSResponses;

    static class LrsTestConfig
    {
        const string FallbackEndpoint = "https://cloud.scorm.com/tc/U2S4SI5FY0/sandbox/";
        const string FallbackUsername = "Nja986GYE1_XrWMmFUE";
        const string FallbackPassword = "Bd9lDr1kjaWWY6RID_4";

        public static RemoteLRS Create()
        {
            string endpoint = Environment.GetEnvironmentVariable("XAPI_ENDPOINT");
            string username = Environment.GetEnvironmentVariable("XAPI_USERNAME");
            string password = Environment.GetEnvironmentVariable("XAPI_PASSWORD");

            if (IsBlank(endpoint) || IsBlank(username) || IsBlank(password))
            {
                string file = FindFile("local.xapi.env");
                if (file != null)
                {
                    ApplyFile(file, ref endpoint, ref username, ref password);
                }
            }

            if (IsBlank(endpoint))
            {
                endpoint = FallbackEndpoint;
            }
            if (IsBlank(username))
            {
                username = FallbackUsername;
            }
            if (IsBlank(password))
            {
                password = FallbackPassword;
            }

            var lrs = new RemoteLRS(endpoint, TCAPIVersion.V200, username, password);
            NegotiateVersion(lrs);
            return lrs;
        }

        static void NegotiateVersion(RemoteLRS lrs)
        {
            AboutLRSResponse about = lrs.About();
            if (!about.success || about.content == null || about.content.version == null)
            {
                return;
            }

            TCAPIVersion bestLegacy = null;
            foreach (TCAPIVersion advertised in about.content.version)
            {
                if (advertised.major >= 2)
                {
                    lrs.version = TCAPIVersion.V200;
                    return;
                }

                if (advertised.major == 1 && (bestLegacy == null || advertised > bestLegacy))
                {
                    bestLegacy = advertised;
                }
            }

            if (bestLegacy != null)
            {
                lrs.version = bestLegacy;
            }
        }

        static void ApplyFile(string path, ref string endpoint, ref string username, ref string password)
        {
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (value.Length == 0)
                {
                    continue;
                }

                if (key == "XAPI_ENDPOINT" && IsBlank(endpoint))
                {
                    endpoint = value;
                }
                else if (key == "XAPI_USERNAME" && IsBlank(username))
                {
                    username = value;
                }
                else if (key == "XAPI_PASSWORD" && IsBlank(password))
                {
                    password = value;
                }
            }
        }

        static string FindFile(string name)
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                string nested = Path.Combine(Path.Combine(dir.FullName, "TinCanTests"), name);
                if (File.Exists(nested))
                {
                    return nested;
                }

                dir = dir.Parent;
            }

            return null;
        }

        static bool IsBlank(string value)
        {
            return value == null || value.Trim().Length == 0;
        }
    }
}
