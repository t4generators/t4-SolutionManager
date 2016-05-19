using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.Globalization;
using System.Runtime.InteropServices;
using VSLangProj;
using System.Xml;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace VsxFactory.Modeling.VisualStudio
{
    public class ProjectNode : HierarchyNode
    {
        /// <summary>
        /// Visual Studio Project
        /// </summary>
        private Microsoft.VisualStudio.Shell.Interop.IVsProject project;
        private ProjectKind _kind = ProjectKind.NotDefined;

        internal VSProject VSProject
        {
            get { return Project.Object as VSProject; }
        }

        internal Microsoft.VisualStudio.Shell.Interop.IVsProject IVsProject
        {
            get { return project; }
        }

        /// <summary>
        /// Gets the VS project.
        /// </summary>
        /// <value>The VS project.</value>
        public EnvDTE.Project Project
        {
            get
            {
                return ExtObject as EnvDTE.Project;
            }
        }

        public string Namespace
        {
            get
            {
                EnvDTE.Project prj = ((VSLangProj.VSProject)Project.Object).Project;
                return (string)prj.Properties.Item("DefaultNamespace").Value;
            }
            set
            {
                EnvDTE.Project prj = ((VSLangProj.VSProject)Project.Object).Project;
                prj.Properties.Item("DefaultNamespace").Value = value;           
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is referancable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is referancable; otherwise, <c>false</c>.
        /// </value>
        public bool IsReferancable
        {
            get
            {
                if( IsWebProject )
                    return false;

                EnvDTE.Project prj = ( (VSLangProj.VSProject)Project.Object ).Project;
                // Ce n'est pas un executable
                if( prj == null || prj.Properties.Item("OutputType").Value.ToString() != "2" )
                    return false;

                return false;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectNode"/> class.
        /// </summary>
        /// <param name="solution">The solution.</param>
        protected ProjectNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution solution)
            : base(solution)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectNode"/> class.
        /// </summary>
        /// <param name="node">The node.</param>
        internal ProjectNode(HierarchyNode node) : this(node.Solution, node.ProjectGuid)
        {
            Debug.Assert(node.IsProject || node.IsSolution || node.IsFolder);
        }

        /// <summary>
        /// Builds a project node from the project Guid
        /// </summary>
        /// <param name="vsSolution"></param>
        /// <param name="projectGuid"></param>
        public ProjectNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, Guid projectGuid)
            : base(vsSolution, projectGuid)
        {
            this.project = this.Hierarchy as Microsoft.VisualStudio.Shell.Interop.IVsProject;
            // Commented because it will show up an error dialog before getting back control to the recipe (caller)
            //Debug.Assert(project != null);  
            Debug.Assert(ItemId == VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectNode"/> class.
        /// </summary>
        /// <param name="vsSolution">The vs solution.</param>
        /// <param name="uniqueName">Name of the unique.</param>
        public ProjectNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, string uniqueName)
            : base(vsSolution, uniqueName)
        {
            this.project = this.Hierarchy as Microsoft.VisualStudio.Shell.Interop.IVsProject;
            Debug.Assert(ItemId == VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectNode"/> class.
        /// </summary>
        /// <param name="vsSolution">The vs solution.</param>
        /// <param name="hierarchy">The hierarchy.</param>
        public ProjectNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, IVsHierarchy hierarchy)
            : base(vsSolution, hierarchy)
        {
            this.project = this.Hierarchy as Microsoft.VisualStudio.Shell.Interop.IVsProject;
            Debug.Assert(ItemId == VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Builds a project node from the a parent node
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="itemid"></param>
        private ProjectNode(HierarchyNode parent, uint itemid)
            : base(parent, itemid)
        {
            this.project = this.Hierarchy as Microsoft.VisualStudio.Shell.Interop.IVsProject;
            Debug.Assert(project != null);
        }

        /// <summary>
        /// Gets the references.
        /// </summary>
        /// <value>The references.</value>
        public IList<ProjectReference> References
        {
            get
            {
                List<ProjectReference> references = new List<ProjectReference>();

                if (Project != null)
                {
                    SolutionNode parentSolution = new SolutionNode(this.Solution);
                    if (Project.Object is VSProject)
                    {
                        References vsreferences = ((VSProject)Project.Object).References;
                        if (vsreferences != null)
                        {
                            foreach (Reference reference in vsreferences)
                            {
                                ProjectReference rf = ProjectReferenceHelper.CreateVisualStudioReferenceFromReference(parentSolution, reference);
                                if (rf != null)
                                    references.Add(rf);
                            }
                        }
                    }
                    else if (Project.Object is VsWebSite.VSWebSite)
                    {
                        VsWebSite.AssemblyReferences vsreferences = ((VsWebSite.VSWebSite)Project.Object).References;

                        if (vsreferences != null)
                        {
                            foreach (VsWebSite.AssemblyReference reference in vsreferences)
                            {
                                ProjectReference rf = ProjectReferenceHelper.CreateVisualStudioReferenceFromReference(parentSolution, reference);
                                if (rf != null)
                                    references.Add(rf);
                            }
                        }
                    }
                }
                return references;
            }
        }

        /// <summary>
        /// Removes this instance.
        /// </summary>
        public override void Remove()
        {
            Solution.RemoveVirtualProject(Hierarchy, 0);
        }

        /// <summary>
        /// Determines whether this instance [can add item] the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can add item] the specified name; otherwise, <c>false</c>.
        /// </returns>
        public bool CanAddItem(string name)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");
            return IsValidFullPathName(name);
        }

        /// <summary>
        /// Adds the existing project.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        public ProjectNode AddExistingProject(string fullPath)
        {
            Guard.ArgumentNotNullOrEmptyString(fullPath, "fullPath");
            Debug.Assert(IsFolder || IsSolution, "You can add project only on a folder");

            Guid projectType = Guid.Empty;
            Guid iidProject = Guid.Empty;
            IntPtr ppProject;
            int hr = Solution.CreateProject(ref projectType, fullPath, null, null, (uint)__VSCREATEPROJFLAGS.CPF_OPENFILE, ref iidProject, out ppProject);
            if( hr == VSConstants.S_OK )
            {
                return RecursiveFindByPath(fullPath).CastToProjectNode();
            }
            return null;
        }

        public void RemoveItem(string relativeFileName)
        {
            Guard.ArgumentNotNullOrEmptyString(relativeFileName, "relativeFileName");
            if (ProjectDir == null)
                return;
            string fileName = System.IO.Path.Combine(ProjectDir, relativeFileName);
            var item = this.RecursiveFindByPath(fileName);
            if (item != null)
                item.Remove();
            else if (File.Exists(fileName))
                Utils.DeleteFile(fileName);            
        }

        public override void Rename(string name)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");

            EnvDTE.Project pi = this.ExtObject as EnvDTE.Project;
            if (pi != null)
                pi.Name = name;
        }

        public void RenameItem(string oldRelativeFileName, string relativeFileName)
        {
            Guard.ArgumentNotNullOrEmptyString(relativeFileName, "relativeFileName");
            Guard.ArgumentNotNullOrEmptyString(oldRelativeFileName, "oldRelativeFileName");

            string fileName = System.IO.Path.Combine(ProjectDir, oldRelativeFileName);
            var item = this.RecursiveFindByPath(fileName);
            if (item != null)
            {
                item.Rename(System.IO.Path.GetFileName( relativeFileName) );
            }
        }

        /// <summary>
        /// Adds a new item in the project
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        // FXCOP: False positive
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public virtual HierarchyNode AddItem(string name)
        {
            return AddItem(name, null);
        }

        /// <summary>
        /// Adds the item as link.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void AddItemAsLink(string fileName)
        {
            int found;
            uint itemId;
            VSDOCUMENTPRIORITY docPri = VSDOCUMENTPRIORITY.DP_Standard;
            int hr = project.IsDocumentInProject(fileName, out found, new VSDOCUMENTPRIORITY[] { docPri }, out itemId);
            if (found == 0)
            {
                VSADDRESULT result = VSADDRESULT.ADDRESULT_Cancel;
                hr = project.AddItem(ItemId, VSADDITEMOPERATION.VSADDITEMOP_LINKTOFILE,
                  fileName, 1, new[] { fileName }, IntPtr.Zero, new VSADDRESULT[] { result });
                Marshal.ThrowExceptionForHR(hr);
                var node = this.RecursiveFindByPath(fileName);
                EnsureDocumentIsNotInRDT(node);
            }
            return;
        }

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public virtual HierarchyNode AddItem(string name, byte[] content)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");

            if (!CanAddItem(name)) // Verification si le nom est valide
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, VsxFactory.Modeling.Properties.Resources.InvalidFileName, name));
            }

            string subFolder;
            FileInfo fileInfo = EnsureFileExists(name, content, out subFolder);
            uint itemId = VSConstants.VSITEMID_NIL;
            int found = 1;
            VSDOCUMENTPRIORITY docPri = VSDOCUMENTPRIORITY.DP_Standard;
            int hr = project.IsDocumentInProject(fileInfo.FullName, out found, new VSDOCUMENTPRIORITY[] { docPri }, out itemId);
            Marshal.ThrowExceptionForHR(hr);
            if (found == 0)
            {
                VSADDRESULT result = VSADDRESULT.ADDRESULT_Cancel;
                uint folderId = this.ItemId;
                HierarchyNode subFolderNode = (HierarchyNode)FindSubFolder(subFolder);
                if (subFolderNode != null)
                {
                    folderId = subFolderNode.ItemId;
                }
                
                hr = project.AddItem(folderId,
                    VSADDITEMOPERATION.VSADDITEMOP_OPENFILE,
                    fileInfo.Name, 1, new[] { fileInfo.FullName },
                    IntPtr.Zero, new VSADDRESULT[] { result });
                Marshal.ThrowExceptionForHR(hr);
            }
            hr = project.IsDocumentInProject(fileInfo.FullName, out found, new VSDOCUMENTPRIORITY[] { docPri }, out itemId);
            Marshal.ThrowExceptionForHR(hr);
            if (found == 1)
            {
                var node = new HierarchyNode(this, itemId);
                EnsureDocumentIsNotInRDT(node);
                return node;
            }
            return null;
        }

        /// <summary>
        /// Ensures the file exists.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="subFolder">The sub folder.</param>
        /// <returns></returns>
        protected virtual FileInfo EnsureFileExists(string name, byte[] content, out string subFolder)
        {
            FileInfo fileInfo = null;
            subFolder = string.Empty;
            if (System.IO.Path.IsPathRooted(name))
            {
                fileInfo = new FileInfo(name);
            }
            else
            {
                fileInfo = new FileInfo(System.IO.Path.Combine(ProjectDir, name));
            }
            int subFolderIndex = name.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
            if (subFolderIndex != -1)
            {
                subFolder = name.Substring(0, subFolderIndex);
            }

            if (fileInfo.Name.Equals(fileInfo.Extension, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        VsxFactory.Modeling.Properties.Resources.CannotCreateItemWithEmptyName));
            }
            if (!File.Exists(fileInfo.FullName))
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
                File.Create(fileInfo.FullName).Dispose();
            }
            if (content != null)
            {
                File.WriteAllBytes(fileInfo.FullName, content);
            }
            return fileInfo;
        }

        /// <summary>
        /// Finds the sub folder.
        /// </summary>
        /// <param name="subFolder">The sub folder.</param>
        /// <returns></returns>
        public ProjectNode FindSubFolder(string subFolder)
        {
            if (!string.IsNullOrEmpty(subFolder))
            {
                string[] folders = subFolder.Split(new char[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                ProjectNode folderNode = this;
                foreach (string folder in folders)
                {
                    folderNode = folderNode.FindOrCreateFolder(folder);
                    if (folderNode == null)
                    {
                        break;
                    }
                }
                return folderNode;
            }
            return null;
        }

        /// <summary>
        /// Finds or create a folder.
        /// </summary>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns></returns>
        public ProjectNode FindOrCreateFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName) || folderName == ".")
            {
                return this;
            }
            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(this.RelativePath, folderName));
            HierarchyNode subFolder = (HierarchyNode)FindByName(di.Name);
            if (subFolder == null)
            {
                if (!Directory.Exists(di.FullName))
                {
                    Directory.CreateDirectory(di.FullName);
                }
                VSADDRESULT result = VSADDRESULT.ADDRESULT_Cancel;
                int hr = project.AddItem(this.ItemId,
                    (VSADDITEMOPERATION.VSADDITEMOP_OPENFILE), di.Name,
                    1, new string[] { di.FullName },
                    IntPtr.Zero, new VSADDRESULT[] { result });
                Marshal.ThrowExceptionForHR(hr);
            }
            subFolder = (HierarchyNode)FindByName(di.Name);
            if (subFolder != null)
            {
                return new ProjectNode(subFolder, subFolder.ItemId);
            }
            return null;
        }

        /// <summary>
        /// Opens an item using the default view (retourne un IVsWindowFrame)
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        // FXCOP: False positive
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public object OpenItem(HierarchyNode child)
        {
            Guard.ArgumentNotNull(child, "child");

            Guid logicalView = VSConstants.LOGVIEWID_Primary;
            IntPtr existingDocData = IntPtr.Zero;
            IVsWindowFrame windowFrame;
            int hr = project.OpenItem(((HierarchyNode)child).ItemId, ref logicalView, existingDocData, out windowFrame);
            Marshal.ThrowExceptionForHR(hr);
            windowFrame.Show();
            return windowFrame;
        }

        /// <summary>
        /// Gets the MSBuild project
        /// </summary>
        public Project MSBuildProject
        {
            get
            {
                return Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.GetLoadedProjects(Path).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is unloaded.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is unloaded; otherwise, <c>false</c>.
        /// </value>
        public bool IsUnloaded
        {
            get { return Project == null; }
        }

        /// <summary>
        /// Gets the project reference of a project.
        /// </summary>
        /// <value>The projref of project.</value>
        public string ProjrefOfProject
        {
            get
            {
                string result;
                int hr = Solution.GetProjrefOfProject(Hierarchy, out result);
                Marshal.ThrowExceptionForHR(hr);
                return result;
            }
        }

        /// <summary>
        /// Gets the name of the output file.
        /// </summary>
        /// <value>The name of the output file.</value>
        public string OutputFileName
        {
            get
            {
                try
                {
                    // Get the configuration manager from the project.
                    EnvDTE.ConfigurationManager confManager = Project.ConfigurationManager;
                    if (null == confManager)
                    {
                        return String.Empty;
                    }
                    // Get the active configuration.
                    EnvDTE.Configuration config = confManager.ActiveConfiguration;
                    if (null == config)
                    {
                        return String.Empty;
                    }
                    // Get the output path for the current configuration.
                    EnvDTE.Property outputPathProperty = config.Properties.Item("OutputPath");
                    if (null == outputPathProperty)
                    {
                        return String.Empty;
                    }
                    string outputPath = outputPathProperty.Value.ToString();

                    // Ususally the output path is relative to the project path, but it is possible
                    // to set it as an absolute path. If it is not absolute, then evaluate its value
                    // based on the project directory.
                    if (!System.IO.Path.IsPathRooted(outputPath))
                    {
                        outputPath = System.IO.Path.Combine(ProjectDir, outputPath);
                    }

                    // Now get the name of the assembly from the project.
                    if (IsWebProject)
                        return outputPath;

                    EnvDTE.Property assemblyNameProperty = Project.Properties.Item("OutputFileName");
                    if (null == assemblyNameProperty)
                    {
                        return String.Empty;
                    }
                    // build the full path adding the name of the assembly to the output path.
                    outputPath = System.IO.Path.Combine(outputPath, assemblyNameProperty.Value.ToString());

                    return outputPath;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns the language of the underlying project, if available.
        /// </summary>
        public string Language
        {
            get
            {
                if (Project == null)
                {
                    return null;
                }
              
                return GetLanguageFromProject();
            }
        }

        /// <summary>
        /// Gets the kind.
        /// </summary>
        /// <value>The kind.</value>
        public ProjectKind Kind
        {
            get
            {
                if (_kind == ProjectKind.NotDefined && Hierarchy != null)
                {
                    if (String.Compare(System.IO.Path.GetExtension(this.Path), ".dbp", true) == 0)
                    {
                        _kind = ProjectKind.Database;
                        return _kind;
                    }
                    _kind = ProjectKind.WindowsApplication;
                    string aggregateProjectTypeGuids = GetAggregateProjectTypeGuids();
                    if (aggregateProjectTypeGuids != null)
                    {
                        aggregateProjectTypeGuids = aggregateProjectTypeGuids.ToUpperInvariant();
                        if (aggregateProjectTypeGuids.Contains("{E24C65DC-7377-472B-9ABA-BC803B73C61A}"))
                        {
                            _kind = ProjectKind.WebSite;
                        }
                        else if (aggregateProjectTypeGuids.Contains("{349C5851-65DF-11DA-9384-00065B846F21}"))
                        {
                            _kind = ProjectKind.WebApplication;
                        }
                    }
                    else
                    {
                        string str = this.Project.Kind.ToUpper(CultureInfo.InvariantCulture);
                        if (str.Equals("{E24C65DC-7377-472B-9ABA-BC803B73C61A}"))
                        {
                            _kind = ProjectKind.WebSite;
                        }
                        else if (str.Equals("{349C5851-65DF-11DA-9384-00065B846F21}"))
                        {
                            _kind = ProjectKind.WebApplication;
                        }
                    }
                }
                return _kind;
            }
        }

        /// <summary>
        /// Gets the aggregate project type guids.
        /// </summary>
        /// <returns></returns>
        private string GetAggregateProjectTypeGuids()
        {
            string guids = null;
            IVsAggregatableProject project2 = Hierarchy as IVsAggregatableProject;
            if (project2 != null)
            {
                project2.GetAggregateProjectTypeGuids(out guids);
            }
            return guids;
        }

        /// <summary>
        /// Finds the reference node.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        public HierarchyNode FindReferenceNode(string identity)
        {
            string identityAsFileName;
            if (String.Compare(System.IO.Path.GetExtension(identity), ".dll", StringComparison.OrdinalIgnoreCase) == 0)
                identityAsFileName = System.IO.Path.GetFileNameWithoutExtension(identity);
            else
                identityAsFileName = System.IO.Path.GetFileName(identity);

            var node = RecursiveFind(n =>
                {
                    if( n.ExtObject is VSLangProj.Reference )
                    {
                        VSLangProj.Reference r = (VSLangProj.Reference)n.ExtObject;
                        if( String.Compare( r.Name, identityAsFileName, StringComparison.OrdinalIgnoreCase) == 0 )
                            return true;
                    }
                    return false;
                });
            return node;
        }

        /// <summary>
        /// Removes the reference.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        public bool RemoveReference(string identity)
        {
#if DEBUG
            System.Reflection.MethodBase @mb = System.Reflection.MethodBase.GetCurrentMethod();
            System.Diagnostics.Debug.WriteLine("Enter " + @mb.DeclaringType.Name + "." + @mb.Name);
#endif
            Guard.ArgumentNotNullOrEmptyString(identity, "identity");

            if (IsWebProject)
            {
                VsWebSite.AssemblyReference wref = FindWebReference(identity);
                if (wref == null)
                    return false;
                try { wref.Remove(); } catch { }
                return true;
            }

            Reference reference = FindReference(identity);
            if (reference == null)
                return false;

            reference.Remove();
            return true;
        }

        /// <summary>
        /// Finds the reference.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        internal Reference FindReference(string identity)
        {
            Guard.ArgumentNotNullOrEmptyString(identity, "identity");
            if (Project != null)
            {
                VSLangProj.References references = ((VSLangProj.VSProject)Project.Object).References;

                if (references != null)
                {
                    string identityAsFileName;
                    if( String.Compare( System.IO.Path.GetExtension(identity), ".dll",  StringComparison.OrdinalIgnoreCase ) == 0)
                        identityAsFileName = System.IO.Path.GetFileNameWithoutExtension(identity);
                    else
                        identityAsFileName = System.IO.Path.GetFileName(identity);
                   
                    foreach (Reference reference in references)
                    {
                        if (reference.Type == prjReferenceType.prjReferenceTypeActiveX)
                        {
                            if( !identityAsFileName.StartsWith("interop.", StringComparison.OrdinalIgnoreCase ))
                                identityAsFileName = "Interop." + identityAsFileName + "Lib";

                            if ( String.Compare( System.IO.Path.GetFileNameWithoutExtension(reference.Path), identityAsFileName,  StringComparison.OrdinalIgnoreCase ) == 0)
                                return reference;
                        }
                        else
                        {
                            if (String.Compare( reference.Name, identityAsFileName,  StringComparison.OrdinalIgnoreCase ) == 0||
                                string.Compare( System.IO.Path.GetFileNameWithoutExtension(reference.Path), identityAsFileName, StringComparison.OrdinalIgnoreCase ) == 0 )
                                return reference;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the web reference.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        internal VsWebSite.AssemblyReference FindWebReference(string identity)
        {
            Guard.ArgumentNotNullOrEmptyString(identity, "identity");

            if (Project != null)
            {
                VsWebSite.AssemblyReferences references = ((VsWebSite.VSWebSite)Project.Object).References;

                if (references != null)
                {
                    string identityAsFileName = String.Compare( System.IO.Path.GetExtension(identity), ".dll", StringComparison.OrdinalIgnoreCase) == 0 
                                                    ? System.IO.Path.GetFileNameWithoutExtension(identity) 
                                                    : System.IO.Path.GetFileName(identity);
                    foreach( VsWebSite.AssemblyReference reference in references )
                    {
                        string refId = reference.ReferencedProject != null ? reference.ReferencedProject.UniqueName : reference.Name;
                        if( String.Compare(  refId, identity, StringComparison.OrdinalIgnoreCase) == 0 ||
                            String.Compare( System.IO.Path.GetFileNameWithoutExtension(reference.FullPath), identityAsFileName,  StringComparison.OrdinalIgnoreCase) == 0 )
                            return reference;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Adds the assembly reference.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public string AddAssemblyReference(string assemblyPath, string version)
        {
            Guard.ArgumentNotNullOrEmptyString(assemblyPath, "assemblyPath");            
            if (Project != null)
            {
                if (Project.Object is VSLangProj.VSProject)
                {
                    Reference oldReference = FindReference(assemblyPath );
                    if (oldReference != null)
                    {
                        // Si la référence existe et que la version n'est pas discriminante,
                        // on ne fait rien                        
                        if (String.IsNullOrEmpty(version) || oldReference.SourceProject != null)
                            return oldReference.Path;

                        // Sinon, si la version est difèrente, on supprime l'ancienne
                        if (oldReference.Version == version)
                            return oldReference.Path;                            
                        oldReference.Remove();
                        
                    }
                    VSLangProj.References references = ((VSLangProj.VSProject)Project.Object).References;

                    if (references != null )
                    {
                        return references.Add(assemblyPath).Path;
                    }
                }
                else if (Project.Object is VsWebSite.VSWebSite)
                {
                    var wr = FindWebReference(assemblyPath);
                    if (wr != null)
                    {
                        if (String.IsNullOrEmpty(version) || wr.ReferenceKind == VsWebSite.AssemblyReferenceType.AssemblyReferenceClientProject )
                            return wr.FullPath;
                        // On connait pas la version, on supprime pour recréer.
                        wr.Remove();
                    }
                    VsWebSite.AssemblyReferences references = ((VsWebSite.VSWebSite)Project.Object).References;

                    if (references != null && File.Exists(assemblyPath))
                    {
                        references.AddFromFile(assemblyPath);
                        return assemblyPath;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Adds the project reference.
        /// </summary>
        /// <param name="projectId">The project id.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public ProjectReference AddProjectReference(Guid projectId)
        {
            if (ProjectGuid == projectId)
            {
                return null;
            }

            return AddProjectReference( new ProjectNode(Solution, projectId) );
        }

        /// <summary>
        /// Adds the project reference.
        /// </summary>
        /// <param name="uniqueName">The uniqueName.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public ProjectReference AddProjectReference(string uniqueName)
        {
            if (uniqueName == this.UniqueName)
            {
                return null;
            }

            return AddProjectReference(new ProjectNode(Solution, uniqueName));
        }


        /// <summary>
        /// Adds the project reference.
        /// </summary>
        /// <param name="referencedProject">The referenced project.</param>
        public ProjectReference AddProjectReference(ProjectNode referencedProject)
        {
            if( referencedProject == null )
                return null;

            if (Project != null)
            {
                SolutionNode parentSolution = new SolutionNode(this.Solution);
                if (Project.Object is VSLangProj.VSProject)
                {
                    VSLangProj.References references = ((VSLangProj.VSProject)Project.Object).References;

                    if (references != null && referencedProject.ExtObject is EnvDTE.Project)
                    {
                        Reference rference = references.AddProject(referencedProject.ExtObject as EnvDTE.Project);
                        return ProjectReferenceHelper.CreateVisualStudioReferenceFromReference(parentSolution, rference);
                    }
                }
                else if (Project.Object is VsWebSite.VSWebSite)
                {
                    VsWebSite.AssemblyReferences references = ((VsWebSite.VSWebSite)Project.Object).References;

                    if (references != null && referencedProject.ExtObject is EnvDTE.Project)
                    {
                        try
                        {
                            references.AddFromProject(referencedProject.ExtObject as EnvDTE.Project);
                        }
                        catch (COMException)
                        {
                            //Web projects throws exceptions if the reference already exists
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the evaluated property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public string GetEvaluatedProperty(string propertyName)
        {
            return ProjectNode.GetEvaluatedProperty(this.Project, propertyName);
        }

        /// <summary>
        /// Gets the evaluated property.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static string GetEvaluatedProperty(EnvDTE.Project project, string propertyName)
        {
            Guard.ArgumentNotNullOrEmptyString(propertyName, "propertyName");
            if (project == null)
            {
                return string.Empty;
            }

            string value = string.Empty;
            foreach (EnvDTE.Property prop in project.Properties)
            {
                if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value.ToString();
                    break;
                }
            }

            return (value ?? string.Empty);
        }


        /// <summary>
        /// Gets the language from project.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public string GetLanguageFromProject()
        {
            Guard.ArgumentNotNull(project, "project");

            if (Project != null && Project.CodeModel != null)
            {
                return Project.CodeModel.Language;
            }

            CodeDomProvider provider = GetCodeDomProvider();
            if (provider is Microsoft.CSharp.CSharpCodeProvider)
            {
                return EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp;
            }
            if (provider is Microsoft.VisualBasic.VBCodeProvider)
            {
                return EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB;
            }
            return null;
        }

        /// <summary>
        /// Gets the code DOM provider.
        /// </summary>
        /// <returns></returns>
        public CodeDomProvider GetCodeDomProvider()
        {
            if (Project != null)
            {
                return CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(GetDefaultExtension()));
            }
            return CodeDomProvider.CreateProvider("C#");
        }

        /// <summary>
        /// Adds from template.
        /// </summary>
        /// <param name="templateFileName">Name of the template file.</param>
        /// <param name="name">The name.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns></returns>
        public HierarchyNode AddFromTemplate(string templateFileName, string name, bool force)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");
            Guard.ArgumentNotNullOrEmptyString(templateFileName, "templateFileName");

            if (this.ProjectDir == null)
                return null;

            string fullName = System.IO.Path.Combine(this.ProjectDir, name);
            HierarchyNode node = this.RecursiveFindByPath(fullName);
            if (node != null && !force)
                return node;

            if (File.Exists(fullName))
                Utils.DeleteFile(fullName);

            // Résolution du chemin du template
            if (!System.IO.Path.IsPathRooted(templateFileName))
            {
                EnvDTE80.Solution2 sln = this.Parent.ExtObject as EnvDTE80.Solution2;
                if (sln == null)
                    return null;

                string wizardName = templateFileName.Replace("\\", "/");
                string lang = null;
                int pos = wizardName.LastIndexOf('/');
                if (pos >= 0)
                {
                    lang = wizardName.Substring(0, pos);
                    wizardName = wizardName.Substring(pos + 1);
                    templateFileName = sln.GetProjectItemTemplate(wizardName, lang);
                }
                else
                {
                    templateFileName = System.IO.Path.Combine(sln.ProjectItemsTemplatePath(this.Project.Kind), name);
                }
            }

            string folderName = null;
            string fileName = name;
            int pos2 = name.LastIndexOf( System.IO.Path.DirectorySeparatorChar );
            if( pos2 >=0) {
                folderName = name.Substring(0,pos2);
                fileName = name.Substring(pos2+1);
            }
            
            EnvDTE.ProjectItems projectItems = this.Project.ProjectItems;
            if (folderName != null)
            {
                var folder = FindSubFolder(folderName);
                projectItems = ((EnvDTE.ProjectItem)folder.ExtObject).ProjectItems;
            }

            Guard.AssumeNotNull(projectItems, "projectItems (ProjectNode.AddFromTemplate)");

            try
            {
                // Décompression du fichier dans un répertoire temporaire
                templateFileName = ExtractProjectItemInTemporaryFolder(templateFileName, System.IO.Path.GetFileNameWithoutExtension(name), this.Namespace, this.Name);
                if (templateFileName == null || !File.Exists(templateFileName))
                    throw new Exception("Invalid project template " + templateFileName);

                string targetFileName = System.IO.Path.Combine(this.ProjectDir, name);
                if (File.Exists(targetFileName))
                {
                    Utils.DeleteFiles(System.IO.Path.GetDirectoryName(targetFileName), System.IO.Path.ChangeExtension(targetFileName, ".*"));
                }

                projectItems.AddFromTemplate(templateFileName, fileName);

                EnvDTE.ProjectItem item = this.Project.ProjectItems.Item(this.Project.ProjectItems.Count);
                return RecursiveFindByPath(item.get_FileNames(1));
            }
            finally
            {
                Utils.RemoveDirectory(System.IO.Path.GetDirectoryName(templateFileName));
            }
        }

        /// <summary>
        /// Extracts the project item in temporary folder.
        /// </summary>
        /// <param name="templatePath">The template path.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultNamespace">The default namespace.</param>
        /// <returns></returns>
        protected string ExtractProjectItemInTemporaryFolder(string templatePath, string name, string defaultNamespace, string solutionName)
        {
            string tempFolder = System.IO.Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);

            if (System.IO.Path.GetExtension(templatePath) == ".vstemplate")
            {
                Utils.CopyDirectory(System.IO.Path.GetDirectoryName(templatePath), tempFolder);
            }
            else
            {
                var zipFile = new Microsoft.VisualStudio.Zip.ZipFileDecompressor(templatePath);
                zipFile.UncompressToFolder(tempFolder);
                foreach(var f in Utils.SearchFile(tempFolder, "*.*"))
                {
                    File.SetAttributes(f, FileAttributes.Normal);
                }
            }

            templatePath = Utils.SearchFile(tempFolder, "*.vstemplate").FirstOrDefault();

            if (templatePath != null)
            {
                // Parcours de tous les fichiers pour faire le remplacement
                foreach (var csFile in GetTextFiles(tempFolder))
                {
                    ReplaceParameters(name, defaultNamespace, csFile, solutionName);
                }
            }
            return templatePath;
        }

        public IEnumerable<Type> GetAvailableTypes<T>(bool includeReferences)
        {
            var typeService = (Microsoft.VisualStudio.Shell.Design.DynamicTypeService)ServiceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Design.DynamicTypeService));
            Debug.Assert(typeService != null, "No dynamic type service registered.");

            var discovery = typeService.GetTypeDiscoveryService(this.Hierarchy); // La recherche s'effectue dans le scope du IVsHierarchy
            if (discovery != null)
            {
                foreach (Type type in discovery.GetTypes(typeof(T), includeReferences))
                {
                    yield return type;
                }
            }
        }

        private IEnumerable<string> GetTextFiles(string folder)
        {
            foreach (var csFile in Utils.SearchFile(folder, "*.*"))
            {
                string ext = System.IO.Path.GetExtension(csFile).ToLower();
                switch (ext)
                {
                    case ".config":
                    case ".sitemap":
                    case ".aspx":
                    case ".ascx":
                    case ".cs":
                    case ".svc":
                    case ".master":
                    case ".ashx":
                    case ".htm":
                    case ".cshtml":
                    case ".asmx":
                        yield return csFile;
                        break;
                }
            }            
        }

        /// <summary>
        /// Replaces the parameters.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="defaultNamespace">The default namespace.</param>
        /// <param name="fileName">Name of the file.</param>
        private void ReplaceParameters(string name, string defaultNamespace, string fileName, string solutionName)
        {
            string content = File.ReadAllText(fileName);
            content = content.Replace("$guid1$", Guid.NewGuid().ToString("B"));
            content = content.Replace("$guid2$", Guid.NewGuid().ToString("B"));
            content = content.Replace("$guid3$", Guid.NewGuid().ToString("B"));
            content = content.Replace("$year$", DateTime.Now.Year.ToString());
            content = content.Replace("$month$", DateTime.Now.Month.ToString());
            content = content.Replace("$day$", DateTime.Now.Day.ToString("0#"));
            content = content.Replace("$solutionname$", solutionName);
            content = content.Replace("$safeprojectname$", name);
            content = content.Replace("$projectname$", name.Replace('.', ' '));
            content = content.Replace("$rootnamespace$", defaultNamespace);
            content = content.Replace("$safeitemrootname$", name);
            content = content.Replace("$targetframeworkversion$", TargetFramework.ToString(2));
            File.WriteAllText(fileName, content);
        }

        /// <summary>
        /// Adds the file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public virtual HierarchyNode AddFile(string file, byte[] content)
        {
            // add the file to the project
            HierarchyNode node = (HierarchyNode)AddItem(file, content);
            if (node == null)
            {
                return null;
            }

            EnsureDocumentIsNotInRDT(node);
            return node;
        }

        /// <summary>
        /// Checkouts the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void CheckoutFile(string fileName)
        {
            Microsoft.VisualStudio.Shell.Design.Serialization.DocData docData = GetDocData(VSConstants.LOGVIEWID_Primary) as Microsoft.VisualStudio.Shell.Design.Serialization.DocData;
            if( docData != null)
                docData.CheckoutFile(ServiceProvider);
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        public override bool Save()
        {
            unchecked
            {
                return Solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, Hierarchy, (uint)__VSHPROPID.VSHPROPID_NIL) == VSConstants.S_OK;
            }
        }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public override IServiceProvider ServiceProvider
        {
            get
            {
                EnvDTE.DTE dte = this.Project != null ? this.Project.DTE : this.ProjectItem.DTE;
                return new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
            }
        }

        public Version TargetFramework
        {
            get
            {
                uint version = 0;
                //if( !this.IsDeviceProject(project) )
                //{
                //    version = GetProperty<uint>(__VSHPROPID3.VSHPROPID_TargetFrameworkVersion);
                //}
                //else
                {
                    IVsBuildPropertyStorage storage = Hierarchy as IVsBuildPropertyStorage;
                    if (storage != null)
                    {
                        string pbstrPropValue;
                        storage.GetPropertyValue("TargetFrameworkVersion", null, 1, out pbstrPropValue);
                        if (!string.IsNullOrEmpty(pbstrPropValue))
                        {
                            if (pbstrPropValue == "v3.5")
                                version = 0x30005;
                            else if (pbstrPropValue == "v3.0")
                                version = 0x30000;
                        }
                    }
                }

                if (version == 0x30005)
                    return new Version(3, 5, 0, 0);
                if (version == 0x30000)
                    return new Version(3, 0, 0, 0);
                return new Version(2, 0, 0);
            }
        }

        /// <summary>
        /// Gets the default extension.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns></returns>
        public string GetDefaultExtension()
        {
            if (this.Language == EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB )
            {
                return ".vb";
            }
            return ".cs";
        }

        /// <summary>
        /// Gets a value indicating whether this instance is web project.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is web project; otherwise, <c>false</c>.
        /// </value>
        public bool IsWebProject
        {
            get
            {
                return Kind == ProjectKind.WebSite || Kind == ProjectKind.WebApplication;
            }
        }

        /// <summary>
        /// Ensures the document is not in RDT.
        /// </summary>
        /// <param name="node">The node.</param>
        public void EnsureDocumentIsNotInRDT(HierarchyNode node)
        {
            IVsHierarchy ppHier;
            uint docCookie;
            uint itemid;
            if (node == null)
                return;

            IVsRunningDocumentTable rdt = ServiceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (rdt != null)
            {
                IntPtr ptr = IntPtr.Zero;
                ErrorHandler.ThrowOnFailure(
                    rdt.FindAndLockDocument(0, node.Path, out ppHier, out itemid, out ptr, out docCookie));
                if (ptr == IntPtr.Zero || docCookie == 0)
                    return;

                IVsRunningDocumentTable2 rdt2 = (IVsRunningDocumentTable2)rdt;
                if (rdt2 != null)
                {
                    ErrorHandler.ThrowOnFailure(
                        rdt2.CloseDocuments((uint)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty, null, docCookie));
                }
            }
        }

    }
}
