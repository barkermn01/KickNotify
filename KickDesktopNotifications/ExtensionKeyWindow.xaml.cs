using System.Windows;
using KickDesktopNotifications.Core;

namespace KickDesktopNotifications
{
    public partial class ExtensionKeyWindow : Window
    {
        public ExtensionKeyWindow()
        {
            InitializeComponent();
            txtKey.Text = ExtensionServer.GetOrCreateKey();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(txtKey.Text);
        }

        private void RegenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Regenerating the key will disconnect any currently linked browser extension. Continue?",
                "Regenerate Key",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                txtKey.Text = ExtensionServer.RegenerateKey();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
