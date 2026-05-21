using SnapMind.Shared;
using Microsoft.AspNetCore.Mvc;

namespace SnapMind.AIService.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : Controller
    {
        private readonly ILogger<AIController> _logger;
        private readonly OllamaClient _ollamaClient;

        public AIController(
            ILogger<AIController> logger,
            OllamaClient ollamaClient)
        {
            _logger = logger;
            _ollamaClient = ollamaClient;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate(
            [FromBody] GenerateRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Generation request received for model {Model}",
                    request.Model);

                string? response;

                if (!string.IsNullOrWhiteSpace(request.ImageBase64))
                {
                    response = await _ollamaClient.ProcessImageAsync(
                        request.Model,
                        request.Prompt,
                        request.ImageBase64);
                }
                else
                {
                    response = await _ollamaClient.GenerateTextAsync(
                        request.Model,
                        request.Prompt);
                }

                return Ok(new GenerateResponse(response ?? ""));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI generation failed");

                return StatusCode(500, new
                {
                    Error = ex.Message
                });
            }
        }
    }
}
