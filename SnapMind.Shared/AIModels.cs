namespace SnapMind.Shared
{
    public record GenerateRequest(
        string Model,
        string Prompt,
        string? ImageBase64 = null
    );

    public record GenerateResponse(
        string Response
    );
}
