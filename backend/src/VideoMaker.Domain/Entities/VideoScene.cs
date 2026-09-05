namespace VideoMaker.Domain.Entities;

public class VideoScene
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoId { get; set; }
    public Video? Video { get; set; }
    public int SceneNumber { get; set; }
    public int Duration { get; set; }
    public string VisualPrompt { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public string? TextOverlay { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
}
