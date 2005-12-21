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
		volatile bool _stop;

		public LogEventPump(LogEventQueueList queues, BuildListenerCollection handlers)
		{
			_queues = queues;
			_handlers = handlers;
			_pumpThread = null;
			_stop = false;
		}

		public void Start() {
			if (_pumpThread == null) {
				_stop = false;
				_pumpThread = new Thread(new ThreadStart(PumpEvents));
				_pumpThread.Start();
			}
		}

		public void Stop() {
			if (_pumpThread != null) {
				_stop = true;
				_pumpThread.Join();
			}
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
			}
		}
	}
}
