using System;
using System.Windows;
using System.Windows.Input;

namespace ARADAutoLauncher {
    /// <summary>
    /// OneTimePassWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class OneTimePassWindow :Window 
    {

        public string otp;
        public OneTimePassWindow() 
        {
            InitializeComponent();
        }

        private void Button_Click( object sender, RoutedEventArgs e ) 
        {
            otp  = textBox_Otp.Text;
            Close();
        }
        private void ButtonExit_Click( object sender, RoutedEventArgs e ) 
        {
            Close();
        }

        private void Window_MouseLeftButtonDown( object sender, MouseButtonEventArgs e ) 
        {
            this.DragMove();
        }

        private void textBox_Otp_KeyDown( object sender, KeyEventArgs e ) 
        {
            if( e.Key == Key.Return )
            {
                otp = textBox_Otp.Text;
                Close();
            }
        }

    }
}
