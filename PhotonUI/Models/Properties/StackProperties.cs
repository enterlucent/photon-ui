using PhotonUI.Interfaces;

namespace PhotonUI.Models.Properties
{
    public enum Orientation
    {
        Vertical,
        Horizontal
    }
    public enum StackFillType
    {
        None,
        Equal,
        First,
        Last
    }

    public interface IStackProperties : IStyleProperties
    {
        Orientation StackOrientation { get; }
        StackFillType StackFillType { get; }
    }

    public readonly record struct StackProperties(Orientation StackOrientation, StackFillType StackFillType) : IStackProperties
    {
        public static StackProperties Default => new(Orientation.Vertical, StackFillType.None);
    }
}