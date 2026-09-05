using Microsoft.EntityFrameworkCore;
using VideoMaker.Domain.Entities;

namespace VideoMaker.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Video> Videos { get; }
    DbSet<VideoScene> VideoScenes { get; }
    DbSet<VideoGenerationJob> VideoGenerationJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
