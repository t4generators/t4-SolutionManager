
using System.IO;
using System;
using System.Text;
using EnvDTE;
using System.Diagnostics;
using VSLangProj;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE80;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using VsxFactory.Modeling.VisualStudio.Synchronization;

namespace VsxFactory.Modeling.VisualStudio
{
    /// <summary>
    /// Classe permettant de manipuler l'explorateur de solution.
    /// </summary>
    public class SolutionManagerService : IVsUpdateSolutionEvents, VsxFactory.Modeling.VisualStudio.ISolutionManagerService
    {
        private IServiceProvider _serviceProvider;
        private SolutionListener _solutionListener;
//        private SolutionBuildListener _buildListener;
        private uint _sdmCookie;

        /// <summary>
        /// Occurs when [reference changed].
        /// </summary>
        public event EventHandler<ProjectChangedEventArg> ProjectChanged;

        /// <summary>
        /// Occurs when [configuration changed].
        /// </summary>
        public event EventHandler<EventArgs> ConfigurationChanged;

        /// <summary>
        /// Occurs when [solution events].
        /// </summary>
        public event SolutionEventsHandler SolutionEvents;

        /// <summary>
        /// Nom du format d'un projet Visual Studio dans le presse papier
        /// </summary>
        private const string ProjectReferenceFormat = "CF_VSREFPROJECTS";

        /// <summary>
        /// Nom du format d'un projet item Visual Studio dans le presse papier
        /// </summary>
        private const string ProjectItemReferenceFormat = "CF_VSREFPROJECTITEMS";

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionManager"/> class.
        /// </summary>
        public SolutionManagerService(IServiceProvider serviceProvider)
        {
            Guard.ArgumentNotNull(serviceProvider, "ServiceProvider");
            _serviceProvider = serviceProvider;
            _solutionListener = new SolutionListener(this, serviceProvider);
            _solutionListener.ProjectChanged += new EventHandler<ProjectChangedEventArg>(OnProjectChanged);
            _solutionListener.SolutionEvents += new SolutionEventsHandler(OnSolutionEvents);
            IVsSolutionBuildManager vsSolutionManager = serviceProvider.GetService(typeof(IVsSolutionBuildManager)) as IVsSolutionBuildManager;
            if (vsSolutionManager != null)
                vsSolutionManager.AdviseUpdateSolutionEvents(this, out _sdmCookie);
        }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        /// <summary>
        /// S_solutions the listener_ reference changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void OnProjectChanged(object sender, ProjectChangedEventArg e)
        {
            //Debug.WriteLine(string.Format("{0} {1}", e.Action, e.Project.Name));
            if (ProjectChanged != null)
                ProjectChanged(sender, e);
        }

