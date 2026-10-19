using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;

namespace GameHub.Service.Mapping;

public class GameHistoryMappingProfile : Profile
{
    public GameHistoryMappingProfile()
    {
        // דורש Include ל-Host, Guest, Players ו-Players.Player ב-Repository
        CreateMap<GameSession, GameHistoryItemResponse>()
            .ForMember(d => d.SessionId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.GameType, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.HostUsername, o => o.MapFrom(s => s.Host.Username))
            .ForMember(d => d.GuestUsername, o => o.MapFrom(s => s.Guest != null ? s.Guest.Username : null))
            .ForMember(d => d.HostScore, o => o.MapFrom(s =>
                s.Players.Where(p => p.PlayerId == s.HostId).Select(p => p.Score).FirstOrDefault()))
            .ForMember(d => d.GuestScore, o => o.MapFrom(s =>
                s.Players.Where(p => p.PlayerId == s.GuestId).Select(p => (int?)p.Score).FirstOrDefault()))
            .ForMember(d => d.WinnerUsername, o => o.MapFrom(s =>
                s.Players.Where(p => p.IsWinner).Select(p => p.Player.Username).FirstOrDefault()));
    }
}