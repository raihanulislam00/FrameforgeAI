using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Ai;
using VideoMaker.Domain.Enums;

namespace VideoMaker.Infrastructure.Services;

public sealed class VideoGenerationBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<VideoGenerationBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var generator = scope.ServiceProvider.GetRequiredService<IVideoGenerationService>();
                var job = await context.VideoGenerationJobs.Include(x => x.Video)
                    .Where(x => x.Status == JobStatus.Pending)
                    .OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(stoppingToken);

                if (job is not null && job.Video is not null)
                {
                    job.Status = JobStatus.Processing;
                    job.StartedAt = DateTime.UtcNow;
                    job.Provider = generator.ProviderName;
                    job.Video.Status = VideoStatus.Processing;
                    await context.SaveChangesAsync(stoppingToken);

                    var plan = JsonSerializer.Deserialize<VideoPlanDto>(job.Video.VideoPlanJson ?? "{}") ?? new();
                    var result = await generator.GenerateAsync(new VideoGenerationRequest
                    {
                        VideoId = job.Video.Id, Title = job.Video.Title, Description = job.Video.Description, VideoPlan = plan
                    }, stoppingToken);

                    job.Progress = 100;
                    job.Status = result.Success ? JobStatus.Completed : JobStatus.Failed;
                    job.ErrorMessage = result.ErrorMessage;
                    job.ProviderJobId = result.ProviderJobId;
                    job.CompletedAt = DateTime.UtcNow;
                    job.Video.Status = result.Success ? VideoStatus.Completed : VideoStatus.Failed;
                    job.Video.VideoUrl = result.VideoUrl;
                    job.Video.ThumbnailUrl = result.ThumbnailUrl;
                    job.Video.Provider = generator.ProviderName;
                    job.Video.CompletedAt = result.Success ? DateTime.UtcNow : null;
                    job.Video.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video generation worker iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
