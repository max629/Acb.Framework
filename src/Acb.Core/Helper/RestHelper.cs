﻿using Acb.Core.Exceptions;
using Acb.Core.Extensions;
using Acb.Core.Logging;
using Acb.Core.Timing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Acb.Core.Helper
{
    /// <summary> 接口调用辅助 </summary>
    public class RestHelper
    {
        private readonly string _baseUri;
        private const string Prefix = "sites:";
        private readonly int _retryCount;
        private readonly ILogger _logger = LogManager.Logger<RestHelper>();

        /// <summary> 构造函数 </summary>
        /// <param name="baseUri"></param>
        /// <param name="retry">重试次数</param>
        public RestHelper(string baseUri, int retry = 3)
        {
            _baseUri = baseUri;
            _retryCount = retry;

        }

        /// <inheritdoc />
        /// <summary> 构造函数 </summary>
        /// <param name="siteEnum"></param>
        /// <param name="retry">重试次数</param>
        public RestHelper(Enum siteEnum, int retry = 3) : this(
            $"{Prefix}{siteEnum.ToString().ToLower()}".Config<string>(), retry)
        {
        }

        private static string GetTicket()
        {
            var key = Consts.AppTicketKey.Config<string>();
            var timestamp = Clock.Now.ToTimestamp();
            return $"{timestamp}{EncryptHelper.Hash($"{key}{timestamp}", EncryptHelper.HashFormat.MD532).ToLower()}";
        }

        /// <summary> 请求接口 </summary>
        /// <param name="api"></param>
        /// <param name="paras"></param>
        /// <param name="data"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<string> RequestAsync(string api, object paras = null, object data = null,
            HttpMethod method = null, IDictionary<string, string> headers = null, HttpContent content = null)
        {
            var current = 0;
            if (string.IsNullOrWhiteSpace(api))
                throw new BusiException("接口api不能为空");
            var url = string.Concat(_baseUri?.TrimEnd('/') ?? string.Empty, "/", api.TrimStart('/'));
            if (paras != null)
            {
                url += url.IndexOf('?') > 0 ? "&" : "?";
                url += paras.ToDictionary().ToUrl();
            }

            headers = headers ?? new Dictionary<string, string>();

            headers.Add("App-Ticket", GetTicket());
            while (current < _retryCount)
            {
                try
                {
                    var resp = await HttpHelper.Instance.RequestAsync(method ?? HttpMethod.Get, url, null, data,
                        headers, content);
                    if (resp.StatusCode == HttpStatusCode.OK)
                        return await resp.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                }

                current++;
            }

            _logger.Fatal($"接口数据异常:{url}[retry={_retryCount}]");
            throw new BusiException("接口请求异常");
        }

        /// <summary> 获取API接口返回的实体对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="paras"></param>
        /// <param name="data"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<T> RequestAsync<T>(string api, object paras = null, object data = null,
            HttpMethod method = null, IDictionary<string, string> headers = null, HttpContent content = null)
            where T : DResult, new()
        {
            try
            {
                var html = await RequestAsync(api, paras, data, method, headers, content);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    var setting = new JsonSerializerSettings();
                    setting.Converters.Add(new DateTimeConverter());
                    return JsonConvert.DeserializeObject<T>(html, setting);
                }
            }
            catch (Exception ex)
            {
                if (ex is BusiException busiEx)
                {
                    return new T { Code = busiEx.Code, Message = busiEx.Message };
                }
                _logger.Error(ex.Message, ex);
            }

            return new T { Code = -1, Message = "服务器数据异常" };
        }

        /// <summary> 请求接口 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="paras"></param>
        /// <param name="data"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<DResult<T>> ResultAsync<T>(string api, object paras = null, object data = null,
            HttpMethod method = null, IDictionary<string, string> headers = null, HttpContent content = null)
        {
            return await RequestAsync<DResult<T>>(api, paras, data, method, headers, content);
        }

        /// <summary> 获取API接口返回的实体对象 </summary>
        /// <param name="api"></param>
        /// <param name="paras"></param>
        /// <param name="data"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<DResult> ResultAsync(string api, object paras = null, object data = null,
            HttpMethod method = null, IDictionary<string, string> headers = null, HttpContent content = null)
        {
            return await RequestAsync<DResult>(api, paras, data, method, headers, content);
        }

        public async Task<T> GetAsync<T>(string api, object paras = null, IDictionary<string, string> headers = null)
            where T : DResult, new() => await RequestAsync<T>(api, paras, null, HttpMethod.Get, headers);

        public async Task<T> PostAsync<T>(string api, object data = null, object paras = null,
            IDictionary<string, string> headers = null, HttpContent content = null)
            where T : DResult, new() => await RequestAsync<T>(api, paras, data, HttpMethod.Post, headers, content);

        public async Task<T> PutAsync<T>(string api, object data = null, object paras = null,
            IDictionary<string, string> headers = null, HttpContent content = null)
            where T : DResult, new() => await RequestAsync<T>(api, paras, data, HttpMethod.Put, headers, content);

        public async Task<T> DeleteAsync<T>(string api, object paras = null, IDictionary<string, string> headers = null)
            where T : DResult, new() => await RequestAsync<T>(api, paras, null, HttpMethod.Delete, headers);
    }
}
