using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Application.DTOs.Auth;
using RecetasOCR.Application.Features.Auth;

namespace RecetasOCR.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
        [FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(CancellationToken ct)
    {
        var result = await mediator.Send(new LogoutCommand(), ct);
        return Ok(ApiResponse<bool>.Ok(result));
    }
}
