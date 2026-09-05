using VideoMaker.Contracts.Ai;

namespace VideoMaker.Contracts.Videos;

public class CreateVideoRequest
{
    public Guid? ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VideoPlanDto VideoPlan { get; set; } = default!;
}

public class CreateVideoResponse
{
    public Guid VideoId { get; set; }
    public string Status { get; set; } = "Pending";
}

public class VideoDetailsDto
{
    public Guid Id { get; set; }
    public Guid? ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Provider { get; set; }
    public VideoPlanDto? VideoPlan { get; set; }
    public List<VideoSceneDto> Scenes { get; set; } = new();
    public List<VideoJobDto> Jobs { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class VideoSceneDto
{
    public Guid Id { get; set; }
    public int SceneNumber { get; set; }
    public int Duration { get; set; }
    public string VisualPrompt { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public string? TextOverlay { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
}

public class VideoJobDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? Provider { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class VideoStatusDto
{
    public Guid VideoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VideoListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}
