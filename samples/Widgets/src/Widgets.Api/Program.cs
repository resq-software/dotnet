using ResQ.BuildingBlocks.Adapters.Web;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.ServiceDefaults;
using Widgets.Application;
using Widgets.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// AddServiceDefaults wires OTel / health / resilience / IClock — but deliberately NOT the pipeline
// behaviors, so the host keeps full control of behavior order below.
builder.AddServiceDefaults();

builder.Services
    .AddResqApplication(typeof(CreateWidgetCommand).Assembly)
    .AddValidatorsFrom(typeof(CreateWidgetCommand).Assembly, typeof(Program).Assembly)
    .AddLoggingPipeline()            // outermost
    .AddValidationPipeline();

// Tracing then Metrics — registered exactly once, AFTER validation and BEFORE the persistence
// transaction behavior (which AddWidgetsInfrastructure registers last = innermost).
builder.Services.AddResqObservabilityBehaviors();

// Pass this API's assembly so endpoint discovery works under WebApplicationFactory (integration tests),
// where Assembly.GetEntryAssembly() would otherwise resolve to the test runner.
builder.Services.AddResqWeb(builder.Configuration, endpointAssemblies: typeof(Program).Assembly);
builder.Services.AddWidgetsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseResqWeb();
app.MapDefaultEndpoints();
app.Run();

// Resulting behavior order: Logging → Validation → Tracing → Metrics → Transaction → handler.

/// <summary>Exposes the top-level entry-point class so WebApplicationFactory can bootstrap the host in tests.</summary>
public partial class Program;
