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
        LogEventQueueList _logEventQueueList;
		ArrayList _asyncProjects;
		LogEventPump _eventPump;
        ArrayList _multitaskNames;
        bool _serialize;

        /// <summary>
        /// Forces child &lt;multitask&gt; elements to run sequentially instead of concurrently.
        /// This is equivalent to omitting the multitasks/multitask elements and calling each task 
        /// sequentially in the build file.
        /// 
        /// Defaults to <c>false</c>; set to <c>true</c> during testing and when troubleshooting, to 
        /// eliminate threading complications as a potential cause.
        /// </summary>
        [TaskAttribute("serialize")]
        [BooleanValidator()]
        public bool Serialize
        {
            get { return _serialize; }
            set { _serialize = value; }
        }

        internal String GenerateMultitaskName(String baseName) {
            //If the base name is not already in the list, add it then return it
            if (!_multitaskNames.Contains(baseName)) {
                _multitaskNames.Add(baseName);
                return baseName;
            }

            //Else, append a decimal number to make a unique name
            int counter = 1;
            while (_multitaskNames.Contains(String.Format("{0} ({1})", baseName, counter))) {
                counter++;
            }

            _multitaskNames.Add(String.Format("{0} ({1})", baseName, counter));
            return String.Format("{0} ({1})", baseName, counter);
        }

		internal void RunProject(String threadName, Project proj, String targetName) {
            ManualCloseLogEventQueue q = new ManualCloseLogEventQueue(threadName);
            AsyncProject ap = new AsyncProject(threadName, proj, targetName, q);

            _logEventQueueList.Add(q);
            _asyncProjects.Add(ap);

            ap.Start();

            InstallNewAutoCloseEventListener();

            if (_serialize) {
                ap.WaitForFinish();
            }
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
            _multitaskNames = new ArrayList();
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
				WaitForAsyncProjects();

				//Wait for the event pump to finish
				StopEventPump();
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
		private void WaitForAsyncProjects() {
			foreach (AsyncProject ap in _asyncProjects) {
				ap.WaitForFinish();
			}
		} 

		private void StartEventPump(BuildListenerCollection handlers) {
			_eventPump = new LogEventPump(_logEventQueueList, handlers);
			_eventPump.Start();
		}

		private void StopEventPump() {
			_logEventQueueList.Flush();

			if (_eventPump != null) {
				Exception e = _eventPump.Stop();
				_eventPump = null;

				if (e != null) {
					if (e is BuildException) {
						throw new BuildException(e.Message, 
							e.InnerException);
					} else {
						throw new BuildException("One or more errors occurred in the <multitask> threads; check log output above this line for details.  The first (and possibly only) error encountered was:",
							Location,
							e);
					}
				}
			}
		}
    }
}

