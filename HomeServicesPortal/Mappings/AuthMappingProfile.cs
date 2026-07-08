using AutoMapper;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Entities;

namespace HomeServicesPortal.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<(UsersLogin User, Client Client), RegistrationResponse>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.User.Uid))
            .ForMember(d => d.ProfileId, o => o.MapFrom(s => s.Client.Uid))
            .ForMember(d => d.UserType, o => o.MapFrom(s => s.User.UserType))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.Client.FullName))
            .ForMember(d => d.MobileNo, o => o.MapFrom(s => s.User.MobileNo));

        CreateMap<(UsersLogin User, Provider Provider), RegistrationResponse>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.User.Uid))
            .ForMember(d => d.ProfileId, o => o.MapFrom(s => s.Provider.Uid))
            .ForMember(d => d.UserType, o => o.MapFrom(s => s.User.UserType))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.Provider.FullName))
            .ForMember(d => d.MobileNo, o => o.MapFrom(s => s.User.MobileNo));

        CreateMap<(UsersLogin User, Staff Staff), RegistrationResponse>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.User.Uid))
            .ForMember(d => d.ProfileId, o => o.MapFrom(s => s.Staff.Uid))
            .ForMember(d => d.UserType, o => o.MapFrom(s => s.User.UserType))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.Staff.FullName))
            .ForMember(d => d.MobileNo, o => o.MapFrom(s => s.User.MobileNo));
    }
}
