using GuliERP.Foundation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddGuliErpFoundation();

var app = builder.Build();

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new
{
    service = "GuliERP.Next.Api",
    gate = "GULIERP_GREENFIELD_BOOTSTRAPPED",
    foundation = "boundary-only"
}));

app.Run();

public partial class Program;

