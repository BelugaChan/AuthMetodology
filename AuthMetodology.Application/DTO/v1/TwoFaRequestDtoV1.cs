using AuthMetodology.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.DTO.v1
{
    public class TwoFaRequestDtoV1
    {
        public required Guid Id { get; set; }

        public required string Code { get; set; }

        public static TwoFaRequestDtoV1 Create(Guid id, string code)
            => new TwoFaRequestDtoV1()
            {
                Id = id,
                Code = code
            };
    }
}
