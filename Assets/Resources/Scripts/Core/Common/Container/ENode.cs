using System.Collections.Generic;

namespace ppCore
{
    public class ENode<T> where T : class, new()
    {
        public T value;
        public ENode<T> next;

        public ENode<T> prev;
        public ENode()
        {
            value = null;
            next = null;
            prev = null;
        }

        public void Reset()
        {
            value = null;
            next = null;
            prev = null;
        }
    }
}