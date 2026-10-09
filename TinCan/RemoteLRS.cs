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
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
using TinCan.Documents;
using TinCan.Http;
using TinCan.LRSResponses;
using System.Linq;

namespace TinCan
{
    public class RemoteLRS : ILRS
    {
        public Uri endpoint { get; set; }
        public TCAPIVersion version { get; set; }
        public String auth { get; set; }
        public Dictionary<String, String> extended { get; set; } = new Dictionary<String, String>();

        public void SetAuth(String username, String password)
        {
            auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
        }

        public RemoteLRS() { }
        public RemoteLRS(Uri endpoint, TCAPIVersion version, String username, String password)
        {
            this.endpoint = endpoint;
            this.version = version;
            this.SetAuth(username, password);
        }
        public RemoteLRS(String endpoint, TCAPIVersion version, String username, String password) : this(new Uri(endpoint), version, username, password) { }
        public RemoteLRS(String endpoint, String username, String password) : this(endpoint, TCAPIVersion.latest(), username, password) { }

        private class MyHTTPRequest
        {
            public String method { get; set; }
            public String resource { get; set; }
            public Dictionary<String, String> queryParams { get; set; } = new Dictionary<String, String>();
            public Dictionary<String, String> headers { get; set; } = new Dictionary<String, String>();
            public String contentType { get; set; }
            public byte[] content { get; set; }
            public String accept { get; set; }
            public String ifMatch { get; set; }
            public String ifNoneMatch { get; set; }
        }

        private class MyHTTPResponse
        {
            public HttpStatusCode status { get; set; }
            public String contentType { get; set; }
            public byte[] content { get; set; }
            public DateTime lastModified { get; set; }
            public String etag { get; set; }
            public String lrsVersion { get; set; }
            public String consistentThrough { get; set; }
            public Exception ex { get; set; }

            public MyHTTPResponse() { }
            public MyHTTPResponse(HttpWebResponse webResp)
            {
                status = webResp.StatusCode;
                contentType = webResp.ContentType;
                etag = webResp.Headers.Get("ETag");
                lrsVersion = webResp.Headers.Get("X-Experience-API-Version");
                consistentThrough = webResp.Headers.Get("X-Experience-API-Consistent-Through");
                lastModified = webResp.LastModified;

                using (var stream = webResp.GetResponseStream())
                {
                    content = ReadFully(stream, (int)webResp.ContentLength);
                }
            }
        }

        private string AppendParamsToExistingQueryString(string currentQueryString,  IEnumerable<KeyValuePair<string,  string>> parameters)
        {
            foreach (KeyValuePair<String, String> entry in parameters)
            {
                if (currentQueryString != "")
                {
                    currentQueryString += "&";
                }
                currentQueryString += HttpUtility.UrlEncode(entry.Key) + "=" + HttpUtility.UrlEncode(entry.Value);
            }

            return currentQueryString;
        }

