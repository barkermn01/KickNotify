using System.Text.Json.Serialization;

namespace KickDesktopNotifications.JsonStructure.Kick
{
    public class KickUser
    {
        [JsonPropertyName("broadcaster_user_id")]
        public int BroadcasterUserId { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("banner_picture")]
        public string BannerPicture { get; set; }

        [JsonPropertyName("channel_description")]
        public string ChannelDescription { get; set; }
    }
}
