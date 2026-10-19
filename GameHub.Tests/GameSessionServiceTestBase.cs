using AutoMapper;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Service;
using GameHub.Service.Mapping;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Tests;

public abstract class GameSessionServiceTestBase
{
    protected readonly Mock<IGameSessionRepository> Sessions = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();
    protected readonly Mock<IGameSettingRepository> Settings = new();
    protected readonly Mock<IPlayerRepository> Players = new();
    protected readonly Mock<IGameCodeGenerator> CodeGenerator = new();
    protected readonly Mock<IGameItemGenerator> ItemGenerator = new();
    protected readonly Mock<IGameSessionConfigurator> Configurator = new();
    protected readonly Mock<IClock> Clock = new();

    protected readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GameSessionMappingProfile>()).CreateMapper();

    protected readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    protected GameSessionServiceTestBase()
    {
        Clock.Setup(c => c.UtcNow).Returns(Now);
        Configurator.Setup(c => c.Type).Returns(GameType.LootCollection);
    }

    protected GameSessionService CreateService()
    {
        return new GameSessionService(
            Sessions.Object,
            UnitOfWork.Object,
            Settings.Object,
            Players.Object,
            CodeGenerator.Object,
            new[] { ItemGenerator.Object },
            new[] { Configurator.Object },
            Mapper,
            Clock.Object,
            Mock.Of<ILogger<GameSessionService>>());
    }
}