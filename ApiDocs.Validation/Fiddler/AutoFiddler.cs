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

namespace ApiDocs.Validation.Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// Handles storing HTTP request/response sessions into a file format compatible with
    /// Telerik Fiddler Web Debugger to make debugging API issues easier.
    /// </summary>
    public class AutoFiddler
    {
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n" +
            "<Types xmlns = \"http://schemas.openxmlformats.org/package/2006/content-types\" >\r\n" +
            "< Default Extension=\"htm\" ContentType=\"text/html\" />\r\n" +
            "<Default Extension = \"xml\" ContentType=\"application/xml\" />\r\n" +
            "<Default Extension = \"txt\" ContentType=\"text/plain\" />\r\n" +
            "</Types>\r\n";
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

    
    internal class SessionMetadata
    {
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
        

        

    }

    internal class SessionTimers
    {
        [XmlAttribute]
        public DateTimeOffset ClientConnected
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ClientBeginRequest
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset GotRequestHeaders
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ClientDoneRequest
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
        public DateTimeOffset ServerConnected { get; set; }

        [XmlAttribute]
        public DateTimeOffset FiddlerBeginRequest
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ServerGotRequest
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ServerBeginResponse
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset GotResponseHeaders
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ServerDoneResponse
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ClientBeginResposne
        {
            get; set;
        }

        [XmlAttribute]
        public DateTimeOffset ClientDoneResponse
        {
            get; set;
        }

    }
}
