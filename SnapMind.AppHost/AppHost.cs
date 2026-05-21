var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithHttpEndpoint(targetPort: 11434)
    .WithVolume("ollama-data", "/root/.ollama")
    .WithArgs("serve")
    .WithContainerRuntimeArgs("--gpus", "all");

builder.AddContainer("ollama-init", "curlimages/curl")
    .WithArgs("sh", "-c",
        "sleep 10 && curl http://ollama:11434/api/pull -d '{\"name\":\"qwen3.5:4b\"}'")
    .WaitFor(ollama);

builder.AddProject<Projects.SnapMind_AIService>("snapmind-aiservice")
    .WithReference(ollama.GetEndpoint("http"))
    .WaitFor(ollama);


builder.Build().Run();
