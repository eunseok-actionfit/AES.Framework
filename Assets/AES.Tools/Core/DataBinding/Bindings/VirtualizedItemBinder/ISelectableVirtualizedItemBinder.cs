namespace AES.Tools.Bindings
{
    public interface ISelectableVirtualizedItemBinder : IVirtualizedItemBinder
    {
        void SetSelected(bool selected);
    }
}