using System.Text.Json.Serialization;

namespace KickDesktopNotifications.JsonStructure.Kick
{
    public class KickChannelsResponse
    {
        [JsonPropertyName("data")]
        public List<KickChannel> Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
