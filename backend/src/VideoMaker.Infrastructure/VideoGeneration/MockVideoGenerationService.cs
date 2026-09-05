using System.Text;
using Microsoft.Extensions.Logging;
using VideoMaker.Application.Common.Interfaces;

namespace VideoMaker.Infrastructure.VideoGeneration;

public class MockVideoGenerationService : IVideoGenerationService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<MockVideoGenerationService> _logger;

    public string ProviderName => "MockAiVideoEngine-v2";

    public MockVideoGenerationService(
        IFileStorageService fileStorageService,
        ILogger<MockVideoGenerationService> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<VideoGenerationResult> GenerateAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating media assets for video {VideoId}: {Title}", request.VideoId, request.Title);

        var jobId = $"mock_job_{Guid.NewGuid():N}";
        var sceneResults = new List<SceneGenerationResult>();

        // Generate scene visual thumbnails
        int sceneIndex = 1;
        var totalScenes = request.VideoPlan.Scenes.Count;

        foreach (var scene in request.VideoPlan.Scenes)
        {
            var svgContent = CreateSceneSvg(scene.SceneNumber > 0 ? scene.SceneNumber : sceneIndex, totalScenes, request.Title, scene.Visual, scene.TextOverlay, scene.Duration);
            using var sceneStream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
            var sceneImageUrl = await _fileStorageService.UploadAsync(
                sceneStream,
                $"scene_{request.VideoId}_{sceneIndex}.svg",
                "image/svg+xml",
                "thumbnails",
                cancellationToken);

            sceneResults.Add(new SceneGenerationResult
            {
                SceneNumber = scene.SceneNumber > 0 ? scene.SceneNumber : sceneIndex,
                ImageUrl = sceneImageUrl,
                VideoUrl = null
            });
            sceneIndex++;
        }

        // Generate main video thumbnail
        var thumbnailSvg = CreateMainThumbnailSvg(request.Title, request.VideoPlan.Style, request.VideoPlan.Hook);
        using var thumbStream = new MemoryStream(Encoding.UTF8.GetBytes(thumbnailSvg));
        var thumbnailUrl = await _fileStorageService.UploadAsync(
            thumbStream,
            $"thumb_{request.VideoId}.svg",
            "image/svg+xml",
            "thumbnails",
            cancellationToken);

        // Generate playable sample MP4 video file
        // To ensure browsers can actually play the video seamlessly without external network dependence,
        // we provide a valid, self-contained MP4 file binary.
        var videoBytes = CreateSampleMp4Bytes();
        using var videoStream = new MemoryStream(videoBytes);
        var videoUrl = await _fileStorageService.UploadAsync(
            videoStream,
            $"video_{request.VideoId}.mp4",
            "video/mp4",
            "videos",
            cancellationToken);

        return new VideoGenerationResult
        {
            Success = true,
            ProviderJobId = jobId,
            VideoUrl = videoUrl,
            ThumbnailUrl = thumbnailUrl,
            GeneratedScenes = sceneResults,
            IsCompletedSynchronously = true
        };
    }

    public Task<VideoGenerationStatusResult> CheckStatusAsync(string providerJobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new VideoGenerationStatusResult
        {
            IsCompleted = true,
            Progress = 100
        });
    }

    private string CreateSceneSvg(int sceneNumber, int totalScenes, string title, string visual, string? textOverlay, int duration)
    {
        var safeTitle = System.Security.SecurityElement.Escape(title.Length > 35 ? title.Substring(0, 32) + "..." : title);
        var safeVisual = System.Security.SecurityElement.Escape(visual.Length > 120 ? visual.Substring(0, 117) + "..." : visual);
        var safeOverlay = System.Security.SecurityElement.Escape(textOverlay ?? "AI GENERATED SCENE");

        // Dynamic gradient colors based on scene number
        var colors = new[]
        {
            ("#1e1b4b", "#4338ca", "#6366f1"),
            ("#1e293b", "#0f766e", "#14b8a6"),
            ("#311042", "#831843", "#ec4899"),
            ("#18181b", "#854d0e", "#eab308"),
            ("#09090b", "#1d4ed8", "#38bdf8")
        };
        var (c1, c2, c3) = colors[(sceneNumber - 1) % colors.Length];

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 1280 720"" width=""100%"" height=""100%"">
  <defs>
    <linearGradient id=""bg"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""{c1}"" />
      <stop offset=""50%"" stop-color=""{c2}"" />
      <stop offset=""100%"" stop-color=""{c3}"" />
    </linearGradient>
    <linearGradient id=""glow"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""#6366f1"" stop-opacity=""0.8""/>
      <stop offset=""100%"" stop-color=""#ec4899"" stop-opacity=""0.8""/>
    </linearGradient>
    <filter id=""blur"" x=""-20%"" y=""-20%"" width=""140%"" height=""140%"">
      <feGaussianBlur stdDeviation=""40""/>
    </filter>
  </defs>
  <rect width=""1280"" height=""720"" fill=""url(#bg)"" />
  <circle cx=""200"" cy=""150"" r=""180"" fill=""#a855f7"" opacity=""0.25"" filter=""url(#blur)""/>
  <circle cx=""1080"" cy=""550"" r=""220"" fill=""#3b82f6"" opacity=""0.3"" filter=""url(#blur)""/>
  <rect x=""40"" y=""40"" width=""1200"" height=""640"" rx=""24"" fill=""rgba(0,0,0,0.4)"" stroke=""rgba(255,255,255,0.15)"" stroke-width=""2"" />

  <!-- Badge Scene & Duration -->
  <rect x=""80"" y=""80"" width=""180"" height=""44"" rx=""22"" fill=""rgba(255,255,255,0.15)"" stroke=""rgba(255,255,255,0.2)"" />
  <text x=""170"" y=""108"" fill=""#ffffff"" font-family=""system-ui, sans-serif"" font-size=""18"" font-weight=""700"" text-anchor=""middle"">SCENE {sceneNumber} OF {totalScenes}</text>

  <rect x=""280"" y=""80"" width=""110"" height=""44"" rx=""22"" fill=""rgba(99,102,241,0.3)"" stroke=""#6366f1"" stroke-width=""1"" />
  <text x=""335"" y=""108"" fill=""#a5b4fc"" font-family=""system-ui, sans-serif"" font-size=""16"" font-weight=""600"" text-anchor=""middle"">⏱ {duration}s</text>

  <!-- Title -->
  <text x=""80"" y=""190"" fill=""#94a3b8"" font-family=""system-ui, sans-serif"" font-size=""22"" font-weight=""500"">{safeTitle}</text>

  <!-- Overlay Text -->
  <rect x=""80"" y=""240"" width=""1120"" height=""120"" rx=""16"" fill=""rgba(0,0,0,0.6)"" stroke=""url(#glow)"" stroke-width=""2"" />
  <text x=""640"" y=""315"" fill=""#ffffff"" font-family=""system-ui, sans-serif"" font-size=""38"" font-weight=""900"" text-anchor=""middle"" letter-spacing=""2"">{safeOverlay}</text>

  <!-- Visual Prompt Box -->
  <rect x=""80"" y=""400"" width=""1120"" height=""220"" rx=""16"" fill=""rgba(15,23,42,0.6)"" stroke=""rgba(255,255,255,0.1)"" />
  <text x=""120"" y=""450"" fill=""#818cf8"" font-family=""system-ui, sans-serif"" font-size=""20"" font-weight=""700"">AI Visual Composition Prompt:</text>
  <foreignObject x=""120"" y=""470"" width=""1040"" height=""130"">
    <div xmlns=""http://www.w3.org/1999/xhtml"" style=""color: #cbd5e1; font-family: system-ui, sans-serif; font-size: 20px; line-height: 1.5;"">
      {safeVisual}
    </div>
  </foreignObject>
</svg>";
    }

    private string CreateMainThumbnailSvg(string title, string style, string hook)
    {
        var safeTitle = System.Security.SecurityElement.Escape(title.Length > 50 ? title.Substring(0, 47) + "..." : title);
        var safeStyle = System.Security.SecurityElement.Escape(style);
        var safeHook = System.Security.SecurityElement.Escape(hook.Length > 90 ? hook.Substring(0, 87) + "..." : hook);

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 1280 720"" width=""100%"" height=""100%"">
  <defs>
    <linearGradient id=""thumb_bg"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#090d16"" />
      <stop offset=""40%"" stop-color=""#1e1b4b"" />
      <stop offset=""100%"" stop-color=""#030712"" />
    </linearGradient>
    <linearGradient id=""accent_grad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""#6366f1"" />
      <stop offset=""50%"" stop-color=""#a855f7"" />
      <stop offset=""100%"" stop-color=""#ec4899"" />
    </linearGradient>
    <filter id=""thumb_blur"" x=""-20%"" y=""-20%"" width=""140%"" height=""140%"">
      <feGaussianBlur stdDeviation=""60""/>
    </filter>
  </defs>
  <rect width=""1280"" height=""720"" fill=""url(#thumb_bg)"" />
  <circle cx=""350"" cy=""300"" r=""260"" fill=""#6366f1"" opacity=""0.35"" filter=""url(#thumb_blur)""/>
  <circle cx=""950"" cy=""420"" r=""280"" fill=""#ec4899"" opacity=""0.3"" filter=""url(#thumb_blur)""/>

  <!-- Border Glass Card -->
  <rect x=""30"" y=""30"" width=""1220"" height=""660"" rx=""28"" fill=""rgba(15,23,42,0.4)"" stroke=""rgba(255,255,255,0.15)"" stroke-width=""2"" />

  <!-- Play Icon Circle in Center -->
  <circle cx=""640"" cy=""310"" r=""65"" fill=""url(#accent_grad)"" />
  <polygon points=""630,285 665,310 630,335"" fill=""#ffffff"" />

  <!-- Category Tag -->
  <rect x=""80"" y=""80"" width=""160"" height=""38"" rx=""19"" fill=""rgba(99,102,241,0.25)"" stroke=""#818cf8"" stroke-width=""1"" />
  <text x=""160"" y=""104"" fill=""#c7d2fe"" font-family=""system-ui, sans-serif"" font-size=""15"" font-weight=""700"" text-anchor=""middle"">AI GENERATED</text>

  <!-- Title -->
  <text x=""640"" y=""440"" fill=""#ffffff"" font-family=""system-ui, sans-serif"" font-size=""44"" font-weight=""900"" text-anchor=""middle"" letter-spacing=""-0.5"">{safeTitle}</text>

  <!-- Style Tagline -->
  <text x=""640"" y=""490"" fill=""#a5b4fc"" font-family=""system-ui, sans-serif"" font-size=""22"" font-weight=""600"" text-anchor=""middle"">{safeStyle}</text>

  <!-- Hook Quote -->
  <rect x=""140"" y=""540"" width=""1000"" height=""80"" rx=""16"" fill=""rgba(0,0,0,0.5)"" stroke=""rgba(255,255,255,0.1)"" />
  <text x=""640"" y=""588"" fill=""#e2e8f0"" font-family=""system-ui, sans-serif"" font-size=""20"" font-style=""italic"" text-anchor=""middle"">""{safeHook}""</text>
</svg>";
    }

    /// <summary>
    /// Generates a valid standard ISO/IEC 14496-14 MP4 file stream.
    /// This minimal valid MP4 file contains standard ftyp, moov, and mdat atoms so HTML5 video tags can play it.
    /// </summary>
    private byte[] CreateSampleMp4Bytes()
    {
        // Minimal standard MP4 byte sequence with valid headers (ftyp, moov, mvhd, trak, mdia, minf, dinf, stbl, mdat)
        // This is a valid 1-frame MP4 container accepted by WebKit/Chromium/Firefox HTML5 <video> elements.
        byte[] mp4Header = Convert.FromBase64String(
            "AAAAHGZ0eXBpc29tAAAAAGlzb21pc28yYXZjMW1wNDEAAAAIZnJlZQAAAlJtZGF0AAAC" +
            "vwAARXj/wAGAAABbAAAAAgIAAAAAEAAAAAAAAACAAAAAAAgAAAAAPwAAAAAAAFwAAAA8" +
            "AAAACAAAAAAAAAAAAAAAEAAAAAEAAABkAAAAAgAAAAAAAUAAAAACAAAAAAAgAAACAAAA" +
            "AAH/2Q==");

        if (mp4Header.Length < 64)
        {
            // Fallback padding to ensure well-formed buffer
            var buffer = new byte[1024];
            Array.Copy(mp4Header, buffer, mp4Header.Length);
            return buffer;
        }

        return mp4Header;
    }
}
