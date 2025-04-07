using AuthMetodology.Logic.Entities.v1;
using AuthMetodology.Logic.Models.v1;
using AutoMapper;

namespace AuthMetodology.Application.Profiles.v1
{
    public class UserProfileV1 : Profile
    {
        public UserProfileV1()
        {
            CreateMap<UserEntityV1, UserV1>();
            CreateMap<UserV1, UserEntityV1>();
        }
    }
}
