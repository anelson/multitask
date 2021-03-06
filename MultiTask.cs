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
        const String DEFAULT_MULTITASK_TARGET_NAME = "multitask-generated-target";

        String _taskName;

        [TaskAttribute("name")]
        public String TaskName {
            get { return _taskName; }
            set { _taskName = value; }
        }

		/// <summary>
		/// Automatically exclude build elements that are defined on the task
		/// from things that get executed, as they are evaluated normally during
		/// XML task initialization.
		/// </summary>
		/// <param name="taskNode"><see cref="XmlNode" /> used to initialize the container.</param>
		protected override void InitializeTask(XmlNode taskNode) {
			base.InitializeTask(taskNode);
		}


		/// <summary>
		/// Creates and executes the embedded (child XML nodes) elements.
		/// </summary>
		/// <remarks>
		/// Skips any element defined by the host <see cref="Task" /> that has
		/// a <see cref="BuildElementAttribute" /> defined.
		/// </remarks>
		protected override void ExecuteTask() {
			MultiTasks mt = FindMultiTasksAncestor();

            String taskName = mt.GenerateMultitaskName(_taskName == null ? DEFAULT_MULTITASK_TARGET_NAME : _taskName);

            Project forkedProj = Fork(taskName);

            mt.RunProject(taskName, forkedProj, taskName);
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

			throw new BuildException("multitask cannot be used outside of multitasks", Location);
		}

		private Project Fork(String multitaskTargetName) {			
			//Duplicate the current project in a new Project object
			Project forkedProj = CreateForkedProject();

            Target targ = CreateMultitaskTarget(forkedProj, multitaskTargetName);

			SetDefaultTarget(targ, forkedProj);

            return forkedProj;
		}

		private Project CreateForkedProject() {
			//HACK: Had to copy these consts from Project.cs in the nant codebase, 
			//as they are marked 'internal' and thus not visible outside nant.core
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
			Project forkedProj = new Project(Project.BuildFileUri.AbsoluteUri, 
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

			return forkedProj;
		}

        private Target CreateMultitaskTarget(Project proj, String multitaskTargetName) {
			//Create a new target which will contain the tasks from the <multitask>
			//To preserve line numbers as much as possible, build the Target object
			//from the XmlNode for this task; it shouldn't work, but it does, since
			//the Initialize() method for each NAnt element assumes the node being passed to it
			//is the right type.

			//Change the 'name' property so the 'target' will be identifiable
            XmlAttribute name = XmlNode.Attributes["name"];
            String oldName = null;
            if (name == null) {
                name = XmlNode.OwnerDocument.CreateAttribute("name");
            } else {
                oldName = name.Value;
            }

            try {
                name.Value = multitaskTargetName;
                XmlNode.Attributes.Append(name);

                Target targ = new Target();
                targ.Project = proj;
                targ.NamespaceManager = NamespaceManager;
                targ.Initialize(XmlNode);

                return targ;
            } finally {
                //Restore the previous name
                if (oldName == null) {
                    XmlNode.Attributes.Remove(name);
                } else {
                    name.Value = oldName;
                }
            }

		} 

		private void SetDefaultTarget(Target targ, Project proj) {
			proj.Targets.Add(targ);

            //Replace any existing build targets with this target
            proj.BuildTargets.Clear();
            proj.BuildTargets.Add(targ.Name);
		}
    }
}
