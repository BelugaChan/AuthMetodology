namespace AuthMetodology.Infrastructure.Models
{
    public class RedisTwoFa
    {
        public required Guid Id { get; set; }

        public required string Code { get; set; }

        public required DateTime CodeExpire { get; set; }

        public static RedisTwoFa Create(Guid id, string code, DateTime codeExpire)
            => new RedisTwoFa()
            {
                Id = id,
                Code = code,
                CodeExpire = codeExpire
            };
    }
}
