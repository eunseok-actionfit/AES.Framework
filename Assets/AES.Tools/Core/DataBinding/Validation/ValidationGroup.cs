using System;
using System.Linq;

namespace AES.Tools
{
    public sealed class ValidationGroup : IDisposable
    {
        readonly IValidatableProperty[] _properties;

        public bool HasError => _properties.Any(p => p.HasError);

        public event Action OnStateChanged = delegate { };

        public ValidationGroup(params IValidatableProperty[] properties)
        {
            _properties = properties ?? Array.Empty<IValidatableProperty>();

            foreach (var p in _properties)
            {
                if (p == null) continue;
                p.OnValidationChanged += HandleValidationChanged;
            }
        }

        void HandleValidationChanged(IValidatableProperty _)
        {
            OnStateChanged();
        }

        public void Dispose()
        {
            foreach (var p in _properties)
            {
                if (p == null) continue;
                p.OnValidationChanged -= HandleValidationChanged;
            }
        }
    }
}