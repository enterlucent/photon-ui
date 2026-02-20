namespace PhotonUI.Events
{
    public class FrameworkEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;
        public bool Preview { get; set; } = false;
    }
}