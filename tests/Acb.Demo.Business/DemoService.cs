﻿using Acb.Demo.Contracts;
using Acb.Demo.Contracts.Dtos;

namespace Acb.Demo.Business
{
    public class DemoService : IDemoService
    {
        public DemoDto Hello(string id, DemoInputDto dto)
        {
            return new DemoDto
            {
                Id = id,
                Demo = dto.Demo,
                Name = dto.Name + ",Success",
                Time = dto.Time
            };
        }
    }
}
