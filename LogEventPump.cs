using System;
using System.Threading;

using NAnt.Core;

namespace MultiTask
{
	/// <summary>
	/// Summary description for LogEventPump.
	/// </summary>
	internal class LogEventPump
	{
		LogEventQueueList _queues;
		BuildListenerCollection _handlers;
		Thread _pumpThread;
		Exception _firstException;
		volatile bool _stop;

		public LogEventPump(LogEventQueueList queues, BuildListenerCollection handlers)
		{
			_queues = queues;
			_handlers = handlers;
			_pumpThread = null;
			_firstException = null;
			_stop = false;
		}

		public void Start() {
			if (_pumpThread == null) {
				_stop = false;
				_firstException = null;
				_pumpThread = new Thread(new ThreadStart(PumpEvents));
				_pumpThread.Start();
			}
		}

		public Exception Stop() {
			if (_pumpThread != null) {
				_stop = true;
				_pumpThread.Join();
			}

			return _firstException;
		}

		private void PumpEvents() {
			LogEvent evt = null;
			do {
				evt = _queues.GetNextLogEvent();
				if (evt == null) {
					Thread.Sleep(10);
					continue;
				}

				foreach (IBuildListener bl in _handlers) {
					PassEventToHandler(evt, bl);
				}
			} while (!_queues.IsEmpty || _stop == false);
		}

		private void PassEventToHandler(LogEvent evt, IBuildListener bl) {
			switch (evt.EventType) {
			case LogEventType.TargetFinished:
				bl.TargetFinished(evt.Sender, evt.Args);
				break;

			case LogEventType.MessageLogged:
				bl.MessageLogged(evt.Sender, evt.Args);
				break;

			case LogEventType.BuildStarted:
				bl.BuildStarted(evt.Sender, evt.Args);
				break;

			case LogEventType.BuildFinished:
				bl.BuildFinished(evt.Sender, evt.Args);
				break;

			case LogEventType.TaskFinished:
				bl.TaskFinished(evt.Sender, evt.Args);
				break;

			case LogEventType.TargetStarted:
				bl.TargetStarted(evt.Sender, evt.Args);
				break;

			case LogEventType.TaskStarted:
				bl.TaskStarted(evt.Sender, evt.Args);
				break;

			case LogEventType.Exception:
				//If this is the first exception, save it
				if (_firstException == null) {
					_firstException = evt.Exception;
				}

				//Report this in the form of a message
				BuildEventArgs e = new BuildEventArgs(evt.Project);
				e.Exception = evt.Exception;
				e.MessageLevel = Level.Error;
				e.Message = String.Format("Error running async task: {0}", evt.Exception.Message);
				bl.MessageLogged(evt.Project, e);
				break;
			}
		}
	}
}
