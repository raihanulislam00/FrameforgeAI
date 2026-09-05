using Microsoft.EntityFrameworkCore;
using VideoMaker.Domain.Entities;

namespace VideoMaker.Infrastructure.Persistence;

public class VideoMakerDbContext : DbContext
{
    public VideoMakerDbContext(DbContextOptions<VideoMakerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<VideoScene> VideoScenes { get; set; }
    public DbSet<VideoGenerationJob> VideoGenerationJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>()
            .HasKey(rt => rt.Id);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Conversation configuration
        modelBuilder.Entity<Conversation>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.User)
            .WithMany(u => u.Conversations)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>()
            .HasKey(cm => cm.Id);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(cm => cm.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>()
            .Property(cm => cm.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Video configuration
        modelBuilder.Entity<Video>()
            .HasKey(v => v.Id);

        modelBuilder.Entity<Video>()
            .HasOne(v => v.User)
            .WithMany(u => u.Videos)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Video>()
            .HasOne(v => v.Conversation)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Video>()
            .Property(v => v.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // VideoScene configuration
        modelBuilder.Entity<VideoScene>()
            .HasKey(vs => vs.Id);

        modelBuilder.Entity<VideoScene>()
            .HasOne(vs => vs.Video)
            .WithMany(v => v.Scenes)
            .HasForeignKey(vs => vs.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VideoScene>()
            .Property(vs => vs.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // VideoGenerationJob configuration
        modelBuilder.Entity<VideoGenerationJob>()
            .HasKey(vgj => vgj.Id);

        modelBuilder.Entity<VideoGenerationJob>()
            .HasOne(vgj => vgj.Video)
            .WithMany(v => v.GenerationJobs)
            .HasForeignKey(vgj => vgj.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VideoGenerationJob>()
            .Property(vgj => vgj.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
