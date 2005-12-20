using System;
using System.Collections;
using System.Text;

namespace MultiTask
{
    class LogEventQueueList
    {
        ArrayList _queueList;

        public LogEventQueueList()
        {
            _queueList = new ArrayList();
        }

        public Object GetNextLogEvent()
        {
            lock (_queueList.SyncRoot)
            {
                while (_queueList.Count > 0)
                {
                    LogEventQueue queue = _queueList[0];

                    if (queue.Peek() != null)
                    {
                        return queue.Dequeue();
                    }

                    //Else, queue is empty.  If it's closed, remove it.  If it's open, just
                    //need to be patient and wait for another log event
                    if (queue.Open)
                    {
                        break;
                    }

                    _queueList.RemoveAt(0);
                }

                return null;
            }
        }
    }
}
