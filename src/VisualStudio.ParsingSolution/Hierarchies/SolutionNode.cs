using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE80;
using System.IO;
using System.Collections.Generic;
using VsxFactory.Modeling.VisualStudio.Synchronization;
using System.Linq;
using System.Runtime.InteropServices;

namespace VsxFactory.Modeling.VisualStudio
{
    /// <summary>
    /// 
    /// </summary>
    public class SolutionNode : ProjectNode
    {
        public const string SolutionItemsName = "Solution Items";

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionNode"/> class.
        /// </summary>
        /// <param name="solution">The solution.</param>
        public SolutionNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution solution)
            : base(solution)
        {
        }

        /// <summary>
        /// Creates the project.
        /// </summary>
        /// <param name="solutionManager"></param>
        /// <param name="solutionFolderPath"></param>
        /// <param name="name">The name.</param>
        /// <param name="template">The template.</param>
        /// <param name="defaultTemplate"></param>
        /// <returns></returns>
        public ProjectNode CreateProject(IVsSolutionExplorer solutionManager, string projectTemplatesFolder, string solutionFolderPath, string name, string assemblyName, string template, string defaultNamespace, string solutionName)
        {
            Guard.ArgumentNotNull(solutionManager, "solutionManager");
            Guard.ArgumentNotNullOrEmptyString(name, "name");
            Guard.ArgumentNotNullOrEmptyString(template, "template");
            Guard.ArgumentNotNullOrEmptyString(solutionName, "solutionName");

            Solution2 sln = this.ExtObject as Solution2;
            if (sln == null)
                return null; 
            
            var projectName = name;
            var projectDirectory = name;
            name = name.Replace(@"\", "/");
            var pos = name.LastIndexOf('/');
            if (pos >= 0)
            {
                projectDirectory = name.Substring(0, pos);
                projectName = name.Substring(pos + 1);
            }

            if (projectName.Length == 0)
                throw new Exception("Invalid project name");

            //var node = RecursiveFindByName(projectName);
            //if (node != null && node.IsProject)
            //    return node.CastToProjectNode();

            string templatePath = template;

            try
            {
                // TODO using (ProjectEventsDisabler disabler = new ProjectEventsDisabler(solutionManager, name))
                {
                    EnvDTE.Project p;
                    string projectFolder = System.IO.Path.Combine(ProjectDir, projectDirectory);
                    HierarchyNode hierarchyNode = RecursiveFindByName(projectName);
                    ProjectNode projectNode = null;
                    if (hierarchyNode != null && hierarchyNode.IsProject)
                        projectNode = hierarchyNode.CastToProjectNode();

                    if (projectNode == null)
                    {
                        // --------------------------------------------------------------
                        // Résolution du template

                        // Si le chemin n'est pas absolu
                        if (!System.IO.Path.IsPathRooted(templatePath))
                        {
                            // Recherche dans les templates VisualFabric
                            templatePath = projectTemplatesFolder != null ? System.IO.Path.Combine(projectTemplatesFolder, template) : template;
                            if (!File.Exists(templatePath))
                            {
                                // Recherche d'abord dans les templates de l'utilisateur
                                templatePath = System.IO.Path.Combine(solutionManager.GetExportedProjectTemplatesDir(), template);
                                if (!File.Exists(templatePath))
                                {
                                    // Sinon dans les templates standards
                                    string wizardName = template.Replace("\\", "/");

                                    string lang = null;
                                    pos = wizardName.LastIndexOf('/');
                                    if (pos >= 0)
                                    {
                                        lang = wizardName.Substring(0, pos);
                                        wizardName = wizardName.Substring(pos + 1);
                                    }
                                    try
                                    {
                                        templatePath = sln.GetProjectTemplate(wizardName, lang);
                                    }
                                    catch
                                    {
                                        throw new Exception(String.Format("Incorrect template name {0}", template));
                                    }
                                }
                            }
                        }
                        
                        // Décompression du fichier dans un répertoire temporaire
                        templatePath = ExtractProjectItemInTemporaryFolder(templatePath, projectName, defaultNamespace, solutionName);
                        if (templatePath == null || !File.Exists(templatePath))
                            throw new Exception("Invalid project template " + template);

                        if (!String.IsNullOrEmpty(solutionFolderPath))
                        {
                            EnvDTE80.SolutionFolder container = CreateSolutionFolder(solutionFolderPath).Object as EnvDTE80.SolutionFolder;
                            container.AddFromTemplate(templatePath, projectFolder, projectName);
                            p = container.Parent.ProjectItems.Item(container.Parent.ProjectItems.Count).Object as EnvDTE.Project;
                        }
                        else
                        {
                            Utils.RemoveDirectory(projectFolder);
                            sln.AddFromTemplate(templatePath, projectFolder, projectName, false);
                            p = sln.Projects.Item(sln.Projects.Count);
                        }
                    }
                    else
                    {
                        p = projectNode.Project;
                    }

                    if (p != null)
                    {
                        if (!String.IsNullOrEmpty(defaultNamespace))
                        {
                            try
                            {
                                p.Properties.Item("DefaultNamespace").Value = defaultNamespace;
                            }
                            catch { /* projet web */ }
                        }
                        if (!String.IsNullOrEmpty(assemblyName))
                        {
                            try
                            {
                                p.Properties.Item("AssemblyName").Value = assemblyName;
                            }
                            catch { /* projet web */ }
                        }
                    }

                    return projectNode ?? new ProjectNode(Solution, p.UniqueName);
                }
            }
            finally
            {
                Utils.RemoveDirectory(System.IO.Path.GetDirectoryName(templatePath));
            }
        }

        /// <summary>
        /// Creates the solution folder.
        /// </summary>
        /// <param name="solutionFolderPath">The solution folder path.</param>
        /// <returns></returns>
        private EnvDTE.Project CreateSolutionFolder(string solutionFolderPath)
        {
            Guard.ArgumentNotNullOrEmptyString(solutionFolderPath, "solutionFolderPath");

            EnvDTE80.Solution2 sln = ExtObject as EnvDTE80.Solution2;
            EnvDTE.Project folderNode = null;

            string[] folders = solutionFolderPath.Split(System.IO.Path.DirectorySeparatorChar);
            foreach( var folder in folders )
            {
                if( folderNode == null )
                {
                    foreach( EnvDTE.Project p in sln.Projects )
                    {
                        if( String.Compare(p.Name, folder,  StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            folderNode = p;
                            break;
                        }
                    }
                    if( folderNode == null )
                        folderNode = sln.AddSolutionFolder(folder);
                }
                else
                {
                    EnvDTE.Project tmp = null;
                    foreach( EnvDTE.ProjectItem pi in folderNode.ProjectItems )
                    {
                        if( String.Compare( pi.Name, folder,  StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            tmp = pi.Object as EnvDTE.Project;
                            break;
                        }
                    }

                    if( tmp == null )
                    {
                        EnvDTE80.SolutionFolder sf = folderNode.Object as EnvDTE80.SolutionFolder;
                        folderNode = sf.AddSolutionFolder(folder);
                    }
                }
            }

            return folderNode;
        }

        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <param name="uniqueName">Name of the unique.</param>
        /// <returns></returns>
        public ProjectNode GetProject(string uniqueName)
        {
            try
            {
                return new ProjectNode(this.Solution, uniqueName);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <param name="projectGuid">The project GUID.</param>
        /// <returns></returns>
        public ProjectNode GetProject(Guid projectGuid)
        {
            try
            {
                return projectGuid == Guid.Empty ? null : new ProjectNode(this.Solution, projectGuid);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public ProjectNode GetProject(HierarchyNode node)
        {
            return GetProject(node.ProjectGuid);
        }

        /// <summary>
        /// Gets all projects.
        /// </summary>
        /// <value>All projects.</value>
        public IEnumerable<ProjectNode> AllProjects
        {
            get
            {
                foreach (var item in Children)
                {
                    if (item.IsProject)
                        yield return GetProject(item);
                }
            }
        }

        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <returns></returns>
        public ProjectNode GetProject(IVsHierarchy hierarchy)
        {
            try
            {
                return new ProjectNode(this.Solution, hierarchy);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Gets the project output path.
        /// </summary>
        /// <param name="projectGuid">The project GUID.</param>
        /// <returns></returns>
        public string GetProjectOutputFileName(Guid projectGuid)
        {
            ProjectNode pn = GetProject(projectGuid);
            return pn != null ? pn.OutputFileName : null;
        }

        /// <summary>
        /// Adds the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public override HierarchyNode AddFile(string fileName, byte[] content)
        {
            string subFolder;
            FileInfo fileInfo = EnsureFileExists(fileName, content, out subFolder);
            fileInfo.Attributes = FileAttributes.Normal;
            return AddItem(fileName);
        }

        /// <summary>
        /// Adds a new item in the solution items folder
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override HierarchyNode AddItem(string name, byte[] content=null)
        {
            var folder = CreateSolutionFolder(SolutionItemsName);
            string subFolder;
            FileInfo fileInfo = EnsureFileExists(name, content, out subFolder);
            var node = RecursiveFindByPath(fileInfo.FullName);
            if (node != null)
                return node;

            EnvDTE.ProjectItem pi = folder.ProjectItems.AddFromFile(fileInfo.FullName);
            node = RecursiveFindByPath(fileInfo.FullName);
            return node;
        }

        /// <summary>
        /// Finds the or create solution items.
        /// </summary>
        /// <returns></returns>
        public ProjectNode FindOrCreateSolutionItems()
        {
            return new ProjectNode(Solution, CreateSolutionFolder(SolutionItemsName).UniqueName);
        }

        /// <summary>
        /// Returns the solution directory
        /// </summary>
        /// <value></value>
        public override string ProjectDir
        {
            get
            {
                string solutionDirectory;
                string solutionFile;
                string solutionOpts;
                Solution.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionOpts);
                return solutionDirectory;
            }
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        public override bool Save()
        {
            unchecked
            {
                return Solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, null, (uint)__VSHPROPID.VSHPROPID_NIL) == VSConstants.S_OK;
            }
        }

        /// <summary>
        /// Checks the project exists.
        /// </summary>
        /// <param name="projectGuid">The project GUID.</param>
        /// <returns></returns>
        public bool CheckProjectExists(Guid projectGuid)
        {
            if (projectGuid == Guid.Empty)
                return false;
            return  GetProject(projectGuid) != null;
        }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public override IServiceProvider ServiceProvider
        {
            get
            {              
               return new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)((EnvDTE.Solution)this.ExtObject).DTE);
            }
        }
    }
}
