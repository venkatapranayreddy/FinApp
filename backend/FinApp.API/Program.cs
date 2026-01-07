using FinApp.API.Data;
using FinApp.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
              .AllowAnyMethod();
    });
});

// Register services
builder.Services.AddSingleton<SupabaseContext>();
builder.Services.AddSingleton<IMassiveApiService, MassiveApiService>();
builder.Services.AddScoped<IStockService, StockService>();

var app = builder.Build();

// Initialize Supabase
var supabase = app.Services.GetRequiredService<SupabaseContext>();
await supabase.InitializeAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
