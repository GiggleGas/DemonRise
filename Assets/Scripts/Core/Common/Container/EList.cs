using System;
using System.Collections.Generic;

public class EList<T>
{
    private const int _defaultCapacity = 4;

    //实际存储对象的地方
    private T[] _items;
    private int _size;

    static readonly T[] _emptyArray = new T[0];

    public EList()
    {
        _items = _emptyArray;
    }

    public EList(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException();

        _items = (capacity == 0) ? _emptyArray : new T[capacity];
    }

    public EList(EList<T> list)
    {
        if (list == null)
            throw new ArgumentNullException();

        int count = list.Count;
        if (count == 0)
            _items = _emptyArray;
        else
        {
            _items = new T[count];
            list.CopyTo(_items, 0);
            _size = count;
        }
    }

    public EList(T[] a)
    {
        if (a == null)
            throw new ArgumentNullException();

        int count = a.Length;
        if (count == 0)
            _items = _emptyArray;
        else
        {
            _items = new T[count];
            a.CopyTo(_items, 0);
            _size = count;
        }
    }
    //在 set 方法中传入的容量不能够小于当前的容量
    //否则抛出 ArgumentOutOfRangeException;
    public int Capacity
    {
        get
        {
            return _items.Length;
        }
        set
        {
            if (value < _size)
                throw new ArgumentOutOfRangeException();

            if (value != _items.Length)
            {
                if (value > 0)
                {
                    T[] newItems = new T[value];
                    if (_size > 0)
                        Array.Copy(_items, 0, newItems, 0, _size);
                    _items = newItems;
                }
                else
                    _items = _emptyArray;
            }
        }
    }

