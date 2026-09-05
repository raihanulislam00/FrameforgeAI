using VideoMaker.Domain.Enums;

namespace VideoMaker.Domain.Entities;

public class Video
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VideoStatus Status { get; set; } = VideoStatus.Pending;
    public int Duration { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Provider { get; set; }
    public string? VideoPlanJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<VideoScene> Scenes { get; set; } = new List<VideoScene>();
    public ICollection<VideoGenerationJob> Jobs { get; set; } = new List<VideoGenerationJob>();
}
