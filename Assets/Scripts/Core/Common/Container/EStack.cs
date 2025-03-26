using System.Collections.Generic;

namespace ppCore
{
    public class EStack<T> where T : class, new()
    {
        private ENode<T> m_top;
        private int m_count;

        public EStack(int size)
        {
            m_top = null;
            m_count = 0;

            for (int i = 0; i < size; ++i)
            {
                AllocNode();
            }
        }

        private void AllocNode()
        {
            ENode<T> node = new ENode<T>();
            push(node);
        }

        public void push(ENode<T> node)
        {
            node.next = m_top;
            m_top = node;
            ++m_count;
        }


        public ENode<T> Pop(bool allocNode = false)
        {
            if (m_count == 0)
            {
                //LOG.Log(LogHeader.A(m_top == null), "EStack count Error: ", GetCount());

                if (allocNode)
                {
                    AllocNode();
                }
                else
                {
                    return null;
                }                
            }

            ENode<T> node = m_top;
            m_top = m_top.next;
            node.next = null;
            --m_count;
            return node;
        }

        public int GetCount()
        {
            return m_count;
        }

        public void Clear()
        {
            m_top = null;
            m_count = 0;
        }
    }
}