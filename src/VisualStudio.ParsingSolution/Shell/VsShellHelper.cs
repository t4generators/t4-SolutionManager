#pragma warning disable 3001
#pragma warning disable 3002

using System;
using System.ComponentModel.Design;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Design;
using VisualStudio.ParsingSolution.Projects.Codes;
using VisualStudio.ParsingSolution;
using System.Linq;
using System.Text.RegularExpressions;

namespace VisualStudio.ParsingSolution.Shell
{
    /// <summary>
    /// Helper for Visual Studio (from Daniel Cazzulino Blog)
    /// </summary>
    public static class VsShellHelper
    {

        /// <summary>
        /// Get the current Hierarchy
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IVsHierarchy GetCurrentHierarchy(System.IServiceProvider provider)
        {
            DTE vs = (DTE)provider.GetService(typeof(DTE));
            if (vs == null) throw new InvalidOperationException("DTE not found.");

            return ToHierarchy(vs.SelectedItems.Item(1).ProjectItem.ContainingProject);
        }


        /// <summary>
        /// Get the hierarchy corresponding to a Project
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static IVsHierarchy ToHierarchy(EnvDTE.Project project)
        {
            if (project == null) throw new ArgumentNullException("project");

            string projectGuid = null;

            // DTE does not expose the project GUID that exists at in the msbuild project file.
            // Cannot use MSBuild object model because it uses a static instance of the Engine, 
            // and using the Project will cause it to be unloaded from the engine when the 
            // GC collects the variable that we declare.
            using (XmlReader projectReader = XmlReader.Create(project.FileName))
            {
                projectReader.MoveToContent();
                object nodeName = projectReader.NameTable.Add("ProjectGuid");
                while (projectReader.Read())
                {
                    if (Object.Equals(projectReader.LocalName, nodeName))
                    {
                        //   projectGuid = projectReader.ReadContentAsString();
                        projectGuid = projectReader.ReadElementContentAsString();
                        break;
                    }
                }
            }

            Debug.Assert(!String.IsNullOrEmpty(projectGuid));

            System.IServiceProvider serviceProvider = new ServiceProvider(project.DTE as
                Microsoft.VisualStudio.OLE.Interop.IServiceProvider);

            return VsShellUtilities.GetHierarchy(serviceProvider, new Guid(projectGuid));
        }


        /// <summary>
        /// Get a IVsProject3 from a project
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static IVsProject3 ToVsProject(this EnvDTE.Project project)
        {
            if (project == null) throw new ArgumentNullException("project");

            IVsProject3 vsProject = ToHierarchy(project) as IVsProject3;

            if (vsProject == null)
            {
                throw new ArgumentException("Project is not a VS project.");
            }

            return vsProject;
        }

        /// <summary>
        /// Get a Project from a hierarchy
        /// </summary>
        /// <param name="hierarchy"></param>
        /// <returns></returns>
        public static EnvDTE.Project ToDteProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null) throw new ArgumentNullException("hierarchy");

