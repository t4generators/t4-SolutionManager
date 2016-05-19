using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using VSLangProj;
using Microsoft.VisualStudio;
using System.Diagnostics;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    public class SolutionListener : IDisposable, IVsSolutionEvents, IVsSolutionEvents4
    {
        /// <summary>
        /// Quand on décharge un projet, Visual Studio génére 2 événements :
        ///  - OnBeforeUnloadProject puis OnBeforeCloseProject
        /// Cette variable sert à stocker lors du premier appel le projet qui
        /// vient d'être déchargé afin d'empecher l'évenement sa prise en compte 
        /// dans l'événement OnClose
        /// </summary>
        private ProjectNode _unloadedProject;

        private UInt32 _cookieIVsSolutionEvents;
        private IServiceProvider _serviceProvider;
        private List<ProjectListener> _projectListeners = new List<ProjectListener>();
        private Queue<string> _projectEventsDisabled = new Queue<string>();
        private SolutionManagerService _solutionManager;

        /// <summary>
        /// Occurs when [reference changed].
        /// </summary>
        public event EventHandler<ProjectChangedEventArg> ProjectChanged;

        /// <summary>
        /// Occurs when [solution events].
        /// </summary>
        public event SolutionEventsHandler SolutionEvents;

        internal class ProjectListener
        {
            private ReferencesEvents _referencesEvents;
            private bool _enabled;
            private EventHandler<ProjectChangedEventArg> _callback;
            private SolutionManagerService _solutionManager;

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name {get;private set;}

            /// <summary>
            /// Initializes a new instance of the <see cref="ProjectListener"/> class.
            /// </summary>
            /// <param name="project">The project.</param>
            public ProjectListener(SolutionManagerService solutionManager, ProjectNode project, EventHandler<ProjectChangedEventArg> callback)
            {
                Debug.Assert(project != null);
                _solutionManager = solutionManager;
                Name = project.UniqueName;
                VSProject prj = project.VSProject;
                if (prj == null)
                {
                    _referencesEvents = null;
                    return;
                }
                
                _referencesEvents = prj.Events.ReferencesEvents;
                _referencesEvents.ReferenceAdded += new _dispReferencesEvents_ReferenceAddedEventHandler(OnReferenceAdded);
                _referencesEvents.ReferenceChanged += new _dispReferencesEvents_ReferenceChangedEventHandler(OnReferenceChanged);
                _referencesEvents.ReferenceRemoved += new _dispReferencesEvents_ReferenceRemovedEventHandler(OnReferenceRemoved);
                _callback = callback;
                _enabled = true;
            }

            /// <summary>
            /// Unregister project events.
            /// </summary>
            /// <param name="project">The project.</param>
            public void Dispose()
            {
                if (_referencesEvents != null)
                {
                    _referencesEvents.ReferenceAdded -= new _dispReferencesEvents_ReferenceAddedEventHandler(OnReferenceAdded);
                    _referencesEvents.ReferenceChanged -= new _dispReferencesEvents_ReferenceChangedEventHandler(OnReferenceChanged);
                    _referencesEvents.ReferenceRemoved -= new _dispReferencesEvents_ReferenceRemovedEventHandler(OnReferenceRemoved);
                }
            }

            /// <summary>
            /// Referenceses the events_ reference removed.
            /// </summary>
            /// <param name="pReference">The p reference.</param>
            void OnReferenceRemoved(Reference pReference)
            {
                if (!_enabled)
                    return;
#if DEBUG
                Debug.WriteLine(String.Format("VS reference {0} removed from {1}", pReference.Name, pReference.ContainingProject.Name));
#endif
                ProjectNode project = _solutionManager.CurrentSolution.GetProject(pReference.ContainingProject.UniqueName);
                ProjectReference rf = ProjectReferenceHelper.CreateVisualStudioReferenceFromReference(_solutionManager.CurrentSolution, pReference);
                _callback(this, new ProjectChangedEventArg {Project=project, ProjectReference = rf, Action = EventAction.ReferenceRemoved });
            }

            /// <summary>
            /// Referenceses the events_ reference changed.
            /// </summary>
            /// <param name="pReference">The p reference.</param>
            void OnReferenceChanged(Reference pReference)
            {
                if (!_enabled)
                    return;
#if DEBUG
                Debug.WriteLine(String.Format("VS reference {0} changed from {1}", pReference.Name, pReference.ContainingProject.Name));
#endif
                ProjectNode project = _solutionManager.CurrentSolution.GetProject(pReference.ContainingProject.UniqueName);
                ProjectReference rf = ProjectReferenceHelper.CreateVisualStudioReferenceFromReference(_solutionManager.CurrentSolution, pReference);
                _callback(this, new ProjectChangedEventArg { Project = project, ProjectReference = rf, Action = EventAction.ReferenceChanged });
            }

            /// <summary>
            /// Referenceses the events_ reference added.
            /// </summary>
            /// <param name="pReference">The p reference.</param>
            void OnReferenceAdded(Reference pReference)
            {
                if (!_enabled)
                    return;
#if DEBUG
                Debug.WriteLine(String.Format("VS reference {0} added from {1}", pReference.Name, pReference.ContainingProject.Name));
#endif
                ProjectNode project = _solutionManager.CurrentSolution.GetProject(pReference.ContainingProject.UniqueName);
                _callback(this, new ProjectChangedEventArg { Project = project, Action = EventAction.ReferenceAdded });

            }

            /// <summary>
            /// Enables the events.
            /// </summary>
            internal void EnableEvents()
            {
                _enabled = true;
            }

            /// <summary>
            /// Disables the events.
            /// </summary>
            internal void DisableEvents()
            {
                _enabled = false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionReferencesListener"/> class.
        /// </summary>
        public SolutionListener(SolutionManagerService solutionManager, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _solutionManager = solutionManager;

            var solution = (Microsoft.VisualStudio.Shell.Interop.IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out this._cookieIVsSolutionEvents));
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="SolutionReferencesListener"/> is reclaimed by garbage collection.
        /// </summary>
        ~SolutionListener()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_cookieIVsSolutionEvents != 0)
            {
                if (_serviceProvider != null)
                {
                    var solution = (Microsoft.VisualStudio.Shell.Interop.IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
                    if (solution != null)
                        ErrorHandler.ThrowOnFailure(solution.UnadviseSolutionEvents(this._cookieIVsSolutionEvents));
                    _cookieIVsSolutionEvents = 0;
                }
            }
        }
        
        /// <summary>
        /// Registers the project events.
        /// </summary>
        /// <param name="project">The project.</param>
        private void RegisterProjectEvents(ProjectNode project)
        {
            if (project != null)
            {
                _projectListeners.Add(new ProjectListener(_solutionManager, project, OnProjectChanged));
            }
        }

        /// <summary>
        /// Called when [references changed].
        /// </summary>
        /// <param name="e">The e.</param>
        private void OnProjectChanged(object sender, ProjectChangedEventArg e)
        {
            if (ProjectChanged != null)
            {
                ProjectChanged(this, e);
            }
        }

        /// <summary>
        /// Unregister project events.
        /// </summary>
        /// <param name="project">The project.</param>
        private void UnRegisterProjectEvents(ProjectNode project)
        {
            if (project != null)
            {
                VSProject vsp = project.VSProject;
                if (vsp != null)
                {
                    ProjectListener pl = _projectListeners.Find(l => l.Name == project.UniqueName);
                    if (pl != null)
                    {
                        pl.Dispose();
                        _projectListeners.Remove(pl);
                    }
                }
            }
        }

        #region IVsSolutionEvents2 Members

        /// <summary>
        /// Called when [after close solution].
        /// </summary>
        /// <param name="pUnkReserved">The p unk reserved.</param>
        /// <returns></returns>
        public int OnAfterCloseSolution(object pUnkReserved)
        {
#if DEBUG
            Debug.WriteLine("Enter " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            if( SolutionEvents != null )
            {
                SolutionEvents(_serviceProvider, new SolutionEventArgs(true));
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when [after load project].
        /// </summary>
        /// <param name="pStubHierarchy">The stub hierarchy.</param>
        /// <param name="pRealHierarchy">The real hierarchy.</param>
        /// <returns></returns>
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when [after merge solution].
        /// </summary>
        /// <param name="pUnkReserved">The p unk reserved.</param>
        /// <returns></returns>
        public int OnAfterMergeSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when [after open project].
        /// </summary>
        /// <param name="pHierarchy">The p hierarchy.</param>
        /// <param name="fAdded">The f added.</param>
        /// <returns></returns>
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
#if DEBUG
            //ProjectNode prj = ((SolutionNode)_solutionManager.CurrentSolution).GetProject(pHierarchy);
            //Debug.WriteLine(String.Format("{0} project {1}", fAdded == 0 ? "open" : "add", prj.Name));
#endif
            EventAction action = EventAction.ProjectAdded;
            PerformProjectChanged(pHierarchy, action, true);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Performs the project changed.
        /// </summary>
        /// <param name="pHierarchy">The p hierarchy.</param>
        /// <param name="action">The action.</param>
        /// <param name="raiseEvent">if set to <c>true</c> [raise event].</param>
        private void PerformProjectChanged(IVsHierarchy pHierarchy, EventAction action, bool raiseEvent)
        {
#if DEBUG
            Debug.WriteLine("Enter " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            if (_solutionManager.CurrentSolution == null)
                return;

            ProjectNode prj = ((SolutionNode)_solutionManager.CurrentSolution).GetProject(pHierarchy);
            // Test ce projet ne doit pas répondre aux événements
            if (IsProjectEventsEnabled(prj))
            {
                if (action == EventAction.ProjectAdded)
                    RegisterProjectEvents(prj);
                else if (action == EventAction.ProjectRemoved)
                    UnRegisterProjectEvents(prj);
                if (raiseEvent )
                    OnProjectChanged(this, new ProjectChangedEventArg { Project = prj, Action = action });
            }
        }

        /// <summary>
        /// Determines whether [is project events disabled] [the specified PRJ].
        /// </summary>
        /// <param name="prj">The PRJ.</param>
        /// <returns>
        /// 	<c>true</c> if [is project events disabled] [the specified PRJ]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsProjectEventsEnabled(ProjectNode prj)
        {
            if (!prj.IsProject)
                return false;

            foreach (string uniqueName in _projectEventsDisabled)
            {
                if (prj.UniqueName == uniqueName || prj.Name == uniqueName)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Called when [after open solution].
        /// </summary>
        /// <param name="pUnkReserved">The p unk reserved.</param>
        /// <param name="fNewSolution">The f new solution.</param>
        /// <returns></returns>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
#if DEBUG
            Debug.WriteLine("Enter " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            try
            {
                if( SolutionEvents != null )
                {
                    SolutionEvents(_serviceProvider, new SolutionEventArgs(false));
                }

                OnProjectChanged(this, new ProjectChangedEventArg { Project = _solutionManager.CurrentSolution, Action = EventAction.SolutionLoaded });
            }
            catch { }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when [before close project].
        /// </summary>
        /// <param name="pHierarchy">The p hierarchy.</param>
        /// <param name="fRemoved">The f removed.</param>
        /// <returns></returns>
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            if (_solutionManager == null || _solutionManager.CurrentSolution == null)
                return VSConstants.S_OK;

            ProjectNode prj = ((SolutionNode)_solutionManager.CurrentSolution).GetProject(pHierarchy);
            if (_unloadedProject != null && _unloadedProject.ProjectGuid == prj.ProjectGuid)
            {
                // On ignore si le fichier a été déchargé
                _unloadedProject = null;
                return VSConstants.S_OK;
            }
#if DEBUG
            Debug.WriteLine(String.Format("VS {0} project {1}", fRemoved == 0 ? "close" : "remove", prj.Name));
#endif
            // On distingue la fermeture de la suppression. Cette dernière ne génére pas la mise à jour du modèle
            PerformProjectChanged(pHierarchy, EventAction.ProjectRemoved, fRemoved!=0);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when [before close solution].
        /// </summary>
        /// <param name="pUnkReserved">The p unk reserved.</param>
        /// <returns></returns>
        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called when [before unload project].
        /// </summary>
        /// <param name="pRealHierarchy">The p real hierarchy.</param>
        /// <param name="pStubHierarchy">The p stub hierarchy.</param>
        /// <returns></returns>
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            _unloadedProject = ((SolutionNode)_solutionManager.CurrentSolution).GetProject(pRealHierarchy);
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion


        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Enables the reference events on project.
        /// </summary>
        /// <param name="projectUniqueName">Name of the project unique.</param>
        internal void EnableReferenceEventsOnProject()
        {
            Debug.Assert(_projectEventsDisabled.Count > 0);
            string uniqueName = _projectEventsDisabled.Dequeue();
            if (uniqueName == "*")
            {
                foreach (ProjectListener projectListener in _projectListeners)
                {
                    projectListener.EnableEvents();
                }
                return;
            }        

            ProjectListener pl = _projectListeners.Find(l => l.Name == uniqueName);
            if (pl != null)
            {
                pl.EnableEvents();
            }
        }

        /// <summary>
        /// Disables the reference events on project.
        /// </summary>
        /// <param name="project">The project.</param>
        internal void DisableReferenceEventsOnProject(string projectName)
        {
            _projectEventsDisabled.Enqueue(projectName);
            if (projectName == "*")
            {
                foreach (ProjectListener projectListener in _projectListeners)
                {
                    projectListener.DisableEvents();
                }
                return;
            }        
            ProjectListener pl = _projectListeners.Find(l => l.Name == projectName);
            if (pl != null)
            {
                pl.DisableEvents();
            }
        }

        #region IVsSolutionEvents4 Members

        public int OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterChangeProjectParent(IVsHierarchy pHierarchy)
        {
#if DEBUG
            Debug.WriteLine("Enter " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            PerformProjectChanged(pHierarchy, EventAction.ProjectChanged, true);
            return VSConstants.S_OK;
        }

        public int OnAfterRenameProject(IVsHierarchy pHierarchy)
        {
#if DEBUG
            Debug.WriteLine("Enter " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            PerformProjectChanged(pHierarchy, EventAction.ProjectChanged, true);
            return VSConstants.S_OK;
        }

        public int OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
