using System;
using System.Collections.Generic;
using UnityEngine;


namespace AES.Tools
{
    public static class EventBus<T> where T : IEvent {
        static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    
        public static void Register(IEventBinding<T> binding) => bindings.Add(binding);
        public static void Deregister(IEventBinding<T> binding) => bindings.Remove(binding);
        

        public static void Raise(T @event) {
            var snapshot = new HashSet<IEventBinding<T>>(bindings);

            foreach (var binding in snapshot) {
                if (!bindings.Contains(binding))
                    continue;

                try
                {
                    if (binding is EventBinding<T> concrete)
                    {
                        concrete.InvokeInternal(@event);
                    }
                    else
                    {
                        binding.OnEvent?.Invoke(@event);
                        binding.OnEventNoArgs?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    if (binding is EventBinding<T> concrete)
                    {
                        Debug.LogError(
                            $"[EventBus<{typeof(T).Name}>] " +
                            $"Binding '{concrete.Name ?? "<no-name>"}' (Owner: {concrete.Owner ?? "null"}) threw: {ex}"
                        );
                    }
                    else
                    {
                        Debug.LogError(
                            $"[EventBus<{typeof(T).Name}>] Binding threw: {ex}"
                        );
                    }
                }
            }
        }

        static void Clear() {
            Debug.Log($"Clearing {typeof(T).Name} bindings");
            bindings.Clear();
        }
    }
}
