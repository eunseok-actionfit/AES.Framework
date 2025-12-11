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
        public Action<int,T> ItemAdded = delegate { };
        public Action<int,T> ItemRemoved = delegate { };
        private void Notify()
        {
            OnListChanged.Invoke();
        }

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
            ItemAdded.Invoke(Count-1, item);
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
            ItemAdded.Invoke(index, item);
            Notify();
        }

        public bool Remove(T item)
        {
            var index = _inner.IndexOf(item);
            if (index < 0)
                return false;

            _inner.RemoveAt(index);
            ItemRemoved.Invoke(index, item);
            Notify();
            return true;
        }
  
        
        public void RemoveAt(int index)
        {
            var item = _inner[index];
           _inner.RemoveAt(index);
           ItemRemoved.Invoke(index, item);
            Notify();
        }
        
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        #endregion
    }
}