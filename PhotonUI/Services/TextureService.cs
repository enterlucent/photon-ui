using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using SDL3;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PhotonUI.Services
{
    public class TextureService : IDisposable, ITextureService
    {
        protected record SurfaceEntry(IntPtr Handle, int Width, int Height, string Path);

        protected readonly Dictionary<string, Stack<SurfaceEntry>> Cache = [];

        public void LoadSurface(string path, string name)
        {
            IntPtr surfacePtr = Image.Load(path);

            if (surfacePtr == IntPtr.Zero)
                throw new Exception($"Failed to load image: {path}");

            SDL.Surface surface = Marshal.PtrToStructure<SDL.Surface>(surfacePtr);
            SurfaceEntry entry = new(surfacePtr, surface.Width, surface.Height, path);

            if (!this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack))
            {
                stack = new Stack<SurfaceEntry>();
                this.Cache[name] = stack;
            }

            stack.Push(entry);
        }
        public void LoadEmbeddedSurface(string resourceId, string name)
        {
            Assembly appAssembly = Assembly.GetEntryAssembly()
                ?? throw new InvalidOperationException("No entry assembly found");

            using Stream? stream = appAssembly.GetManifestResourceStream(resourceId)
                ?? throw new Exception($"Embedded resource not found: {resourceId}");

            using MemoryStream ms = new();
            stream.CopyTo(ms);
            byte[] data = ms.ToArray();

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                nuint size = (nuint)data.Length;

                IntPtr io = SDL.IOFromConstMem(ptr, size);
                IntPtr surfacePtr = Image.LoadIO(io, false);

                SDL.CloseIO(io);

                if (surfacePtr == IntPtr.Zero)
                    throw new Exception($"SDL image load failed: {SDL.GetError()}");

                SDL.Surface surface = Marshal.PtrToStructure<SDL.Surface>(surfacePtr);
                SurfaceEntry entry = new(surfacePtr, surface.Width, surface.Height, $"embedded:{resourceId}");

                if (!this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack))
                {
                    stack = new Stack<SurfaceEntry>();

                    this.Cache[name] = stack;
                }

                stack.Push(entry);
            }
            finally
            {
                handle.Free();
            }
        }

        public IntPtr GetSurface(string name)
        {
            if (!this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack) || stack.Count == 0)
                throw new InvalidOperationException($"Surface not loaded: {name}");

            return stack.Peek().Handle;
        }
        public IEnumerable<(string Path, int Width, int Height)> GetAllSurfaces(string name)
        {
            if (!this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack)) return [];

            return stack.Select(e => (e.Path, e.Width, e.Height));
        }
        public void UnloadSurface(string name)
        {
            if (this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack) && stack.Count > 0)
            {
                SurfaceEntry entry = stack.Pop();
                SDL.DestroySurface(entry.Handle);

                if (stack.Count == 0)
                    this.Cache.Remove(name);
            }
        }

        public Size GetSurfaceSize(string name)
        {
            if (!this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack) || stack.Count == 0)
                throw new InvalidOperationException($"Surface not loaded: {name}");

            SurfaceEntry entry = stack.Peek();

            return new Size(entry.Width, entry.Height);
        }
        public bool CreateTexture(IntPtr renderer, string name, out IntPtr texture, out Size size)
        {
            texture = IntPtr.Zero;
            size = Size.Empty;

            if (!this.Cache.TryGetValue(name, out Stack<SurfaceEntry>? stack) || stack.Count == 0)
                return false;

            SurfaceEntry entry = stack.Peek();
            SDL.Surface surface = Marshal.PtrToStructure<SDL.Surface>(entry.Handle);

            size = new Size(surface.Width, surface.Height);
            texture = SDL.CreateTextureFromSurface(renderer, entry.Handle);

            return texture != IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (Stack<SurfaceEntry> stack in this.Cache.Values)
                foreach (SurfaceEntry entry in stack)
                    SDL.DestroySurface(entry.Handle);

            this.Cache.Clear();

            GC.SuppressFinalize(this);
        }
    }
}