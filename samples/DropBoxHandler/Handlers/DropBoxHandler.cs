using System;
using System.Threading.Tasks;
using Microsoft.AspNet.WebHooks;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace DropBoxHandler.Handlers {
    public class DropBoxHandler : WebHookHandler {
        public DropBoxHandler() {
            Receiver = DropboxWebHookReceiver.ReceiverName;
        }

        public override Task ExecuteAsync(String receiver, WebHookHandlerContext context) {
            HttpRequestMessage request = context.Request;
            Debug.WriteLine(request);

            Debug.WriteLine(context.Actions.FirstOrDefault() ?? String.Empty);
            JObject entry = context.GetDataOrDefault<JObject>();
            Debug.WriteLine(entry);
            return Task.FromResult(true);
        }
    }
}