/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation.HttpLog
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Packaging;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Handles storing HTTP request/response sessions into a file format compatible with
    /// Telerik Fiddler Web Debugger to make debugging API issues easier.
    /// </summary>
    public class HttpLogGenerator
    {
        private Package outputPackage;
        private int nextSessionNumber = 1;

        public string OutputFile
        {
            get; private set;
        }

        public HttpLogGenerator(string outputFile)
        {
            this.OutputFile = outputFile;
        }

        public void InitializePackage()
        {
            this.outputPackage = Package.Open(this.OutputFile,
                System.IO.FileMode.Create,
                System.IO.FileAccess.ReadWrite);

            this.outputPackage.CreatePart(new Uri("/_index.htm", UriKind.Relative), "text/html");
        }

        public void ClosePackage()
        {
            this.outputPackage.Close();
            this.outputPackage = null;
        }


        public async Task RecordSessionAsync(Http.HttpRequest request, Http.HttpResponse response, TimeSpan duration)
        {
            int sessionId = -1;
            lock(this)
            {
                sessionId = nextSessionNumber++;
            }

            // Write client request
            var requestPart = this.outputPackage.CreatePart(UriForPart(sessionId, 'c'), "text/plain");
            using (var requestStream = requestPart.GetStream(System.IO.FileMode.Create))
            {
                await request.WriteToStreamAsync(requestStream);
            }

            var responsePart = this.outputPackage.CreatePart(UriForPart(sessionId, 's'), "text/plain");
            using (var responseStream = responsePart.GetStream(System.IO.FileMode.Create))
            {
                await response.WriteToStreamAsync(responseStream);
            }

            var metadataPart = this.outputPackage.CreatePart(UriForPart(sessionId, 'm'), "application/xml");
            var metadata = CreateMetadataForSession(sessionId, request.StartTime, duration);
            using (var metadataStream = metadataPart.GetStream(System.IO.FileMode.Create))
            {
                await metadata.WriteToStreamAsync(metadataStream);
            }
        }

        private static SessionMetadata CreateMetadataForSession(int sessionId, DateTimeOffset startTime, TimeSpan duration)
        {
            var metadata = new SessionMetadata
            {
                SessionID = sessionId,
                BitFlags = 59
            };

            metadata.PipeInfo = new PipeInfo { Streamed = true, Reused = false, CltReuse = false };
            const string format = @"yyyy-MM-ddTHH\:mm\:ss.fffffffzzz";
            metadata.SessionTimers = new SessionTimers
            {
                ClientConnected = startTime.ToString(format),
                ClientBeginRequest = startTime.ToString(format),
                GotRequestHeaders = startTime.ToString(format),
                ClientDoneRequest = startTime.ToString(format),
                ServerConnected = startTime.ToString(format),
                FiddlerBeginRequest = startTime.ToString(format),
                ServerGotRequest = startTime.ToString(format),
                ServerBeginResponse = startTime.Add(duration).ToString(format),
                GotResponseHeaders = startTime.Add(duration).ToString(format),
                ServerDoneResponse = startTime.Add(duration).ToString(format),
                ClientBeginResponse = startTime.Add(duration).ToString(format),
                ClientDoneResponse = startTime.Add(duration).ToString(format)
            };

            metadata.SessionFlags.Add(new SessionFlag { Name = SessionFlag.ClientIP, Value = "127.0.0.1" });
            metadata.SessionFlags.Add(new SessionFlag { Name = SessionFlag.ProcessInfo, Value = "apidocs.exe:1234" });

            return metadata;
        }

        private static Uri UriForPart(int sessionId, char mode)
        {
            return new Uri(string.Format("/raw/{0:D4}_{1}.{2}", sessionId, mode, (mode == 'm') ? "xml" : "txt"), UriKind.Relative);
        }
    }


    internal class SessionLogEntry
    {
        public int EntryNumber
        {
            get; set;
        }

        public string Protocol
        {
            get; set;
        }

        public string Host
        {
            get; set;
        }

        public string URL
        {
            get; set;
        }

        public int BodySize
        {
            get; set;
        }

        public string CachingHeaders
        {
            get; set;
        }

        public string ContentType
        {
            get; set;
        }

        public string Process
        {
            get; set;
        }

        public string Comments
        {
            get; set;
        }

        public string Custom
        {
            get; set;
        }

    }
    
    [XmlRoot("Session")]
    public class SessionMetadata
    {
        public SessionMetadata()
        {
            this.SessionFlags = new List<SessionFlag>();
        }

        [XmlAttribute("SID")]
        public int SessionID
        {
            get; set;
        }    

        [XmlAttribute]
        public int BitFlags
        {
            get; set;
        }

        [XmlElement]
        public SessionTimers SessionTimers { get; set; }
        
        [XmlElement]
        public PipeInfo PipeInfo { get; set; }

        [XmlArray("SessionFlags")]
        public List<SessionFlag> SessionFlags
        {
            get; set;
        }

        internal Task WriteToStreamAsync(Stream metadataStream)
        {
            XmlSerializer serialize = new XmlSerializer(this.GetType());
            serialize.Serialize(metadataStream, this);
            return Task.FromResult<bool>(true);
        }
    }

    public class PipeInfo
    {
        [XmlAttribute("Streamed")]
        public bool Streamed
        {
            get; set;
        }

        [XmlAttribute("CltReuse")]
        public bool CltReuse
        {
            get; set;
        }

        [XmlAttribute("Reused")]
        public bool Reused  {get;set;}
    }

    public class SessionFlag
    {
        [XmlAttribute("N")]
        public string Name { get; set; }

        [XmlAttribute("V")]
        public string Value { get; set; }

        public const string EgressPort = "x-egressport";
        public const string ResponseBodyTransferLength = "x-responsebodytransferlength";
        public const string ClientPort = "x-clientport";
        public const string ClientIP = "x-clientip";
        public const string ServerSocket = "x-serversocket";
        public const string HostIP = "x-hostip";
        public const string ProcessInfo = "x-processinfo";
    }

    public class SessionTimers
    {
        [XmlAttribute]
        public string ClientConnected
        {
            get; set;
        }

        [XmlAttribute]
        public string ClientBeginRequest
        {
            get; set;
        }

        [XmlAttribute]
        public string GotRequestHeaders
        {
            get; set;
        }

        [XmlAttribute]
        public string ClientDoneRequest
        {
            get; set;
        }

        [XmlAttribute]
        public int GatewayTime
        {
            get; set;
        }

        [XmlAttribute]
        public int DNSTime
        {
            get; set;
        }

        [XmlAttribute]
        public int TCPConnectTime
        {
            get; set;
        }

        [XmlAttribute]
        public int HTTPSHandshakeTime {get;set;}

        [XmlAttribute]
        public string ServerConnected { get; set; }

        [XmlAttribute]
        public string FiddlerBeginRequest
        {
            get; set;
        }

        [XmlAttribute]
        public string ServerGotRequest
        {
            get; set;
        }

        [XmlAttribute]
        public string ServerBeginResponse
        {
            get; set;
        }

        [XmlAttribute]
        public string GotResponseHeaders
        {
            get; set;
        }

        [XmlAttribute]
        public string ServerDoneResponse
        {
            get; set;
        }

        [XmlAttribute]
        public string ClientBeginResponse
        {
            get; set;
        }

        [XmlAttribute]
        public string ClientDoneResponse
        {
            get; set;
        }

    }
}
