using GameHub.API.Models;
using GameHub.Core.DTOs;
using GameHub.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[Route("api/players")]
public class PlayersController : ApiControllerBase
{
    private const string AdminRole = "Admin";

    private readonly IPlayerService _players;

    public PlayersController(IPlayerService players)
    {
        _players = players;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var result = await _players.GetByIdAsync(id, CurrentPlayerId, User.IsInRole(AdminRole), ct);

        return ToActionResult(result, player => Ok(player));
    }
}