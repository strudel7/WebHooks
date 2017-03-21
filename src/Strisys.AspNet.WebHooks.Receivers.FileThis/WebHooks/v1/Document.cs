using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;

namespace Strisys.AspNet.WebHooks.Receivers.FileThis.V1 {
    /// <summary>
    /// Represents the document content and meta-data associated with it that 
    /// is received from <b>FileThis</b>.
    /// </summary>
    public class Document : IEquatable<Document> {
        #region Fields

        private readonly HttpRequestMessage _request;
        private readonly HttpContent _header, _data;
        private readonly JObject _headerJson;
        private Object _tag;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Document" /> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="header">The header.</param>
        /// <param name="headerJson">The header json.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="ArgumentNullException"> </exception>
        public Document(HttpRequestMessage request, HttpContent header, JObject headerJson, HttpContent data) {
            if (null == request) {
                throw new ArgumentNullException(nameof(request));
            }

            if (header == null) {
                throw new ArgumentNullException(nameof(header));
            }

            if (headerJson == null) {
                throw new ArgumentNullException(nameof(headerJson));
            }

            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }

            _request = request;
            _header = header;
            _headerJson = headerJson;
            _data = data;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <value>
        /// The request message.
        /// </value>
        public virtual HttpRequestMessage Request {
            get { return _request; }
        }

        /// <summary>
        /// Gets the <see cref="HttpContent"/> header.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        public virtual HttpContent RawHeader {
            get { return _header; }
        }

