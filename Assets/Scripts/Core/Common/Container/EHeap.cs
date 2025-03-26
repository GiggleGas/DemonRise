/************************************************************************
Author：veleonli
Date：2019/03/08 14:01:38
Refer:

Description:最小堆实现
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore
{
    public class EHeap<T> where T : IComparable<T>
    {
        private T[] m_items;
        private int m_length;
        private int m_capacity;

        static readonly T[] s_emptyArray = new T[0];

        public EHeap(int size = 0)
        {
            m_length = 0;
            m_capacity = size;
            m_items = size > 0 ? new T[size] : s_emptyArray;
        }

        public int Capacity
        {
            get
            {
                return m_capacity;
            }
            set
            {
                if (value <= m_capacity)
                {
                    return;
                }

                T[] newItems = new T[value];
                Array.Copy(m_items, 0, newItems, 0,m_length);
                m_items = newItems;

                m_capacity = value;
            }
        }

        public int Length
        {
            get
            {
                return m_length;
            }
        }

        public T[] Datas
        {
            get
            {
                return m_items;
            }
        }

        public T Head()
        {
            if (Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return m_items[0];
        }

        public void Clear()
        {
            for (int i = 0; i < m_length; ++i)
            {
                m_items[i] = default(T);
            }
            m_length = 0;
        }

        public void Push(T item)
        {
            if (Length + 1 > Capacity)
            {
                Capacity = (Length + 1) * 2;
            }
            ++m_length;
            //向上回溯维持最小堆
            int index = m_length - 1;
            while (index != 0)
            {
                int parentIndex = (index - 1) / 2;
                if (item.CompareTo(m_items[parentIndex]) >= 0)
                {
                    break;
                }
                m_items[index] = m_items[parentIndex];
                index = parentIndex;
            }

            //找到最终位置
            m_items[index] = item;
        }

        public void Remove(T value)
        {
            int index = -1;
            for (int i = 0; i < m_length; ++i)
            {
                if (!EqualityComparer<T>.Default.Equals(m_items[i], value))
                {
                    continue;
                }
                index = i;
                break;
            }
            if (index == -1)
            {//没有找到对象
                return;
            }
            m_items[index] = default(T);

            --m_length;
            if (m_length == 0 || index == m_length)
            {
                return;
            }
            T tailItem = m_items[m_length];


            if (index == 0 || (index > 0 && m_items[(index - 1)/2].CompareTo(tailItem) < 0))
            {//父节点小，需要下沉
                HeapFixDown(index);
            }
            else
            {//父节点大，需要上浮
                while (index != 0)
                {
                    int parentIndex = (index - 1) / 2;
                    if (tailItem.CompareTo(m_items[parentIndex]) >= 0)
                    {
                        break;
                    }
                    m_items[index] = m_items[parentIndex];
                    index = parentIndex;
                }

                //找到最终位置
                m_items[index] = tailItem;
                m_items[m_length] = default(T);
            }
        }
        private void HeapFixDown(int index)
        {
            T tailItem = m_items[m_length];
            //下沉，直到只有左节点或者没有子节点
            while (index*2 +1 < Length)
            {//有左节点
                T left = m_items[index * 2 + 1];
                int smallIndex = index * 2 + 1;
                if (index*2 + 2 < Length && m_items[index * 2 + 2].CompareTo(left) < 0)
                {
                    smallIndex = index * 2 + 2;
                }
                if (tailItem.CompareTo(m_items[smallIndex]) <= 0)
                {
                    break;
                }
                m_items[index] = m_items[smallIndex];
                index = smallIndex;
            }
            m_items[index] = tailItem;
            m_items[m_length] = default(T);
        }

        public T Pop()
        {
            if (m_length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            T item = m_items[0];
            --m_length;
            if (m_length == 0)
            {
                m_items[0] = default(T);
                return item;
            }
            //根节点空出来，将最后一个对象，从根节点向下沉
            HeapFixDown(0);

            return item;
        }
    }
}
