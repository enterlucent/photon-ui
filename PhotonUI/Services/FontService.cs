using PhotonUI.Interfaces.Services;
using PhotonUI.Models.Properties;
using SDL3;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PhotonUI.Services
{
    public enum LoadFontResult
    {
        Success,
        AlreadyLoaded,
        FileNotFound,
        ResourceNotFound,
        Error
    }

    public enum FontRenderMode
    {
        Solid,
        Shaded,
        Blended
    }

    public record FontKey(string Family, string Style);
    public record FontInstanceKey(string Family, string Style, int Size);

    public class FontService : IFontService
    {
        protected readonly Dictionary<FontKey, byte[]> FontBlobs = [];
        protected readonly Dictionary<FontInstanceKey, IntPtr> FontInstances = [];

        public FontService()
        {
            this.LoadFont(
                "PhotonUI.Assets.Fonts.DejaVuSanMono.DejaVuSansMono.ttf",
                TextProperties.Default.FontFamily,
                TextProperties.Default.FontStyle
            );
        }

        public virtual IntPtr GetFont(string familyName, string styleName, int size)
        {
            FontInstanceKey instanceKey = new(familyName, styleName, size);

            if (this.FontInstances.TryGetValue(instanceKey, out nint cached))
                return cached;

            FontKey blobKey = new(familyName, styleName);

            if (!this.FontBlobs.TryGetValue(blobKey, out byte[]? fontData))
                return IntPtr.Zero;

            GCHandle handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

            try
            {
                IntPtr io = SDL.IOFromConstMem(handle.AddrOfPinnedObject(), (uint)fontData.Length);
                IntPtr font = TTF.OpenFontIO(io, true, size);

                if (font == IntPtr.Zero)
                    return IntPtr.Zero;

                this.FontInstances[instanceKey] = font;

                return font;
            }
            finally
            {
                handle.Free();
            }
        }
        public virtual IEnumerable<string> GetFamilies() =>
            this.FontBlobs.Keys.Select(k => k.Family).Distinct();

        public virtual LoadFontResult LoadFont(string path, string familyName, string styleName)
        {
            FontKey key = new(familyName, styleName);

            if (this.FontBlobs.ContainsKey(key))
                return LoadFontResult.AlreadyLoaded;

            try
            {
                Stream? stream = null;

                if (File.Exists(path))
                {
                    stream = File.OpenRead(path);
                }
                else
                {
                    Assembly? hostAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

                    if (hostAssembly != null)
                        stream = hostAssembly.GetManifestResourceStream(path);

                    if (stream == null)
                    {
                        Assembly libraryAssembly = typeof(FontService).Assembly;
                        stream = libraryAssembly.GetManifestResourceStream(path);
                    }

                    if (stream == null)
                        return LoadFontResult.ResourceNotFound;
                }

                using (stream)
                {
                    using (MemoryStream ms = new())
                    {
                        stream.CopyTo(ms);
                        this.FontBlobs[key] = ms.ToArray();
                    }
                }

                return LoadFontResult.Success;
            }
            catch (FileNotFoundException)
            {
                return LoadFontResult.FileNotFound;
            }
            catch
            {
                return LoadFontResult.Error;
            }
        }
    }
}