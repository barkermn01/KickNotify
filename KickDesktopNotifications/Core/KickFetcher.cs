﻿﻿using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using KickDesktopNotifications.JsonStructure;
using KickDesktopNotifications.JsonStructure.Kick;

namespace KickDesktopNotifications.Core
{
    internal class KickerRefreshException : Exception
    {
        public KickerRefreshException(string? message, Exception? innerException) : base(message, innerException) { 
        }
    }
    internal class KickFetcher
    {
        private KickFetcher() {
        }

        ReconnectionNeeded rnFrm;
        private string codeVerifier;

        public void OpenFailedNotification()
        {
            if (rnFrm == null)
            {
                rnFrm = new ReconnectionNeeded();
            }
            if (rnFrm.IsActive)
            {
                rnFrm.Show();
            }
        }

        public static KickFetcher instance { get; private set; }

        Dictionary<string, StreamerState> streamerStates = new Dictionary<string, StreamerState>();

        public string guid { get; private set; }

        public static KickFetcher GetInstance()
        {
            if(instance == null)
            {
                instance = new KickFetcher();
            }
            return instance;
        }

        private byte[] buildPostData(Dictionary<string, string> postData)
        {
            string content = "";
            foreach (var pair in postData)
            {
                content += HttpUtility.UrlEncode(pair.Key) + "=" + HttpUtility.UrlEncode(pair.Value) + "&";
            }
            content = content.TrimEnd('&');
            return Encoding.UTF8.GetBytes(content);
        }

        private T MakeRequest<T>(string endpoint)
        {
            if (DataStore.GetInstance().Store == null || DataStore.GetInstance().Store.Authentication == null)
            {
                throw new Exception("Not Authenticated");
            }
            if (DataStore.GetInstance().Store.Authentication.ExpiresAsDate <= DateTime.UtcNow)
            {
                Refresh();
            }

            try
            {
                string url = "https://api.kick.com/" + endpoint;
                string token = DataStore.GetInstance().Store.Authentication.AccessToken;
                string authHeader = $"Bearer {token}";
                
                Logger.GetInstance().WriteLine("=== REQUEST DEBUG ===");
                Logger.GetInstance().WriteLine($"URL: {url}");
                Logger.GetInstance().WriteLine($"Method: GET");
                Logger.GetInstance().WriteLine($"Token (full): {token}");
                Logger.GetInstance().WriteLine($"Authorization Header: {authHeader}");
                Logger.GetInstance().WriteLine($"Token Length: {token.Length}");
                Logger.GetInstance().WriteLine($"Token Type: {DataStore.GetInstance().Store.Authentication.TokenType}");
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers[HttpRequestHeader.Authorization] = authHeader;
                request.Accept = "application/json, */*";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36";
                request.Headers["Origin"] = "https://docs.kick.com";
                request.Headers["Referer"] = "https://docs.kick.com/apis/channels";
                request.Headers["Sec-Ch-Ua"] = "\"Not A Brand\";v=\"99\", \"Microsoft Edge\";v=\"145\", \"Chromium\";v=\"145\"";
                request.Headers["Sec-Ch-Ua-Mobile"] = "?0";
                request.Headers["Sec-Ch-Ua-Platform"] = "\"Windows\"";
                request.Headers["Sec-Fetch-Dest"] = "empty";
                request.Headers["Sec-Fetch-Mode"] = "cors";
                request.Headers["Sec-Fetch-Site"] = "same-site";
                
                Logger.GetInstance().WriteLine("=== ALL REQUEST HEADERS ===");
                foreach (string header in request.Headers.AllKeys)
                {
                    Logger.GetInstance().WriteLine($"{header}: {request.Headers[header]}");
                }
                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Logger.GetInstance().WriteLine($"=== RESPONSE ===");
                Logger.GetInstance().WriteLine($"Status Code: {response.StatusCode}");
                Logger.GetInstance().WriteLine($"Status Description: {response.StatusDescription}");
                
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                Logger.GetInstance().WriteLine($"Response Body: {responseFromServer}");
                reader.Close();
                dataStream.Close();
                response.Close();

                return JsonSerializer.Deserialize<T>(responseFromServer);
            }
            catch (WebException ex)
            {
                Logger.GetInstance().WriteLine($"=== WEB EXCEPTION ===");
                Logger.GetInstance().WriteLine($"Message: {ex.Message}");
                Logger.GetInstance().WriteLine($"Status: {ex.Status}");
                if (ex.Response != null)
                {
                    HttpWebResponse errorResponse = (HttpWebResponse)ex.Response;
                    Logger.GetInstance().WriteLine($"HTTP Status Code: {errorResponse.StatusCode}");
                    Logger.GetInstance().WriteLine($"HTTP Status Description: {errorResponse.StatusDescription}");
                    
                    using (Stream errorStream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(errorStream))
                    {
                        string errorBody = reader.ReadToEnd();
                        Logger.GetInstance().WriteLine($"Error Body: {errorBody}");
                    }
                }
                throw;
            }
            catch (KickerRefreshException ex)
            {
                OpenFailedNotification();
            }
            catch(Exception ex)
            {
                Logger.GetInstance().WriteLine($"=== EXCEPTION ===");
                Logger.GetInstance().WriteLine(ex.ToString());
            }
            return default(T);
        }

