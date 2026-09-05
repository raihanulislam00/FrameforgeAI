using VideoMaker.Contracts.Ai;

namespace VideoMaker.Application.Common.Interfaces;

public interface IGeminiService
{
    Task<(string ResponseText, VideoPlanDto? VideoPlan)> AnalyzeAndPlanVideoAsync(
        string userMessage,
        List<VideoMaker.Contracts.Conversations.ChatMessageDto> history,
        CancellationToken cancellationToken = default);
}

public class VideoGenerationRequest
{
    public Guid VideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VideoPlanDto VideoPlan { get; set; } = default!;
}

public class VideoGenerationResult
{
    public bool Success { get; set; }
    public string? ProviderJobId { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<SceneGenerationResult> GeneratedScenes { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool IsCompletedSynchronously { get; set; }
}

public class SceneGenerationResult
{
    public int SceneNumber { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
}

public class VideoGenerationStatusResult
{
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public int Progress { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IVideoGenerationService
{
    string ProviderName { get; }
    Task<VideoGenerationResult> GenerateAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default);
    Task<VideoGenerationStatusResult> CheckStatusAsync(string providerJobId, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string category, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<string> GetUrlAsync(string relativePath);
}

public interface IVideoNotificationService
{
    Task NotifyProgressAsync(Guid userId, Guid videoId, int progress, string status, string? message = null);
    Task NotifyStatusChangedAsync(Guid userId, Guid videoId, string status, string? videoUrl = null, string? thumbnailUrl = null);
}
