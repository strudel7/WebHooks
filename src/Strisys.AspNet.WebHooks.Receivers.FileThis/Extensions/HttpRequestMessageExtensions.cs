// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Diagnostics;

namespace System.Net.Http {
    /// <summary>
    /// Extension methods for <see cref="HttpRequestMessage"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpRequestMessageExtensions {
        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public static ILogger GetLogger(this HttpRequestMessage request) {
            return request.GetConfiguration().DependencyResolver.GetLogger();
        }
    }
}
