using System.Text.Json.Serialization;

namespace SnapMind.AIService
{
    public class OllamaClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<OllamaClient> _logger;

        public OllamaClient(HttpClient http, ILogger<OllamaClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ──────────────────────────── public API ────────────────────────────

        public Task<string?> GenerateTextAsync(string model, string prompt)
            => SendChatAsync(model, prompt, images: null);

        public Task<string?> ProcessImageAsync(string model, string prompt, string base64Image)
            => SendChatAsync(model, prompt, images: [base64Image]);

        public Task<string?> ProcessImagesAsync(string model, string prompt, IEnumerable<string> base64Images)
            => SendChatAsync(model, prompt, images: base64Images.ToList());

        // ──────────────────────────── internals ─────────────────────────────

        private async Task<string?> SendChatAsync(
            string model,
            string prompt,
            List<string>? images)
        {
            await EnsureModelReadyAsync(model);

            var request = BuildChatRequest(model, prompt, images);
            var response = await PostJsonAsync<OllamaChatRequest, OllamaChatResponse>(
                "/api/chat", request);

            return response?.Message?.Content;
        }

        private static OllamaChatRequest BuildChatRequest(
            string model,
            string prompt,
            List<string>? images)
        {
            var message = new OllamaMessage(
                Role: "user",
                Content: prompt,
                Images: images
            );

            return new OllamaChatRequest(
                Model: model,
                Messages: [message],
                Stream: false,
                Options: new OllamaOptions(Temperature: 0.2f),
                Think: false
            );
        }

        private async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
            string url,
            TRequest request)
        {
            var response = await _http.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        private async Task EnsureModelReadyAsync(string model)
        {
            const int maxAttempts = 10;
            const int delayMinutes = 2;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var tags = await _http.GetStringAsync("/api/tags");

                if (tags.Contains(model))
                    return;

                _logger.LogInformation(
                    "Model {Model} not ready. Attempt {Attempt}/{Max}. Retrying in {Delay} min...",
                    model, attempt + 1, maxAttempts, delayMinutes);

                await Task.Delay(TimeSpan.FromMinutes(delayMinutes));
            }

            throw new InvalidOperationException(
                $"Model '{model}' did not become available after {maxAttempts} attempts.");
        }
    }
}

// ──────────────────────────── DTOs ────────────────────────────

public record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("images")] List<string>? Images = null
);

public record OllamaChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<OllamaMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("options")] OllamaOptions? Options = null,
    [property: JsonPropertyName("think")] bool Think = false
);

public record OllamaOptions(
    [property: JsonPropertyName("temperature")] float Temperature
);

public record OllamaChatResponse(
    [property: JsonPropertyName("message")] OllamaMessage? Message
);
