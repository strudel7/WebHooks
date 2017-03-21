using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.WebHooks;
using Strisys.AspNet.WebHooks.Receivers.FileThis.V1;

namespace Strisys.AspNet.WebHooks.Receivers.FileThis {
    public abstract class WebHookReceiverBase : WebHookReceiver {
        protected virtual async Task<IList<Document>> ReadAsMultiPartContent(HttpRequestMessage request) {
            // TODO: Pivot on which document implementation to use
            // based on version
            return await Document.Parse(request);
        }
    }
}
