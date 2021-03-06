﻿using Acb.Core.Domain.Entities;
using Acb.Core.Serialize;
using Acb.Dapper;
using Acb.Dapper.Domain;
using System.Threading.Tasks;

namespace Acb.Framework.Tests.Repositories
{
    [Naming(NamingType.UrlCase, Name = "t_areas")]
    internal class TAreas : BaseEntity<string>
    {
        /// <summary>城市编码</summary>
        [Key]
        public string CityCode { get; set; }
        /// <summary>城市名字</summary>

        public string CityName { get; set; }
        /// <summary>深度</summary>

        public int Deep { get; set; }

        /// <summary>父级</summary>
        public string ParentCode { get; set; }
    }

    internal class AreaRepository : DapperRepository<TAreas>
    {
        public async Task<TAreas> Get(string code)
        {
            return await Connection.QueryByIdAsync<TAreas>(code);
        }
    }
}
