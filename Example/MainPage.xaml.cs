using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Example
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            VM.PropertyChanged += vm_PropertyChanged;
            webViewMain.Navigate(new Uri("about:blank"));
            webViewMain.DefaultBackgroundColor = Windows.UI.Colors.Transparent;
            base.OnNavigatedTo(e);
        }

        void vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(String.Compare(e.PropertyName, "AuthUri", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                webViewMain.Navigate(new Uri(VM.AuthURI));
            }
        }

        private void Authorize_Button_Click(object sender, RoutedEventArgs e)
        {
            VM.Authorize(new FBF.Yahoo.OAuth.ConsumerInfo(textBoxConsumerKey.Text, textBoxConsumerSecret.Text));
        }

        private void SetAuthCode_Button_Click(object sender, RoutedEventArgs e)
        {
            VM.AuthCode = textBoxAuthCode.Text;
            webViewMain.Navigate(new Uri("about:blank"));
        }

        private void GetURI_Button_Click(object sender, RoutedEventArgs e)
        {
            VM.GetUriResponse();
        }

        private ExampleViewModel VM
        {
            get
            {
                return this.DataContext as ExampleViewModel;
            }
        }
    }
}
