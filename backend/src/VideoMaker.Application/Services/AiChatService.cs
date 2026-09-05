using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Ai;
using VideoMaker.Contracts.Common;
using VideoMaker.Contracts.Conversations;
using VideoMaker.Domain.Entities;
using VideoMaker.Domain.Enums;

namespace VideoMaker.Application.Services;

public class AiChatService : IAiChatService
{
    private readonly IApplicationDbContext _context;
    private readonly IGeminiService _geminiService;

    public AiChatService(IApplicationDbContext context, IGeminiService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    public async Task<ApiResponse<ChatResponse>> ProcessChatAsync(Guid userId, ChatRequest request, CancellationToken ct = default)
    {
        Conversation? conversation;

        if (request.ConversationId.HasValue)
        {
            conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == userId, ct);

            if (conversation == null)
            {
                return ApiResponse<ChatResponse>.ErrorResult("Conversation not found");
            }
        }
        else
        {
            var title = request.Message.Length > 40
                ? request.Message.Substring(0, 37) + "..."
                : request.Message;

            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Conversations.Add(conversation);
        }

        // Add user message
        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(userMsg);

        // Fetch history for Gemini prompt
        var history = await _context.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.CreatedAt)
            .Take(15)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role.ToString(),
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);

        // Call Gemini service
        var (aiResponseText, videoPlan) = await _geminiService.AnalyzeAndPlanVideoAsync(
            request.Message,
            history,
            ct);

        string? videoPlanJson = videoPlan != null ? JsonSerializer.Serialize(videoPlan) : null;

        var assistantMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = aiResponseText,
            VideoPlanJson = videoPlanJson,
            CreatedAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(assistantMsg);

        conversation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return ApiResponse<ChatResponse>.SuccessResult(new ChatResponse
        {
            ConversationId = conversation.Id,
            MessageId = assistantMsg.Id,
            Role = "Assistant",
            Content = aiResponseText,
            VideoPlan = videoPlan,
            CreatedAt = assistantMsg.CreatedAt
        });
    }
}