        public void FetchCurrentUser()
        {
            try
            {
                // Call channels endpoint without parameters to get current authenticated user's channel
                var Response = MakeRequest<KickChannelsResponse>("public/v1/channels");
                if (Response != null && Response.Data != null && Response.Data.Count > 0)
                {
                    var channel = Response.Data[0];
                    DataStore.GetInstance().Store.UserData = new JsonStructure.Kick.KickUser
                    {
                        BroadcasterUserId = channel.BroadcasterUserId,
                        Slug = channel.Slug,
                        BannerPicture = channel.BannerPicture,
                        ChannelDescription = channel.ChannelDescription
                    };
                    DataStore.GetInstance().Save();
                }
            }
            catch (KickerRefreshException ex)
            {
                OpenFailedNotification();
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine(ex.ToString());
            }
        }

        public KickChannel FetchChannelData(int broadcaster_user_id)
        {
            try
            {
                var Response = MakeRequest<KickChannelsResponse>("public/v1/channels?broadcaster_user_id=" + broadcaster_user_id);
                if (Response != null && Response.Data != null && Response.Data.Count > 0)
                {
                    return Response.Data[0];
                }
            }
            catch (KickerRefreshException ex)
            {
                OpenFailedNotification();
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine(ex.ToString());
            }
            return null;
        }

        private string appAccessToken = null;
        private DateTime appTokenExpiry = DateTime.MinValue;

        private string GetAppAccessToken()
        {
            // Check if we have a valid app token
            if (appAccessToken != null && appTokenExpiry > DateTime.UtcNow)
            {
                return appAccessToken;
            }

            try
            {
                Dictionary<string, string> postData = new Dictionary<string, string>();
                postData["grant_type"] = "client_credentials";
                postData["client_id"] = KickDetails.KickClientID;
                postData["client_secret"] = KickDetails.KickClientSecret;

                byte[] byteArray = buildPostData(postData);

                WebRequest request = WebRequest.Create("https://id.kick.com/oauth/token");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                var tokenResponse = JsonSerializer.Deserialize<Authentication>(responseFromServer);
                appAccessToken = tokenResponse.AccessToken;
                appTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresSeconds - 60); // Refresh 1 min early
                
                Logger.GetInstance().WriteLine($"App access token obtained, expires in {tokenResponse.ExpiresSeconds} seconds");
                return appAccessToken;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine($"Error getting app access token: {ex}");
                throw;
            }
        }