        /// <summary>
        /// Gets the configuration mode.
        /// </summary>
        /// <value>The configuration mode.</value>
        public string ConfigurationName
        {
            get
            {
                DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE)) as DTE;
                if (dte != null && dte.Solution != null && dte.Solution.SolutionBuild != null && dte.Solution.SolutionBuild.ActiveConfiguration != null)
                    return dte.Solution.SolutionBuild.ActiveConfiguration.Name;
                return String.Empty;
            }
        }

        /// <summary>
        /// Gets the startup projects.
        /// </summary>
        /// <value>The startup projects.</value>
        public List<ProjectNode> StartupProjects
        {
            get
            {
                List<ProjectNode> startups = new List<ProjectNode>();

                DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE)) as DTE;
                if (dte != null && dte.Solution != null && dte.Solution.SolutionBuild != null)
                {
                    try
                    {
                        object[] files = (object[])dte.Solution.SolutionBuild.StartupProjects;
                        if (files != null && files.Length > 0)
                        {
                            foreach (string startupProjectName in files)
                            {
                                startups.Add(CurrentSolution.GetProject(startupProjectName));
                            }
                        }
                    }
                    catch
                    {
                        // Pas de projet de startup
                    }
                }
                return startups;
            }
        }

        /// <summary>
        /// Gets the configuration names.
        /// </summary>
        /// <value>The configuration names.</value>
        public List<String> ConfigurationNames
        {
            get
            {
                List<string> list = new List<string>();
                DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE)) as DTE; 
                if (dte != null && dte.Solution != null && dte.Solution.SolutionBuild != null)
                {
                    SolutionConfigurations configs = dte.Solution.SolutionBuild.SolutionConfigurations;
                    if (configs != null && configs.Count > 0)
                    {
                        for (int i = 0; i < configs.Count; i++)
                        {
                            string name = configs.Item(i + 1).Name;
                            if (!list.Contains(name))
                                list.Add(name);
                        }
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_solutionListener != null)
            {
                _solutionListener.ProjectChanged -= new EventHandler<ProjectChangedEventArg>(OnProjectChanged);
                _solutionListener.SolutionEvents -= new SolutionEventsHandler(OnSolutionEvents);
                _solutionListener.Dispose();
                _solutionListener = null;
            }
            //if (_buildListener != null)
            //{
            //    _buildListener.BuildCompleted -= new EventHandler<BuildCompleteEventArgs>(OnBuildCompleted);
            //    _buildListener.Dispose();
            //    _buildListener = null;
            //}
            if (_sdmCookie != 0)
            {
                IVsSolutionBuildManager vsSolutionManager = _serviceProvider.GetService(typeof(IVsSolutionBuildManager)) as IVsSolutionBuildManager;
                if (vsSolutionManager != null)
                    vsSolutionManager.UnadviseUpdateSolutionEvents(_sdmCookie);
            }
        }

        /// <summary>
        /// Determines whether [is project dragged] [the specified data].
        /// </summary>
        /// <param name="data">The data object.</param>
        /// <returns>
        /// 	<c>true</c> if [is project dragged] [the specified data]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsProjectDragged(System.Windows.Forms.IDataObject data)
        {
            return data.GetDataPresent(ProjectReferenceFormat);
        }

        /// <summary>
        /// Determines whether [is project item dragged] [the specified i data object].
        /// </summary>
        /// <param name="iDataObject">The i data object.</param>
        public bool IsProjectItemDragged(System.Windows.Forms.IDataObject iDataObject)
        {
            return iDataObject.GetDataPresent(ProjectItemReferenceFormat);
        }

        /// <summary>
        /// Gets the project item dropped.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public HierarchyNode GetProjectItemDropped(System.Windows.Forms.IDataObject data)
        {
            string[] infos = DeserializeData(data, ProjectItemReferenceFormat);
            string fileName = null;
            if (infos != null)
                fileName = infos[2];
            else if (data.GetDataPresent("Text"))
                fileName = (string)data.GetData("Text");
            if (fileName != null)
                return CurrentSolution.RecursiveFindByPath(fileName);
            return null;
        }

        /// <summary>
        /// Gets the project dropped.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public ProjectNode GetProjectDropped(System.Windows.Forms.IDataObject data)
        {
            string[] infos = DeserializeData(data, ProjectReferenceFormat);
            if (infos != null)
                return CurrentSolution.GetProject(infos[1]);

            return null;
        }

        /// <summary>
        /// Deserializes the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="format">The format.</param>
        /// <returns></returns>
        private string[] DeserializeData(System.Windows.Forms.IDataObject data, string format)
        {
            object obj = data.GetData(format);

            MemoryStream ms = obj as MemoryStream;
            if (ms != null)
            {
                // Récupération du nom du projet dans les données véhiculées
                string projectInfo = String.Empty;
                String str = Encoding.Unicode.GetString(ms.ToArray());

                // Récupération des chaines textuelles au format c (0 de terminaison)
                StringBuilder sb = new StringBuilder();
                for (int ix = 0; ix < str.Length; ix++)
                {
                    char ch = str[ix];
                    if (ch == 0) // 0 final
                    {
                        if (sb.Length > 2)
                            projectInfo = sb.ToString(); // On récupère la dernière chaine de plus de 2 car de long
                        if (sb.Length != 0)
                            sb = new StringBuilder(); // Nouvelle chaine
                        continue;
                    }
                    sb.Append(ch);
                }

                return projectInfo.Split('|');
            }
            return null;
        }
        /// <summary>
        /// Gets the current solution.
        /// </summary>
        /// <value>The current solution.</value>
        public SolutionNode CurrentSolution
        {
            get
            {
                DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE)) as DTE;
                if (dte == null)
                    return null;
                return GetSolution(_serviceProvider);
            }
        }

        public SolutionNode GetSolution(IServiceProvider serviceProvider)
        {
            Guard.ArgumentNotNull(serviceProvider, "serviceProvider");
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as Microsoft.VisualStudio.Shell.Interop.IVsSolution;
            if (vsSolution == null)
                return null;
            return new SolutionNode(vsSolution);
        }

        void OnSolutionEvents(object sender, SolutionEventArgs e)
        {
            if (!e.Closing)
            {
                OnSolutionOpened();
            }
            else
            {
                OnSolutionClosed();
            }

            if (SolutionEvents != null)
                SolutionEvents(sender, e);
        }

        /// <summary>
        /// Called when [solution closed].
        /// </summary>
        private void OnSolutionClosed()
        {
#if DEBUG
            System.Reflection.MethodBase @mb = System.Reflection.MethodBase.GetCurrentMethod();
            System.Diagnostics.Debug.WriteLine("Enter " + @mb.DeclaringType.Name + "." + @mb.Name);
#endif
            //// Fermeture de la solution
            //if (_buildListener != null)
            //{
            //    _buildListener.BuildCompleted -= new EventHandler<BuildCompleteEventArgs>(OnBuildCompleted);
            //    _buildListener.Dispose();
            //    _buildListener = null;
            //}
        }

        /// <summary>
        /// Called when [solution opened].
        /// </summary>
        private void OnSolutionOpened()
        {
#if DEBUG
            System.Reflection.MethodBase @mb = System.Reflection.MethodBase.GetCurrentMethod();
            System.Diagnostics.Debug.WriteLine("Enter " + @mb.DeclaringType.Name + "." + @mb.Name);
#endif
            // Ouverture de la solution
            //_buildListener = new SolutionBuildListener(_serviceProvider);
            //_buildListener.BuildCompleted += new EventHandler<BuildCompleteEventArgs>(OnBuildCompleted);

        }


        /// <summary>
        /// Enables the reference events on project.
        /// </summary>
        public void EnableReferenceEventsOnProject()
        {
            _solutionListener.EnableReferenceEventsOnProject();
        }

        /// <summary>
        /// Disables the reference events on project.
        /// </summary>
        /// <param name="project">The project.</param>
        public void DisableReferenceEventsOnProject(string projectName)
        {
            _solutionListener.DisableReferenceEventsOnProject(projectName);
        }

        #region ISolutionManagerService Members
        public List<String> EnumerateTemplates(string languageName, string projectsTemplateFolder)
        {
            List<string> templates = new List<string>();

            // Templates personnalisé
            foreach (string file in VsxFactory.Modeling.VisualStudio.Utils.SearchFile(projectsTemplateFolder, "*.zip"))
            {
                string item = Utils.PathRelativePathToFolder(projectsTemplateFolder, file);
                templates.Add(item);
            }

            // User Exported templates
            string templateFolder = GetExportedProjectTemplatesDir();
            foreach (string file in VsxFactory.Modeling.VisualStudio.Utils.SearchFile(templateFolder, "*.zip"))
            {
                string item = Path.GetFileName(file);
                templates.Add(item);
            }

            // Project templates du language
            templateFolder = Path.Combine(VsxFactory.Modeling.VisualStudio.Utils.VSInstallDir, "ProjectTemplates");
            string folder = Path.Combine(templateFolder, languageName);
            foreach (string file in VsxFactory.Modeling.VisualStudio.Utils.SearchFile(folder, "*.zip"))
            {
                string item = String.Concat(languageName, '/', Path.GetFileName(file));
                templates.Add(item);
            }

            folder = Path.Combine(templateFolder, "web", languageName);
            foreach (string file in VsxFactory.Modeling.VisualStudio.Utils.SearchFile(folder, "*.zip"))
            {
                string item = String.Concat("web/", languageName, "/", Path.GetFileName(file));
                templates.Add(item);
            }

            return templates;
        }

        public string GetExportedProjectTemplatesDir()
        {
            IVsShell shell = ServiceProvider.GetService(typeof(SVsShell)) as IVsShell;
            object str;
            shell.GetProperty((int)__VSSPROPID2.VSSPROPID_VsTemplateUserZipProjectFolder, out str);
            return (string)str;
        }

        /// <summary>
        /// Opens the file in Visaul Studio
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void OpenFile(string fileName)
        {
            if (fileName == null)
                return;
            DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE)) as DTE;
            if (dte == null || !File.Exists(fileName))
                return;
            dte.ItemOperations.OpenFile(fileName, EnvDTE.Constants.vsViewKindPrimary);
        }

        /// <summary>
        /// Determines whether [is document in solution] [the specified relative file name].
        /// </summary>
        /// <param name="relativeFileName">Name of the relative file.</param>
        /// <returns>
        /// 	<c>true</c> if [is document in solution] [the specified relative file name]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDocumentInSolution(string relativeFileName)
        {
            Guard.ArgumentNotNullOrEmptyString(relativeFileName, "relativeFileName");
            if (CurrentSolution == null)
                return false;

            string fileName = System.IO.Path.Combine(CurrentSolution.ProjectDir, relativeFileName);

            Microsoft.VisualStudio.OLE.Interop.IServiceProvider provider;
            uint num;
            int num2;
            IVsUIShellOpenDocument service = _serviceProvider.GetService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            if (service == null)
                return false;
            IVsUIHierarchy ppUIH = null;
            ErrorHandler.ThrowOnFailure(service.IsDocumentInAProject(fileName, out ppUIH, out num, out provider, out num2));
            return num2 == (int)__VSDOCINPROJECT.DOCINPROJ_DocInProject;
        }

        public IEnumerable<HierarchyNode> EnumerateDocumentInRDT()
        {
            var rdt = ServiceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            IEnumRunningDocuments ppEnum;
            rdt.GetRunningDocumentsEnum(out ppEnum);
            uint[] rgelt = new uint[1];
            uint fetched = 0;
            while (true)
            {
                if (ErrorHandler.Succeeded(ppEnum.Next(1, rgelt, out fetched)) && fetched == 1)
                {
                    var info = GetDocumentInfo(rdt, rgelt[0]);
                    yield return new HierarchyNode(CurrentSolution.Solution, info.Hierarchy);
                }
                else
                {
                    break;
                }
            }
        }

        private static RunningDocumentInfo GetDocumentInfo(IVsRunningDocumentTable rdt, uint docCookie)
        {
            RunningDocumentInfo info = new RunningDocumentInfo();
            IntPtr docData;
            int hr = rdt.GetDocumentInfo(docCookie, out info.Flags, out info.ReadLocks, out info.EditLocks, out info.Moniker, out info.Hierarchy, out info.ItemId, out docData);
            //if (hr == VSConstants.S_OK)
            //{
            //    try
            //    {
            //        if (docData != IntPtr.Zero)
            //            info.DocData = Marshal.GetObjectForIUnknown(docData);
            //        return info;
            //    }
            //    finally
            //    {
            //        Marshal.Release(docData);
            //    }
            //}
            return info;
        }

        /// <summary>
        /// Determines whether [is project in model UI hierarchy] [the specified model node].
        /// </summary>
        /// <param name="modelNode">The model node.</param>
        /// <param name="projectGuid">The project GUID.</param>
        /// <returns>
        /// 	<c>true</c> if [is project in model UI hierarchy] [the specified model node]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsProjectInModelUIHierarchy(HierarchyNode modelNode, Guid projectGuid)
        {
            Guard.ArgumentNotNull(modelNode, "modelNode");
            // On regarde si le projet se trouve dans l'arborescence du dossier contenant le modèle
            HierarchyNode folderNode = modelNode.IsProject ? modelNode.Parent : modelNode;
            return folderNode.RecursiveFind(n => n.IsProject && n.ProjectGuid == projectGuid) != null;
        }

        //public ProjectNode CreateProject(string SolutionFolderPath, string projectName, string template, string defaultNamespace, string solutionName)
        //{
        //    Guard.ArgumentNotNullOrEmptyString(projectName, "projectName");
        //    Guard.ArgumentNotNullOrEmptyString(defaultNamespace, "defaultNamespace");
        //    Guard.ArgumentNotNullOrEmptyString(solutionName, "solutionName");

        //    if (CurrentSolution == null)
        //        return null;

        //    return this.CurrentSolution.CreateProject(this, SolutionFolderPath, projectName, template, defaultNamespace, solutionName);
        //}

        #endregion

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            if (ConfigurationChanged != null && pIVsHierarchy == null) // au niveau de la solution uniquement
                ConfigurationChanged(this, new EventArgs());
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }
    }
}
