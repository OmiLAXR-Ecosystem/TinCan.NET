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
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TinCan
{
    public sealed class TCAPIVersion : IComparable<TCAPIVersion>
    {
        public static readonly TCAPIVersion V200 = new TCAPIVersion("2.0.0");
        public static readonly TCAPIVersion V103 = new TCAPIVersion("1.0.3");
        public static readonly TCAPIVersion V102 = new TCAPIVersion("1.0.2");
        public static readonly TCAPIVersion V101 = new TCAPIVersion("1.0.1");
        public static readonly TCAPIVersion V100 = new TCAPIVersion("1.0.0");
        public static readonly TCAPIVersion V095 = new TCAPIVersion("0.95");
        public static readonly TCAPIVersion V090 = new TCAPIVersion("0.9");

        public static TCAPIVersion latest() => V200;

        private static Dictionary<String, TCAPIVersion> known;
        private static Dictionary<String, TCAPIVersion> supported;

        public static Dictionary<String, TCAPIVersion> GetKnown()
        {
            if (known != null) {
                return known;
            }

            known = new Dictionary<String, TCAPIVersion>();
            known.Add("2.0.0", V200);
            known.Add("2.0", V200);
            known.Add("1.0.3", V103);
            known.Add("1.0.2", V102);
            known.Add("1.0.1", V101);
            known.Add("1.0.0", V100);
            known.Add("0.95", V095);
            known.Add("0.9", V090);

            return known;
        }

        public static Dictionary<String, TCAPIVersion> GetSupported()
        {
            if (supported != null) {
                return supported;
            }

            supported = new Dictionary<String, TCAPIVersion>();
            supported.Add("2.0.0", V200);
            supported.Add("1.0.3", V103);
            supported.Add("1.0.2", V102);
            supported.Add("1.0.1", V101);
            supported.Add("1.0.0", V100);

            return supported;
        }

        public static bool TryParse(String vStr, out TCAPIVersion version)
        {
            version = null;
            if (vStr == null)
            {
                return false;
            }

            var text = vStr.Trim();
            if (text.Length == 0)
            {
                return false;
            }

            var catalog = GetKnown();
            if (catalog.ContainsKey(text))
            {
                version = catalog[text];
                return true;
            }

            var parts = text.Split('.');
            if (parts.Length < 2 || parts.Length > 3)
            {
                return false;
            }

            int major;
            int minor;
            int patch = 0;
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major))
            {
                return false;
            }
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor))
            {
                return false;
            }
            if (parts.Length == 3 && !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out patch))
            {
                return false;
            }

            string canonical = major.ToString(CultureInfo.InvariantCulture)
                + "." + minor.ToString(CultureInfo.InvariantCulture)
                + "." + patch.ToString(CultureInfo.InvariantCulture);
            if (catalog.ContainsKey(canonical))
            {
                version = catalog[canonical];
                return true;
            }

            version = new TCAPIVersion(canonical);
            return true;
        }

        public static explicit operator TCAPIVersion(String vStr)
        {
            TCAPIVersion version;
            if (!TryParse(vStr, out version))
            {
                throw new ArgumentException("Unrecognized version: " + vStr);
            }

            return version;
        }

        private String text;

        public int major { get; private set; }
        public int minor { get; private set; }
        public int patch { get; private set; }

        private TCAPIVersion(String value)
        {
            text = value;
            var parts = value.Split('.');
            major = int.Parse(parts[0], CultureInfo.InvariantCulture);
            minor = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
            patch = parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 0;
        }

        public bool IsAtLeast(TCAPIVersion other)
        {
            return CompareTo(other) >= 0;
        }

        public int CompareTo(TCAPIVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            int comparison = major.CompareTo(other.major);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = minor.CompareTo(other.minor);
            if (comparison != 0)
            {
                return comparison;
            }

            return patch.CompareTo(other.patch);
        }

        public static bool operator >(TCAPIVersion left, TCAPIVersion right)
        {
            if (left == null)
            {
                return false;
            }
            return left.CompareTo(right) > 0;
        }

        public static bool operator <(TCAPIVersion left, TCAPIVersion right)
        {
            if (left == null)
            {
                return right != null;
            }
            return left.CompareTo(right) < 0;
        }

        public static bool operator >=(TCAPIVersion left, TCAPIVersion right)
        {
            if (left == null)
            {
                return right == null;
            }
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(TCAPIVersion left, TCAPIVersion right)
        {
            if (left == null)
            {
                return true;
            }
            return left.CompareTo(right) <= 0;
        }

        public override String ToString()
        {
            return text;
        }
    }
}
