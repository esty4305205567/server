using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;

namespace GameHub.Service.Mapping;

public class GameSessionMappingProfile : Profile
{
    public GameSessionMappingProfile()
    {
        // מיפויי הישות עצמה. הם נדרשים כי IncludeMembers משלים מהם את שאר השדות
        CreateMap<GameSession, GameSessionResponse>();
        CreateMap<GameSession, JoinGameSessionResponse>();

        // המיפויים שבשימוש בפועל. HostUsername מגיע מ-HostedSession במפורש,
        // ולכן לא תלוי בכך ש-Host נטען מבסיס הנתונים
        CreateMap<HostedSession, GameSessionResponse>()
            .IncludeMembers(h => h.Session)
            .ForMember(d => d.HostUsername, o => o.MapFrom(h => h.HostUsername));

        CreateMap<HostedSession, JoinGameSessionResponse>()
            .IncludeMembers(h => h.Session)
            .ForMember(d => d.HostUsername, o => o.MapFrom(h => h.HostUsername))
            .ForMember(d => d.StartedAtUtc, o => o.MapFrom((h, _) =>
                h.Session.StartedAtUtc
                ?? throw new InvalidOperationException(
                    "StartedAtUtc must be set before mapping a join response.")));
    }
}