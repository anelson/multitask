using System;

namespace MultiTask
{
	/// <summary>
	/// A log event queue which automatically closes when another queue is placed behind it
	/// </summary>
	internal class AutoCloseLogEventQueue : LogEventQueueBase {
		public AutoCloseLogEventQueue(String src) : base(src) {
		}

		public override bool AutoClose {
			get {
				return true;
			}
		}

	}
}
