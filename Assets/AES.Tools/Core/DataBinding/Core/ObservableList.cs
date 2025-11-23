using System;
using System.Collections;
using System.Collections.Generic;


namespace AES.Tools
{
    public interface IObservableList
    {
        event Action OnListChanged;
        int Count { get; }
        object GetItem(int index);
        IEnumerable Enumerate();
    }

    public class ObservableList<T> : IList<T>, IObservableList
    {
        private readonly List<T> _inner = new();

        public event Action OnListChanged = delegate { };
        private void Notify() => OnListChanged.Invoke();
        
        public int Count => _inner.Count;
        public object GetItem(int index) => _inner[index];
        public IEnumerable Enumerate() => _inner;

        #region IList<T>
        bool ICollection<T>.IsReadOnly => false;
        
        public T this[int index]
        {
            get => _inner[index];
            set
            {
                _inner[index] = value;
                Notify();
            }
        }
        
        public void Add(T item)
        {
            _inner.Add(item);
            Notify();
        }
        
        public void Clear()
        {
            _inner.Clear();
            Notify();
        }
        
        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        public int IndexOf(T item) => _inner.IndexOf(item);

        public void Insert(int index, T item)
        {
            _inner.Insert(index, item);
            Notify();
        }

        public bool Remove(T item)
        {
            var result = _inner.Remove(item);
            if(result) Notify();
            return result;
        }       
        
        public void RemoveAt(int index)
        {
           _inner.RemoveAt(index);
            Notify();
        }
        
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        #endregion
    }
}