        private MyHTTPResponse MakeSyncRequest(MyHTTPRequest req)
        {
            String url;
            if (req.resource.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                url = req.resource;
            }
            else
            {
                url = endpoint.ToString();
                if (! url.EndsWith("/") && ! req.resource.StartsWith("/")) {
                    url += "/";
                }
                url += req.resource;
            }

            String qs = "";
            qs = AppendParamsToExistingQueryString(qs, req.queryParams);
            qs = AppendParamsToExistingQueryString(qs, extended.Where(w => !req.queryParams.ContainsKey(w.Key)));

            if (qs != "")
            {
                url += "?" + qs;
            }

            // TODO: handle special properties we recognize, such as content type, modified since, etc.
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Method = req.method;

            webReq.Headers.Add("X-Experience-API-Version", version.ToString());
            if (auth != null)
            {
                webReq.Headers.Add("Authorization", auth);
            }
            webReq.Accept = string.IsNullOrEmpty(req.accept) ? "application/json" : req.accept;
            if (!string.IsNullOrEmpty(req.ifMatch))
            {
                webReq.Headers.Add("If-Match", req.ifMatch);
            }
            if (!string.IsNullOrEmpty(req.ifNoneMatch))
            {
                webReq.Headers.Add("If-None-Match", req.ifNoneMatch);
            }
            foreach (KeyValuePair<String, String> entry in req.headers)
            {
                webReq.Headers.Add(entry.Key, entry.Value);
            }
            

            if (req.contentType != null)
            {
                webReq.ContentType = req.contentType;
            }
            else
            {
                webReq.ContentType = "application/octet-stream";
            }

            if (req.content != null)
            {
                webReq.ContentLength = req.content.Length;
                using (var stream = webReq.GetRequestStream())
                {
                    stream.Write(req.content, 0, req.content.Length);
                }
            }

            MyHTTPResponse resp;

            try
            {
                using (var webResp = (HttpWebResponse)webReq.GetResponse())
                {
                    resp = new MyHTTPResponse(webResp);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var webResp = (HttpWebResponse)ex.Response)
                    {
                        resp = new MyHTTPResponse(webResp);
                    }
                }
                else
                {
                    resp = new MyHTTPResponse();
                    resp.content = Encoding.UTF8.GetBytes("Web exception without '.Response'");
                }
                resp.ex = ex;
            }

            return resp;
        }

        /// <summary>
        /// See http://www.yoda.arachsys.com/csharp/readbinary.html no license found
        /// 
        /// Reads data from a stream until the end is reached. The
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <param name="initialLength">The initial buffer length</param>
        private static byte[] ReadFully(Stream stream, int initialLength)
        {
            // If we've been passed an unhelpful initial length, just
            // use 32K.
            if (initialLength < 1)
            {
                initialLength = 32768;
            }

            byte[] buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
        }

        private static void ApplyTransport(LRSResponse response, MyHTTPResponse http)
        {
            if (http == null)
            {
                return;
            }

            response.httpStatus = http.status;
            response.lrsVersion = http.lrsVersion;
        }

        private static bool IsDocumentSuccess(MyHTTPResponse res)
        {
            return res.status == HttpStatusCode.NoContent || res.status == HttpStatusCode.OK;
        }

        private void ReadStatementBody(MyHTTPResponse res, out String json, out Dictionary<String, byte[]> files)
        {
            files = null;
            if (res.content == null)
            {
                json = "";
                return;
            }

            String contentType = res.contentType ?? "";
            if (contentType.StartsWith("multipart/mixed", StringComparison.OrdinalIgnoreCase))
            {
                MultipartMixed parsed = MultipartMixed.Parse(contentType, res.content);
                json = parsed.Json ?? "";
                files = parsed.Files;
                return;
            }

            json = Encoding.UTF8.GetString(res.content);
        }

        private static void AttachFiles(Statement statement, Dictionary<String, byte[]> files)
        {
            if (statement == null || statement.attachments == null || files == null)
            {
                return;
            }

            foreach (Attachment attachment in statement.attachments)
            {
                byte[] content;
                if (attachment != null && attachment.sha2 != null && files.TryGetValue(attachment.sha2, out content))
                {
                    attachment.content = content;
                }
            }
        }

        private static List<Attachment> ContentAttachments(IEnumerable<Statement> statements)
        {
            var attachments = new List<Attachment>();
            if (statements == null)
            {
                return attachments;
            }

            foreach (Statement statement in statements)
            {
                if (statement == null || statement.attachments == null)
                {
                    continue;
                }

                foreach (Attachment attachment in statement.attachments)
                {
                    if (attachment != null && attachment.content != null && attachment.content.Length > 0)
                    {
                        attachments.Add(attachment);
                    }
                }
            }

            return attachments;
        }

        private void SetStatementRequestBody(MyHTTPRequest req, String json, IList<Attachment> attachments)
        {
            if (attachments != null && attachments.Count > 0)
            {
                MultipartMixed body = MultipartMixed.Build(json, attachments);
                req.contentType = body.ContentType;
                req.content = body.Body;
                return;
            }

            req.contentType = "application/json";
            req.content = Encoding.UTF8.GetBytes(json);
        }

