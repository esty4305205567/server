using GameHub.API.Models;
using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[Route("api/admin/game-sessions")]
[Authorize(Roles = nameof(PlayerRole.Admin))]
public class AdminGameSessionsController : ApiControllerBase
{
    private readonly IAdminGameSessionService _adminSessions;

    public AdminGameSessionsController(IAdminGameSessionService adminSessions)
    {
        _adminSessions = adminSessions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminGameSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] AdminGameSessionsQuery query,
        CancellationToken ct)
    {
        var result = await _adminSessions.GetAllAsync(query, ct);

        return ToActionResult(result, sessions => Ok(sessions));
    }
}