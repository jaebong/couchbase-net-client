﻿using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Configuration.Client;
using Couchbase.N1QL;
using Couchbase.Views;
using Moq;
using NUnit.Framework;
using Couchbase.UnitTests.Utils;
using System.Net;
using Couchbase.Configuration;

namespace Couchbase.UnitTests.N1Ql
{
    [TestFixture]
    public class QueryClientTests
    {
        #region GetDataMapper

        [Test]
        public void GetDataMapper_IQueryRequest_ReturnsClientDataMapper()
        {
            // Arrange

            var dataMapper = new Mock<IDataMapper>();

            var queryRequest = new Mock<IQueryRequest>();

            var queryClient = new QueryClient(new HttpClient(), dataMapper.Object, new ClientConfiguration(),
                new ConcurrentDictionary<string, QueryPlan>());

            // Act

            var result = queryClient.GetDataMapper(queryRequest.Object);

            // Assert

            Assert.AreEqual(dataMapper.Object, result);
        }

        [Test]
        public void GetDataMapper_IQueryRequestWithDataMapper_NoDataMapper_ReturnsClientDataMapper()
        {
            // Arrange

            var clientDataMapper = new Mock<IDataMapper>();

            var queryRequest = new Mock<IQueryRequestWithDataMapper>();
            queryRequest.SetupProperty(p => p.DataMapper, null);

            var queryClient = new QueryClient(new HttpClient(), clientDataMapper.Object, new ClientConfiguration(),
                new ConcurrentDictionary<string, QueryPlan>());

            // Act

            var result = queryClient.GetDataMapper(queryRequest.Object);

            // Assert

            Assert.AreEqual(clientDataMapper.Object, result);
        }

        [Test]
        public void GetDataMapper_IQueryRequestWithDataMapper_HasDataMapper_ReturnsRequestDataMapper()
        {
            // Arrange

            var clientDataMapper = new Mock<IDataMapper>();
            var requestDataMapper = new Mock<IDataMapper>();

            var queryRequest = new Mock<IQueryRequestWithDataMapper>();
            queryRequest.SetupProperty(p => p.DataMapper, requestDataMapper.Object);

            var queryClient = new QueryClient(new HttpClient(), clientDataMapper.Object, new ClientConfiguration(),
                new ConcurrentDictionary<string, QueryPlan>());

            // Act

            var result = queryClient.GetDataMapper(queryRequest.Object);

            // Assert

            Assert.AreEqual(requestDataMapper.Object, result);
        }

        #endregion

        [Test]
        public void When_MaxServerParallism_Is_Set_Request_Has_It()
        {
            var queryRequest = new QueryRequest("SELECT * FROM default;");
            queryRequest.MaxServerParallelism(4);

            var query = queryRequest.GetFormValues();
            Assert.AreEqual(4.ToString(), query["max_parallelism"]);
        }

        [Test]
        public void When_ScanWait_Is_Set_Request_Has_The_Value()
        {
            var queryRequest = new QueryRequest("SELECT * FROM default;");
            queryRequest.ScanWait(TimeSpan.FromSeconds(10));

            var query = queryRequest.GetFormValues();
            Assert.AreEqual("10000ms", query["scan_wait"]);
        }

        [Test]
        public async Task Test_QueryAsync_CanCancel()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            // create hander that takes some time to return
            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => {
                    Thread.Sleep(TimeSpan.FromMilliseconds(200));
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            ));

            var config = new ClientConfiguration();
            var queryClient = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var result = await queryClient.QueryAsync<dynamic>(queryRequest, cancellationTokenSource.Token);

            Assert.False(result.Success);
            Assert.NotNull(result.Exception);
            Assert.IsInstanceOf<OperationCanceledException>(result.Exception);
        }

        [Test]
        public async Task Test_PrepareQueryAsync_CanCancel()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            // create hander that takes some time to return
            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => {
                    Thread.Sleep(TimeSpan.FromMilliseconds(200));
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            ));

            var config = new ClientConfiguration();
            var queryClient = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var result = await queryClient.PrepareAsync(queryRequest, cancellationTokenSource.Token);

            Assert.False(result.Success);
            Assert.NotNull(result.Exception);
            Assert.IsInstanceOf<OperationCanceledException>(result.Exception);
        }

        [Test]
        public void Query_Sets_LastActivity()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => new HttpResponseMessage(HttpStatusCode.OK))
            );

            var config = new ClientConfiguration();
            var client = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );
            Assert.IsNull(client.LastActivity);

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            client.Query<dynamic>(queryRequest);

            Assert.IsNotNull(client.LastActivity);
        }

        [Test]
        public async Task QueryAsync_Sets_LastActivity()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => new HttpResponseMessage(HttpStatusCode.OK))
            );

            var config = new ClientConfiguration();
            var client = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );
            Assert.IsNull(client.LastActivity);

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            await client.QueryAsync<dynamic>(queryRequest);

            Assert.IsNotNull(client.LastActivity);
        }

        [Test]
        public async Task QueryAsync_With_CancellationToken_Sets_LastActivity()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => new HttpResponseMessage(HttpStatusCode.OK))
            );

            var config = new ClientConfiguration();
            var client = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );
            Assert.IsNull(client.LastActivity);

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            await client.QueryAsync<dynamic>(queryRequest, CancellationToken.None);

            Assert.IsNotNull(client.LastActivity);
        }

        [Test]
        public void Prepare_Sets_LastActivity()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => new HttpResponseMessage(HttpStatusCode.OK))
            );

            var config = new ClientConfiguration();
            var client = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );
            Assert.IsNull(client.LastActivity);

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            client.Prepare(queryRequest);

            Assert.IsNotNull(client.LastActivity);
        }

        [Test]
        public async Task PrepareAsync_Sets_LastActivity()
        {
            ConfigContextBase.QueryUris.Add(new FailureCountingUri("http://localhost"));

            var httpClient = new HttpClient(
                FakeHttpMessageHandler.Create(request => new HttpResponseMessage(HttpStatusCode.OK))
            );

            var config = new ClientConfiguration();
            var client = new QueryClient(
                httpClient,
                new JsonDataMapper(config),
                config,
                new ConcurrentDictionary<string, QueryPlan>()
            );
            Assert.IsNull(client.LastActivity);

            var queryRequest = new QueryRequest("SELECT * FROM `default`;");
            await client.PrepareAsync(queryRequest, CancellationToken.None);

            Assert.IsNotNull(client.LastActivity);
        }
    }
}
