using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using KickDesktopNotifications.Core;

namespace KickDesktopNotifications
{
    /// <summary>
    /// Interaction logic for ManageIgnores.xaml
    /// </summary>
    public partial class ManageIgnores : Window
    {
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_PASSIVE_UPDATE_MODE,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR,
            DWMWA_CAPTION_COLOR,
            DWMWA_TEXT_COLOR,
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
            DWMWA_SYSTEMBACKDROP_TYPE,
            DWMWA_LAST
        }

        // The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function  
        // what value of the enum to set.  
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.  
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(IntPtr hwnd,
            DWMWINDOWATTRIBUTE attribute,
            ref uint pvAttribute,
            uint cbAttribute);

        public ManageIgnores()
        {
            InitializeComponent(); 
            
            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            uint preference = 2;
            DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));

            var attribute2 = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;
            uint colour = 1;
            DwmSetWindowAttribute(hWnd, attribute2, ref colour, sizeof(uint));

            // Fetch followed streamers when window opens
            LoadFollowedStreamers();
        }

        private async void LoadFollowedStreamers()
        {
            try
            {
                // Only fetch if authenticated
                if (DataStore.GetInstance().Store?.Authentication == null)
                {
                    Logger.GetInstance().WriteLine("Skipping followed streamers fetch - not authenticated");
                    return;
                }

                // Fetch followed channels from Kick API
                var followedChannels = await Task.Run(() => KickFetcher.GetInstance().GetFollowedChannels());

                if (followedChannels != null && followedChannels.Count > 0)
                {
                    // Add any new streamers to the list
                    foreach (var channel in followedChannels)
                    {
                        // This will create the streamer if it doesn't exist, or return existing one
                        UIStreamer.GetCreateStreamer(channel.Slug);
                    }

                    // Refresh the DataGrid
                    dgrdIgnore.Items.Refresh();
                    
                    Logger.GetInstance().WriteLine($"Loaded {followedChannels.Count} followed streamers");
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine("Error loading followed streamers: " + ex.ToString());
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private async void AddChannelBtn_Click(object sender, RoutedEventArgs e)
        {
            await ShowAddChannelDialog("");
        }

        private async Task ShowAddChannelDialog(string initialValue)
        {
            // Kick brand colors: #53FC18 (green), #0F0F0F (dark bg), #1A1A1A (lighter bg)
            var inputDialog = new Window
            {
                Title = "Add Channel",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F0F")),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F0F")),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8)
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            var titleText = new TextBlock
            {
                Text = "Enter Channel Name",
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(titleText, 0);

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = initialValue,
                Padding = new Thickness(10),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A")),
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18")),
                BorderThickness = new Thickness(1),
                CaretBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18"))
            };
            Grid.SetRow(textBox, 1);

            // Handle Enter key
            textBox.KeyDown += (s, args) =>
            {
                if (args.Key == System.Windows.Input.Key.Enter)
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        System.Windows.MessageBox.Show("Please enter a channel name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        inputDialog.DialogResult = true;
                        inputDialog.Close();
                    }
                }
            };

            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            var addButton = new System.Windows.Controls.Button
            {
                Content = "Add",
                Width = 80,
                Height = 35,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18")),
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F0F")),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            addButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    System.Windows.MessageBox.Show("Please enter a channel name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    inputDialog.DialogResult = true;
                }
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 35,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A")),
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, args) => inputDialog.DialogResult = false;

            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(titleText);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            mainBorder.Child = grid;
            inputDialog.Content = mainBorder;

            // Auto-focus the textbox when dialog opens
            inputDialog.Loaded += (s, args) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            if (inputDialog.ShowDialog() == true)
            {
                string channelSlug = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(channelSlug))
                {
                    try
                    {
                        // Fetch channel details
                        var channel = await Task.Run(() => KickFetcher.GetInstance().FetchChannelDataBySlug(channelSlug));
                        
                        if (channel != null)
                        {
                            // Add to the list
                            UIStreamer.GetCreateStreamer(channel.Slug);
                            dgrdIgnore.Items.Refresh();
                            Logger.GetInstance().WriteLine($"Added channel: {channel.Slug}");
                        }
                        else
                        {
                            // Show custom retry dialog
                            await ShowChannelNotFoundDialog(channelSlug);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.GetInstance().WriteLine($"Error adding channel: {ex}");
                        await ShowChannelNotFoundDialog(channelSlug);
                    }
                }
            }
        }

        private async Task ShowChannelNotFoundDialog(string channelName)
        {
            var errorDialog = new Window
            {
                Title = "Channel Not Found",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F0F")),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F0F")),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8)
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            var titleText = new TextBlock
            {
                Text = "Channel Not Found",
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4444")),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(titleText, 0);

            var messageText = new TextBlock
            {
                Text = $"Could not find channel '{channelName}'.\nPlease check the spelling and try again.",
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(messageText, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            var retryButton = new System.Windows.Controls.Button
            {
                Content = "Check & Retry",
                Width = 120,
                Height = 35,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18")),
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F0F0F")),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            retryButton.Click += (s, args) => errorDialog.DialogResult = true;

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 35,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A")),
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#53FC18")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, args) => errorDialog.DialogResult = false;

            buttonPanel.Children.Add(retryButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(titleText);
            grid.Children.Add(messageText);
            grid.Children.Add(buttonPanel);
            mainBorder.Child = grid;
            errorDialog.Content = mainBorder;

            if (errorDialog.ShowDialog() == true)
            {
                // Retry - reopen the add channel dialog with the previous value
                await ShowAddChannelDialog(channelName);
            }
        }

        private void HyperLink_Click(object sender, RoutedEventArgs e)
        {
            string link = ((Hyperlink)e.OriginalSource).NavigateUri.OriginalString;

            var psi = new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
