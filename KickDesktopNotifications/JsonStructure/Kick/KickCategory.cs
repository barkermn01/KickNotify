using System.Text.Json.Serialization;

namespace KickDesktopNotifications.JsonStructure.Kick
{
    public class KickCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }
    }
}
