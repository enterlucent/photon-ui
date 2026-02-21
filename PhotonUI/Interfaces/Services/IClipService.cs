using PhotonUI.Components;
using PhotonUI.Interfaces.Clips;

namespace PhotonUI.Interfaces.Services
{
    public interface IClipService
    {
        ClipSequence BuildFromSpriteSheet(IntPtr texture, int framesWide, int framesTall, int offsetX = 0, int offsetY = 0);
        ClipPlayer GetPlayer(nint texture, int framesWide, int framesTall, int offsetX = 0, int offsetY = 0);
    }
}