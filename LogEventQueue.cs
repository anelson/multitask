using System;
using System.Collections;
using System.Text;

namespace MultiTask
{
    class LogEventQueue
    {
        bool _closed;
        String _src;
        Queue _internalQueue;

        public LogEventQueue(String src)
            : base()
        {
            _closed = false;
            _src = src;
            _internalQueue = Queue.Synchronized(new Queue());
        }

        public bool Open { get { return !_closed; } }

        public virtual Object Dequeue()
        {
            return _internalQueue.Dequeue();
        }

        public virtual void Enqueue(Object obj)
        {
            _internalQueue.Enqueue(obj);
        }

        public virtual Object Peek()
        {
            return _internalQueue.Peek();
        }

        public void Close()
        {
            _closed = true;
        }
    }
}
