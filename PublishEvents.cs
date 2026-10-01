using System.Text;
using System.Text.Json;


namespace MatchZy
{
    public partial class MatchZy
    {
        // One client for all events (a new HttpClient per event can exhaust sockets on a busy server). Headers are set per
        // request because each match can have its own remote log URL and header.
        private static readonly HttpClient eventHttpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

        // target: where to send the event. Pass CurrentRemoteLogTarget() captured before a match reset, so the event still goes to
        // the match's URL; by default the current match's settings are used.
        public async Task SendEventAsync(MatchZyEvent @event, RemoteLogTarget? target = null)
        {
            try
            {
                target ??= CurrentRemoteLogTarget();
                if (string.IsNullOrEmpty(target.Url)) return;

                string jsonString = JsonSerializer.Serialize(@event, @event.GetType());
                using var request = new HttpRequestMessage(HttpMethod.Post, target.Url)
                {
                    Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(target.HeaderKey) && !string.IsNullOrEmpty(target.HeaderValue))
                {
                    request.Headers.TryAddWithoutValidation(target.HeaderKey, target.HeaderValue);
                }

                using var httpResponseMessage = await eventHttpClient.SendAsync(request);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    Log($"[SendEventAsync] Sent {@event.EventName} to {MatchZySecurity.RedactUrl(target.Url)} ({(int)httpResponseMessage.StatusCode})");
                }
                else
                {
                    // The payload is only logged when sending fails, to help find what the panel rejected.
                    Log($"[SendEventAsync] Sending {@event.EventName} to {MatchZySecurity.RedactUrl(target.Url)} failed with status code: {httpResponseMessage.StatusCode}, ResponseContent: {await httpResponseMessage.Content.ReadAsStringAsync()}, Data: {jsonString}");
                }
            }
            catch (Exception e)
            {
                Log($"[SendEventAsync FATAL] Sending {@event.EventName} failed: {e.Message}");
            }
        }
    }
}
