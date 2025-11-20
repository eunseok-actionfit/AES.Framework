using System;


namespace AES.Tools
{
    public interface IEventBinding<T> {
        public Action<T> OnEvent { get; set; }
        public Action OnEventNoArgs { get; set; }
    }

    public class EventBinding<T> : IEventBinding<T>, IDisposable where T : IEvent {
        Action<T> onEvent = _ => { };
        Action onEventNoArgs = () => { };

        Action<T> IEventBinding<T>.OnEvent {
            get => onEvent;
            set => onEvent = value;
        }

        Action IEventBinding<T>.OnEventNoArgs {
            get => onEventNoArgs;
            set => onEventNoArgs = value;
        }

        public EventBinding(Action<T> onEvent) => this.onEvent = onEvent;
        public EventBinding(Action onEventNoArgs) => this.onEventNoArgs = onEventNoArgs;
    
        public void Add(Action onEvent) => onEventNoArgs += onEvent;
        public void Remove(Action onEvent) => onEventNoArgs -= onEvent;
    
        public void Add(Action<T> onEvent) => this.onEvent += onEvent;
        public void Remove(Action<T> onEvent) => this.onEvent -= onEvent;
        
        public void Register()
        {
            EventBus<T>.Register(this);
        }

        public void Deregister()
        {
            EventBus<T>.Register(this);
        }
        
        void IDisposable.Dispose()
        {
            EventBus<T>.Deregister(this);
        }
    }
}