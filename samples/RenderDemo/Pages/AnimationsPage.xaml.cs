using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RenderDemo.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RenderDemo.Pages
{
    public class AnimationsPage : UserControl
    {
        public AnimationsPage()
        {
            InitializeComponent();
            this.DataContext = new AnimationsPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        async Task TestCopyFileClpbr()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            // Debug.WriteLine("---------------------------- TestCopyFileClpbr 0");
            // var paste0 = await clipboard.GetDataAsync("x-special/gnome-copied-files");
            // if (paste0 is byte[] xbytes0)
            // {
            //     Debug.WriteLine("x-special 0 :" + System.Text.Encoding.UTF8.GetString(xbytes0));
            // }

            var dataObject = new DataObject();

            const string data = "copy\nfile:///tmp/X11Clipboard.cs";
            dataObject.Set("x-special/gnome-copied-files", data);

            await clipboard.ClearAsync();
            await clipboard.SetDataObjectAsync(dataObject);

            var paste = await clipboard.GetDataAsync("x-special/gnome-copied-files");
            if (paste is byte[] xbytes)
            {
                Debug.WriteLine("x-special :" + System.Text.Encoding.UTF8.GetString(xbytes));
            }
        }

        async Task TestTextClpbr()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            Debug.WriteLine("---------------------------- TestTextClpbr 0");
            await clipboard.SetTextAsync("Hello World!");
            Debug.WriteLine("---------------------------- TestTextClpbr 1");
            var text = await clipboard.GetTextAsync();
            Debug.WriteLine("---------------------------- TestTextClpbr 2");
            Debug.WriteLine("TestTextClpbr :" + text);
        }

        async Task TestTextClpbrLoop()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            Debug.WriteLine("---------------------------- TestTextClpbrLoop 0");
            for (int i = 0; i < 10; i++)
            {
                await clipboard.SetTextAsync($"Hello World! {i}");
                Debug.WriteLine("---------------------------- TestTextClpbrLoop 1");
                var text = await clipboard.GetTextAsync();
                Debug.WriteLine("TestTextClpbr :" + text);
            }
            Debug.WriteLine("---------------------------- TestTextClpbrLoop 2");
        }

        async Task TestGetFormats()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            var formats = await clipboard.GetFormatsAsync();
            if (formats != null)
            {
                var sf = string.Join(", ", formats);
                Debug.WriteLine(sf);
            } else {
                Debug.WriteLine("!!! clipboard empty");
            }
        }

        public async void OnTest0(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"-------------- OnTest0 -------------- {System.DateTime.Now.Ticks}");

            await TestGetFormats();
            await TestTextClpbr();
            await TestCopyFileClpbr();
            // await TestTextClpbrLoop();
            // await TestGetFormats();
        }
    }
}
