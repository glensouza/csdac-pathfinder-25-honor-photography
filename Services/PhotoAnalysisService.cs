using OllamaSharp;
using OllamaSharp.Models;
using System.Text.Json;

namespace PathfinderPhotography.Services;

public class PhotoAnalysisService
{
    private readonly IOllamaClientProvider _ollamaClientProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhotoAnalysisService> _logger;

    public PhotoAnalysisService(
        IOllamaClientProvider ollamaClientProvider,
        IConfiguration configuration,
        ILogger<PhotoAnalysisService> logger)
    {
        _ollamaClientProvider = ollamaClientProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PhotoAnalysisResult> AnalyzePhotoAsync(byte[] imageData, string fileName, string compositionRule)
    {
        _logger.LogInformation("Starting AI analysis for photo: {FileName}", fileName);

        PhotoAnalysisResult result = new PhotoAnalysisResult
        {
            OriginalFileName = fileName
        };

        try
        {
            IOllamaApiClient client = _ollamaClientProvider.GetClient();
            string visionModel = _configuration["AI:Ollama:VisionModel"] ?? "llava";
            string textModel = _configuration["AI:Ollama:TextModel"] ?? "llama2";

            // Step 1: Generate description and title using vision model
            _logger.LogDebug("Generating description and title with vision model: {Model}", visionModel);
            (result.Description, result.Title, result.Rating) = await GenerateDescriptionTitleAndRatingAsync(
                client, visionModel, imageData, fileName, compositionRule);

            // Step 2: Generate marketing content using text model
            _logger.LogDebug("Generating marketing content with text model: {Model}", textModel);
            MarketingContent marketing = await GenerateMarketingContentAsync(
                client, textModel, result.Title, result.Description, compositionRule);
            
            result.MarketingHeadline = marketing.Headline;
            result.MarketingCopy = marketing.MarketingCopy;
            result.SuggestedPrice = marketing.Price;
            result.SocialMediaText = marketing.SocialMediaText;

            // Step 3: Generate marketing image using image generation model (optional)
            string? imageGenModel = _configuration["AI:Ollama:ImageGenerationModel"];
            if (!string.IsNullOrEmpty(imageGenModel))
            {
                _logger.LogDebug("Generating marketing image with model: {Model}", imageGenModel);
                try
                {
                    (result.MarketingImageData, result.MarketingImagePrompt) = await GenerateMarketingImageAsync(
                        client, imageGenModel, result.Title, result.Description, marketing.Headline);
                }
                catch (Exception imgEx)
                {
                    _logger.LogWarning(imgEx, "Failed to generate marketing image, continuing without it");
                    // Continue without marketing image - it's optional
                }
            }

            _logger.LogInformation("AI analysis complete: Title='{Title}', Rating={Rating}", 
                result.Title, result.Rating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze photo {FileName}", fileName);
            
            // Return partial results with fallback values
            if (string.IsNullOrWhiteSpace(result.Title))
            {
                result.Title = Path.GetFileNameWithoutExtension(fileName);
            }
            if (string.IsNullOrWhiteSpace(result.Description))
            {
                result.Description = "AI analysis unavailable";
            }
            if (result.Rating == 0)
            {
                result.Rating = 5;
            }
        }

        return result;
    }

    private async Task<(string description, string title, int rating)> GenerateDescriptionTitleAndRatingAsync(
        IOllamaApiClient client,
        string visionModel,
        byte[] imageData,
        string fileName,
        string compositionRule)
    {
        _logger.LogDebug("Analyzing image with vision model for description, title, and rating");

        string prompt = $@"You are an expert photography instructor analyzing a student's photograph submitted for the '{compositionRule}' composition rule.

Analyze this photograph and provide:
1. A detailed description (2-3 sentences) focusing on composition, subject matter, lighting, and how well it demonstrates the '{compositionRule}' rule
2. A creative, compelling title (3-7 words) that captures the essence of the image
3. A quality/composition rating from 1-10, where 10 is exceptional and demonstrates mastery of the '{compositionRule}' rule

Respond ONLY with valid JSON in this exact format:
{{
  ""description"": ""your detailed description here"",
  ""title"": ""Your Creative Title"",
  ""rating"": 8
}}";

        try
        {
            client.SelectedModel = visionModel;
            
            // Convert image bytes to base64 string for Ollama
            string base64Image = Convert.ToBase64String(imageData);
            
            GenerateRequest request = new GenerateRequest
            {
                Prompt = prompt,
                Images = new[] { base64Image },
                Stream = false,
                Format = "json"
            };

            System.Text.StringBuilder responseBuilder = new System.Text.StringBuilder();
            
            await foreach (GenerateResponseStream? stream in client.GenerateAsync(request))
            {
                if (stream?.Response != null)
                {
                    responseBuilder.Append(stream.Response);
                }
            }

            string jsonResponse = responseBuilder.ToString();
            
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogWarning("Empty response from vision model");
                return ("A photograph", Path.GetFileNameWithoutExtension(fileName), 5);
            }

            _logger.LogDebug("Vision model response: {Response}", jsonResponse);

            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            string description = root.TryGetProperty("description", out JsonElement descElement)
                ? descElement.GetString() ?? "A photograph"
                : "A photograph";

            string title = root.TryGetProperty("title", out JsonElement titleElement)
                ? titleElement.GetString() ?? Path.GetFileNameWithoutExtension(fileName)
                : Path.GetFileNameWithoutExtension(fileName);

            int rating = root.TryGetProperty("rating", out JsonElement ratingElement)
                ? ratingElement.GetInt32()
                : 5;

            // Ensure rating is in valid range
            rating = Math.Clamp(rating, 1, 10);

            return (description, title, rating);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from vision model, using fallback values");
            return ("A photograph demonstrating " + compositionRule, Path.GetFileNameWithoutExtension(fileName), 5);
        }
    }

    private async Task<MarketingContent> GenerateMarketingContentAsync(
        IOllamaApiClient client,
        string textModel,
        string title,
        string description,
        string compositionRule)
    {
        _logger.LogDebug("Generating marketing content for: {Title}", title);

        string prompt = $@"You are a marketing expert helping young photographers (ages 10-15 called Pathfinders) learn how to present and market their photography work.

Photo Details:
- Title: {title}
- Description: {description}
- Composition Rule: {compositionRule}

Create educational marketing content that teaches them professional presentation:

1. Price: Suggest a realistic starting price in USD for this as a small print (8x10), considering it's a student work demonstrating composition skills ($5-$25 range is appropriate)
2. Headline: Create an attention-grabbing headline (5-10 words) that would make someone interested in this photo
3. Marketing Copy: Write 2-3 short paragraphs explaining what makes this photo special, how it demonstrates good composition, and why someone might want it (keep it age-appropriate and educational)
4. Social Media: Create a fun social media post (under 280 characters) with 2-3 hashtags to share this accomplishment

Make it encouraging, educational, and help them see the value in their work while being realistic.

Respond ONLY with valid JSON in this exact format:
{{
  ""price"": 15.00,
  ""headline"": ""Your Headline Here"",
  ""marketingCopy"": ""Your marketing copy paragraphs here..."",
  ""socialMediaText"": ""Your social media post with #hashtags""
}}";

        try
        {
            client.SelectedModel = textModel;
            
            GenerateRequest request = new GenerateRequest
            {
                Prompt = prompt,
                Stream = false,
                Format = "json"
            };

            System.Text.StringBuilder responseBuilder = new System.Text.StringBuilder();
            
            await foreach (GenerateResponseStream? stream in client.GenerateAsync(request))
            {
                if (stream?.Response != null)
                {
                    responseBuilder.Append(stream.Response);
                }
            }

            string jsonResponse = responseBuilder.ToString();
            
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogWarning("Empty response from text model");
                return GetFallbackMarketingContent(compositionRule);
            }

            _logger.LogDebug("Text model response: {Response}", jsonResponse);

            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            decimal price = root.TryGetProperty("price", out JsonElement priceElement)
                ? priceElement.GetDecimal()
                : 10.00m;

            string headline = root.TryGetProperty("headline", out JsonElement headlineElement)
                ? headlineElement.GetString() ?? $"Amazing {compositionRule} Photography"
                : $"Amazing {compositionRule} Photography";

            string marketingCopy = root.TryGetProperty("marketingCopy", out JsonElement copyElement)
                ? copyElement.GetString() ?? "A wonderful photograph demonstrating excellent composition skills."
                : "A wonderful photograph demonstrating excellent composition skills.";

            string socialMediaText = root.TryGetProperty("socialMediaText", out JsonElement socialElement)
                ? socialElement.GetString() ?? $"Check out my photography! #{compositionRule.Replace(" ", "")} #PathfinderPhotography"
                : $"Check out my photography! #{compositionRule.Replace(" ", "")} #PathfinderPhotography";

            // Ensure price is reasonable
            price = Math.Clamp(price, 5.00m, 25.00m);

            return new MarketingContent
            {
                Price = price,
                Headline = headline,
                MarketingCopy = marketingCopy,
                SocialMediaText = socialMediaText
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from text model, using fallback values");
            return GetFallbackMarketingContent(compositionRule);
        }
    }

    private static MarketingContent GetFallbackMarketingContent(string compositionRule)
    {
        return new MarketingContent
        {
            Price = 10.00m,
            Headline = $"Beautiful {compositionRule} Photography",
            MarketingCopy = "This photograph demonstrates excellent understanding of composition principles and showcases developing photography skills. A wonderful piece that captures the essence of the subject while applying fundamental rules of photography.",
            SocialMediaText = $"Proud of my photography work! #{compositionRule.Replace(" ", "")} #PathfinderPhotography #YoungPhotographer"
        };
    }

    private async Task<(byte[]? imageData, string prompt)> GenerateMarketingImageAsync(
        IOllamaApiClient client,
        string imageGenModel,
        string title,
        string description,
        string headline)
    {
        _logger.LogDebug("Generating marketing image for: {Title}", title);

        // Create a prompt for generating a marketing/promotional image
        string prompt = $@"Professional marketing photograph, product photography style: {headline}. 
{description}. 
High quality, commercial photography, product shot, clean background, professional lighting, 
marketing material, advertising quality, HD, detailed, sharp focus";

        try
        {
            // Note: Ollama's image generation support varies by model
            // This is a placeholder - actual implementation depends on the specific
            // image generation model being used (e.g., stable-diffusion via Ollama)
            
            // For now, we'll log and return null since image generation through Ollama
            // requires specific models and may not be available by default
            _logger.LogInformation("Marketing image generation requested with prompt: {Prompt}", prompt);
            _logger.LogWarning("Image generation through Ollama requires specific models (e.g., stable-diffusion). " +
                "This feature is optional and currently returns null. Install an image generation model in Ollama to enable this feature.");
            
            // Return the prompt for future reference, but no image data yet
            return (null, prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate marketing image");
            return (null, prompt);
        }
    }
}

public class PhotoAnalysisResult
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string MarketingHeadline { get; set; } = string.Empty;
    public string MarketingCopy { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public string SocialMediaText { get; set; } = string.Empty;
    public byte[]? MarketingImageData { get; set; }
    public string? MarketingImagePrompt { get; set; }
}

public class MarketingContent
{
    public decimal Price { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string MarketingCopy { get; set; } = string.Empty;
    public string SocialMediaText { get; set; } = string.Empty;
}
