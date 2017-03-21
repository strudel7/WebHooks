// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.AspNet.WebHooks.Config;

namespace System.Web.Http {
    /// <summary>
    /// Extension methods for <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions {
        /// <summary>
        /// Initializes support for receiving FileThis WebHooks.
        /// Set the '<c>MS_WebHookReceiverSecret_FileThis</c>' application setting to the application secrets, optionally using IDs
        /// to differentiate between multiple WebHooks, for example '<c>secret0, id1=secret1, id2=secret2</c>'.
        /// The corresponding WebHook URI is of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/filethis/{id}</c>'.
        /// For details about Dropbox WebHooks, see <c>https://www.dropbox.com/developers/webhooks/docs</c>.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>config.</param>
        public static void InitializeReceiveFileThisWebHooks(this HttpConfiguration config) {
            WebHooksConfig.Initialize(config);
        }
    }
}
