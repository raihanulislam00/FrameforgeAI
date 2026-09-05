using Microsoft.EntityFrameworkCore;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Domain.Entities;
using VideoMaker.Domain.Enums;

namespace VideoMaker.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<VideoScene> VideoScenes => Set<VideoScene>();
    public DbSet<VideoGenerationJob> VideoGenerationJobs => Set<VideoGenerationJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();

            entity.HasMany(u => u.RefreshTokens)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Conversations)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Videos)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.Token);
            entity.Property(r => r.Token).IsRequired().HasMaxLength(256);
        });

        // Conversation
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.UserId);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Videos)
                .WithOne(v => v.Conversation)
                .HasForeignKey(v => v.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.CreatedAt);
            entity.Property(m => m.Role).HasConversion<string>();
            entity.Property(m => m.Content).IsRequired();
        });

        // Video
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.UserId);
            entity.HasIndex(v => v.Status);
            entity.HasIndex(v => v.CreatedAt);
            entity.Property(v => v.Title).IsRequired().HasMaxLength(200);
            entity.Property(v => v.Status).HasConversion<string>();

            entity.HasMany(v => v.Scenes)
                .WithOne(s => s.Video)
                .HasForeignKey(s => s.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.Jobs)
                .WithOne(j => j.Video)
                .HasForeignKey(j => j.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VideoScene
        modelBuilder.Entity<VideoScene>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.VideoId);
            entity.Property(s => s.VisualPrompt).IsRequired();
        });

        // VideoGenerationJob
        modelBuilder.Entity<VideoGenerationJob>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.HasIndex(j => j.VideoId);
            entity.HasIndex(j => j.Status);
            entity.Property(j => j.Status).HasConversion<string>();
        });
    }
}
