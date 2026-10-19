using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Service.Mapping;

namespace GameHub.Tests;

public class PlayerMappingProfileTests
{
    private static MapperConfiguration Configuration() =>
        new(cfg => cfg.AddProfile<PlayerMappingProfile>());

    [Fact]
    public void Configuration_IsValid()
    {
        Configuration().AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_PlayerToResponse_CopiesIdUsernameAndRole()
    {
        var player = new Player
        {
            Id = 7,
            Username = "dana1",
            PasswordHash = "secret-hash",
            Role = PlayerRole.Admin
        };

        var response = Configuration().CreateMapper().Map<PlayerResponse>(player);

        Assert.Equal(7, response.Id);
        Assert.Equal("dana1", response.Username);
        Assert.Equal(PlayerRole.Admin, response.Role);
    }
}