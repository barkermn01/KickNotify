using System.Text.Json.Serialization;
using KickDesktopNotifications.Core;

namespace KickDesktopNotifications.JsonStructure
{
    public class SteamersToIgnore
    {
        [JsonPropertyName("IgnoredStreamers")]
        public List<UIStreamer> Streamers { get; set; } = new List<UIStreamer>();
    }
}
