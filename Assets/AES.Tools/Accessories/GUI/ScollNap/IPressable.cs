using System;


namespace AES.Tools
{
    public interface IPressable
    {
        event Action Clicked;
        void SetInteractable(bool interactable);
    }
}