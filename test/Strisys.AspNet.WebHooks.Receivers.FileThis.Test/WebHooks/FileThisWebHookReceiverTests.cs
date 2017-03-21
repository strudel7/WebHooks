// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Moq;
using Moq.Protected;
using Strisys.AspNet.WebHooks.Receivers.FileThis;
using Xunit;

namespace Microsoft.AspNet.WebHooks {
    public class FileThisWebHookReceiverTests : WebHookReceiverTestsBase<FileThisWebHookReceiver> {
        private const String TestContent = "{ \"key\": \"value\" }";
        private const String TestId = "";
        private const String TestSecret = "82bl18r7h4u85frsp0foqhjh4m";

        private HttpRequestMessage _getRequest;
        private HttpRequestMessage _postRequest;

        private readonly String _testSignature;

        public FileThisWebHookReceiverTests() {
            Byte[] bytes = Encoding.UTF8.GetBytes(TestSecret);
            _testSignature = EncodingUtilities.ToBase64(bytes, true);
        }

        [Fact]
        public void ReceiverName_IsConsistent() {
            // Arrange
            IWebHookReceiver rec = new FileThisWebHookReceiver();
            String expected = "FileThis";

            // Act
            String actual1 = rec.Name;
            String actual2 = FileThisWebHookReceiver.ReceiverName;

            // Assert
            Assert.Equal(expected, actual1);
            Assert.Equal(actual1, actual2);
        }

        [Fact]
        public async Task ReceiveAsync_Throws_IfPostHasNoSignatureHeader() {
            // Arrange
            Initialize(TestSecret);

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("Expecting exactly one 'Authorization' header field in the WebHook request but found 0. Please ensure that the request contains exactly one 'Authorization' header field.", error.Message);
            ReceiverMock.Protected().Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Fact]
        public async Task ReceiveAsync_ReturnError_IfPostHasInvalidSignature() {
            // Arrange
            Initialize(TestSecret);

            String invalid = EncodingUtilities.ToBase64(Encoding.UTF8.GetBytes("wrongsignature"), true);
            _postRequest.Headers.Add(FileThisWebHookReceiver.SignatureHeaderName, invalid);

            // Act
            HttpResponseMessage actual = await ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _postRequest);

            // Assert
            HttpError error = await actual.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook signature provided by the 'Authorization' header field does not match the value expected by the 'FileThisWebHookReceiverProxy' receiver. WebHook request is invalid.", error.Message);
            ReceiverMock.Protected().Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        private HttpRequestMessage ConfigureValidPostRequest() {
            Initialize(TestSecret);
            _postRequest.Headers.Add(FileThisWebHookReceiver.SignatureHeaderName, _testSignature);
            return _postRequest;
        }

        [Fact]
        public async Task ReceiveAsync_Throws_IfPostIsNotJson() {
            // Arrange
            ConfigureValidPostRequest().Content = new StringContent(TestContent, Encoding.UTF8, "text/plain");

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook request must contain an entity body formatted as JSON.", error.Message);
            ReceiverMock.Protected().Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Theory]
        [MemberData(nameof(ValidIdData))]
        public async Task ReceiveAsync_Succeeds_IfValidPostRequest(String id) {
            // Arrange
            Initialize(GetConfigValue(id, TestSecret));
            List<string> actions = new List<string> { "change" };

            _postRequest.Headers.Add(FileThisWebHookReceiver.SignatureHeaderName, _testSignature);

            ReceiverMock.Protected().Setup<Task<HttpResponseMessage>>("ExecuteWebHookAsync", id, RequestContext, _postRequest, actions, ItExpr.IsAny<Object>()).ReturnsAsync(new HttpResponseMessage()).Verifiable();

            // Act
            await ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest);

            // Assert
            ReceiverMock.Verify();
        }

        [Theory]
        [InlineData("")]
        [InlineData("challenge=")]
        [InlineData("invalid")]
        public async Task ReceiveAsync_ReturnsError_IfInvalidGetRequest(string query) {
            // Arrange
            Initialize(TestSecret);
            _getRequest.RequestUri = new Uri("http://localhost?" + query);

            // Act
            HttpResponseMessage actual = await ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _getRequest);

            // Assert
            Assert.False(actual.IsSuccessStatusCode);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _getRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Fact]
        public async Task ReceiveAsync_Succeeds_IfValidGetRequest() {
            // Arrange
            Initialize(TestSecret);
            _getRequest.RequestUri = new Uri("http://localhost?challenge=1234567890");

            // Act
            await ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _getRequest);

            // Assert
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _getRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("PUT")]
        [InlineData("OPTIONS")]
        public async Task ReceiveAsync_ReturnsError_IfInvalidMethod(string method) {
            // Arrange
            Initialize(TestSecret);
            HttpRequestMessage req = new HttpRequestMessage { Method = new HttpMethod(method) };
            req.SetRequestContext(RequestContext);

            // Act
            HttpResponseMessage actual = await ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, req);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, actual.StatusCode);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, req, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        public override void Initialize(string config) {
            base.Initialize(config);

            _getRequest = new HttpRequestMessage();
            _getRequest.SetRequestContext(RequestContext);

            _postRequest = new HttpRequestMessage() { Method = HttpMethod.Post };
            _postRequest.SetRequestContext(RequestContext);
            _postRequest.Content = new StringContent(TestContent, Encoding.UTF8, "application/json");
        }
    }
}
