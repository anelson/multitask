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

        public LogEvent GetNextLogEvent()
        {
            lock (_queueList.SyncRoot)
            {
                while (_queueList.Count > 0)
                {
                    LogEventQueueBase queue = (LogEventQueueBase)_queueList[0];

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

		public void Add(LogEventQueueBase q) {
			lock (_queueList.SyncRoot) {
				if (_queueList.Count > 0) {
					LogEventQueueBase prevTail = (LogEventQueueBase)_queueList[_queueList.Count-1];

					//If this is an auto-close queue, close it now that it's no longer
					//at the tail
					if (prevTail.AutoClose) {
						prevTail.Close();
					}
				}

				_queueList.Add(q);
			}
		}

		public void Clear() {
			lock (_queueList.SyncRoot) {
				_queueList.Clear();
			}
		}

		public void Flush() {
			//Close the tail of the queue, if it's autoclose is enabled
			lock (_queueList.SyncRoot) {
				if (_queueList.Count > 0) {
					LogEventQueueBase prevTail = (LogEventQueueBase)_queueList[_queueList.Count-1];

					//If this is an auto-close queue, close it now that it's no longer
					//at the tail
					if (prevTail.AutoClose) {
						prevTail.Close();
					}
				}
			}
		}

		public bool IsEmpty {
			get {
				lock (_queueList.SyncRoot) {
					CullFinishedQueues();

					return (_queueList.Count == 0);
				}
			}
		}

		private void CullFinishedQueues() {
			while (_queueList.Count > 0) {
				LogEventQueueBase queue = (LogEventQueueBase)_queueList[0];

				if (queue.Peek() == null && !queue.Open) {
					_queueList.RemoveAt(0);
					continue; 
				} else {
					break;
				}
			}
		}
    }
}
