using System;
using AES.Tools.Core;


namespace AES.Tools
{
    public static class InputServiceLocator
    {
        private static IInputService _service;
        public static IInputService Service
        {
            get => _service 
                   ?? throw new InvalidOperationException("UIController not initialized.");
            set => _service = value;
        }
    }
}


