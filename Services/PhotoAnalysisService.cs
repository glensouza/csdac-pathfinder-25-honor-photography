using Google.Cloud.AIPlatform.V1;
using System.Text.Json;
using Grpc.Core;

namespace PathfinderPhotography.Services;

public class PhotoAnalysisService(IGeminiClientProvider geminiClientProvider, IConfiguration configuration, ILogger<PhotoAnalysisService> logger)
{
    public async Task<PhotoAnalysisResult> AnalyzePhotoAsync(byte[] imageData, string fileName, string compositionRule)
    {
        logger.LogInformation("Starting AI analysis for photo: {FileName}", fileName);

        PhotoAnalysisResult result = new()
        {
            OriginalFileName = fileName,
            AiSucceeded = false
        };

        try
        {
            PredictionServiceClient client = geminiClientProvider.GetPredictionClient();
            string projectId = geminiClientProvider.GetProjectId();
            string location = geminiClientProvider.GetLocation();
            
            string visionModel = configuration["AI:Gemini:VisionModel"] ?? "gemini-2.0-flash-exp";
            string imageGenModel = configuration["AI:Gemini:ImageGenerationModel"] ?? "imagen-3.0-generate-001";

            // Step 1: Generate description, title, and rating using Gemini vision model
            logger.LogDebug("Generating description and title with vision model: {Model}", visionModel);
            try
            {
                (result.Description, result.Title, result.Rating, MarketingContent marketing) = 
                    await this.GenerateAnalysisAndMarketingAsync(client, projectId, location, visionModel, imageData, fileName, compositionRule);

                result.MarketingHeadline = marketing.Headline;
                result.MarketingCopy = marketing.MarketingCopy;
                result.SuggestedPrice = marketing.Price;
                result.SocialMediaText = marketing.SocialMediaText;

                // Step 2: Generate marketing image using Imagen
                logger.LogDebug("Generating marketing image with model: {Model}", imageGenModel);
                try
                {
                    result.MarketingImageData = await this.GenerateMarketingImageAsync(
                        client, projectId, location, imageGenModel, result.Title, result.Description, marketing.Headline);
                }
                catch (RpcException rpcEx)
                {
                    // Image generation failed or is unsupported; log and continue with text-only results
                    logger.LogWarning(rpcEx, "Image generation unavailable for model {Model}. Continuing without marketing image.", imageGenModel);
                }

                result.AiSucceeded = true;

                logger.LogInformation("AI analysis complete: Title='{Title}', Rating={Rating}, Marketing image={HasImage}", 
                    result.Title, result.Rating, result.MarketingImageData != null);
            }
            catch (RpcException rpcEx)
            {
                // Known case: Gemini cannot be accessed via Predict API
                logger.LogWarning(rpcEx, "AI model request failed (model: {Model}). Falling back to safe defaults. See https://cloud.google.com/vertex-ai/docs/generative-ai/start/quickstarts/quickstart-multimodal for Gemini usage.", visionModel);

                // Provide safe fallback values without throwing so app continues to work
                string fallbackTitle = Path.GetFileNameWithoutExtension(fileName);
                MarketingContent fallbackMarketing = GetFallbackMarketingContent(compositionRule);

                PhotoAnalysisResult fallbackResult = new PhotoAnalysisResult()
                {
                    OriginalFileName = fileName,
                    Title = fallbackTitle,
                    Description = "AI analysis unavailable",
                    Rating = 5,
                    MarketingHeadline = fallbackMarketing.Headline,
                    MarketingCopy = fallbackMarketing.MarketingCopy,
                    SuggestedPrice = fallbackMarketing.Price,
                    SocialMediaText = fallbackMarketing.SocialMediaText,
                    AiSucceeded = false
                };

                return fallbackResult;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze photo {FileName}", fileName);
            
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

            result.AiSucceeded = false;
        }

        // Sanitize title if model returned an internal filename or placeholder
        if (!string.IsNullOrWhiteSpace(result.Title))
        {
            string trimmed = result.Title.Trim();
            bool looksLikeGeneratedFileName = trimmed.Contains("Gemini_Generated_Image", StringComparison.OrdinalIgnoreCase)
                                              || trimmed.Contains("Generated_Image", StringComparison.OrdinalIgnoreCase)
                                              || trimmed.Length > 80
                                              || System.Text.RegularExpressions.Regex.IsMatch(trimmed, "[_\\-]taj\\d+");

            if (looksLikeGeneratedFileName)
            {
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                result.Title = $"{SanitizeTitle(baseName)} - {compositionRule}";
                logger.LogDebug("Sanitized AI title to: {Title}", result.Title);
            }
        }

        return result;
    }

    private static string SanitizeTitle(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Untitled";
        string s = input.Replace('_', ' ').Replace('-', ' ').Trim();
        if (s.Length > 40) s = s.Substring(0, 40).TrimEnd();
        return s;
    }

    private async Task<(string description, string title, int rating, MarketingContent marketing)> 
        GenerateAnalysisAndMarketingAsync(
            PredictionServiceClient client,
            string projectId,
            string location,
            string model,
            byte[] imageData,
            string fileName,
            string compositionRule)
    {
        logger.LogDebug("Analyzing image with Gemini for description, title, rating, and marketing");

        string prompt = $$"""
                          You are an expert photography instructor analyzing a student's photograph submitted for the '{{compositionRule}}' composition rule.

                          Analyze this photograph and provide comprehensive feedback and marketing content for young photographers (ages 10-15).

                          Provide:
                          1. A detailed description (2-3 sentences) focusing on composition, subject matter, lighting, and how well it demonstrates the '{{compositionRule}}' rule
                          2. A creative, compelling title (3-7 words) that captures the essence of the image
                          3. A quality/composition rating from 1-10, where 10 demonstrates mastery of the '{{compositionRule}}' rule
                          4. A realistic starting price in USD for this as a small print (8x10), considering it's student work demonstrating composition skills ($5-$25 range)
                          5. An attention-grabbing headline (5-10 words) that would make someone interested in this photo
                          6. Marketing copy (2-3 short paragraphs) explaining what makes this photo special, how it demonstrates good composition, and why someone might want it (age-appropriate and educational)
                          7. A fun social media post (under 280 characters) with 2-3 hashtags to share this accomplishment

                          Make it encouraging, educational, and help them see the value in their work while being realistic.

                          Respond ONLY with valid JSON in this exact format:
                          {
                            "description": "your detailed description here",
                            "title": "Your Creative Title",
                            "rating": 8,
                            "price": 15.00,
                            "headline": "Your Headline Here",
                            "marketingCopy": "Your marketing copy paragraphs here...",
                            "socialMediaText": "Your social media post with #hashtags"
                          }
                          """;

        try
        {
            // Build the model resource name for Gemini
            string modelPath = $"projects/{projectId}/locations/{location}/publishers/google/models/{model}";

            // Create the request using GenerateContentAsync for Gemini models
            GenerateContentRequest request = new GenerateContentRequest
            {
                Model = modelPath,
                Contents =
                {
                    new Content
                    {
                        Role = "user",
                        Parts =
                        {
                            new Part { InlineData = new Blob { MimeType = "image/jpeg", Data = Google.Protobuf.ByteString.CopyFrom(imageData) } },
                            new Part { Text = prompt }
                        }
                    }
                }
            };

            GenerateContentResponse response;
            try
            {
                response = await client.GenerateContentAsync(request);
            }
            catch (RpcException rpcEx)
            {
                // If Gemini call fails, return fallbacks and surface guidance in logs
                logger.LogWarning(rpcEx, "GenerateContent call failed for model {Model}. See https://cloud.google.com/vertex-ai/docs/generative-ai/start/quickstarts/quickstart-multimodal for Gemini usage.", model);
                return ("AI analysis unavailable", Path.GetFileNameWithoutExtension(fileName), 5, GetFallbackMarketingContent(compositionRule));
            }

            if (response.Candidates.Count == 0 || response.Candidates[0].Content?.Parts.Count == 0)
            {
                logger.LogWarning("Empty response from Gemini");
                return ("A photograph", Path.GetFileNameWithoutExtension(fileName), 5, GetFallbackMarketingContent(compositionRule));
            }

            string? jsonResponse = response.Candidates[0].Content.Parts[0].Text;
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                logger.LogWarning("Empty text in Gemini response");
                return ("A photograph", Path.GetFileNameWithoutExtension(fileName), 5, GetFallbackMarketingContent(compositionRule));
            }
            
            // Log raw response for diagnosis (DEBUG only)
            logger.LogDebug("Raw Gemini response: {Response}", jsonResponse);

            // Strip markdown code blocks if present (Gemini sometimes wraps JSON in ```json ... ```)
            jsonResponse = jsonResponse.Trim();
            if (jsonResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                int startIndex = jsonResponse.IndexOf('\n') + 1;
                int endIndex = jsonResponse.LastIndexOf("```");
                if (startIndex > 0 && endIndex > startIndex)
                {
                    jsonResponse = jsonResponse.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            else if (jsonResponse.StartsWith("```"))
            {
                int startIndex = jsonResponse.IndexOf('\n') + 1;
                int endIndex = jsonResponse.LastIndexOf("```");
                if (startIndex > 0 && endIndex > startIndex)
                {
                    jsonResponse = jsonResponse.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            string description = root.TryGetProperty("description", out JsonElement descElement)
                ? descElement.GetString() ?? "A photograph"
                : "A photograph";

            string title = root.TryGetProperty("title", out JsonElement titleElement)
                ? titleElement.GetString() ?? Path.GetFileNameWithoutExtension(fileName)
                : Path.GetFileNameWithoutExtension(fileName);

            int rating = root.TryGetProperty("rating", out JsonElement ratingElement) && ratingElement.ValueKind == JsonValueKind.Number
                ? ratingElement.GetInt32()
                : 5;

            decimal price = root.TryGetProperty("price", out JsonElement priceElement) && priceElement.ValueKind == JsonValueKind.Number
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

            // Ensure values are in valid ranges
            rating = Math.Clamp(rating, 1, 10);
            price = Math.Clamp(price, 5.00m, 25.00m);

            MarketingContent marketing = new()
            {
                Price = price,
                Headline = headline,
                MarketingCopy = marketingCopy,
                SocialMediaText = socialMediaText
            };

            return (description, title, rating, marketing);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse JSON response from Gemini, using fallback values");
            return ("A photograph demonstrating " + compositionRule, Path.GetFileNameWithoutExtension(fileName), 5, 
                GetFallbackMarketingContent(compositionRule));
        }
    }

    private async Task<byte[]?> GenerateMarketingImageAsync(
        PredictionServiceClient client,
        string projectId,
        string location,
        string model,
        string title,
        string description,
        string headline)
    {
        logger.LogDebug("Generating marketing image for: {Title}", title);

        string prompt = $"Create a professional marketing image for a photography print featuring:\n\n" +
                       $"Title: {title}\n" +
                       $"Description: {description}\n" +
                       $"Headline: {headline}\n\n" +
                       "Design a clean, modern marketing visual showing this photograph displayed as a high-quality framed print on a gallery wall with soft lighting. " +
                       "The composition should be professional and appealing, showcasing the photograph as a product worth purchasing.\n" +
                       "Style: Professional product photography, clean aesthetic, gallery presentation.";

        try
        {
            // Build the model resource name (works for Gemini image generation models like gemini-2.5-flash-image)
            string modelPath = $"projects/{projectId}/locations/{location}/publishers/google/models/{model}";

            // Create the request using GenerateContentAsync for Gemini image generation models
            GenerateContentRequest request = new GenerateContentRequest
            {
                Model = modelPath,
                Contents =
                {
                    new Content
                    {
                        Role = "user",
                        Parts =
                        {
                            new Part { Text = prompt }
                        }
                    }
                }
            };

            GenerateContentResponse response;
            try
            {
                response = await client.GenerateContentAsync(request);
            }
            catch (RpcException rpcEx)
            {
                // If Gemini image generation call fails, return null (non-critical - app continues without marketing image)
                logger.LogWarning(rpcEx, "Image generation call failed for model {Model}. Continuing without marketing image.", model);
                return null;
            }

            if (response.Candidates.Count == 0 || response.Candidates[0].Content?.Parts.Count == 0)
            {
                logger.LogWarning("No marketing image generated by {Model}", model);
                return null;
            }

            // For image generation models, the response should contain inline image data
            Part firstPart = response.Candidates[0].Content.Parts[0];
            
            // Check if we have inline image data
            if (firstPart.InlineData != null && firstPart.InlineData.Data != null)
            {
                logger.LogDebug("Successfully generated marketing image with {Model}", model);
                return firstPart.InlineData.Data.ToByteArray();
            }

            logger.LogWarning("Could not extract image data from {Model} response - no inline data found", model);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate marketing image, continuing without it");
            return null;
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
    public bool AiSucceeded { get; set; } = false;
}

public class MarketingContent
{
    public decimal Price { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string MarketingCopy { get; set; } = string.Empty;
    public string SocialMediaText { get; set; } = string.Empty;
}
