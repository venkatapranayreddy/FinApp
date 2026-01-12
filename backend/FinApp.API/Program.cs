using FinApp.API.Data;
using FinApp.API.Services;
using FinApp.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add SignalR
builder.Services.AddSignalR();

// Configure CORS for Angular frontend (localhost + Vercel)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://finapp-client.vercel.app",
                "https://*.vercel.app"
              )
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// Register services
builder.Services.AddSingleton<SupabaseContext>();
builder.Services.AddSingleton<IMassiveApiService, MassiveApiService>();
builder.Services.AddSingleton<IFinnhubWebSocketService, FinnhubWebSocketService>();
builder.Services.AddSingleton<IOptionsActivityService, OptionsActivityService>();
builder.Services.AddScoped<IStockService, StockService>();

var app = builder.Build();

// Initialize Supabase
var supabase = app.Services.GetRequiredService<SupabaseContext>();
await supabase.InitializeAsync();

// Initialize Finnhub WebSocket connection
var finnhubService = app.Services.GetRequiredService<IFinnhubWebSocketService>();
_ = Task.Run(async () =>
{
    try
    {
        await finnhubService.ConnectAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to connect to Finnhub WebSocket on startup");
    }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.MapControllers();

// Map SignalR hub
app.MapHub<MarketDataHub>("/hubs/marketdata");

app.Run();
