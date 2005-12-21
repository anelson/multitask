using System;
using System.Collections;
using System.Text;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace MultiTask
{
    [TaskName("multitasks")]
    class MultiTasks : TaskContainer {
//		ArrayList _childMultiTasks;
        LogEventQueueList _logEventQueueList;
		ArrayList _asyncProjects;
		LogEventPump _eventPump;

//        /// <summary>Called by a MultiTask when it is about to run, to register itself with its parent
//        ///     MultiTasks element.</summary>
//        /// 
//        /// <param name="child"></param>
//		internal LogEventQueue ReportChildMultiTask(MultiTask child) {
//			LogEventQueue q = new LogEventQueue(child);
//
//			_childMultiTasks.Add(child);
//			_logEventQueueList.Add(q);
//
//			return q;
//		}

		internal void RunProjectAsync(String threadName, Project proj, String targetName) {
			ManualCloseLogEventQueue q = new ManualCloseLogEventQueue(threadName);
			AsyncProject ap = new AsyncProject(proj, targetName, q);

			_logEventQueueList.Add(q);
			_asyncProjects.Add(ap);

			ap.Start();

			InstallNewAutoCloseEventListener();
		}

		/// <summary>
		/// Automatically exclude build elements that are defined on the task
		/// from things that get executed, as they are evaluated normally during
		/// XML task initialization.
		/// </summary>
		/// <param name="taskNode"><see cref="XmlNode" /> used to initialize the container.</param>
		protected override void InitializeTask(XmlNode taskNode) {
			base.InitializeTask(taskNode);

            _logEventQueueList = new LogEventQueueList();
			_asyncProjects = new ArrayList();
		}

		/// <summary>
		/// Creates and executes the embedded (child XML nodes) elements.
		/// </summary>
		/// <remarks>
		/// Skips any element defined by the host <see cref="Task" /> that has
		/// a <see cref="BuildElementAttribute" /> defined.
		/// </remarks>
		protected override void ExecuteChildTasks() {
			BuildListenerCollection prevBuildListeners = new BuildListenerCollection(Project.BuildListeners);
			Project.DetachBuildListeners();

			try {
				_logEventQueueList.Clear();
				_asyncProjects.Clear();

				StartEventPump(prevBuildListeners);

				InstallNewAutoCloseEventListener();
			
				//Leave the actual execution to the base class
				//Any errors during the build will throw an exception here
				try {
					base.ExecuteChildTasks();
				} catch {
					//Wait for anything running in a child <multitask> element to 
					//finish
					WaitForAsyncProjects();

					//Wait for the event pump to finish
					StopEventPump();

					throw;
				}

				//Wait for anything running in a child <multitask> element to 
				//finish
				bool success = WaitForAsyncProjects();

				//Wait for the event pump to finish
				StopEventPump();

				if (!success) {
					throw new BuildException("There was an error in a <multitask>", Location);
				}

			} finally {
				Project.AttachBuildListeners(prevBuildListeners);
			}
		}

		private void InstallNewAutoCloseEventListener() {
			AutoCloseLogEventQueue q = new AutoCloseLogEventQueue(null);
			_logEventQueueList.Add(q);
			q.Install(Project, null);
		}

        /// <summary>Pauses until all async projects finish executing.</summary>
		private bool WaitForAsyncProjects() {
			bool success = true;

			foreach (AsyncProject ap in _asyncProjects) {
				Exception error = ap.WaitForFinish();
				if (error != null) {
					success = false;

					Log(Level.Error, "Error in <multitask>: {0}",
						error);
				}
			}

			return success;
		} 

		private void StartEventPump(BuildListenerCollection handlers) {
			_eventPump = new LogEventPump(_logEventQueueList, handlers);
			_eventPump.Start();
		}

		private void StopEventPump() {
			_logEventQueueList.Flush();

			if (_eventPump != null) {
				_eventPump.Stop();
				_eventPump = null;
			}
		}
    }
}

