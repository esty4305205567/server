using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using Microsoft.Extensions.Logging;

namespace GameHub.Service;

public class AuthService : IAuthService
{
    private const string UsernameTakenMessage = "שם המשתמש כבר תפוס";
    private const string InvalidCredentialsMessage = "שם המשתמש או הסיסמה שגויים";

    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IPlayerRepository players,
        IUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        ITokenService tokens,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _players = players;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PlayerResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken ct)
    {
        var existing = await _players.GetByUsernameAsync(request.Username, ct);
        if (existing is not null)
        {
            return Result<PlayerResponse>.Conflict(UsernameTakenMessage);
        }

        var player = new Player
        {
            Username = request.Username,
            PasswordHash = _hasher.Hash(request.Password),
            Role = PlayerRole.Player
        };

        await _players.AddAsync(player, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateUsernameException)
        {
            _logger.LogWarning("Registration collided on a username that was taken concurrently");
            return Result<PlayerResponse>.Conflict(UsernameTakenMessage);
        }

        _logger.LogInformation("Player {PlayerId} registered", player.Id);

        return Result<PlayerResponse>.Ok(_mapper.Map<PlayerResponse>(player));
    }

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request, CancellationToken ct)
    {
        var player = await _players.GetByUsernameAsync(request.Username.Trim(), ct);

        var hashToCheck = player?.PasswordHash ?? _hasher.DummyHash;
        var passwordMatches = _hasher.Verify(request.Password, hashToCheck);

        if (player is null || !passwordMatches)
        {
            _logger.LogWarning("Failed login attempt. PlayerId: {PlayerId}", player?.Id);
            return Result<LoginResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            Token = _tokens.CreateToken(player),
            Username = player.Username,
            Role = player.Role
        });
    }
}