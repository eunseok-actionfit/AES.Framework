namespace AES.Tools.UI
{
    public struct BannerHeightChangedEvent : IEvent
    {
        public readonly int HeightPx;
        public readonly bool Visible;

        public BannerHeightChangedEvent(int heightPx, bool visible)
        {
            HeightPx = heightPx;
            Visible = visible;
        }
    }
}