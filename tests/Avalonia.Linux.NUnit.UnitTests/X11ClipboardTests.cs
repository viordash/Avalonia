using Avalonia.Input.Platform;
using System.Diagnostics;
using Moq;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.X11.NUnit.UnitTests
{
    public class X11ClipboardTests
    {

        Mock<IPlatformRenderInterface> mockIPlatformRenderInterface = new();
        Mock<IRenderLoop> mockIRenderLoop = new();
        // IPlatformRenderInterface factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

        [SetUp]
        public void Setup()
        {
            Debug.WriteLine("---------------------------- Setup 0");
            AvaloniaLocator.CurrentMutable.Bind<IRenderLoop>()
                .ToConstant(mockIRenderLoop.Object);

            var options = new X11PlatformOptions() { RenderingMode = new[] { X11RenderingMode.Software } };
            AvaloniaX11PlatformExtensions.InitializeX11Platform(options);

            Debug.WriteLine("---------------------------- Setup 1");
        }

        [TearDown]
        public void Teardown()
        {
            //  Die("Call from invalid thread");
        }

        [Test]
        public async Task Text_Test()
        {
            Debug.WriteLine("---------------------------- TestTextClpbr 0");
            var clipboard = AvaloniaLocator.Current.GetService<IClipboard>();

            Assert.That(clipboard, Is.Not.Null);

            await clipboard.SetTextAsync("Hello World!");
            // Debug.WriteLine("---------------------------- TestTextClpbr 1");
            var text = await clipboard.GetTextAsync();
            // Debug.WriteLine("---------------------------- TestTextClpbr 2");

            Assert.That(text, Is.EqualTo("Hello World!"));

        }
    }
}
