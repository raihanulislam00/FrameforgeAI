using Microsoft.EntityFrameworkCore;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Auth;
using VideoMaker.Contracts.Common;
using VideoMaker.Contracts.Dashboard;
using VideoMaker.Domain.Enums;

namespace VideoMaker.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DashboardResponse>> GetDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
        {
            return ApiResponse<DashboardResponse>.ErrorResult("User not found");
        }

        var totalVideos = await _context.Videos.CountAsync(v => v.UserId == userId, ct);
        var completedVideos = await _context.Videos.CountAsync(v => v.UserId == userId && v.Status == VideoStatus.Completed, ct);
        var processingVideos = await _context.Videos.CountAsync(v => v.UserId == userId && (v.Status == VideoStatus.Processing || v.Status == VideoStatus.Pending), ct);
        var failedVideos = await _context.Videos.CountAsync(v => v.UserId == userId && v.Status == VideoStatus.Failed, ct);

        var recentVideos = await _context.Videos
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .Take(6)
            .Select(v => new RecentVideoDto
            {
                Id = v.Id,
                Title = v.Title,
                Status = v.Status.ToString(),
                Progress = v.Jobs.OrderByDescending(j => j.CreatedAt).Select(j => j.Progress).FirstOrDefault(),
                ThumbnailUrl = v.ThumbnailUrl,
                VideoUrl = v.VideoUrl,
                Duration = v.Duration,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync(ct);

        var response = new DashboardResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            },
            Statistics = new DashboardStatsDto
            {
                TotalVideos = totalVideos,
                CompletedVideos = completedVideos,
                ProcessingVideos = processingVideos,
                FailedVideos = failedVideos
            },
            RecentVideos = recentVideos
        };

        return ApiResponse<DashboardResponse>.SuccessResult(response);
    }
}
