using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json.Linq;
using Strisys.AspNet.WebHooks.Receivers.FileThis;
using Strisys.AspNet.WebHooks.Receivers.FileThis.V1;

namespace DropBoxHandler.Handlers {
    public class FileThisHandler : WebHookHandler {
        private const String TargetBaseDirectory = @"C:\\Temp\\FileThis";

        public FileThisHandler() {
            Receiver = FileThisWebHookReceiver.ReceiverName;
        }

        public override Task ExecuteAsync(String receiver, WebHookHandlerContext context) {
            HttpRequestMessage request = context.Request;
            Debug.WriteLine(request);

            NameValueCollection nameValueCollection = (context.Data as NameValueCollection);

            if (null != nameValueCollection) {
                return Task.FromResult(true);
            }

            JObject jobject = (context.Data as JObject);

            if (null != jobject) {
                Debug.WriteLine(jobject);
                return Task.FromResult(true);
            }
            
            return Task.Run(() => TryProcessMultPartData(context));
        }

        private Task TryProcessMultPartData(WebHookHandlerContext context) {
            IList<Document> documents = (context.Data as IList<Document>);

            if (null == documents) {
                return Task.FromResult(true);
            }

            try {
                foreach (Document document in documents) {
                    String path = String.Format("{0}\\{1}\\{2}\\{3}\\{4}", TargetBaseDirectory, document.AccountName, document.DocumentSubType, document.Date.ToString("yyyy-MM-dd"), document.FileName);
                    document.WriteTo(path);
                }
            }
            catch (Exception ex) {
                context.Request.GetLogger().Error("Unrecognized content", ex);
            }

            return Task.FromResult(true);
        }
    }
}