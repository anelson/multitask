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
        String _logSrcName;
		Project _project;
		String _target;
		Thread _thread;
		ManualCloseLogEventQueue _logEventQueue;

		public AsyncProject(String logSrcName, Project proj, String target, ManualCloseLogEventQueue q)
		{
            _logSrcName = logSrcName;
			_project = proj;
			_target = target;
			_thread = null;
			_logEventQueue = q;
		}

		public void Start() {
			if (_thread == null) {
				//And start the thread
				_thread = new Thread(new ThreadStart(ExecuteProjectAsync));
				_thread.Start();
			}
		}

		public void WaitForFinish() {
			if (_thread != null) {
				_thread.Join();
				_thread = null;
			}
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
				_project.Execute();
			} catch (Exception e) {
				_logEventQueue.Enqueue(new LogEvent(_logSrcName, _project, e));
			} finally {
				_logEventQueue.Close();
			}
		}
	}
}
