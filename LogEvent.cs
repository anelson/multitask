using System;

using NAnt.Core;

namespace MultiTask
{
	internal enum LogEventType {
		TargetFinished,
		MessageLogged,
		BuildStarted,
		BuildFinished,
		TaskFinished,
		TargetStarted,
		TaskStarted
	};
	
	/// <summary>
	/// Summary description for LogEvent.
	/// </summary>
	internal class LogEvent
	{
		Object _sender;
		BuildEventArgs _args;
		LogEventType _eventType;

		public LogEvent(Object sender, BuildEventArgs args, LogEventType eventType)
		{
			_sender = sender;
			_args = args;
			_eventType = eventType;
		}

		public Object Sender { get { return _sender; } }
		public BuildEventArgs Args { get { return _args; } }
		public LogEventType EventType { get { return _eventType; } }
	}
}
