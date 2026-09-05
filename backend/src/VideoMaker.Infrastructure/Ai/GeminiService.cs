using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Ai;
using VideoMaker.Contracts.Conversations;

namespace VideoMaker.Infrastructure.Ai;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;

    private const string SystemPrompt =
        @"You are an expert AI video producer, storytelling strategist, scriptwriter, social media strategist, cinematographer and audience-engagement expert.
Your job is to transform a user's basic video idea into a highly engaging video concept.

Analyze the user's idea.
Improve the hook.
Identify the target audience.
Create a strong story structure.
Recommend scenes with visual prompts, narration, and text overlays.
Recommend music style and emotional mood.
Recommend pacing.
Create an engaging ending and call-to-action.
Optimize the video for modern platforms such as YouTube Shorts, TikTok, Instagram Reels, and YouTube.

You MUST always provide an engaging conversational response, followed by a valid JSON object representing the video plan.
Format your output as:
[A warm, expert producer explanation and breakdown of why this video concept works and how it hooks the audience]

```json
{
  ""title"": ""Compelling Video Title"",
  ""targetAudience"": ""Specific target demographic and interest group"",
  ""duration"": 60,
  ""style"": ""Visual and cinematic style description"",
  ""hook"": ""The killer first 3-second hook that stops users from scrolling"",
  ""scenes"": [
    {
      ""sceneNumber"": 1,
      ""duration"": 5,
      ""visual"": ""Detailed cinematic visual prompt describing camera angle, action, lighting"",
      ""narration"": ""Exact spoken voiceover words for this scene"",
      ""textOverlay"": ""Bold punchy text overlay on screen""
    }
  ],
  ""music"": {
    ""style"": ""e.g., Cinematic electronic / upbeat synthwave / dramatic orchestral"",
    ""mood"": ""e.g., Futuristic, Inspirational, Energetic, Tense""
  },
  ""cta"": ""Clear call-to-action for the viewer at the end"",
  ""emotionalTone"": ""Primary emotional trajectory (e.g. Curiosity -> Shock -> Empowerment)"",
  ""keywords"": [""ai"", ""software"", ""future""],
  ""thumbnailConcept"": ""Striking thumbnail visual with high contrast and emotional face or symbol""
}
```
Always ensure the JSON block is syntactically valid.";

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(string ResponseText, VideoPlanDto? VideoPlan)> AnalyzeAndPlanVideoAsync(
        string userMessage,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"];

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var result = await CallGeminiApiAsync(apiKey, userMessage, history, cancellationToken);
                if (result.VideoPlan != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call Gemini API, falling back to intelligent video planner heuristic");
            }
        }
        else
        {
            _logger.LogInformation("No GEMINI_API_KEY configured. Utilizing intelligent video producer generator.");
        }

        return GenerateHeuristicVideoPlan(userMessage);
    }

    private async Task<(string ResponseText, VideoPlanDto? VideoPlan)> CallGeminiApiAsync(
        string apiKey,
        string userMessage,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        var model = _configuration["GEMINI_MODEL"] ?? _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var contents = new List<object>();

        // System instruction & conversation history
        foreach (var msg in history.TakeLast(8))
        {
            var role = msg.Role.Equals("User", StringComparison.OrdinalIgnoreCase) ? "user" : "model";
            contents.Add(new
            {
                role = role,
                parts = new object[] { new { text = msg.Content } }
            });
        }

        // Current message
        contents.Add(new
        {
            role = "user",
            parts = new object[] { new { text = userMessage } }
        });

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new object[] { new { text = SystemPrompt } }
            },
            contents = contents,
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Gemini API returned status code {StatusCode}: {Error}", response.StatusCode, err);
            return GenerateHeuristicVideoPlan(userMessage);
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = ParseGeminiResponse(responseString);
        return parsed;
    }

    private (string ResponseText, VideoPlanDto? VideoPlan) ParseGeminiResponse(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() > 0)
            {
                var content = candidates[0].GetProperty("content");
                var parts = content.GetProperty("parts");
                if (parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString() ?? string.Empty;
                    var (commentary, plan) = ExtractVideoPlanFromMarkdown(text);
                    return (commentary, plan);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response JSON");
        }

        return ("Here is your video plan based on your creative idea!", null);
    }

    private (string Commentary, VideoPlanDto? Plan) ExtractVideoPlanFromMarkdown(string text)
    {
        var jsonStartIndex = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (jsonStartIndex == -1)
        {
            jsonStartIndex = text.IndexOf("```", StringComparison.OrdinalIgnoreCase);
        }

        if (jsonStartIndex != -1)
        {
            var commentary = text.Substring(0, jsonStartIndex).Trim();
            var jsonCodeStart = text.IndexOf('\n', jsonStartIndex) + 1;
            var jsonEndIndex = text.IndexOf("```", jsonCodeStart, StringComparison.OrdinalIgnoreCase);

            if (jsonEndIndex != -1)
            {
                var jsonStr = text.Substring(jsonCodeStart, jsonEndIndex - jsonCodeStart).Trim();
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var plan = JsonSerializer.Deserialize<VideoPlanDto>(jsonStr, options);
                    return (commentary, plan);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not deserialize parsed json block from Gemini output");
                }
            }
        }

        // If direct json
        if (text.TrimStart().StartsWith("{"))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var plan = JsonSerializer.Deserialize<VideoPlanDto>(text.Trim(), options);
                return ("Here is the complete structured video plan ready for production.", plan);
            }
            catch
            {
                // ignored
            }
        }

        return (text, null);
    }

    private (string ResponseText, VideoPlanDto? VideoPlan) GenerateHeuristicVideoPlan(string userPrompt)
    {
        var promptLower = userPrompt.ToLowerInvariant();
        var topic = userPrompt.Trim();
        if (topic.Length > 50) topic = topic.Substring(0, 47) + "...";

        string title = $"The Ultimate Guide to {topic}";
        string style = "Cinematic Documentary with High-Energy Visuals";
        string audience = "Tech-savvy creators, ambitious professionals, and digital innovators";
        string hook = $"What if everything you thought you knew about {topic} is about to change forever?";
        string musicStyle = "Cinematic Hybrid Electronic";
        string musicMood = "Futuristic, Epic, Suspenseful";
        string cta = "Hit Subscribe and comment below what you want to see next!";
        string emotionalTone = "Curiosity -> Revelation -> Inspiration";

        if (promptLower.Contains("ai") || promptLower.Contains("software") || promptLower.Contains("developer") || promptLower.Contains("code"))
        {
            title = "How AI is Reshaping the Future of Software";
            style = "Sleek Dark Tech Aesthetic with Cyberpunk Glitch & Code Overlays";
            audience = "Software Engineers, Tech Founders, and AI Enthusiasts";
            hook = "What if AI could write, test, and deploy entire apps in seconds?";
            musicStyle = "Synthesizer Arpeggios & Deep Ambient Bass";
            musicMood = "Futuristic, Accelerating, Mind-Bending";
            cta = "Drop your favorite AI tool in the comments and follow for daily tech insights!";
            emotionalTone = "Awe -> Acceleration -> Empowerment";
        }
        else if (promptLower.Contains("elon") || promptLower.Contains("musk") || promptLower.Contains("motivation") || promptLower.Contains("success"))
        {
            title = "The Relentless Mindset: Lessons from Impossible Ambition";
            style = "Dramatic High-Contrast Monochrome with Golden Lens Flares";
            audience = "Entrepreneurs, Students, and High Performers";
            hook = "When the world said it was impossible, they risked everything.";
            musicStyle = "Orchestral Strings Building to a Triumphant Climax";
            musicMood = "Emotional, Inspiring, Heroic";
            cta = "Share this with someone who needs the fire to keep building.";
            emotionalTone = "Struggle -> Resilience -> Victory";
        }

        var plan = new VideoPlanDto
        {
            Title = title,
            TargetAudience = audience,
            Duration = 60,
            Style = style,
            Hook = hook,
            Music = new MusicPlanDto
            {
                Style = musicStyle,
                Mood = musicMood
            },
            Cta = cta,
            EmotionalTone = emotionalTone,
            Keywords = new List<string> { "viral", "trending", "innovation", "cinematic", "video" },
            ThumbnailConcept = $"Ultra high-contrast dramatic portrait with neon glow accents and bold title typography",
            Scenes = new List<VideoScenePlanDto>
            {
                new()
                {
                    SceneNumber = 1,
                    Duration = 6,
                    Visual = "Dynamic fast zoom-in on an glowing ultra-modern workstation surrounded by holographic data streams in 4K cinematic lighting.",
                    Narration = hook,
                    TextOverlay = "THE GAME HAS CHANGED"
                },
                new()
                {
                    SceneNumber = 2,
                    Duration = 12,
                    Visual = "Split-screen comparison showing traditional slow workflow versus rapid instant intelligent transformation.",
                    Narration = "For decades, traditional processes took months of painful iteration. But today, a revolution is taking place right beneath our feet.",
                    TextOverlay = "THE SHIFT IS HAPPENING NOW"
                },
                new()
                {
                    SceneNumber = 3,
                    Duration = 18,
                    Visual = "Macro close-up of interconnected neural networks lighting up with pulsing electric blue energy waves across a dark glass screen.",
                    Narration = "Intelligent systems are taking over the heavy lifting, amplifying human creativity by tenfold and unlocking possibilities previously thought to be science fiction.",
                    TextOverlay = "10X LEVERAGE UNLOCKED"
                },
                new()
                {
                    SceneNumber = 4,
                    Duration = 16,
                    Visual = "Vibrant montage of people across the globe building groundbreaking projects, smiling at digital breakthroughs in high-speed cinematic cuts.",
                    Narration = "The real winners won't be those who fear change, but those who harness these tools to build the future before anyone else.",
                    TextOverlay = "ADAPT OR GET LEFT BEHIND"
                },
                new()
                {
                    SceneNumber = 5,
                    Duration = 8,
                    Visual = "Clean gradient logo reveal with floating particle effects and animated subscribe/follow bell notification badge.",
                    Narration = cta,
                    TextOverlay = "JOIN THE REVOLUTION"
                }
            }
        };

        var commentary =
            $"I've analyzed your idea: \"{userPrompt}\".\n\n" +
            $"### Creative Strategy & Video Producer Blueprint:\n" +
            $"1. **The Hook Strategy**: We kick off with a high-impact pattern-interrupt question (\"{hook}\") that achieves maximum retention in the first 3 seconds.\n" +
            $"2. **Pacing & Narrative Curve**: A 5-scene 60-second narrative arc that shifts from immediate tension to explosive resolution, optimized for high completion rates on YouTube Shorts, TikTok, and Instagram Reels.\n" +
            $"3. **Cinematic Style & Sound Design**: Paired with `{musicStyle}` ({musicMood}) to create an immersive, premium atmosphere.\n\n" +
            $"Review the structured video plan on the right. You can tweak individual scene prompts, durations, or click **Generate Video** when you're ready!";

        return (commentary, plan);
    }
}
