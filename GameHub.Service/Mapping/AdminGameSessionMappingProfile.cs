using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;

namespace GameHub.Service.Mapping;

public class AdminGameSessionMappingProfile : Profile
{
    public AdminGameSessionMappingProfile()
    {
        // Host ו-Guest נטענים ב-Include ב-Repository. אורח null (חדר שממתין) ממופה ל-null
        CreateMap<GameSession, AdminGameSessionResponse>()
            .ForMember(d => d.HostUsername, o => o.MapFrom(s => s.Host.Username))
            .ForMember(d => d.GuestUsername, o => o.MapFrom(s => s.Guest != null ? s.Guest.Username : null));
    }
}