using System;


namespace AES.Tools
{
    public interface IEventBinding<T> {
        public Action<T> OnEvent { get; set; }
        public Action OnEventNoArgs { get; set; }
    }

    public class EventBinding<T> : IEventBinding<T>, IDisposable where T : IEvent {
        Action<T> onEvent = delegate { };
        Action onEventNoArgs = delegate { };
        
        public string Name { get; set; }
        public object Owner { get; set; }
        public Predicate<T> Filter { get; set; }
        public bool OneShot { get; set; }


        Action<T> IEventBinding<T>.OnEvent {
            get => onEvent;
            set => onEvent = value;
        }

        Action IEventBinding<T>.OnEventNoArgs {
            get => onEventNoArgs;
            set => onEventNoArgs = value;
        }

        public EventBinding() { }
        public EventBinding(Action<T> onEvent) => this.onEvent = onEvent;
        public EventBinding(Action onEventNoArgs) => this.onEventNoArgs = onEventNoArgs;
    
        public void Add(Action onEvent) => onEventNoArgs += onEvent;
        public void Remove(Action onEvent) => onEventNoArgs -= onEvent;
    
        public void Add(Action<T> onEvent) => this.onEvent += onEvent;
        public void Remove(Action<T> onEvent) => this.onEvent -= onEvent;
        
        internal void InvokeInternal(T @event)
        {
            if (Filter != null && !Filter(@event))
                return;

            onEvent?.Invoke(@event);
            onEventNoArgs?.Invoke();

            if (OneShot)
            {
                Deregister();
            }
        }

        
        public void Register() => EventBus<T>.Register(this);

        public void Deregister() => EventBus<T>.Deregister(this);

        void IDisposable.Dispose() => Deregister();
    }
}