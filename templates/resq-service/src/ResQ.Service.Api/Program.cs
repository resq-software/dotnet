using ResQ.BuildingBlocks.Adapters.Web;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.ServiceDefaults;
using ResQ.Service.Application;
using ResQ.Service.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// AddServiceDefaults wires OTel / health / resilience / IClock — but deliberately NOT the pipeline
// behaviors, so the host keeps full control of behavior order below.
builder.AddServiceDefaults();

builder.Services
    .AddResqApplication(typeof(CreateSampleCommand).Assembly)
    .AddValidatorsFrom(typeof(CreateSampleCommand).Assembly, typeof(Program).Assembly)
    .AddLoggingPipeline()            // outermost
    .AddValidationPipeline();

// Tracing then Metrics — registered exactly once, AFTER validation and BEFORE the persistence
// transaction behavior (which AddSampleInfrastructure registers last = innermost).
builder.Services.AddResqObservabilityBehaviors();

builder.Services.AddResqWeb(builder.Configuration);
builder.Services.AddSampleInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseResqWeb();
app.MapDefaultEndpoints();
app.Run();

// Resulting behavior order: Logging → Validation → Tracing → Metrics → Transaction → handler.

/// <summary>Exposes the top-level entry-point class so WebApplicationFactory can bootstrap the host in tests.</summary>
public partial class Program;
