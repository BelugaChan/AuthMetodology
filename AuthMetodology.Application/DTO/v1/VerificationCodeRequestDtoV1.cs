namespace AuthMetodology.Application.DTO.v1
{
    public class VerificationCodeRequestDtoV1
    {
        public required Guid Id { get; set; }

        public required string Code { get; set; }

        public static VerificationCodeRequestDtoV1 Create(Guid id, string code)
            => new VerificationCodeRequestDtoV1()
            {
                Id = id,
                Code = code
            };
    }
}