    public int Count
    {
        get
        {
            return _size;
        }
    }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_size)
                throw new ArgumentOutOfRangeException();
            return _items[index];
        }

        set
        {
            if ((uint)index >= (uint)_size)
                throw new ArgumentOutOfRangeException();
            _items[index] = value;
        }
    }

    public T[] Items
    {
        get
        {
            return _items;
        }
    }


    public void Add(T item)
    {
        if (_size == _items.Length)
            EnsureCapacity(_size + 1);
        _items[_size++] = item;
    }

    public void AddRange(EList<T> list)
    {
        InsertRange(_size, list);
    }

    public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException();

        if (count < 0)
            throw new ArgumentOutOfRangeException();

        if (_size - index < count)
            throw new ArgumentException();

        return Array.BinarySearch<T>(_items, index, count, item, comparer);
    }

    public int BinarySearch(T item)
    {
        return BinarySearch(0, Count, item, null);
    }

    public int BinarySearch(T item, IComparer<T> comparer)
    {
        return BinarySearch(0, Count, item, comparer);
    }

    public void Clear()
    {
        if (_size > 0)
        {
            Array.Clear(_items, 0, _size);
            _size = 0;
        }
    }

    public bool Contains(T item)
    {
        if ((Object)item == null)
        {
            for (int i = 0; i < _size; i++)
                if ((Object)_items[i] == null)
                    return true;
            return false;
        }
        else
        {
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < _size; i++)
                if (c.Equals(_items[i], item))
                    return true;
            return false;
        }
    }

    public void CopyTo(T[] array)
    {
        CopyTo(array, 0);
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        if (_size - index < count)
            throw new ArgumentException();

        Array.Copy(_items, index, array, arrayIndex, count);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, _size);
    }

    //EnsureCapacity 如果当前的容量小于传入的设定容量 min 则对 Capacity 进行扩展。
    private void EnsureCapacity(int min)
    {
        if (_items.Length < min)
        {
            int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
            if (newCapacity < min) newCapacity = min;
            Capacity = newCapacity;
        }
    }

    // Predicate<T> 是一个 delegate
    // public delegate bool Predicate<T> TObject;
    public bool Exists(Predicate<T> match)
    {
        return FindIndex(match) != -1;
    }

    public T Find(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException();

        for (int i = 0; i < _size; i++)
            if (match(_items[i]))
                return _items[i];

        return default(T);
    }

    public EList<T> FindAll(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException();

        EList<T> list = new EList<T>();
        for (int i = 0; i < _size; i++)
            if (match(_items[i]))
                list.Add(_items[i]);

        return list;
    }

    public int FindIndex(Predicate<T> match)
    {
        return FindIndex(0, _size, match);
    }

    public int FindIndex(int startIndex, Predicate<T> match)
    {
        return FindIndex(startIndex, _size - startIndex, match);
    }

    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
        if ((uint)startIndex > (uint)_size)
            throw new ArgumentOutOfRangeException();

        if (count < 0 || startIndex > _size - count)
            throw new ArgumentOutOfRangeException();

        if (match == null)
            throw new ArgumentNullException();

        int endIndex = startIndex + count;
        for (int i = startIndex; i < endIndex; i++)
            if (match(_items[i]))
                return i;

        return -1;
    }

    public T FindLast(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException();

        for (int i = _size - 1; i >= 0; i--)
            if (match(_items[i]))
                return _items[i];

        return default(T);
    }

    public int FindLastIndex(Predicate<T> match)
    {
        return FindLastIndex(_size - 1, _size, match);
    }

    public int FindLastIndex(int startIndex, Predicate<T> match)
    {
        return FindLastIndex(startIndex, startIndex + 1, match);
    }

    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException();
        }

        if (_size == 0)
        {
            if (startIndex != -1)
                throw new ArgumentOutOfRangeException();
        }
        else
        {
            if ((uint)startIndex >= (uint)_size)
                throw new ArgumentOutOfRangeException();
        }

        if (count < 0 || startIndex - count + 1 < 0)
            throw new ArgumentOutOfRangeException();

        int endIndex = startIndex - count;
        for (int i = startIndex; i > endIndex; i--)
            if (match(_items[i]))
                return i;

        return -1;
    }

    public EList<T> GetRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException();

        if (count < 0)
            throw new ArgumentOutOfRangeException();

        if (_size - index < count)
            throw new ArgumentException();

        EList<T> list = new EList<T>(count);
        Array.Copy(_items, index, list._items, 0, count);
        list._size = count;
        return list;
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(_items, item, 0, _size);
    }

    public int IndexOf(T item, int index)
    {
        if (index > _size)
            throw new ArgumentOutOfRangeException();

        return Array.IndexOf(_items, item, index, _size - index);
    }

    public int IndexOf(T item, int index, int count)
    {
        if (index > _size)
            throw new ArgumentOutOfRangeException();

        if (count < 0 || index > _size - count)
            throw new ArgumentOutOfRangeException();

        return Array.IndexOf(_items, item, index, count);
    }

    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)_size)
            throw new ArgumentOutOfRangeException();

        if (_size == _items.Length) EnsureCapacity(_size + 1);
        if (index < _size)
        {
            Array.Copy(_items, index, _items, index + 1, _size - index);
        }
        _items[index] = item;
        _size++;
    }

    public void InsertRange(int index, EList<T> list)
    {
        if (list == null)
            throw new ArgumentNullException();

        if ((uint)index > (uint)_size)
            throw new ArgumentOutOfRangeException();

        int count = list.Count;
        if (count > 0)
        {
            EnsureCapacity(_size + count);
            if (index < _size)
                Array.Copy(_items, index, _items, index + count, _size - index);

            if (this == list)
            {
                Array.Copy(_items, 0, _items, index, index);
                Array.Copy(_items, index + count, _items, index * 2, _size - index);
            }
            else
            {
                // Milo: optimize by removing new[]
                // T[] itemsToInsert = new T[count];
                // list.CopyTo(itemsToInsert, 0);
                // itemsToInsert.CopyTo(_items, index);
                Array.Copy(list._items, 0, _items, index, count);
            }
            _size += count;
        }
    }

    public int LastIndexOf(T item)
    {
        if (_size == 0)
            return -1;
        else
            return LastIndexOf(item, _size - 1, _size);
    }

    public int LastIndexOf(T item, int index)
    {
        if (index >= _size)
            throw new ArgumentOutOfRangeException();

        return LastIndexOf(item, index, index + 1);
    }

    public int LastIndexOf(T item, int index, int count)
    {
        if (Count != 0 && index < 0)
            throw new ArgumentOutOfRangeException();

        if (Count != 0 && count < 0)
            throw new ArgumentOutOfRangeException();

        if (_size == 0)
            return -1;

        if (index >= _size)
            throw new ArgumentOutOfRangeException();

        if (count > index + 1)
            throw new ArgumentOutOfRangeException();

        return Array.LastIndexOf(_items, item, index, count);
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 快速删除，但是会破坏列表顺序。调用前请确定你的列表是否关心顺序
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool RemoveFast(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveFastAt(index);
            return true;
        }

        return false;
    }

    public int RemoveAll(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException();

        int freeIndex = 0;

        while (freeIndex < _size && !match(_items[freeIndex]))
            freeIndex++;

        if (freeIndex >= _size)
            return 0;

        int current = freeIndex + 1;
        while (current < _size)
        {
            while (current < _size && match(_items[current]))
                current++;

            if (current < _size)
                _items[freeIndex++] = _items[current++];
        }

        Array.Clear(_items, freeIndex, _size - freeIndex);
        int result = _size - freeIndex;
        _size = freeIndex;
        return result;
    }



    public void RemoveRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException();

        if (count < 0)
            throw new ArgumentOutOfRangeException();

        if (_size - index < count)
            throw new ArgumentException();

        if (count > 0)
        {
            _size -= count;
            if (index < _size)
                Array.Copy(_items, index + count, _items, index, _size - index);
            Array.Clear(_items, _size, count);
        }
    }

    public void Reverse()
    {
        Reverse(0, Count);
    }

    public void Reverse(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException();

        if (count < 0)
            throw new ArgumentOutOfRangeException();

        if (_size - index < count)
            throw new ArgumentException();
        Array.Reverse(_items, index, count);
    }

    public void Sort()
    {
        Sort(0, Count, null);
    }

    public void Sort(IComparer<T> comparer)
    {
        Sort(0, Count, comparer);
    }

    public void Sort(int index, int count, IComparer<T> comparer)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException();

        if (count < 0)
            throw new ArgumentOutOfRangeException();

        if (_size - index < count)
            throw new ArgumentException();
        //内部直接调用 Sort 即可
        Array.Sort<T>(_items, index, count, comparer);
    }

    public T[] ToArray()
    {
        T[] array = new T[_size];
        Array.Copy(_items, 0, array, 0, _size);
        return array;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_size)
            throw new ArgumentOutOfRangeException();

        _size--;
        if (index < _size)
            Array.Copy(_items, index + 1, _items, index, _size - index);
        _items[_size] = default(T);
    }

    /// <summary>
    /// 快速删除，但是会破坏列表顺序。调用前请确定你的列表是否关心顺序
    /// </summary>
    /// <param name="index"></param>
    public void RemoveFastAt(int index)
    {
        if ((uint)index >= (uint)_size)
            throw new ArgumentOutOfRangeException();

        _size--;
        _items[index] = _items[_size];
        _items[_size] = default(T);
    }
}