            object prjObject = null;
            if (hierarchy.GetProperty(0xfffffffe, -2027, out prjObject) >= 0)
            {
                return (EnvDTE.Project)prjObject;
            }
            else
            {
                throw new ArgumentException("Hierarchy is not a project.");
            }
        }

        /// <summary>
        /// Get a EnvDTE.Project from a IVsProject
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static EnvDTE.Project ToDteProject(Microsoft.VisualStudio.Shell.Interop.IVsProject project)
        {
            if (project == null)
                throw new ArgumentNullException("project");
            return ToDteProject(project as IVsHierarchy);
        }

        /// <summary>
        /// Retrieving available types in current project and its references (without locking) 
        /// </summary>
        /// <param name="dteProject">Project the types, and referenced types of, we are interested in</param>
        /// <param name="baseType">All the types we are interested in will derive from the <paramref name="baseType"/></param>
        /// <param name="excludeGlobalTypes"></param>
        /// <param name="includePrivate">Include or not the private types</param>
        /// <returns>A dictionnary of Types, by their full name</returns>
        public static Dictionary<string, Type> GetAvailableTypes(this Project dteProject, Type baseType, bool excludeGlobalTypes, bool includePrivate)
        {

            System.IServiceProvider serviceProvider = new ServiceProvider(dteProject.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);

            DynamicTypeService typeService = serviceProvider.GetService(typeof(DynamicTypeService)) as DynamicTypeService;
            Debug.Assert(typeService != null, "No dynamic type service registered.");

            IVsHierarchy hier = VsShellHelper.ToHierarchy(dteProject);
            Dictionary<string, Type> availableTypes = new Dictionary<string, Type>();
            if (hier != null)
            {
                ITypeDiscoveryService discovery = typeService.GetTypeDiscoveryService(hier);

                if (discovery != null)
                    foreach (Type type in discovery.GetTypes(baseType, excludeGlobalTypes))
                    {
                        if (includePrivate || type.IsPublic)
                            if (!availableTypes.ContainsKey(type.FullName))
                            {
                                availableTypes.Add(type.FullName, type);
                            }
                    }
            }
            return availableTypes;
        }

        /// <summary>
        /// Retrieving available types in current project and its references (without locking) 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="includeReferences"></param>
        /// <returns></returns>
        public static Dictionary<string, Type> GetAvailableTypes(this System.IServiceProvider provider, bool includeReferences)
        {
            Dictionary<string, Type> availableTypes = new Dictionary<string, Type>();

            IVsHierarchy hier = VsShellHelper.GetCurrentHierarchy(provider);
            Debug.Assert(hier != null, "No active hierarchy is selected.");

            DynamicTypeService typeService = (DynamicTypeService)provider.GetService(typeof(DynamicTypeService));
            Debug.Assert(typeService != null, "No dynamic type service registered.");

            ITypeDiscoveryService discovery = typeService.GetTypeDiscoveryService(hier);

            foreach (Type type in discovery.GetTypes(typeof(object), includeReferences))
            {
                // We will never allow non-public types selection, as it's terrible practice.
                if (type.IsPublic)
                {
                    if (!availableTypes.ContainsKey(type.FullName))
                    {
                        availableTypes.Add(type.FullName, type);
                    }
                }
            }

            return availableTypes;
        }

        /// <summary>
        /// Gets the available types defined in the code of the solution.
        /// </summary>
        /// <param name="dteProject">The DTE project.</param>
        /// <returns></returns>
        public static IEnumerable<BaseInfo> GetAvailableCodeTypes(this Project dteProject)
        {

            NodeProject node = new NodeProject(dteProject);

            List<NodeItem> files = node.GetItem<NodeItem>().ToList();

            foreach (NodeItem file in files)
                foreach (BaseInfo item in file.GetClassItems())
                    yield return item;

        }

        /// <summary>
        /// Gets the available types defined in the code of the solution.
        /// </summary>
        /// <param name="dteProject">The DTE project.</param>
        /// <returns></returns>
        public static IEnumerable<BaseInfo> GetAvailableCodeTypes(this NodeProject node)
        {

            List<NodeItem> files = node.GetItem<NodeItem>().ToList();

            foreach (NodeItem file in files)
                foreach (BaseInfo item in file.GetClassItems())
                    yield return item;

        }

        public static EnvDTE.ProjectItem FindProjectItem(EnvDTE.Project project, string file)
        {
            return FindProjectItem(project.ProjectItems, file);
        }

        public static EnvDTE.ProjectItem FindProjectItem(EnvDTE.ProjectItems items, string file)
        {
            string atom = file.Substring(0, file.IndexOf("\\") + 1);
            foreach (EnvDTE.ProjectItem item in items)
            {
                //if ( item
                //if (item.ProjectItems.Count > 0)
                if (atom.StartsWith(item.Name))
                {
                    // then step in
                    EnvDTE.ProjectItem ritem = FindProjectItem(item.ProjectItems, file.Substring(file.IndexOf("\\") + 1));
                    if (ritem != null)
                        return ritem;
                }
                if (Regex.IsMatch(item.Name, file))
                {
                    return item;
                }
                if (item.ProjectItems.Count > 0)
                {
                    EnvDTE.ProjectItem ritem = FindProjectItem(item.ProjectItems, file.Substring(file.IndexOf("\\") + 1));
                    if (ritem != null)
                        return ritem;
                }
            }
            return null;
        }

        public static List<EnvDTE.ProjectItem> FindProjectItems(EnvDTE.ProjectItems items, string match)
        {
            List<EnvDTE.ProjectItem> values = new List<EnvDTE.ProjectItem>();

            foreach (EnvDTE.ProjectItem item in items)
            {
                if (Regex.IsMatch(item.Name, match))
                {
                    values.Add(item);
                }
                if (item.ProjectItems.Count > 0)
                {
                    values.AddRange(FindProjectItems(item.ProjectItems, match));
                }
            }
            return values;
        }

    }
}

