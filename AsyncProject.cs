using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace MultiTask
{
	/// <summary>
	/// A Nant <code>Project</code>, running asynchronously
	/// </summary>
	internal class AsyncProject
	{
		Project _project;
		String _target;
		Thread _thread;
		ManualCloseLogEventQueue _logEventQueue;
		Exception _exception;

		public AsyncProject(Project proj, String target, ManualCloseLogEventQueue q)
		{
			_project = proj;
			_target = target;
			_thread = null;
			_logEventQueue = q;
		}

		public void Start() {
			if (_thread == null) {
				//And start the thread
				_exception = null;
				_thread = new Thread(new ThreadStart(ExecuteProjectAsync));
				_thread.Start();
			}
		}

		public Exception WaitForFinish() {
			if (_thread != null) {
				_thread.Join();
				_thread = null;
			}

			return _exception;
		}

		public bool IsRunning {
			get {
				return _thread != null && _thread.IsAlive;
			}
		}

		private void ExecuteProjectAsync() {
			//Wire up handlers to hook the project events, and insert them
			//into the log event queue, which will be monitored by
			//the main execution thread and pumped into the 
			_logEventQueue.Install(_project, _target);
			try {
				_project.Execute(_target);
			} catch (Exception e) {
				_exception = e;
			} finally {
				_logEventQueue.Close();
			}
		}
	}
}
