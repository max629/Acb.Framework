﻿using Acb.Core;
using Acb.Core.Exceptions;
using Acb.Core.Extensions;
using Acb.Core.Helper;
using Acb.Redis;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace Acb.MicroService.Client
{
    /// <inheritdoc />
    /// <summary> 代理调用 </summary>
    /// <typeparam name="T"></typeparam>
    public class InvokeProxy<T> : DispatchProxy where T : IMicroService
    {
        private const string MicroSreviceKey = "micro_service";
        private const string RegistCenterKey = MicroSreviceKey + ":center";

        private string RedisKey
        {
            get
            {
                var key = $"{MicroSreviceKey}:redisKey".Config<string>();
                return string.IsNullOrWhiteSpace(key) ? RegistCenterKey : key;
            }
        }


        /// <summary> 接口类型 </summary>
        private readonly Type _type;

        /// <inheritdoc />
        /// <summary> 构造函数 </summary>
        public InvokeProxy()
        {
            _type = typeof(T);
        }

        private string GetTypeService()
        {
            var redis = RedisManager.Instance.GetDatabase();
            var assemblyKey = _type.Assembly.AssemblyKey();
            var url = redis.SetRandomMember($"{RedisKey}:{assemblyKey}");
            if (string.IsNullOrWhiteSpace(url))
                throw new BusiException($"{_type.FullName},没有可用的服务");
            return $"{url}{_type.Name}/";
        }

        /// <inheritdoc />
        /// <summary> 接口调用 </summary>
        /// <param name="targetMethod"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var url = string.Concat(GetTypeService(), targetMethod.Name);
            //http请求
            var resp = HttpHelper.Instance
                .RequestAsync(HttpMethod.Post, url, data: args).Result;
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var html = resp.Content.ReadAsStringAsync().Result;
                var type = targetMethod.ReturnType;
                return JsonConvert.DeserializeObject(html, type);
            }
            else
            {
                var html = resp.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<DResult>(html);
                throw new BusiException(result.Message, result.Code);
            }
        }
    }
}
