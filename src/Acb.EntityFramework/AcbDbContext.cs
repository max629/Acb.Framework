﻿using Acb.Core.Domain;
using Acb.Core.Extensions;
using Acb.Core.Logging;
using Acb.EntityFramework.Config;
using Microsoft.EntityFrameworkCore;
using System;

namespace Acb.EntityFramework
{
    public abstract class AcbDbContext : DbContext, IUnitOfWork
    {
        private const string Prefix = "dapper:";
        private const string DefaultConfigName = "dapperDefault";
        private const string DefaultName = "default";
        private static readonly ILogger Logger = LogManager.Logger<AcbDbContext>();
        private readonly string _configName;

        protected ConnectionConfig Connection
        {
            get
            {
                var name = string.Empty;
                if (string.IsNullOrWhiteSpace(_configName))
                    name = DefaultConfigName.Config<string>();
                if (string.IsNullOrWhiteSpace(name))
                    name = DefaultName;
                return $"{Prefix}{name}".Config<ConnectionConfig>();
            }
        }

        protected AcbDbContext(string configName = null)
        {
            _configName = configName;
        }

        public bool IsTransaction { get; set; }

        public TResult Transaction<TResult>(Func<TResult> action)
        {
            throw new NotImplementedException();
        }

        #region 私有方法

        /// <summary>
        /// 由错误码返回指定的自定义SqlException异常信息
        /// </summary>
        /// <param name="number"> </param>
        /// <returns> </returns>
        private static string GetSqlExceptionMessage(int number)
        {
            var msg = string.Empty;
            switch (number)
            {
                case 2:
                    msg = "连接数据库超时，请检查网络连接或者数据库服务器是否正常。";
                    break;
                case 17:
                    msg = "SqlServer服务不存在或拒绝访问。";
                    break;
                case 17142:
                    msg = "SqlServer服务已暂停，不能提供数据服务。";
                    break;
                case 2812:
                    msg = "指定存储过程不存在。";
                    break;
                case 208:
                    msg = "指定名称的表不存在。";
                    break;
                case 4060: //数据库无效。
                    msg = "所连接的数据库无效。";
                    break;
                case 18456: //登录失败
                    msg = "使用设定的用户名与密码登录数据库失败。";
                    break;
                case 547:
                    msg = "外键约束，无法保存数据的变更。";
                    break;
                case 2627:
                    msg = "主键重复，无法插入数据。";
                    break;
                case 2601:
                    msg = "未知错误。";
                    break;
            }
            return msg;
        }
        #endregion
    }
}
