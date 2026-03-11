using System.Text.Json.Serialization;

namespace KickDesktopNotifications.JsonStructure.Kick
{
    public class KickStream
    {
        [JsonPropertyName("custom_tags")]
        public List<string> CustomTags { get; set; }

        [JsonPropertyName("is_live")]
        public bool IsLive { get; set; }

        [JsonPropertyName("is_mature")]
        public bool IsMature { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("viewer_count")]
        public int ViewerCount { get; set; }
    }
}
