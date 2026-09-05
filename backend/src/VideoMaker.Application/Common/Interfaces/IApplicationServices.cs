using VideoMaker.Contracts.Ai;
using VideoMaker.Contracts.Auth;
using VideoMaker.Contracts.Common;
using VideoMaker.Contracts.Conversations;
using VideoMaker.Contracts.Dashboard;
using VideoMaker.Contracts.Videos;

namespace VideoMaker.Application.Common.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> LogoutAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardResponse>> GetDashboardAsync(Guid userId, CancellationToken ct = default);
}

public interface IAiChatService
{
    Task<ApiResponse<ChatResponse>> ProcessChatAsync(Guid userId, ChatRequest request, CancellationToken ct = default);
}

public interface IConversationService
{
    Task<ApiResponse<List<ConversationSummaryDto>>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<ConversationDetailsDto>> GetConversationByIdAsync(Guid userId, Guid conversationId, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteConversationAsync(Guid userId, Guid conversationId, CancellationToken ct = default);
}

public interface IVideoService
{
    Task<ApiResponse<CreateVideoResponse>> CreateVideoAsync(Guid userId, CreateVideoRequest request, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<RecentVideoDto>>> GetUserVideosAsync(Guid userId, VideoListQuery query, CancellationToken ct = default);
    Task<ApiResponse<VideoDetailsDto>> GetVideoByIdAsync(Guid userId, Guid videoId, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteVideoAsync(Guid userId, Guid videoId, CancellationToken ct = default);
    Task<ApiResponse<bool>> CancelVideoAsync(Guid userId, Guid videoId, CancellationToken ct = default);
    Task<ApiResponse<VideoStatusDto>> GetVideoStatusAsync(Guid userId, Guid videoId, CancellationToken ct = default);
}