        private MyHTTPResponse SendDocument(String resource, Dictionary<String, String> queryParams, Document document, String method, String etag)
        {
            var req = new MyHTTPRequest();
            req.method = method;
            req.resource = resource;
            req.queryParams = queryParams;
            req.contentType = document.contentType;
            req.content = document.content;
            if (!String.IsNullOrEmpty(etag))
            {
                req.ifMatch = etag;
            }
            else
            {
                req.ifNoneMatch = "*";
            }

            return MakeSyncRequest(req);
        }

        private String FetchDocumentEtag(String resource, Dictionary<String, String> queryParams)
        {
            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = resource;
            req.queryParams = queryParams;

            var res = MakeSyncRequest(req);
            if (res.status == HttpStatusCode.OK && !String.IsNullOrEmpty(res.etag))
            {
                return res.etag;
            }

            return null;
        }

        private MyHTTPResponse GetDocument(String resource, Dictionary<String, String> queryParams, Document document)
        {
            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = resource;
            req.queryParams = queryParams;

            var res = MakeSyncRequest(req);
            if (res.status == HttpStatusCode.OK)
            {
                document.content = res.content;
                document.contentType = res.contentType;
                document.timestamp = res.lastModified;
                document.etag = res.etag;
            }

            return res;
        }

        private ProfileKeysLRSResponse GetProfileKeys(String resource, Dictionary<String, String> queryParams)
        {
            var r = new ProfileKeysLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = resource;
            req.queryParams = queryParams;

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;

            var keys = JArray.Parse(Encoding.UTF8.GetString(res.content));
            if (keys.Count > 0) {
                r.content = new List<String>();
                foreach (JToken key in keys) {
                    r.content.Add((String)key);
                }
            }

            return r;
        }

        private LRSResponse SaveDocument(String resource, Dictionary<String, String> queryParams, Document document, String method = "PUT")
        {
            var r = new LRSResponse();
            String originalEtag = document.etag;

            var res = SendDocument(resource, queryParams, document, method, originalEtag);
            if (IsDocumentSuccess(res))
            {
                if (!String.IsNullOrEmpty(res.etag))
                {
                    document.etag = res.etag;
                }
                r.success = true;
                ApplyTransport(r, res);
                return r;
            }

            if (String.IsNullOrEmpty(originalEtag) &&
                (res.status == HttpStatusCode.Conflict || res.status == HttpStatusCode.PreconditionFailed))
            {
                String currentEtag = FetchDocumentEtag(resource, queryParams);
                if (!String.IsNullOrEmpty(currentEtag))
                {
                    res = SendDocument(resource, queryParams, document, method, currentEtag);
                    if (IsDocumentSuccess(res))
                    {
                        document.etag = String.IsNullOrEmpty(res.etag) ? currentEtag : res.etag;
                        r.success = true;
                        ApplyTransport(r, res);
                        return r;
                    }
                }
            }

            r.success = false;
            r.httpException = res.ex;
            ApplyTransport(r, res);
            if (res.content != null)
            {
                r.SetErrMsgFromBytes(res.content);
            }
            return r;
        }

        private LRSResponse DeleteDocument(String resource, Dictionary<String, String> queryParams, String etag = null, bool concurrency = true)
        {
            var r = new LRSResponse();

            if (concurrency && String.IsNullOrEmpty(etag))
            {
                etag = FetchDocumentEtag(resource, queryParams);
            }

            var req = new MyHTTPRequest();
            req.method = "DELETE";
            req.resource = resource;
            req.queryParams = queryParams;
            if (concurrency && !String.IsNullOrEmpty(etag))
            {
                req.ifMatch = etag;
            }

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (!IsDocumentSuccess(res))
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;

            return r;
        }

