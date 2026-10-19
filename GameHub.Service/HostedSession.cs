using GameHub.Core.Entities;

namespace GameHub.Service;

internal sealed record HostedSession(GameSession Session, string HostUsername);