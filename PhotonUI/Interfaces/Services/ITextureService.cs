using PhotonUI.Models;

namespace PhotonUI.Interfaces.Services
{
    public interface ITextureService
    {
        void LoadSurface(string path, string name);
        Size GetSurfaceSize(string name);
        bool CreateTexture(IntPtr renderer, string name, out IntPtr texture, out Size size);
        void UnloadSurface(string name);
        void Dispose();
        void LoadEmbeddedSurface(string resourceId, string name);
    }
}