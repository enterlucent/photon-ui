using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Console.ViewModel;
using PhotonUI.Components;
using PhotonUI.Controls;
using PhotonUI.Controls.Decorators;
using PhotonUI.Interfaces.Services;
using PhotonUI.Services;
using PhotonUI.Services.Interpolators;
using PhotonUI.Demo;
using SDL3;

partial class Program
{
    static void Main(string[] args)
    {
        MainArgsHandler(args);

        Window window = null!;
        object view = null!;
        object viewModel = null!;

        Vacuum.Excite(
            services =>
            {
                //services.AddSingleton<IProviderFontFamily, ProviderFontDejaVuSansMono>();
                services.AddSingleton<IFontService, FontService>();
                services.AddSingleton<ITextureService, TextureService>();
                services.AddSingleton<IBindingService, BindingService>();
                services.AddSingleton<IKeyBindingService, KeyBindingService>();
                services.AddSingleton<IClipService, ClipService>();

                services.AddSingleton<IInterpolator, IntInterpolator>();
                services.AddSingleton<IInterpolator, FloatInterpolator>();
                services.AddSingleton<IInterpolator, SDLColorInterpolator>();
                services.AddSingleton<IInterpolatorService, InterpolatorService>();
                services.AddSingleton<IAnimationBuilder, AnimationBuilder>();

                services.AddTransient<MainView>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<Window>();
            },
            provider =>
            {
                window = provider.GetRequiredService<Window>();
                view = provider.GetRequiredService<MainView>();
                viewModel = provider.GetRequiredService<MainViewModel>();

                if (!SDL.Init(SDL.InitFlags.Video))
                    throw new InvalidProgramException(SDL.GetError());

                if (!TTF.Init())
                    throw new InvalidProgramException(SDL.GetError());
            }
        );

        if (window == null)
            throw new NullReferenceException("Window instance == null after DI resolution.");

        window.Name = "Window";
        window.Child = (Border)view;
        window.Child.DataContext = viewModel;

        Vacuum.Emit(window);
    }

    #region PhotonUI.Demo: Argumentation

    private static void MainArgsHandler(string[] args)
    {
        foreach (string arg in args)
            if (string.IsNullOrEmpty(arg)) { }
    }

    #endregion
}