using System;

namespace MultiTask {
	/// <summary>
	/// A log event queue which does not automatically close when another queue is placed behind it
	/// </summary>
	internal class ManualCloseLogEventQueue : LogEventQueueBase {
		public ManualCloseLogEventQueue(String src) : base(src) {
		}

		public override bool AutoClose {
			get {
				return false;
			}
		}

	}
}
