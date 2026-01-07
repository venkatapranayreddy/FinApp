using Supabase;

namespace FinApp.API.Data;

public class SupabaseContext
{
    private readonly Client _client;

    public SupabaseContext(IConfiguration configuration)
    {
        // Check environment variables first, then fall back to config
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? configuration["Supabase:Url"]
            ?? throw new ArgumentNullException("Supabase:Url configuration is missing");
        var key = Environment.GetEnvironmentVariable("SUPABASE_KEY")
            ?? configuration["Supabase:Key"]
            ?? throw new ArgumentNullException("Supabase:Key configuration is missing");

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _client = new Client(url, key, options);
    }

    public async Task InitializeAsync()
    {
        await _client.InitializeAsync();
    }

    public Client Client => _client;
}
