using GameHub.API.Models;
using GameHub.Core.DTOs;
using GameHub.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[Route("api/game-sessions")]
public class GameSessionsController : ApiControllerBase
{
    private readonly IGameSessionService _sessions;
    private readonly IGameCaptureService _captures;
    private readonly IGameStateService _state;

    public GameSessionsController(
        IGameSessionService sessions,
        IGameCaptureService captures,
        IGameStateService state)
    {
        _sessions = sessions;
        _captures = captures;
        _state = state;
    }

    [HttpPost]
    [ProducesResponseType(typeof(GameSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGameSessionRequest request,
        CancellationToken ct)
    {
        var result = await _sessions.CreateAsync(request.GameType!.Value, CurrentPlayerId, ct);

        return ToActionResult(result, response =>
            CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpPost("{code}/players")]
    [ProducesResponseType(typeof(JoinGameSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(
        [FromRoute] string code,
        CancellationToken ct)
    {
        var result = await _sessions.JoinAsync(code, CurrentPlayerId, ct);

        return ToActionResult(result, response =>
            CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var result = await _state.GetStateAsync(id, CurrentPlayerId, ct);

        return ToActionResult(result, state => Ok(state));
    }

    [HttpPost("{id:int}/captures")]
    [ProducesResponseType(typeof(CaptureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(
        [FromRoute] int id,
        [FromBody] CaptureRequest request,
        CancellationToken ct)
    {
        var result = await _captures.CaptureAsync(id, CurrentPlayerId, request, ct);

        return ToActionResult(result, response => Ok(response));
    }
}