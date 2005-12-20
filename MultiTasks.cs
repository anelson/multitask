using System;
using System.Collections.Generic;
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
		List<MultiTask> _childMultiTasks;
        LogEventQueueList _logEventQueueList;

        /// <summary>Called by a MultiTask when it is about to run, to register itself with its parent
        ///     MultiTasks element.</summary>
        /// 
        /// <param name="child"></param>
		internal void ReportChildMultiTask(MultiTask child) {
			_childMultiTasks.Add(child);
		}


		/// <summary>
		/// Automatically exclude build elements that are defined on the task
		/// from things that get executed, as they are evaluated normally during
		/// XML task initialization.
		/// </summary>
		/// <param name="taskNode"><see cref="XmlNode" /> used to initialize the container.</param>
		protected override void InitializeTask(XmlNode taskNode) {
			base.InitializeTask(taskNode);

			_childMultiTasks = new List<MultiTask>();
            _logEventQueueList = new LogEventQueueList();
		}

		/// <summary>
		/// Creates and executes the embedded (child XML nodes) elements.
		/// </summary>
		/// <remarks>
		/// Skips any element defined by the host <see cref="Task" /> that has
		/// a <see cref="BuildElementAttribute" /> defined.
		/// </remarks>
		protected override void ExecuteChildTasks() {
			_childMultiTasks.Clear();
            _logEventQueueList.Clear();
			
			//Leave the actual execution to the base class
			base.ExecuteChildTasks();

			//Wait for anything running in a child <multitask> element to 
			//finish
			WaitForMultiTasks();
		}

        /// <summary>Pauses until all tasks running in a child &lt;multitask&gt; finish executing.</summary>
		private void WaitForMultiTasks() {
			foreach (MultiTask mt in _childMultiTasks) {
				mt.WaitForCompletion();
			}
		}
    }
}

