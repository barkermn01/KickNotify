// See https://aka.ms/new-console-template for more information
using Microsoft.Toolkit.Uwp.Notifications;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using KickDesktopNotifications;
using KickDesktopNotifications.Core;
using KickDesktopNotifications.JsonStructure;
using Windows.UI.Core.Preview;

// Custom renderer for Kick-styled context menu
internal class KickMenuRenderer : ToolStripProfessionalRenderer
{
    public KickMenuRenderer() : base(new KickColorTable()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected)
        {
            // Kick green highlight: #53FC18
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(83, 252, 24)))
            {
                e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }
            // Change text color to dark when highlighted
            e.Item.ForeColor = System.Drawing.Color.FromArgb(15, 15, 15);
        }
        else
        {
            // Dark background
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(15, 15, 15)))
            {
                e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }
            e.Item.ForeColor = System.Drawing.Color.White;
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        // Kick green separator
        using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(83, 252, 24), 1))
        {
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(pen, 5, y, e.Item.Width - 5, y);
        }
    }
}

internal class KickColorTable : ProfessionalColorTable
{
    public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(83, 252, 24); // #53FC18
    public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(83, 252, 24);
    public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(83, 252, 24);
    public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(83, 252, 24);
    public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(83, 252, 24);
    public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(107, 255, 61);
    public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(107, 255, 61);
}

internal class Program
{

    static bool isConnecting = false;
    static WebServer ws = WebServer.GetInstance();

    private static NotifyIcon notifyIcon;
    private static ContextMenuStrip cms;
    private static ManageIgnores? manageIgnores;
    private static ExtensionKeyWindow? extensionKeyWindow;

    public static void Ws_CodeRecived(object? sender, EventArgs e)
    {
        try
        {
            ws.CodeRecived -= Ws_CodeRecived;

            Logger.GetInstance().WriteLine("OAuth callback received, exchanging code for token...");
            
            string response = KickFetcher.GetInstance().endConnection(((WebServer)sender).KickCode);

            if (!DataStore.GetInstance().isLoaded)
            {
                DataStore.GetInstance().Load();
            }

            DataStore.GetInstance().Store.Authentication = JsonSerializer.Deserialize<Authentication>(response);

            DateTime unixStart = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            DataStore.GetInstance().Store.Authentication.ExpiresAt = (long)Math.Floor((DateTime.Now.AddSeconds(DataStore.GetInstance().Store.Authentication.ExpiresSeconds) - unixStart).TotalMilliseconds);
            DataStore.GetInstance().Save();

            Logger.GetInstance().WriteLine("Authentication saved successfully");
            isConnecting = false;
            ws.Stop();
        }
        catch (Exception ex)
        {
            Logger.GetInstance().WriteLine("Error in Ws_CodeRecived: " + ex.ToString());
            isConnecting = false;
            ws.Stop();
            KickFetcher.GetInstance().OpenFailedNotification();
        }
    }

    protected static void Reconnect_Click(object? sender, System.EventArgs e)
    {
        TriggerAuthentication();
    }

    protected static void ManageIgnores_Click(object? sender, System.EventArgs e)
    {
        if (manageIgnores == null) {
            manageIgnores = new ManageIgnores();
            manageIgnores.Closed += ManageIgnores_Closed;
        }
        manageIgnores.Show();
        manageIgnores.Focus();
    }

    private static void ManageIgnores_Closed(object? sender, EventArgs e)
    {
        manageIgnores = null;
    }

    protected static void ExtensionKey_Click(object? sender, System.EventArgs e)
    {
        if (extensionKeyWindow == null)
        {
            extensionKeyWindow = new ExtensionKeyWindow();
            extensionKeyWindow.Closed += (s, args) => extensionKeyWindow = null;
        }
        extensionKeyWindow.Show();
        extensionKeyWindow.Focus();
    }

    protected static void Quit_Click(object? sender, System.EventArgs e)
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        Environment.Exit(0);
    }

    private async static void TriggerAuthentication()
    {
        ws.CodeRecived += Ws_CodeRecived;
        ws.Start();
        isConnecting = true;
        KickFetcher.GetInstance().BeginConnection();
        if (DataStore.GetInstance().Store.Authentication == null)
        {
            if (isConnecting)
            {
                KickFetcher.GetInstance().OpenFailedNotification();
            }
        }
    }
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon("Assets/icon.ico");
            notifyIcon.Text = "Kick Notify";

            cms = new ContextMenuStrip();
            // Kick brand colors: #0F0F0F (dark bg), #53FC18 (green)
            cms.BackColor = System.Drawing.Color.FromArgb(15, 15, 15); // #0F0F0F
            cms.ForeColor = System.Drawing.Color.White;
            cms.ShowImageMargin = false;
            cms.Renderer = new KickMenuRenderer();
            
            var manageStreamsItem = new ToolStripMenuItem("Manage Streams", null, new EventHandler(ManageIgnores_Click));
            manageStreamsItem.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            cms.Items.Add(manageStreamsItem);
            
            cms.Items.Add(new ToolStripSeparator());
            
            var reconnectItem = new ToolStripMenuItem("Reconnect", null, new EventHandler(Reconnect_Click));
            reconnectItem.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            cms.Items.Add(reconnectItem);
            
            cms.Items.Add(new ToolStripSeparator());

            var extensionKeyItem = new ToolStripMenuItem("Browser Extension", null, new EventHandler(ExtensionKey_Click));
            extensionKeyItem.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            cms.Items.Add(extensionKeyItem);
            
            cms.Items.Add(new ToolStripSeparator());
            
            var quitItem = new ToolStripMenuItem("Quit", null, new EventHandler(Quit_Click), "Quit");
            quitItem.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            cms.Items.Add(quitItem);

            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.Visible = true;

            if (DataStore.GetInstance().Store.Authentication == null)
            {
                TriggerAuthentication();
            }

            // Start the WebSocket server for browser extension communication
            ExtensionServer.GetInstance().Start();

            var autoEvent = new AutoResetEvent(false);
            var timer = new System.Threading.Timer((Object? stateInfo) => {
                if (DataStore.GetInstance().Store != null)
                {
                    KickFetcher.GetInstance().GetLiveFollowingUsers();
                }
            }, autoEvent, 1000, 60000);
            

            Application.Run();

            Application.ApplicationExit += (object? sender, EventArgs e) => {
                ToastNotificationManagerCompat.Uninstall();
            };
        }
        catch (Exception e) {
            Logger.GetInstance().WriteLine(e.ToString());
        }

    }
}