using Microsoft.Extensions.DependencyInjection;
using PhotonUI.Controls;
using SDL3;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PhotonUI.Demo
{
    public class Vacuum
    {
        public static void Excite(Action<IServiceCollection>? configure = null, Action<IServiceProvider>? ready = null)
        {
            ServiceCollection serviceCollection = new();

            configure?.Invoke(serviceCollection);

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            ready?.Invoke(serviceProvider);
        }
        public static void Emit(Window window)
        {
            bool quit = false;

            window.Initialize();

            LoadDefaultWidowIcon(window);

            while (!quit)
            {
                while (SDL.PollEvent(out SDL.Event e))
                {
                    window.Event(e);

                    switch (e.Type)
                    {
                        case (uint)SDL.EventType.WindowResized:
                            window.SetWindowSize(e.Window.Data1, e.Window.Data2);
                            break;

                        case (uint)SDL.EventType.KeyDown:
                            {
                                switch (e.Key.Key)
                                {
                                    case SDL.Keycode.PrintScreen:
                                        window.GetScreenshot(Path.Combine(AppContext.BaseDirectory, "screenshot.png"));
                                        break;

                                    case SDL.Keycode.Tab:
                                        if ((e.Key.Mod & SDL.Keymod.Shift) != 0)
                                            window.TabStopBackward();
                                        else
                                            window.TabStopForward();
                                        break;
                                }
                                break;
                            }

                        case (uint)SDL.EventType.Quit:
                            quit = true;
                            break;
                    }
                }

                window.Tick();

                SDL.Delay(1000 / 42);
            }

            if (window.Handle != IntPtr.Zero)
                SDL.DestroyWindow(window.Handle);

            SDL.Quit();
        }

        private static void LoadDefaultWidowIcon(Window window)
        {
            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PhotonUI.Demo.Assets.Images.photon.icon.png");

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
    }
}