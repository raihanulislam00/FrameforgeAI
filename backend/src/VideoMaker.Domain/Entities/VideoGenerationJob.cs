using VideoMaker.Domain.Enums;

namespace VideoMaker.Domain.Entities;

public class VideoGenerationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoId { get; set; }
    public Video? Video { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Progress { get; set; }
    public string? Provider { get; set; }
    public string? ProviderJobId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
