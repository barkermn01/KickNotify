using System.Net;
using System.Text;
using System.Web;
using System.IO;

namespace KickDesktopNotifications.Core
{
    public class WebServer : SingletonFactory<WebServer>, Singleton
    {
        public int Port = 32584;

        private HttpListener listener;

        public String KickCode { get; private set; }
        public String KickState { get; set; }

        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + Port.ToString() + "/");
            listener.Prefixes.Add("http://localhost:" + Port.ToString() + "/");
            listener.Start();
            new Thread(new ThreadStart(ThreadManagedServer)).Start();
        }

        public event EventHandler CodeRecived;

        public void Stop()
        {
            listener.Stop();
        }

        private void RespondConnection(HttpListenerRequest request, HttpListenerResponse response)
        {
            var query = HttpUtility.ParseQueryString(request.Url.Query);
            
            if (request.HttpMethod == "GET" && query["state"] == this.KickState)
            {
                this.KickCode = query["code"]; 
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/html";
                response.OutputStream.Write(Encoding.ASCII.GetBytes("<!DOCTYPE html><html><head><title>Kick Connected!</title><style>body{font-family:Arial,sans-serif;background:#1a1a1a;color:#fff;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;}p.title{font-size:24px;font-weight:bold;margin:0 0 15px 0;color:#bf94ff;}.container{width:320px;border:2px solid #bf94ff;padding:30px;border-radius:10px;background:#2a2a2a;text-align:center;box-shadow:0 4px 6px rgba(0,0,0,0.3);}p.msg{font-size:16px;margin:0;line-height:1.5;}.success{color:#53fc18;font-size:48px;margin-bottom:15px;}</style></head><body><div class=\"container\"><div class=\"success\">✓</div><p class=\"title\">Kick Desktop Notification</p><p class=\"msg\">Successfully connected!<br><br>You can now close this tab.</p></div></body></html>"));
                response.OutputStream.Close();
                CodeRecived?.Invoke(this, new EventArgs());
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.ContentType = "text/html";
                response.OutputStream.Write(Encoding.ASCII.GetBytes("<!DOCTYPE html><html><head><title>State Missmatch</title></head><body><h1>State Missmatch</h1><p>State does not match up preventing XSS.</p></body></html>"));
                response.OutputStream.Close();
            }
        }

        private void RespondFavicon(HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "image/x-icon";
            response.OutputStream.Write(File.ReadAllBytes("Assets/icon.ico"));
            response.OutputStream.Close();
        }

        private void processRequestThread(object? obj)
        {
            HttpListenerContext context = (HttpListenerContext)obj;
            HttpListenerRequest request = context.Request;

            if (request.Url.AbsolutePath == "/favicon.ico")
            {
                RespondFavicon(context.Response);
            }
            else if (request.Url.AbsolutePath == "/kickRedirect")
            {
                RespondConnection(request, context.Response);
            }
            else
            {
                HttpListenerResponse response = context.Response;
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.ContentType = "text/html";
                response.OutputStream.Write(Encoding.ASCII.GetBytes("<!DOCTYPE html><html><head><title>Not Found</title></head><body><h1>Not Found</h1><p>File not found</p></body></html>"));
                response.OutputStream.Close();
            }
        }

        private void ThreadManagedServer()
        {
            while (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ParameterizedThreadStart pts = new ParameterizedThreadStart(processRequestThread);
                    pts.Invoke(context);
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
