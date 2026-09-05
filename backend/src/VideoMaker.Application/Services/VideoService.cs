using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Ai;
using VideoMaker.Contracts.Common;
using VideoMaker.Contracts.Dashboard;
using VideoMaker.Contracts.Videos;
using VideoMaker.Domain.Entities;
using VideoMaker.Domain.Enums;

namespace VideoMaker.Application.Services;

public class VideoService : IVideoService
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public VideoService(IApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<ApiResponse<CreateVideoResponse>> CreateVideoAsync(Guid userId, CreateVideoRequest request, CancellationToken ct = default)
    {
        if (request.ConversationId.HasValue)
        {
            var conversationExists = await _context.Conversations
                .AnyAsync(c => c.Id == request.ConversationId.Value && c.UserId == userId, ct);

            if (!conversationExists)
            {
                return ApiResponse<CreateVideoResponse>.ErrorResult("Conversation not found or not owned by user");
            }
        }

        var plan = request.VideoPlan;
        var totalDuration = plan.Scenes.Sum(s => s.Duration);
        if (totalDuration == 0)
        {
            totalDuration = plan.Duration > 0 ? plan.Duration : 60;
        }

        var video = new Video
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConversationId = request.ConversationId,
            Title = request.Title.Trim(),
            Description = request.Description ?? $"{plan.Style} - {plan.Hook}",
            Status = VideoStatus.Pending,
            Duration = totalDuration,
            VideoPlanJson = JsonSerializer.Serialize(plan),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add scenes
        int sceneIndex = 1;
        foreach (var sceneDto in plan.Scenes)
        {
            video.Scenes.Add(new VideoScene
            {
                Id = Guid.NewGuid(),
                VideoId = video.Id,
                SceneNumber = sceneDto.SceneNumber > 0 ? sceneDto.SceneNumber : sceneIndex,
                Duration = sceneDto.Duration > 0 ? sceneDto.Duration : 5,
                VisualPrompt = sceneDto.Visual,
                Narration = sceneDto.Narration,
                TextOverlay = sceneDto.TextOverlay
            });
            sceneIndex++;
        }

        // Add VideoGenerationJob
        var job = new VideoGenerationJob
        {
            Id = Guid.NewGuid(),
            VideoId = video.Id,
            Status = JobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };
        video.Jobs.Add(job);

        _context.Videos.Add(video);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<CreateVideoResponse>.SuccessResult(new CreateVideoResponse
        {
            VideoId = video.Id,
            Status = VideoStatus.Pending.ToString()
        }, "Video generation job queued successfully");
    }

    public async Task<ApiResponse<PagedResult<RecentVideoDto>>> GetUserVideosAsync(Guid userId, VideoListQuery query, CancellationToken ct = default)
    {
        var videosQuery = _context.Videos
            .Where(v => v.UserId == userId);

        // Status filter
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VideoStatus>(query.Status, true, out var status))
        {
            videosQuery = videosQuery.Where(v => v.Status == status);
        }

        // Search query
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            videosQuery = videosQuery.Where(v => v.Title.ToLower().Contains(search) || (v.Description != null && v.Description.ToLower().Contains(search)));
        }

        var totalCount = await videosQuery.CountAsync(ct);

        // Sorting
        videosQuery = (query.SortBy?.ToLower()) switch
        {
            "title" => query.SortDescending ? videosQuery.OrderByDescending(v => v.Title) : videosQuery.OrderBy(v => v.Title),
            "duration" => query.SortDescending ? videosQuery.OrderByDescending(v => v.Duration) : videosQuery.OrderBy(v => v.Duration),
            "status" => query.SortDescending ? videosQuery.OrderByDescending(v => v.Status) : videosQuery.OrderBy(v => v.Status),
            _ => query.SortDescending ? videosQuery.OrderByDescending(v => v.CreatedAt) : videosQuery.OrderBy(v => v.CreatedAt)
        };

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await videosQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return ApiResponse<PagedResult<RecentVideoDto>>.SuccessResult(new PagedResult<RecentVideoDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<VideoDetailsDto>> GetVideoByIdAsync(Guid userId, Guid videoId, CancellationToken ct = default)
    {
        var video = await _context.Videos
            .Include(v => v.Scenes.OrderBy(s => s.SceneNumber))
            .Include(v => v.Jobs.OrderByDescending(j => j.CreatedAt))
            .FirstOrDefaultAsync(v => v.Id == videoId && v.UserId == userId, ct);

        if (video == null)
        {
            return ApiResponse<VideoDetailsDto>.ErrorResult("Video not found");
        }

        VideoPlanDto? plan = null;
        if (!string.IsNullOrEmpty(video.VideoPlanJson))
        {
            try
            {
                plan = JsonSerializer.Deserialize<VideoPlanDto>(video.VideoPlanJson);
            }
            catch
            {
                // ignored
            }
        }

        var response = new VideoDetailsDto
        {
            Id = video.Id,
            ConversationId = video.ConversationId,
            Title = video.Title,
            Description = video.Description,
            Status = video.Status.ToString(),
            Duration = video.Duration,
            VideoUrl = video.VideoUrl,
            ThumbnailUrl = video.ThumbnailUrl,
            Provider = video.Provider,
            VideoPlan = plan,
            CreatedAt = video.CreatedAt,
            CompletedAt = video.CompletedAt,
            Scenes = video.Scenes.Select(s => new VideoSceneDto
            {
                Id = s.Id,
                SceneNumber = s.SceneNumber,
                Duration = s.Duration,
                VisualPrompt = s.VisualPrompt,
                Narration = s.Narration,
                TextOverlay = s.TextOverlay,
                ImageUrl = s.ImageUrl,
                VideoUrl = s.VideoUrl
            }).ToList(),
            Jobs = video.Jobs.Select(j => new VideoJobDto
            {
                Id = j.Id,
                Status = j.Status.ToString(),
                Progress = j.Progress,
                Provider = j.Provider,
                ErrorMessage = j.ErrorMessage,
                CreatedAt = j.CreatedAt,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt
            }).ToList()
        };

        return ApiResponse<VideoDetailsDto>.SuccessResult(response);
    }

    public async Task<ApiResponse<bool>> DeleteVideoAsync(Guid userId, Guid videoId, CancellationToken ct = default)
    {
        var video = await _context.Videos
            .Include(v => v.Scenes)
            .Include(v => v.Jobs)
            .FirstOrDefaultAsync(v => v.Id == videoId && v.UserId == userId, ct);

        if (video == null)
        {
            return ApiResponse<bool>.ErrorResult("Video not found");
        }

        // Delete physical files
        if (!string.IsNullOrEmpty(video.VideoUrl))
        {
            await _fileStorageService.DeleteAsync(video.VideoUrl, ct);
        }
        if (!string.IsNullOrEmpty(video.ThumbnailUrl))
        {
            await _fileStorageService.DeleteAsync(video.ThumbnailUrl, ct);
        }

        _context.Videos.Remove(video);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResult(true, "Video deleted successfully");
    }

    public async Task<ApiResponse<bool>> CancelVideoAsync(Guid userId, Guid videoId, CancellationToken ct = default)
    {
        var video = await _context.Videos
            .Include(v => v.Jobs)
            .FirstOrDefaultAsync(v => v.Id == videoId && v.UserId == userId, ct);

        if (video == null)
        {
            return ApiResponse<bool>.ErrorResult("Video not found");
        }

        if (video.Status == VideoStatus.Completed || video.Status == VideoStatus.Cancelled)
        {
            return ApiResponse<bool>.ErrorResult($"Cannot cancel video with status {video.Status}");
        }

        video.Status = VideoStatus.Cancelled;
        video.UpdatedAt = DateTime.UtcNow;

        var activeJob = video.Jobs.FirstOrDefault(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Processing);
        if (activeJob != null)
        {
            activeJob.Status = JobStatus.Cancelled;
            activeJob.CompletedAt = DateTime.UtcNow;
            activeJob.ErrorMessage = "Job was cancelled by user.";
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.SuccessResult(true, "Video generation cancelled");
    }

    public async Task<ApiResponse<VideoStatusDto>> GetVideoStatusAsync(Guid userId, Guid videoId, CancellationToken ct = default)
    {
        var video = await _context.Videos
            .Include(v => v.Jobs.OrderByDescending(j => j.CreatedAt))
            .FirstOrDefaultAsync(v => v.Id == videoId && v.UserId == userId, ct);

        if (video == null)
        {
            return ApiResponse<VideoStatusDto>.ErrorResult("Video not found");
        }

        var latestJob = video.Jobs.FirstOrDefault();

        return ApiResponse<VideoStatusDto>.SuccessResult(new VideoStatusDto
        {
            VideoId = video.Id,
            Status = video.Status.ToString(),
            Progress = latestJob?.Progress ?? (video.Status == VideoStatus.Completed ? 100 : 0),
            ErrorMessage = latestJob?.ErrorMessage
        });
    }
}
