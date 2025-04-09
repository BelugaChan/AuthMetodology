using AuthMetodology.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.DTO.v1
{
    public class TwoFaRequestDto
    {
        public required Guid Id { get; set; }

        public required string Code { get; set; }

        public static TwoFaRequestDto Create(Guid id, string code)
            => new TwoFaRequestDto()
            {
                Id = id,
                Code = code
            };
    }
}
