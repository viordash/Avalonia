using Avalonia;
using Avalonia.Markup.Xaml;

namespace Avalonia.X11.NUnit.UnitTests
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
