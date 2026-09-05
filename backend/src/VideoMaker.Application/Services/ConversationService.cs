using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Ai;
using VideoMaker.Contracts.Common;
using VideoMaker.Contracts.Conversations;

namespace VideoMaker.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IApplicationDbContext _context;

    public ConversationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<ConversationSummaryDto>>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default)
    {
        var conversations = await _context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync(ct);

        return ApiResponse<List<ConversationSummaryDto>>.SuccessResult(conversations);
    }

    public async Task<ApiResponse<ConversationDetailsDto>> GetConversationByIdAsync(Guid userId, Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation == null)
        {
            return ApiResponse<ConversationDetailsDto>.ErrorResult("Conversation not found");
        }

        var messages = conversation.Messages.Select(m =>
        {
            VideoPlanDto? plan = null;
            if (!string.IsNullOrEmpty(m.VideoPlanJson))
            {
                try
                {
                    plan = JsonSerializer.Deserialize<VideoPlanDto>(m.VideoPlanJson);
                }
                catch
                {
                    // ignored if legacy or invalid format
                }
            }

            return new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role.ToString(),
                Content = m.Content,
                VideoPlan = plan,
                CreatedAt = m.CreatedAt
            };
        }).ToList();

        return ApiResponse<ConversationDetailsDto>.SuccessResult(new ConversationDetailsDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = messages
        });
    }

    public async Task<ApiResponse<bool>> DeleteConversationAsync(Guid userId, Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation == null)
        {
            return ApiResponse<bool>.ErrorResult("Conversation not found");
        }

        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResult(true, "Conversation deleted successfully");
    }
}
