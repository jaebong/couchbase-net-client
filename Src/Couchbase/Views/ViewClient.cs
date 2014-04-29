﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Couchbase.Core;

namespace Couchbase.Views
{
    internal class ViewClient : IViewClient
    {
        private readonly ILog _log = LogManager.GetCurrentClassLogger();

        public ViewClient(HttpClient httpClient, IDataMapper mapper)
        {
            HttpClient = httpClient;
            Mapper = mapper;
        }

        public async Task<IViewResult<T>> ExecuteAsync<T>(IViewQuery query)
        {
            IViewResult<T> viewResult = new ViewResult<T>();
            try
            {
                var result = await HttpClient.GetStreamAsync(query.RawUri());
                viewResult = Mapper.Map<ViewResult<T>>(result);
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(e =>
                {
                    _log.Error(e);
                    return true;
                });
            }
            return viewResult;
        }

        public IViewResult<T> Execute<T>(IViewQuery query)
        {
            IViewResult<T> viewResult = new ViewResult<T>();
            var task = HttpClient.GetStreamAsync(query.RawUri());
            try
            {
                task.Wait();
                var result = task.Result;
                viewResult = Mapper.Map<ViewResult<T>>(result);
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(e =>
                {
                    _log.Error(e);
                    return true;
                });
            }

            return viewResult;
        }

        public HttpClient HttpClient { get; set; }

        public IDataMapper Mapper { get; set; }
    }
}
