using SnapMind.AIService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddHttpClient<OllamaClient>(client =>
{
    client.BaseAddress = new Uri("http://ollama");
    client.Timeout = Timeout.InfiniteTimeSpan;
})
.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);

        options.Retry.MaxRetryAttempts = 2;

        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
        options.CircuitBreaker.MinimumThroughput = 2;
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
