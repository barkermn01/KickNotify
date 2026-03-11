using System.Text.Json.Serialization;

namespace KickDesktopNotifications.JsonStructure.Kick
{
    public class KickChannel
    {
        [JsonPropertyName("active_subscribers_count")]
        public int ActiveSubscribersCount { get; set; }

        [JsonPropertyName("banner_picture")]
        public string BannerPicture { get; set; }

        [JsonPropertyName("broadcaster_user_id")]
        public int BroadcasterUserId { get; set; }

        [JsonPropertyName("canceled_subscribers_count")]
        public int CanceledSubscribersCount { get; set; }

        [JsonPropertyName("category")]
        public KickCategory Category { get; set; }

        [JsonPropertyName("channel_description")]
        public string ChannelDescription { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("stream")]
        public KickStream Stream { get; set; }

        [JsonPropertyName("stream_title")]
        public string StreamTitle { get; set; }
    }
}