        private StatementLRSResponse GetStatement(Dictionary<String, String> queryParams)
        {
            var r = new StatementLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = "statements";
            req.queryParams = queryParams;
            if (queryParams.ContainsKey("attachments") && queryParams["attachments"] == "true")
            {
                req.accept = "multipart/mixed, application/json";
            }

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            String json;
            Dictionary<String, byte[]> files;
            ReadStatementBody(res, out json, out files);

            r.success = true;
            r.content = new Statement(new Json.StringOfJSON(json));
            AttachFiles(r.content, files);

            return r;
        }

        public AboutLRSResponse About()
        {
            var r = new AboutLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = "about";

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;
            r.content = new About(Encoding.UTF8.GetString(res.content));

            return r;
        }

        public StatementLRSResponse SaveStatement(Statement statement)
        {
            var r = new StatementLRSResponse();
            var req = new MyHTTPRequest();
            req.queryParams = new Dictionary<String, String>();
            req.resource = "statements";

            if (statement.id == null)
            {
                req.method = "POST";
            }
            else
            {
                req.method = "PUT";
                req.queryParams.Add("statementId", statement.id.ToString());
            }

            SetStatementRequestBody(req, statement.ToJSON(version), ContentAttachments(new Statement[] { statement }));

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (statement.id == null)
            {
                if (res.status != HttpStatusCode.OK)
                {
                    r.success = false;
                    r.httpException = res.ex;
                    if (res.content != null)
                    {
                        r.SetErrMsgFromBytes(res.content);
                    }
                    return r;
                }

                var ids = JArray.Parse(Encoding.UTF8.GetString(res.content));
                statement.id = new Guid((String)ids[0]);
            }
            else {
                if (res.status != HttpStatusCode.NoContent)
                {
                    r.success = false;
                    r.httpException = res.ex;
                    if (res.content != null)
                    {
                        r.SetErrMsgFromBytes(res.content);
                    }
                    return r;
                }
            }

            r.success = true;
            r.content = statement;

            return r;
        }
        public StatementLRSResponse VoidStatement(Guid id, Agent agent)
        {
            var voidStatement = new Statement
            {
                actor = agent,
                verb = new Verb
                {
                    id = new Uri("http://adlnet.gov/expapi/verbs/voided"),
                    display = new LanguageMap()
                },
                target = new StatementRef { id = id }
            };
            voidStatement.verb.display.Add("en-US", "voided");

            return SaveStatement(voidStatement);
        }
        public StatementsResultLRSResponse SaveStatements(List<Statement> statements)
        {
            var r = new StatementsResultLRSResponse();

            var req = new MyHTTPRequest();
            req.resource = "statements";
            req.method = "POST";

            var jarray = new JArray();
            foreach (Statement st in statements)
            {
                jarray.Add(st.ToJObject(version));
            }
            SetStatementRequestBody(req, jarray.ToString(), ContentAttachments(statements));

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            var ids = JArray.Parse(Encoding.UTF8.GetString(res.content));
            for (int i = 0; i < ids.Count; i++)
            {
                statements[i].id = new Guid((String)ids[i]);
            }

            r.success = true;
            r.content = new StatementsResult(statements);

            return r;
        }
        public StatementLRSResponse RetrieveStatement(Guid id)
        {
            return RetrieveStatement(id, false);
        }
        public StatementLRSResponse RetrieveStatement(Guid id, bool attachments)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("statementId", id.ToString());
            if (attachments)
            {
                queryParams.Add("attachments", "true");
            }

