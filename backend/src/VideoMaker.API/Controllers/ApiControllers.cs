using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Ai;
using VideoMaker.Contracts.Auth;
using VideoMaker.Contracts.Common;
using VideoMaker.Contracts.Videos;

namespace VideoMaker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected IActionResult Result<T>(ApiResponse<T> response) => response.Success ? Ok(response) : BadRequest(response);
}

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) => Result(await auth.RegisterAsync(request, ct));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => Result(await auth.LoginAsync(request, ct));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct) => Result(await auth.RefreshTokenAsync(request, ct));

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct) => Result(await auth.LogoutAsync(UserId, ct));

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Result(await auth.GetCurrentUserAsync(UserId, ct));
}

[Authorize, ApiController, Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Result(await dashboard.GetDashboardAsync(UserId, ct));
}

[Authorize, ApiController, Route("api/ai")]
public sealed class AiController(IAiChatService chat) : ApiControllerBase
{
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(ChatRequest request, CancellationToken ct) => Result(await chat.ProcessChatAsync(UserId, request, ct));
}

[Authorize, ApiController, Route("api/videos")]
public sealed class VideosController(IVideoService videos) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] VideoListQuery query, CancellationToken ct) => Result(await videos.GetUserVideosAsync(UserId, query, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateVideoRequest request, CancellationToken ct) => Result(await videos.CreateVideoAsync(UserId, request, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Result(await videos.GetVideoByIdAsync(UserId, id, ct));

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken ct) => Result(await videos.GetVideoStatusAsync(UserId, id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Result(await videos.DeleteVideoAsync(UserId, id, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Result(await videos.CancelVideoAsync(UserId, id, ct));
}

[Authorize, ApiController, Route("api/conversations")]
public sealed class ConversationsController(IConversationService conversations) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Result(await conversations.GetUserConversationsAsync(UserId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Result(await conversations.GetConversationByIdAsync(UserId, id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Result(await conversations.DeleteConversationAsync(UserId, id, ct));
}
