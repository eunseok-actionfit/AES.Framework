namespace AES.Tools
{
    public interface ISelectableVirtualizedItemBinder : IVirtualizedItemBinder
    {
        void SetSelected(bool selected);
    }
}