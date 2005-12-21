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
		TaskStarted,
		Exception
	};
	
	/// <summary>
	/// Summary description for LogEvent.
	/// </summary>
	internal class LogEvent
	{
		Object _sender;
		BuildEventArgs _args;
		LogEventType _eventType;
		Exception _exception;
		Project _project;

		public LogEvent(Object sender, BuildEventArgs args, LogEventType eventType)
		{
			_sender = sender;
			_args = args;
			_eventType = eventType;
			_exception = null;
		}

		public LogEvent(Project project, Exception exception) : this(project, null, LogEventType.Exception) {
			_project = project;
			_exception = exception;
		}

		public Object Sender { get { return _sender; } }
		public BuildEventArgs Args { get { return _args; } }
		public LogEventType EventType { get { return _eventType; } }
		public Exception Exception { get { return _exception; } }
		public Project Project { get { return _project; } }
	}
}
