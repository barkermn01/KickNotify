namespace KickDesktopNotifications.Core
{
    internal class StreamerState
    {
        public string Slug { get; set; }
        public bool IsLive { get; set; }
        public bool NotificationSent { get; set; }
        public DateTime LastChecked { get; set; } = DateTime.MinValue;
        public DateTime LastSeenLive { get; set; } = DateTime.MinValue;

        private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LiveCooldown = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Returns true if we have recent successful API data (checked within the last 5 minutes).
        /// </summary>
        public bool HasFreshData => (DateTime.UtcNow - LastChecked) < StalenessThreshold;

        /// <summary>
        /// Returns true if the streamer was known-live within the last 5 minutes.
        /// Used to suppress re-notifications from transient API drops.
        /// </summary>
        public bool WasRecentlyLive => (DateTime.UtcNow - LastSeenLive) < LiveCooldown;

        /// <summary>
        /// Call when the API successfully returns data for this streamer.
        /// Returns true if a notification should be sent.
        /// </summary>
        public bool UpdateFromApiResult(bool isLive)
        {
            bool shouldNotify = false;
            bool hadFreshData = HasFreshData;

            LastChecked = DateTime.UtcNow;

            if (isLive)
            {
                LastSeenLive = DateTime.UtcNow;

                if (!IsLive && !NotificationSent)
                {
                    // Streamer just went live (or first time seeing them live)
                    if (hadFreshData)
                    {
                        // We had good data continuity, trust this transition
                        shouldNotify = true;
                        NotificationSent = true;
                    }
                    // else: stale data, can't trust this is a real transition, update silently
                }

                IsLive = true;
            }
            else
            {
                // API says offline
                if (IsLive && !WasRecentlyLive)
                {
                    // They've been offline long enough, reset state
                    IsLive = false;
                    NotificationSent = false;
                }
                // If WasRecentlyLive, keep state as-is (cooldown for brief stream drops)
            }

            return shouldNotify;
        }
    }
}