        public KickChannel FetchChannelDataBySlug(string slug)
        {
            try
            {
                // Use app access token for public API calls
                Logger.GetInstance().WriteLine($"Attempting to fetch channel: {slug}");
                string appToken = GetAppAccessToken();
                
                string url = $"https://api.kick.com/public/v1/channels?slug={slug}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers[HttpRequestHeader.Authorization] = $"Bearer {appToken}";
                request.Accept = "application/json";
                
                Logger.GetInstance().WriteLine($"Using app access token: {appToken.Substring(0, Math.Min(20, appToken.Length))}...");
                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                var Response = JsonSerializer.Deserialize<KickChannelsResponse>(responseFromServer);
                if (Response != null && Response.Data != null && Response.Data.Count > 0)
                {
                    Logger.GetInstance().WriteLine($"Successfully fetched channel: {Response.Data[0].Slug}");
                    return Response.Data[0];
                }
                else
                {
                    Logger.GetInstance().WriteLine($"Channel {slug} not found - empty or null response");
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine($"Error fetching channel {slug}: {ex.Message}");
            }
            return null;
        }

        public List<KickChannel> GetFollowedChannels()
        {
            try
            {
                Logger.GetInstance().WriteLine("Attempting to fetch followed channels from kick.com...");
                
                // Use kick.com instead of api.kick.com (the live site uses this)
                if (DataStore.GetInstance().Store == null || DataStore.GetInstance().Store.Authentication == null)
                {
                    Logger.GetInstance().WriteLine("Not authenticated, cannot fetch followed channels");
                    return new List<KickChannel>();
                }

                if (DataStore.GetInstance().Store.Authentication.ExpiresAsDate <= DateTime.UtcNow)
                {
                    Refresh();
                }

                WebRequest request = WebRequest.Create("https://kick.com/api/v2/channels/followed");
                request.Method = "GET";
                request.Headers[HttpRequestHeader.Authorization] = String.Format("Bearer {0}", DataStore.GetInstance().Store.Authentication.AccessToken);
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                Logger.GetInstance().WriteLine($"Response from kick.com: {responseFromServer.Substring(0, Math.Min(200, responseFromServer.Length))}...");

                var Response = JsonSerializer.Deserialize<KickChannelsResponse>(responseFromServer);

                if (Response != null && Response.Data != null)
                {
                    Logger.GetInstance().WriteLine($"Successfully fetched {Response.Data.Count} followed channels");
                    return Response.Data;
                }
                else
                {
                    Logger.GetInstance().WriteLine("Response was null or had no data");
                }
            }
            catch (KickerRefreshException ex)
            {
                Logger.GetInstance().WriteLine("KickerRefreshException: " + ex.ToString());
                OpenFailedNotification();
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine("Exception fetching followed channels: " + ex.ToString());
            }
            return new List<KickChannel>();
        }

        public void GetLiveFollowingUsers()
        {
            try
            {
                var streamersToCheck = DataStore.GetInstance().Store.SteamersToIgnore.Streamers;

                if (streamersToCheck == null || streamersToCheck.Count == 0)
                {
                    return;
                }

                foreach (var streamer in streamersToCheck)
                {
                    if (!streamerStates.ContainsKey(streamer.Name))
                    {
                        streamerStates[streamer.Name] = new StreamerState { Slug = streamer.Name };
                    }

                    var state = streamerStates[streamer.Name];

                    try
                    {
                        var channel = FetchChannelDataBySlug(streamer.Name);
                        if (channel == null)
                        {
                            // API call failed, don't touch state
                            continue;
                        }

                        bool isLive = channel.Stream != null && channel.Stream.IsLive;
                        bool shouldNotify = state.UpdateFromApiResult(isLive);

                        if (shouldNotify && NotifyManager.ShouldNotify(channel.Slug))
                        {
                            Notification.GetInstance().sendNotification(
                                channel.Slug,
                                "https://kick.com/" + channel.Slug,
                                channel.BannerPicture,
                                channel.Stream.Thumbnail,
                                channel.StreamTitle
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // API call threw, don't touch state
                        Logger.GetInstance().WriteLine($"Error checking channel {streamer.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine("Error in GetLiveFollowingUsers: " + ex.ToString());
            }
        }

        public void Refresh()
        {
            try
            {
                Dictionary<string, string> postData = new Dictionary<string, string>();

                postData["grant_type"] = "refresh_token";
                postData["client_id"] = KickDetails.KickClientID;
                postData["client_secret"] = KickDetails.KickClientSecret;
                postData["refresh_token"] = DataStore.GetInstance().Store.Authentication.RefreshToken;

                byte[] byteArray = buildPostData(postData);

                WebRequest request = WebRequest.Create("https://id.kick.com/oauth/token");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                DataStore.GetInstance().Store.Authentication = JsonSerializer.Deserialize<Authentication>(responseFromServer);
                DateTime unixStart = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
                DataStore.GetInstance().Store.Authentication.ExpiresAt = (long)Math.Floor((DateTime.Now.AddSeconds(DataStore.GetInstance().Store.Authentication.ExpiresSeconds) - unixStart).TotalMilliseconds);
                DataStore.GetInstance().Save();
            }catch(Exception e)
            {
                throw new KickerRefreshException("Unable to refresh", e);
            }
        }

        private string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(challengeBytes)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
            }
        }

        async public void BeginConnection()
        {
            guid = Guid.NewGuid().ToString();
            codeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(codeVerifier);
            
            WebServer.GetInstance().KickState = guid;
            Process myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.FileName = String.Format("https://id.kick.com/oauth/authorize?response_type=code&client_id={0}&redirect_uri=http://localhost:32584/kickRedirect&scope=user:read%20channel:read&code_challenge={1}&code_challenge_method=S256&state={2}", 
                KickDetails.KickClientID, 
                codeChallenge, 
                guid);
            myProcess.Start();
        }

        public string endConnection(string code)
        {
            try
            {
                Dictionary<string, string> postData = new Dictionary<string, string>();

                postData["grant_type"] = "authorization_code";
                postData["client_id"] = KickDetails.KickClientID;
                postData["client_secret"] = KickDetails.KickClientSecret;
                postData["redirect_uri"] = "http://localhost:32584/kickRedirect";
                postData["code_verifier"] = codeVerifier;
                postData["code"] = code;

                byte[] byteArray = buildPostData(postData);

                WebRequest request = WebRequest.Create("https://id.kick.com/oauth/token");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                
                Logger.GetInstance().WriteLine("OAuth token exchange successful");
                return responseFromServer;
            }
            catch (WebException ex)
            {
                Logger.GetInstance().WriteLine("OAuth token exchange failed: " + ex.ToString());
                if (ex.Response != null)
                {
                    using (Stream errorStream = ex.Response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(errorStream))
                    {
                        string errorResponse = reader.ReadToEnd();
                        Logger.GetInstance().WriteLine("Error response: " + errorResponse);
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine("Unexpected error in endConnection: " + ex.ToString());
                throw;
            }
        }
    }
}
