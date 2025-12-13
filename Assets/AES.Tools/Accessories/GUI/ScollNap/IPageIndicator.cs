using System;


namespace AES.Tools
{
    public interface IPageIndicator
    {
        void SetPageCount(int count);
        void SetCurrentPage(int page);
        event Action<int> OnPageClicked;
    }
}