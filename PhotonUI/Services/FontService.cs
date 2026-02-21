using PhotonUI.Interfaces.Services;
using SDL3;
using System.Runtime.InteropServices;

namespace PhotonUI.Services
{
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
    }
}