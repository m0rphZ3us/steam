using System.Text;
using System.Text.Json;

namespace SteamWatcher.Infrastructure;

public static class StatusClient
{
    public static async Task SendAsync(string url, string state, string appId)
    {
        try
        {
            var payload = new
            {
                state,
                game = "",
                appId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                Logger.Error($"event=status_http_failed state={state} status={response.StatusCode}");
            else
                Logger.Info($"event=status_sent state={state} appId={appId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"event=status_failed error=\"{ex.Message}\"");
        }
    }
}