﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json.Linq;
using Strisys.AspNet.WebHooks.Receivers.FileThis.Properties;
using Strisys.AspNet.WebHooks.Receivers.FileThis.V1;

namespace Strisys.AspNet.WebHooks.Receivers.FileThis {
    /// <summary>
    /// Provides an <see cref="IWebHookReceiver"/> implementation which supports WebHooks generated by FileThis. 
    /// Set the '<c>MS_WebHookReceiverSecret_FileThis</c>' application setting to the application secrets, optionally using IDs
    /// to differentiate between multiple WebHooks, for example '<c>secret0, id1=secret1, id2=secret2</c>'.
    /// The corresponding WebHook URI is of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/FileThis/{id}</c>'.
    /// For details about FileThis WebHooks, see <c>https://filethis.com/developers/</c>.
    /// </summary>
    public class FileThisWebHookReceiver : WebHookReceiverBase {
        internal const Int32 SecretMinLength = 13, SecretMaxLength = 52;
        internal const String RecName = "FileThis";
        internal const String SignatureHeaderName = "X-FileThis-Signature";
        internal const String DefaultAction = "change";

        /// <summary>
        /// Gets the receiver name for this receiver.
        /// </summary>
        public static String ReceiverName {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override String Name {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override async Task<HttpResponseMessage> ReceiveAsync(String id, HttpRequestContext context, HttpRequestMessage request) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Method != HttpMethod.Post) {
                return CreateBadMethodResponse(request);
            }

            Boolean valid = await VerifySignature(request, id);

            if (false == valid) {
                return CreateBadSignatureResponse(request, SignatureHeaderName);
            }

            // Read the request entity body
            if (request.Content.IsJson()) {
                JObject data = await ReadAsJsonAsync(request);
                return await ExecuteWebHookAsync(id, context, request, new[] { DefaultAction }, data);
            }

            // Call registered handlers
            if (request.Content.IsFormData()) {
                NameValueCollection nameValues = await ReadAsFormDataAsync(request);
                return await ExecuteWebHookAsync(id, context, request, new[] { DefaultAction }, nameValues);
            }

            try {
                IList<Document> values = await ReadAsMultiPartContent(request);
                return await ExecuteWebHookAsync(id, context, request, new[] { DefaultAction }, values);
            }
            catch(Exception ex) {
                String message = "Unrecognized content";
                request.GetConfiguration().DependencyResolver.GetLogger().Error("Unrecognized content", ex);

                HttpResponseMessage invalidEncoding = request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
                throw new HttpResponseException(invalidEncoding);
            }
        }
        
        /// <summary>
        /// Verifies that the signature header matches that of the actual body.
        /// </summary>
        protected virtual async Task<Boolean> VerifySignature(HttpRequestMessage request, String id) {
            //return true;

            String secretKey = await GetReceiverConfig(request, Name, id, SecretMinLength, SecretMaxLength);
            Byte[] expectedHash = Encoding.ASCII.GetBytes(secretKey);

            // Get the expected hash from the signature header
            String signatureHeaderValue = GetRequestHeader(request, SignatureHeaderName);
            Byte[] actualHash;

            try {
                actualHash = EncodingUtilities.FromBase64(signatureHeaderValue);
            }
            catch (Exception ex) {
                String msg = String.Format(CultureInfo.CurrentCulture, FileThisReceiverResources.Receiver_BadHeaderEncoding, SignatureHeaderName);
                request.GetConfiguration().DependencyResolver.GetLogger().Error(msg, ex);

                HttpResponseMessage invalidEncoding = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(invalidEncoding);
            }

            // Now verify that the provided hash matches the expected hash.
            SecretEqual(expectedHash, actualHash);
            return true;
        }
    }
}
