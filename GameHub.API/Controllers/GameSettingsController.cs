using GameHub.API.Models;
using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[Route("api/game-settings")]
public class GameSettingsController : ApiControllerBase
{
    private readonly IGameSettingService _settings;

    public GameSettingsController(IGameSettingService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GameSettingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _settings.GetAllAsync(ct);

        return ToActionResult(result, settings => Ok(settings));
    }

    [HttpPut("{gameType}")]
    [Authorize(Roles = nameof(PlayerRole.Admin))]
    [ProducesResponseType(typeof(GameSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] GameType gameType,
        [FromBody] UpdateGameSettingRequest request,
        CancellationToken ct)
    {
        var result = await _settings.UpdateAsync(gameType, request, ct);

        return ToActionResult(result, setting => Ok(setting));
    }
}