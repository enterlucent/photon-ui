using PhotonUI.Interfaces;

namespace PhotonUI.Models.Properties
{
    public enum Orientation
    {
        Vertical,
        Horizontal
    }

    public interface IStackProperties : IStyleProperties
    {
        Orientation StackOrientation { get; }
        float Spacing { get; }
    }

    public readonly record struct StackProperties(Orientation StackOrientation, float Spacing) : IStackProperties
    {
        public static StackProperties Default => new(
            Orientation.Vertical,
            0
        );
    }
}