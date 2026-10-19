using GameHub.API.Models;
using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[Route("api/game-history")]
public class GameHistoryController : ApiControllerBase
{
    private readonly IGameHistoryService _history;

    public GameHistoryController(IGameHistoryService history)
    {
        _history = history;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GameHistoryItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] GameHistoryQuery query,
        CancellationToken ct)
    {
        var isAdmin = User.IsInRole(nameof(PlayerRole.Admin));
        var result = await _history.GetHistoryAsync(CurrentPlayerId, isAdmin, query, ct);

        return ToActionResult(result, items => Ok(items));
    }
}