using PhotonUI.Controls;
using PhotonUI.Controls.Content;
using PhotonUI.Controls.Decorators;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;

namespace PhotonUI.Desktop.Views
{
    public class MainView(IServiceProvider serviceProvider, IBindingService bindingService, ITextureService textureService)
        : Border(serviceProvider, bindingService)
    {
        protected readonly ITextureService TexureService = textureService;

        public override void OnInitialize(Window window)
        {
            this.TexureService.LoadEmbeddedSurface("PhotonUI.Desktop.Assets.Images.sdl_logo.png", "sdl_logo");

            this.Name = "MainView";
            this.BackgroundColor = new SDL.Color() { A = 255, R = 51, G = 56, B = 63 };
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.Margin = new(10);
            this.Padding = new(10);
            this.BorderThickness = new(5);
            this.BorderColors = new BorderColors(new SDL.Color() { A = 255, R = 112, G = 128, B = 144 });

            TextBlock textBlock = this.Create<TextBlock>();
            textBlock.Text = "Welcome to PhotonUI!";
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.MinHeight = 24;
            textBlock.MinWidth = 230;

            this.Child = textBlock;
        }
    }
}