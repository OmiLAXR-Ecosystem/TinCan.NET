using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TinCan.Http
{
    internal class MultipartMixed
    {
        public string ContentType { get; set; }
        public byte[] Body { get; set; }
        public string Json { get; set; }
        public Dictionary<string, byte[]> Files { get; set; }

        public static MultipartMixed Build(string json, IList<Attachment> attachments)
        {
            string boundary = "----TinCanBoundary" + Guid.NewGuid().ToString("N");
            var stream = new MemoryStream();
            var seen = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            WriteAscii(stream, "--" + boundary + "\r\n");
            WriteAscii(stream, "Content-Type: application/json\r\n\r\n");
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json ?? "");
            stream.Write(jsonBytes, 0, jsonBytes.Length);
            WriteAscii(stream, "\r\n");

            if (attachments != null)
            {
                foreach (Attachment attachment in attachments)
                {
                    if (attachment == null || attachment.content == null)
                    {
                        continue;
                    }

                    attachment.EnsureHashAndLength();
                    if (!string.IsNullOrEmpty(attachment.sha2))
                    {
                        if (seen.ContainsKey(attachment.sha2))
                        {
                            continue;
                        }
                        seen[attachment.sha2] = true;
                    }

                    string partType = string.IsNullOrEmpty(attachment.contentType)
                        ? "application/octet-stream"
                        : attachment.contentType;

                    WriteAscii(stream, "--" + boundary + "\r\n");
                    WriteAscii(stream, "Content-Type: " + partType + "\r\n");
                    WriteAscii(stream, "Content-Transfer-Encoding: binary\r\n");
                    WriteAscii(stream, "X-Experience-API-Hash: " + attachment.sha2 + "\r\n\r\n");
                    stream.Write(attachment.content, 0, attachment.content.Length);
                    WriteAscii(stream, "\r\n");
                }
            }

            WriteAscii(stream, "--" + boundary + "--\r\n");

            return new MultipartMixed
            {
                ContentType = "multipart/mixed; boundary=" + boundary,
                Body = stream.ToArray(),
                Json = json,
                Files = null
            };
        }

        public static MultipartMixed Parse(string contentType, byte[] body)
        {
            if (body == null)
            {
                body = new byte[0];
            }

            string boundary = ExtractBoundary(contentType);
            byte[] delimiter = Encoding.ASCII.GetBytes("--" + boundary);
            var parsed = new MultipartMixed
            {
                ContentType = contentType,
                Body = body,
                Files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
            };

            int pos = IndexOf(body, delimiter, 0);
            while (pos >= 0)
            {
                pos += delimiter.Length;
                if (pos + 1 < body.Length && body[pos] == (byte)'-' && body[pos + 1] == (byte)'-')
                {
                    break;
                }
                if (pos + 1 < body.Length && body[pos] == (byte)'\r' && body[pos + 1] == (byte)'\n')
                {
                    pos += 2;
                }

                int next = IndexOf(body, delimiter, pos);
                if (next < 0)
                {
                    break;
                }

                int partEnd = next;
                if (partEnd >= 2 && body[partEnd - 2] == (byte)'\r' && body[partEnd - 1] == (byte)'\n')
                {
                    partEnd -= 2;
                }

                ConsumePart(Slice(body, pos, partEnd), parsed);
                pos = next;
            }

            return parsed;
        }

        static void ConsumePart(byte[] part, MultipartMixed parsed)
        {
            byte[] separator = new byte[] { 13, 10, 13, 10 };
            int split = IndexOf(part, separator, 0);
            if (split < 0)
            {
                return;
            }

            string headerBlock = Encoding.ASCII.GetString(part, 0, split);
            byte[] content = Slice(part, split + separator.Length, part.Length);
            var headers = ParseHeaders(headerBlock);

            string hash;
            if (headers.TryGetValue("X-Experience-API-Hash", out hash) && !string.IsNullOrEmpty(hash))
            {
                parsed.Files[hash] = content;
                return;
            }

            if (parsed.Json == null)
            {
                parsed.Json = Encoding.UTF8.GetString(content);
            }
        }

        static Dictionary<string, string> ParseHeaders(string block)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = block.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }
                headers[line.Substring(0, colon).Trim()] = line.Substring(colon + 1).Trim();
            }
            return headers;
        }

        static string ExtractBoundary(string contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentException("multipart content type is missing a boundary");
            }

            string[] pieces = contentType.Split(';');
            foreach (string piece in pieces)
            {
                string trimmed = piece.Trim();
                if (trimmed.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
                {
                    string boundary = trimmed.Substring("boundary=".Length).Trim();
                    if (boundary.Length >= 2 && boundary[0] == '"' && boundary[boundary.Length - 1] == '"')
                    {
                        boundary = boundary.Substring(1, boundary.Length - 2);
                    }
                    return boundary;
                }
            }

            throw new ArgumentException("multipart content type is missing a boundary");
        }

        static void WriteAscii(Stream stream, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        static int IndexOf(byte[] buffer, byte[] pattern, int start)
        {
            if (pattern.Length == 0 || buffer.Length < pattern.Length)
            {
                return -1;
            }

            int last = buffer.Length - pattern.Length;
            for (int i = start; i <= last; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        static byte[] Slice(byte[] buffer, int start, int end)
        {
            int length = end - start;
            if (length < 0)
            {
                length = 0;
            }
            var slice = new byte[length];
            if (length > 0)
            {
                Array.Copy(buffer, start, slice, 0, length);
            }
            return slice;
        }
    }
}
