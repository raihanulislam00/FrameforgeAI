using VideoMaker.Domain.Enums;

namespace VideoMaker.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? VideoPlanJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
