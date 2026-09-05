using VideoMaker.Contracts.Auth;

namespace VideoMaker.Contracts.Dashboard;

public class DashboardResponse
{
    public UserDto User { get; set; } = default!;
    public DashboardStatsDto Statistics { get; set; } = default!;
    public List<RecentVideoDto> RecentVideos { get; set; } = new();
}

public class DashboardStatsDto
{
    public int TotalVideos { get; set; }
    public int CompletedVideos { get; set; }
    public int ProcessingVideos { get; set; }
    public int FailedVideos { get; set; }
}

public class RecentVideoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedAt { get; set; }
}
