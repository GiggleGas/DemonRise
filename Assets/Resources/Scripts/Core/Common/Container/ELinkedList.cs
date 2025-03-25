using System.Collections.Generic;

namespace ppCore
{
    public class ELinkedList<T>
        where T : class, new()
    {
        private ENode<T> m_first;
        public ENode<T> first
        {
            get
            {
                return m_first;
            }
        }

        private ENode<T> m_last;
        public ENode<T> last
        {
            get
            {
                return m_last;
            }
        }

        private EStack<T> m_pool;
        private int m_count;
        public ELinkedList(): this(4)
        {
        }

        public ELinkedList(int capacity)
        {
            m_first = null;
            m_last = null;
            m_count = 0;
            m_pool = new EStack<T>(capacity);
        }

        public T GetFirst()
        {
            if (m_first != null)
                return m_first.value;
            return null;
        }

        public T GetLast()
        {
            if (m_last != null)
                return m_last.value;
            return null;
        }

        public void AddFirst(T value)
        {
            ENode<T> node = m_pool.Pop(true);
            node.value = value;
            if (m_count > 0)
            {
                node.prev = null;
                node.next = m_first;
                m_first.prev = node;
                m_first = node;
            }
            else
            {
                node.prev = null;
                node.next = null;
                m_first = node;
                m_last = node;
            }

            ++m_count;
        }

        public ENode<T> AddBefore(ENode<T> node, T value)
        {
            if (node == m_first)
            {
                AddFirst(value);
                return m_first;
            }

            var newNode = m_pool.Pop(true);
            newNode.value = value;
            InternalInsertNodeBefore(node, newNode);
            return newNode;
        }

        public ENode<T> AddAfter(ENode<T> node, T value)
        {
            if (node == m_last)
            {
                AddLast(value);
                return m_last;
            }

            var newNode = m_pool.Pop(true);
            newNode.value = value;
            InternalInsertNodeBefore(node.next, newNode);
            return newNode;
        }

        private void InternalInsertNodeBefore(ENode<T> node, ENode<T> newNode)
        {
            newNode.next = node;
            newNode.prev = node.prev;
            node.prev.next = newNode;
            node.prev = newNode;
            ++m_count;
        }

        public void AddLast(T value)
        {
            ENode<T> node = m_pool.Pop(true);
            node.value = value;
            if (m_count > 0)
            {
                node.prev = m_last;
                node.next = null;
                m_last.next = node;
                m_last = node;
            }
            else
            {
                node.prev = null;
                node.next = null;
                m_first = node;
                m_last = node;
            }

            ++m_count;
        }

        public void RemoveFirst()
        {
            if (m_count > 0)
            {
                ENode<T> node = m_first;
                m_first = m_first.next;
                if (null != m_first)
                {
                    m_first.prev = null;
                }
                else
                {
                    m_last = null;
                }

                node.Reset();
                m_pool.push(node);
                --m_count;
            }
        }

        public void RemoveLast()
        {
            if (m_count > 0)
            {
                ENode<T> node = m_last;
                m_last = m_last.prev;
                if (null != m_last)
                {
                    m_last.next = null;
                }
                else
                {
                    m_first = null;
                }

                node.Reset();
                m_pool.push(node);
                --m_count;
            }
        }

        public void Remove(T value)
        {
            ENode<T> node = m_first;
            while (null != node)
            {
                if (node.value == value)
                {
                    if (node == m_first)
                    {
                        RemoveFirst();
                    }
                    else
                    {
                        if (node == m_last)
                        {
                            RemoveLast();
                        }
                        else
                        {
                            ENode<T> nextNode = node.next;
                            ENode<T> prevNode = node.prev;
                            prevNode.next = nextNode;
                            nextNode.prev = prevNode;
                            node.Reset();
                            m_pool.push(node);
                            --m_count;
                        }
                    }

                    break;
                }

                node = node.next;
            }
        }

        public void Clear()
        {
            var current = m_first;
            while (current != null)
            {
                var temp = current;
                current = current.next;
                temp.Reset();
                m_pool.push(temp);
            }

            m_first = null;
            m_last = null;
            m_count = 0;
        }

        public int GetCount()
        {
            return m_count;
        }

        public void Dump()
        {
            ENode<T> node = m_first;
            while (null != node)
            {

                node = node.next;
            }
        }

        public T[] ToArray()
        {
            if (m_count > 0)
            {
                T[] array = new T[m_count];
                int index = 0;
                ENode<T> node = m_first;
                while (null != node)
                {
                    array[index] = node.value;
                    node = node.next;
                    ++index;
                }

                return array;
            }
            else
            {
                return null;
            }
        }
        public bool Contains(T obj)
        {
            ENode<T> node = m_first;
            while (null != node)
            {
                if (node.value == obj)
                {
                    return true;
                }

                node = node.next;
            }

            return false;
        }
    }
}