using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTourGuide.Business.DTOs.Authentication;
using SmartTourGuide.Business.Interfaces;

namespace SmartTourGuide.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(AuthenticatedUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthenticatedUserResponse> GetCurrentUser()
    {
        var idValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var email = User.FindFirstValue(
            ClaimTypes.Email);

        var displayName = User.FindFirstValue(
            ClaimTypes.Name);

        var role = User.FindFirstValue(
            ClaimTypes.Role);

        var language = User.FindFirstValue(
            "preferred_language");

        if (!Guid.TryParse(idValue, out var id)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(role)
            || string.IsNullOrWhiteSpace(language))
        {
            return Unauthorized();
        }

        return Ok(new AuthenticatedUserResponse
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            Role = role,
            PreferredLanguageCode = language
        });
    }
}