using Microsoft.Extensions.DependencyInjection;
using PhotonUI.Components;
using PhotonUI.Controls;
using PhotonUI.Controls.Decorators;
using PhotonUI.Desktop;
using PhotonUI.Desktop.Diagnostics;
using PhotonUI.Desktop.ViewModels;
using PhotonUI.Desktop.Views;
using PhotonUI.Diagnostics;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Services;
using PhotonUI.Services.Interpolators;
using SDL3;
using System.Reflection;
using System.Runtime.InteropServices;

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
                services.AddSingleton<IPhotonDiagnostics, DiagnosticXMLSink>();

                services.AddSingleton<IFontService, FontService>();
                services.AddSingleton<ITextureService, TextureService>();
                services.AddSingleton<IBindingService, BindingService>();
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
                PhotonDiagnostics.Sink = provider.GetService<IPhotonDiagnostics>();

                view = provider.GetRequiredService<MainView>();
                viewModel = provider.GetRequiredService<MainViewModel>();
                window = provider.GetRequiredService<Window>();

                if (!SDL.Init(SDL.InitFlags.Video))
                    throw new InvalidProgramException(SDL.GetError());

                if (!TTF.Init())
                    throw new InvalidProgramException(SDL.GetError());
            }
        );

        if (window == null)
            throw new NullReferenceException("Window instance == null after DI resolution.");

        DiagnosticXMLSink? sink = PhotonDiagnostics.Sink as DiagnosticXMLSink;

        sink?
            .SetHeader("<PhotonDiagnostics>")
            .SetFooter("</PhotonDiagnostics>")
            .ResetOutput()
            .Start();

        window.Name = "Root";
        window.Child = (Border)view;
        window.Child.DataContext = viewModel;
        window.Initialize("PhotonUI :: Demo", new Size(800, 600), SDL.WindowFlags.Resizable);

        LoadDefaultWidowIcon(window);

        Vacuum.Emit(window);
    }

    #region PhotonUI.Desktop: Argumentation

    private static void MainArgsHandler(string[] args)
    {
        foreach (string arg in args)
            if (string.IsNullOrEmpty(arg)) { }
    }

    #endregion

    #region PhotonUI.Desktop: Window Customization

    private static void LoadDefaultWidowIcon(Window window)
    {
        Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PhotonUI.Desktop.Assets.Images.photon.icon.png");

        if (stream != null)
        {
            byte[] bytes;

            using (MemoryStream ms = new())
            {
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                nuint size = (nuint)bytes.Length;

                IntPtr io = SDL.IOFromMem(ptr, size);
                IntPtr surface = Image.LoadIO(io, true);

                if (surface == IntPtr.Zero)
                    throw new InvalidOperationException(SDL.GetError());

                SDL.SetWindowIcon(window.Handle, surface);
                SDL.DestroySurface(surface);
            }
            finally
            {
                handle.Free();
            }
        }
    }

    #endregion
}