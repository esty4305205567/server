using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;

namespace GameHub.Service.Mapping;

public class GameSettingMappingProfile : Profile
{
    public GameSettingMappingProfile()
    {
        CreateMap<GameSetting, GameSettingResponse>();
    }
}