        /// <summary>
        /// Gets the <see cref="JObject"/> header.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        public virtual JObject HeaderJson {
            get { return _headerJson; }
        }

        /// <summary>
        /// Gets the <see cref="HttpContent"/> data.
        /// </summary>
        public virtual HttpContent RawData {
            get { return _data; }
        }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public Object Tag {
            get {  return _tag; }
            set { _tag = value; }
        }

        #endregion

        #region Header Properties

        /// <summary>
        /// Gets the version of the meta-data.
        /// </summary>
        public virtual String Version {
            get { return _headerJson["version"].Value<String>(); }
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        public virtual DateTime Date {
            get {
                try {
                    return _headerJson["document"]["date"].Value<DateTime>();
                }
                catch {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Gets the source identifier.
        /// </summary>
        public virtual String SourceId {
            get { return _headerJson["document"]["sourceId"].Value<String>(); }
        }

        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String DocumentId {
            get { return _headerJson["document"]["documentId"].Value<String>(); }
        }

        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String RawFileName {
            get { return _headerJson["document"]["filename"].Value<String>(); }
        }

        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String FileName {
            get {
                String val = RawFileName;
                val = val.Replace(PartnerAccountName, String.Empty);

                while (val.StartsWith("-")) {
                    val = val.Substring(3);
                }

                return val;
            }
        }
    
        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String Institution {
            get { return _headerJson["document"]["institution"].Value<String>(); }
        }

        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String DocumentType {
            get {
                try {
                    return _headerJson["document"]["documentType"].Value<String>();
                }
                catch {
                    return "unknown";
                }                
            }
        }

        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String DocumentSubType {
            get {
                String docType = DocumentType;
                String subType = null;

                try {
                    if (docType.Equals("notice", StringComparison.OrdinalIgnoreCase)) {
                        subType = _headerJson["document"]["documentSubtype"].Value<String>();
                    }
                }
                catch {
                    subType = String.Format("{0}-notspecified", subType);
                }

                return (subType ?? docType);
            }
        }

        /// <summary>
        /// Gets the name of the document.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String ConnectionId {
            get { return _headerJson["document"]["connectionId"].Value<String>(); }
        }

        /// <summary>
        /// Gets the name of the account.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public virtual String PartnerAccountName {
            get { return _headerJson["partnerAccountId"].Value<String>();  }
        }

        #endregion

        #region Parse

        /// <summary>
        /// Parses the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public static async Task<IList<Document>> Parse(HttpRequestMessage request) {
            return await ReadAsMultiPartContent(request);
        }

        /// <summary>
        /// Reads the content of as multi-part message.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private static async Task<IList<Document>> ReadAsMultiPartContent(HttpRequestMessage request) {
            Stream body = await request.Content.ReadAsStreamAsync();

            StreamContent streamContent = new StreamContent(body);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.Content.Headers.ContentType.ToString());

            MultipartStreamProvider provider = await streamContent.ReadAsMultipartAsync();
            HttpConfiguration config = request.GetConfiguration();
            IList<Document> documents = new List<Document>(5);
            HttpContent header = null;
            JObject headerJson = null;

            // Documents come through as json/content pairs so interate 
            // through the content and pair them
            foreach(HttpContent httpContent in provider.Contents) {
                if (httpContent.IsJson()) {
                    headerJson = await httpContent.ReadAsAsync<JObject>(config.Formatters);
                    header = httpContent;

                    System.Diagnostics.Debug.WriteLine(headerJson);
                    continue;
                }

                documents.Add(new Document(request, header, headerJson, httpContent));
            }

            return await Task.FromResult(documents.ToArray());
        }

        #endregion

        #region WriteToAsync
        /// <summary>
        /// Gets the content bytes.  If <paramref name="includeMetaData"/> is <c>true</c>
        /// the first file will be the meta-data file.
        /// </summary>
        /// <param name="includeMetaData">if set to <c>true</c> [include meta data].</param>
        /// <returns></returns>
        public Task<IList<Byte[]>> GetBytesAsync(Boolean includeMetaData = true) {
            return GetStreamsAsync().ContinueWith((t) => {
                IList<Byte[]> bytesList = new List<Byte[]>(t.Result.Count);

                foreach (Stream stream in t.Result) {
                    bytesList.Add(ToBytes(stream));
                }

                foreach (Byte[] bytes in bytesList) {
                    if (0 == bytes.Length) {
                        System.Diagnostics.Debug.Write("HERE");
                    }
                }

                return bytesList;
            });
        }

        private Byte[] ToBytes(Stream stream) {
            Byte[] bytes;

            using (stream) {
                stream.Seek(0, SeekOrigin.Begin);

                MemoryStream memoryStream = (stream as MemoryStream);

                if (null != memoryStream) {
                    using (memoryStream) {
                        return memoryStream.ToArray();
                    }
                }

                using (MemoryStream buffer = new MemoryStream()) {
                    stream.CopyTo(buffer);
                    bytes = buffer.ToArray();
                }
            }

            if (0 == bytes.Length) {
                System.Diagnostics.Debug.Write("HERE");
            }

            return bytes;
        }

        /// <summary>
        /// Gets the content streams.  If <paramref name="includeMetaData"/> is <c>true</c>
        /// the first file will be the meta-data file.
        /// </summary>
        /// <param name="includeMetaData">if set to <c>true</c> [include meta data].</param>
        /// <returns></returns>
        public Task<IList<Stream>> GetStreamsAsync(Boolean includeMetaData = true) {
            IList<Task> ioTasks = new List<Task>();

            return Task.Run(() => {
                IList<Stream> streams = new List<Stream>(2);
                
                if (includeMetaData) {
                    String json = _headerJson.ToString();
                    Byte[] bytes = Encoding.UTF8.GetBytes(json);

                    MemoryStream metaStream = new MemoryStream();
                    streams.Add(metaStream);

                    ioTasks.Add(metaStream.WriteAsync(bytes, 0, bytes.Length).ContinueWith((f) => {
                        metaStream.Seek(0, SeekOrigin.Begin);
                    }));
                }

                ioTasks.Add(_data.ReadAsStreamAsync().ContinueWith((t) => {
                    // Always add file stream 2nd
                    Stream stream = t.Result;
                    stream.Seek(0, SeekOrigin.Begin);
                    streams.Add(stream);
                }));
                
                Task.WaitAll(ioTasks.ToArray());

                return streams;
            });
        }

        /// <summary>
        /// Gets the meta-data file path give the full path of the file.
        /// </summary>
        /// <param name="fullPath">The file path.</param>
        /// <returns></returns>
        public virtual String GetMetaDataFilePath(String fullPath) {
            String directory = CreateDirectoryName(fullPath);
            String fileName = Path.GetFileNameWithoutExtension(fullPath);
            return Path.Combine(directory, String.Format("{0}.json", fileName));
        }

        /// <summary>
        /// Writes this document to the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="includeMetaData">if set to <c>true</c> [include meta data].</param>
        /// <returns></returns>
        public Task<IList<String>> WriteToAsync(String path, Boolean includeMetaData = true) {
            IList<Task> ioTasks = new List<Task>();

            return Task.Run(() => {
                IList<String> paths = new List<String>();

                ioTasks.Add(_data.ReadAsStreamAsync().ContinueWith((t) => {
                    Stream stream = t.Result;
                    stream.Seek(0, SeekOrigin.Begin);

                    String directory = CreateDirectoryName(path);

                    if ((false == String.IsNullOrWhiteSpace(directory)) && (false == Directory.Exists(directory))) {
                        Directory.CreateDirectory(directory);
                    }
                    
                    if (includeMetaData) {
                        String metaPath = GetMetaDataFilePath(path);
                        paths.Add(metaPath);

                        String json = _headerJson.ToString();
                        Byte[] bytes = Encoding.UTF8.GetBytes(json);

                        FileStream metaStream = File.Create(metaPath, bytes.Length, FileOptions.Asynchronous);

                        ioTasks.Add(metaStream.WriteAsync(bytes, 0, bytes.Length).ContinueWith((f) => {
                            metaStream.Close();
                        }));
                    }
                    
                    String fullPath = Path.Combine(directory, Path.GetFileName(path));
                    paths.Add(fullPath);

                    FileStream fileStream = File.Create(fullPath, ((Int32)stream.Length), FileOptions.Asynchronous);
                    stream.CopyTo(fileStream);

                    ioTasks.Add(stream.CopyToAsync(fileStream).ContinueWith((f) => {
                        fileStream.Close();
                    }));
                }));

                Task.WaitAll(ioTasks.ToArray());

                return paths;
            });
        }

        private static String CreateDirectoryName(String path) {
            String directory = Path.GetDirectoryName(path);
            Char[] characters = Path.GetInvalidPathChars();
            String replacementChar = "-";

            directory = directory.Replace("*", replacementChar)
                                 .Replace("?", replacementChar)
                                 .Replace("<", replacementChar).Replace(">", replacementChar)
                                 .Replace("|", replacementChar);

            foreach (Char c in characters) {
                directory = directory.Replace(c.ToString(), "-");
            }

            return directory;
        }

        #endregion

        #region Equality

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override Int32 GetHashCode() {
            return _request.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        public override Boolean Equals(Object obj) {
            return Equals(obj as Document);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Boolean Equals(Document other) {
            return ((null != other) && (ReferenceEquals(this, other) || _request.Equals(other.Request)));
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override String ToString() {
            return FileName;
        }
        
        #endregion
    }
}