            return GetStatement(queryParams);
        }
        public StatementLRSResponse RetrieveVoidedStatement(Guid id)
        {
            return RetrieveVoidedStatement(id, false);
        }
        public StatementLRSResponse RetrieveVoidedStatement(Guid id, bool attachments)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("voidedStatementId", id.ToString());
            if (attachments)
            {
                queryParams.Add("attachments", "true");
            }

            return GetStatement(queryParams);
        }
        public StatementsResultLRSResponse QueryStatements(StatementsQuery query)
        {
            var r = new StatementsResultLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = "statements";
            req.queryParams = query.ToParameterMap(version);
            if (query.attachments == true)
            {
                req.accept = "multipart/mixed, application/json";
            }

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;
            r.content = ReadStatementsResult(res);

            return r;
        }
        public StatementsResultLRSResponse MoreStatements(StatementsResult result)
        {
            var r = new StatementsResultLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = new Uri(endpoint, result.more).AbsoluteUri;
            req.accept = "multipart/mixed, application/json";

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;
            r.content = ReadStatementsResult(res);

            return r;
        }

        private StatementsResult ReadStatementsResult(MyHTTPResponse res)
        {
            String json;
            Dictionary<String, byte[]> files;
            ReadStatementBody(res, out json, out files);

            var result = new StatementsResult(new Json.StringOfJSON(json));
            if (result.statements != null)
            {
                foreach (Statement statement in result.statements)
                {
                    AttachFiles(statement, files);
                }
            }

            return result;
        }

        public ProfileKeysLRSResponse RetrieveStateIds(Activity activity, Agent agent, Nullable<Guid> registration = null, Nullable<DateTime> since = null)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("activityId", activity.id.ToString());
            queryParams.Add("agent", agent.ToJSON(version));
            if (registration != null)
            {
                queryParams.Add("registration", registration.ToString());
            }
            if (since != null)
            {
                queryParams.Add("since", Json.Timestamp.Format(since.Value));
            }

            return GetProfileKeys("activities/state", queryParams);
        }
        public StateLRSResponse RetrieveState(String id, Activity activity, Agent agent, Nullable<Guid> registration = null)
        {
            var r = new StateLRSResponse();

            var queryParams = new Dictionary<String, String>();
            queryParams.Add("stateId", id);
            queryParams.Add("activityId", activity.id.ToString());
            queryParams.Add("agent", agent.ToJSON(version));

            var state = new StateDocument();
            state.id = id;
            state.activity = activity;
            state.agent = agent;

            if (registration != null)
            {
                queryParams.Add("registration", registration.ToString());
                state.registration = registration;
            }

            var resp = GetDocument("activities/state", queryParams, state);
            ApplyTransport(r, resp);
            if (resp.status != HttpStatusCode.OK && resp.status != HttpStatusCode.NotFound)
            {
                r.success = false;
                r.httpException = resp.ex;
                r.SetErrMsgFromBytes(resp.content);
                return r;
            }
            r.success = true;
            r.content = state;

            return r;
        }
        public LRSResponse SaveState(StateDocument state)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("stateId", state.id);
            queryParams.Add("activityId", state.activity.id.ToString());
            queryParams.Add("agent", state.agent.ToJSON(version));
            if (state.registration != null)
            {
                queryParams.Add("registration", state.registration.ToString());
            }

            return SaveDocument("activities/state", queryParams, state);
        }
        public LRSResponse UpdateState(StateDocument state)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("stateId", state.id);
            queryParams.Add("activityId", state.activity.id.ToString());
            queryParams.Add("agent", state.agent.ToJSON(version));
            if (state.registration != null)
            {
                queryParams.Add("registration", state.registration.ToString());
            }

            return SaveDocument("activities/state", queryParams, state, "POST");
        }
        public LRSResponse DeleteState(StateDocument state)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("stateId", state.id);
            queryParams.Add("activityId", state.activity.id.ToString());
            queryParams.Add("agent", state.agent.ToJSON(version));
            if (state.registration != null)
            {
                queryParams.Add("registration", state.registration.ToString());
            }

            return DeleteDocument("activities/state", queryParams, state.etag, true);
        }
        public LRSResponse ClearState(Activity activity, Agent agent, Nullable<Guid> registration = null)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("activityId", activity.id.ToString());
            queryParams.Add("agent", agent.ToJSON(version));
            if (registration != null)
            {
                queryParams.Add("registration", registration.ToString());
            }

            return DeleteDocument("activities/state", queryParams, null, false);
        }

        public ProfileKeysLRSResponse RetrieveActivityProfileIds(Activity activity, Nullable<DateTime> since = null)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("activityId", activity.id.ToString());
            if (since != null)
            {
                queryParams.Add("since", Json.Timestamp.Format(since.Value));
            }

            return GetProfileKeys("activities/profile", queryParams);
        }
        public ActivityProfileLRSResponse RetrieveActivityProfile(String id, Activity activity)
        {
            var r = new ActivityProfileLRSResponse();

            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", id);
            queryParams.Add("activityId", activity.id.ToString());

            var profile = new ActivityProfileDocument();
            profile.id = id;
            profile.activity = activity;

            var resp = GetDocument("activities/profile", queryParams, profile);
            ApplyTransport(r, resp);
            if (resp.status != HttpStatusCode.OK && resp.status != HttpStatusCode.NotFound)
            {
                r.success = false;
                r.httpException = resp.ex;
                r.SetErrMsgFromBytes(resp.content);
                return r;
            }
            r.success = true;
            r.content = profile;

            return r;
        }
        public LRSResponse SaveActivityProfile(ActivityProfileDocument profile)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("activityId", profile.activity.id.ToString());

            return SaveDocument("activities/profile", queryParams, profile);
        }
        public LRSResponse UpdateActivityProfile(ActivityProfileDocument profile)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("activityId", profile.activity.id.ToString());

            return SaveDocument("activities/profile", queryParams, profile, "POST");
        }
        public LRSResponse DeleteActivityProfile(ActivityProfileDocument profile)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("activityId", profile.activity.id.ToString());

            return DeleteDocument("activities/profile", queryParams, profile.etag, true);
        }

        public ProfileKeysLRSResponse RetrieveAgentProfileIds(Agent agent, Nullable<DateTime> since = null)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("agent", agent.ToJSON(version));
            if (since != null)
            {
                queryParams.Add("since", Json.Timestamp.Format(since.Value));
            }

            return GetProfileKeys("agents/profile", queryParams);
        }
        public AgentProfileLRSResponse RetrieveAgentProfile(String id, Agent agent)
        {
            var r = new AgentProfileLRSResponse();

            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", id);
            queryParams.Add("agent", agent.ToJSON(version));

            var profile = new AgentProfileDocument();
            profile.id = id;
            profile.agent = agent;

            var resp = GetDocument("agents/profile", queryParams, profile);
            ApplyTransport(r, resp);
            if (resp.status != HttpStatusCode.OK && resp.status != HttpStatusCode.NotFound)
            {
                r.success = false;
                r.httpException = resp.ex;
                r.SetErrMsgFromBytes(resp.content);
                return r;
            }
            r.success = true;
            r.content = profile;

            return r;
        }
        public LRSResponse SaveAgentProfile(AgentProfileDocument profile)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("agent", profile.agent.ToJSON(version));

            return SaveDocument("agents/profile", queryParams, profile);
        }
        public LRSResponse UpdateAgentProfile(AgentProfileDocument profile)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("agent", profile.agent.ToJSON(version));

            return SaveDocument("agents/profile", queryParams, profile, "POST");
        }
        public LRSResponse DeleteAgentProfile(AgentProfileDocument profile)
        {
            var queryParams = new Dictionary<String, String>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("agent", profile.agent.ToJSON(version));

            return DeleteDocument("agents/profile", queryParams, profile.etag, true);
        }

        public PersonLRSResponse RetrieveAgent(Agent agent)
        {
            var r = new PersonLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = "agents";
            req.queryParams.Add("agent", agent.ToJSON(version));

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;
            r.content = new Person(new Json.StringOfJSON(Encoding.UTF8.GetString(res.content)));

            return r;
        }

        public ActivityLRSResponse RetrieveActivity(Uri activityId)
        {
            var r = new ActivityLRSResponse();

            var req = new MyHTTPRequest();
            req.method = "GET";
            req.resource = "activities";
            req.queryParams.Add("activityId", activityId.ToString());

            var res = MakeSyncRequest(req);
            ApplyTransport(r, res);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                if (res.content != null)
                {
                    r.SetErrMsgFromBytes(res.content);
                }
                return r;
            }

            r.success = true;
            r.content = new Activity(new Json.StringOfJSON(Encoding.UTF8.GetString(res.content)));

            return r;
        }
    }
}
