namespace VideoMaker.Contracts.Ai;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
}

public class ChatResponse
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public string Role { get; set; } = "Assistant";
    public string Content { get; set; } = string.Empty;
    public VideoPlanDto? VideoPlan { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class VideoPlanDto
{
    public string Title { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Style { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public List<VideoScenePlanDto> Scenes { get; set; } = new();
    public MusicPlanDto Music { get; set; } = new();
    public string Cta { get; set; } = string.Empty;
    public string? EmotionalTone { get; set; }
    public List<string>? Keywords { get; set; }
    public string? ThumbnailConcept { get; set; }
}

public class VideoScenePlanDto
{
    public int SceneNumber { get; set; }
    public int Duration { get; set; }
    public string Visual { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public string TextOverlay { get; set; } = string.Empty;
}

public class MusicPlanDto
{
    public string Style { get; set; } = string.Empty;
    public string Mood { get; set; } = string.Empty;
}
