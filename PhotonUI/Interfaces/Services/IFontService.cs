namespace PhotonUI.Interfaces.Services
{
    public interface IFontService
    {
        IEnumerable<string> GetFamilies();
        IntPtr GetFont(string familyName, string styleName, int size);
    }
}