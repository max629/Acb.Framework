﻿using Acb.Core.Exceptions;
using Acb.Core.Logging;
using System.Collections.Concurrent;
using System.Data;

namespace Acb.Dapper.Adapters
{
    public static class DbConnectionManager
    {
        private static readonly ConcurrentDictionary<string, IDbConnectionAdapter> Adapters;
        private static readonly ILogger Logger;

        static DbConnectionManager()
        {
            Adapters = new ConcurrentDictionary<string, IDbConnectionAdapter>();
            AddAdapter(new SqlServerAdapter());
            Logger = LogManager.Logger(nameof(DbConnectionManager));
        }

        /// <summary> 添加适配器 </summary>
        /// <param name="adapter"></param>
        public static void AddAdapter(IDbConnectionAdapter adapter)
        {
            if (adapter == null || string.IsNullOrWhiteSpace(adapter.ProviderName))
                return;
            var key = adapter.ProviderName.ToLower();
            if (Adapters.ContainsKey(key))
                return;
            Adapters.TryAdd(key, adapter);
        }

        /// <summary> 创建数据库适配器 </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IDbConnectionAdapter Create(string providerName = null)
        {
            var key = string.IsNullOrWhiteSpace(providerName) ? SqlServerAdapter.Name : providerName;
            if (Adapters.TryGetValue(key?.ToLower(), out var adapter))
                return adapter;
            throw new BusiException($"不支持的DbProvider：{providerName}");
        }

        /// <summary> 格式化SQL </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string FormatSql(this IDbConnection conn, string sql)
        {
            foreach (var adapter in Adapters.Values)
            {
                if (adapter.ConnectionType != conn.GetType())
                    continue;
                sql = adapter.FormatSql(sql);
                Logger.Debug(sql);
                return sql;
            }
            return sql;
        }

        /// <summary> 生成分页SQL </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="columns"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static string PagedSql(this IDbConnection conn, string sql, string columns, string order)
        {
            foreach (var adapter in Adapters.Values)
            {
                if (adapter.ConnectionType == conn.GetType())
                    return adapter.FormatSql(adapter.PageSql(sql, columns, order));
            }

            return sql;
        }
    }
}
