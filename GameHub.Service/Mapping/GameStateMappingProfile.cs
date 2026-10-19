using System.Globalization;
using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;

namespace GameHub.Service.Mapping;

public class GameStateMappingProfile : Profile
{
    public GameStateMappingProfile()
    {
        CreateMap<GameItem, LootBoxResponse>()
            .ForMember(d => d.X, o => o.MapFrom((s, _) => Required(s.X, nameof(s.X))))
            .ForMember(d => d.Y, o => o.MapFrom((s, _) => Required(s.Y, nameof(s.Y))))
            .ForMember(d => d.AppearAtMs, o => o.MapFrom((s, _) => Required(s.AppearAtMs, nameof(s.AppearAtMs))))
            .ForMember(d => d.DisappearAtMs, o => o.MapFrom((s, _) => Required(s.DisappearAtMs, nameof(s.DisappearAtMs))));

        CreateMap<GameItem, RoundResponse>()
            .ForMember(d => d.Options, o => o.MapFrom((s, _) => ParseOptions(s.Options)));
    }

    private static T Required<T>(T? value, string field) where T : struct
    {
        return value ?? throw new InvalidOperationException(
            $"GameItem.{field} must be set to map a loot box.");
    }

    private static IReadOnlyList<int> ParseOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return Array.Empty<int>();
        }

        return options
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
            .ToArray();
    }
}