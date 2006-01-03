using System;
using System.Collections;
using System.Text;

using NAnt.Core;

namespace MultiTask
{
    abstract class LogEventQueueBase : IBuildListener
    {
        bool _closed;
        String _src;
        Queue _internalQueue;
		String _hideTarget;

        public LogEventQueueBase(String src)
        {
            _closed = false;
            _src = src;
            _internalQueue = Queue.Synchronized(new Queue());
        }

        public bool Open { get { return !_closed; } }

		public abstract bool AutoClose {get;}

        public virtual LogEvent Dequeue()
        {
            return (LogEvent)_internalQueue.Dequeue();
        }

        public virtual void Enqueue(LogEvent obj)
        {
			if (_closed) {
				throw new InvalidOperationException("Cannot enqueue an event when the queue is closed");
			}
            _internalQueue.Enqueue(obj);
        }

        public virtual LogEvent Peek()
        {
			if (_internalQueue.Count == 0) {
				return null;
			}

            return (LogEvent)_internalQueue.Peek();
        }

        public void Close()
        {
            _closed = true;
		}

		/// <summary>
		/// Replaces existing build event handlers with a handler that enqueues
		/// build events into this queue
		/// </summary>
		/// <param name="proj"></param>
		public void Install(Project proj, String hideTarget) {
			_hideTarget = hideTarget;

			BuildListenerCollection coll = new BuildListenerCollection();
			coll.Add(this);

			proj.DetachBuildListeners();
			proj.AttachBuildListeners(coll);
		}

		#region IBuildListener Members

		public void TargetFinished(object sender, BuildEventArgs e) {
			if (e.Target != null && e.Target.Name == _hideTarget) {
				return;
			}
            Enqueue(new LogEvent(_src, sender, e, LogEventType.TargetFinished));
		}

		public void MessageLogged(object sender, BuildEventArgs e) {
            Enqueue(new LogEvent(_src, sender, e, LogEventType.MessageLogged));
		}

		public void BuildStarted(object sender, BuildEventArgs e) {
            Enqueue(new LogEvent(_src, sender, e, LogEventType.BuildStarted));
		}

		public void BuildFinished(object sender, BuildEventArgs e) {
            Enqueue(new LogEvent(_src, sender, e, LogEventType.BuildFinished));
		}

		public void TaskFinished(object sender, BuildEventArgs e) {
            Enqueue(new LogEvent(_src, sender, e, LogEventType.TaskFinished));
		}

		public void TargetStarted(object sender, BuildEventArgs e) {
			if (e.Target != null && e.Target.Name == _hideTarget) {
				return;
			}
            Enqueue(new LogEvent(_src, sender, e, LogEventType.TargetStarted));
		}

		public void TaskStarted(object sender, BuildEventArgs e) {
            Enqueue(new LogEvent(_src, sender, e, LogEventType.TaskStarted));
		}

		#endregion
	}
}
