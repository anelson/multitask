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
    [TaskName("multitask")]
    class MultiTask : TaskContainer {
        /// <summary>The thread on which the async execution of the tasks within the multitask.</summary>
		Thread _execThread;
        Project _forkedProj;
        const String MULTITASK_TARGET_NAME = "multitask-generated-target";

		[ThreadStatic]
		static DeepCopier _deepCopier = new DeepCopier(new Type[] {
			typeof(MultiTasks)
		});

        /// <summary>Blocks the caller until the multitask finishes running.  Returns immediately
        ///     if the multitask is not currently running.</summary>
		internal void WaitForCompletion() {
			if (_execThread != null) {
				if (_execThread.IsAlive) {
					_execThread.Join();
				}

				_execThread = null;
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

			_execThread = null;
			_forkedProj = null;
		}


		/// <summary>
		/// Creates and executes the embedded (child XML nodes) elements.
		/// </summary>
		/// <remarks>
		/// Skips any element defined by the host <see cref="Task" /> that has
		/// a <see cref="BuildElementAttribute" /> defined.
		/// </remarks>
		protected override void ExecuteTask() {
			//Instead of executing the child tasks synchronously, asynchronously 
			//invoke ExecuteForkedProject using a background thread.

			//First, find the <multitasks> element containing this element
			MultiTasks mt = FindMultiTasksAncestor();
			if (mt == null) {
				throw new BuildException("multitask cannot be used outside of multitasks", Location);
			}

			_forkedProj = Fork();			

			//Inform the multitasks task that a multitask child is about to run
			mt.ReportChildMultiTask(this);

			//And start the thread
			_execThread = new Thread(new ThreadStart(ExecuteForkedProject));
			_execThread.Start();
			//ExecuteForkedProject();
		}

		private void ExecuteForkedProject() {
            _forkedProj.Execute(MULTITASK_TARGET_NAME);
		}

        /// <summary>Walks the task ancestry looking for the nearest <code>multitasks</code> task.</summary>
        /// 
        /// <returns></returns>
		private MultiTasks FindMultiTasksAncestor() {
			Element elem = this;

            while (elem != null) {
                if (elem.Parent is MultiTasks)
                {
                    return (MultiTasks)elem.Parent;
				}

                elem = elem.Parent as Element;
			}

			return null;
		}

		private Project Fork() {
			//HACK: Had to copy these consts from Project.cs in the nant codebase, 
			//as they are marked 'internal' and thus not visible outside nant.core
			const string NAntPlatform = "nant.platform";
			const string NAntPlatformName = NAntPlatform + ".name";
			const string NAntPropertyFileName = "nant.filename";
			const string NAntPropertyVersion = "nant.version";
			const string NAntPropertyLocation = "nant.location";
			const string NAntPropertyProjectName = "nant.project.name";
			const string NAntPropertyProjectBuildFile = "nant.project.buildfile";
			const string NAntPropertyProjectBaseDir = "nant.project.basedir";
			const string NAntPropertyProjectDefault = "nant.project.default";
			const string NAntPropertyOnSuccess = "nant.onsuccess";
			const string NAntPropertyOnFailure = "nant.onfailure";
			
			//Duplicate the current project in a new Project object
			Project forkedProj = new Project(Project.Document, 
											 Project.Threshold, 
											 Project.IndentationLevel, 
											 Project.ConfigurationNode);

			

            // have the new project inherit the runtime framework from the 
            // current project
            if (Project.RuntimeFramework != null && forkedProj.Frameworks.Contains(Project.RuntimeFramework.Name)) {
                forkedProj.RuntimeFramework = forkedProj.Frameworks[Project.RuntimeFramework.Name];
            }

            // have the new project inherit the current framework from the 
            // current project 
            if (Project.TargetFramework != null && forkedProj.Frameworks.Contains(Project.TargetFramework.Name)) {
                forkedProj.TargetFramework = forkedProj.Frameworks[Project.TargetFramework.Name];
            }
			
            // have the new project inherit properties from the current project
			StringCollection excludes = new StringCollection();
			excludes.Add(NAntPropertyFileName);
			excludes.Add(NAntPropertyLocation);
			excludes.Add(NAntPropertyOnSuccess);
			excludes.Add(NAntPropertyOnFailure);
			excludes.Add(NAntPropertyProjectBaseDir);
			excludes.Add(NAntPropertyProjectBuildFile);
			excludes.Add(NAntPropertyProjectDefault);
			excludes.Add(NAntPropertyProjectName);
			excludes.Add(NAntPropertyVersion);
			forkedProj.Properties.Inherit(Properties, excludes);

			// pass datatypes thru to the child project
			forkedProj.DataTypeReferences.Inherit(Project.DataTypeReferences);

			//Create a new target which will contain the tasks from the <multitask>
			//To preserve line numbers as much as possible, copy the <multitask> branch
			//and mutate the multitask element into a task element

			XmlNode targetNode = XmlNode.CloneNode(true);
            XmlAttribute name = targetNode.OwnerDocument.CreateAttribute("name");
            name.Value = MULTITASK_TARGET_NAME;
            targetNode.Attributes.Append(name);

			Target targ = new Target();
			targ.Project = forkedProj;
			targ.NamespaceManager = NamespaceManager;
            targ.Initialize(targetNode);

			forkedProj.Targets.Add(targ);

            return forkedProj;
		}
    }
}
