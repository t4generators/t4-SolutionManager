using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    public class BuildCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is rebuild.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is rebuild; otherwise, <c>false</c>.
        /// </value>
        public bool IsRebuild { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BuildCompleteEventArgs"/> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }
        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        /// <value>The name of the configuration.</value>
        public string ConfigurationName { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SolutionBuildListener
    {
        private IServiceProvider _serviceProvider;
        private BuildEvents _buildEvents;
        private bool _buildSuccess;
        private bool _disposing;
        private string _lastConfigurationName;

        public event EventHandler<BuildCompleteEventArgs> BuildCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionBuildListener"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public SolutionBuildListener(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            DTE dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            if (dte == null)
                return;

            _buildEvents = dte.Events.BuildEvents;

            try
            {
                _buildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;
            }
            catch { }
            try
            {
                _buildEvents.OnBuildBegin += OnBuildBegin;
            }
            catch { }
            try
            {
                _buildEvents.OnBuildDone += OnBuildDone;
            }
            catch { }
        }

        /// <summary>
        /// Builds the events_ on build done.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="action">The action.</param>
        void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (action == vsBuildAction.vsBuildActionBuild || action == vsBuildAction.vsBuildActionRebuildAll)
            {
                if (scope == vsBuildScope.vsBuildScopeSolution || scope == vsBuildScope.vsBuildScopeProject)
                {
                    OnBuildCompleted(new BuildCompleteEventArgs
                    {                        
                        IsRebuild = action == vsBuildAction.vsBuildActionRebuildAll,
                        ConfigurationName = _lastConfigurationName,
                        Success = _buildSuccess
                    });
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:BuildCompleted"/> event.
        /// </summary>
        /// <param name="e">The <see cref="DSLFactory.VSDM.DependenciesModel.BuildCompleteEventArgs"/> instance containing the event data.</param>
        private void OnBuildCompleted(BuildCompleteEventArgs e)
        {
            if (BuildCompleted != null)
                BuildCompleted(_serviceProvider, e);
        }

        /// <summary>
        /// Démarrage du processus de build
        /// </summary>
        /// <param name="Scope">The scope.</param>
        /// <param name="Action">The action.</param>
        void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            _buildSuccess = false;
        }

        /// <summary>
        /// A chaque fin de compil d'un projet, on vérifie qu'elle s'est bien terminée
        /// </summary>
        /// <param name="Project">The project.</param>
        /// <param name="ProjectConfig">The project config.</param>
        /// <param name="Platform">The platform.</param>
        /// <param name="SolutionConfig">The solution config.</param>
        /// <param name="Success">if set to <c>true</c> [success].</param>
        void OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            // On veut savoir si les compilations se sont bien déroulées
            if (Success)
                _buildSuccess = true;
            _lastConfigurationName = ProjectConfig;

        }

        #region IDisposable Members
        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ProjectEventsDisabler"/> is reclaimed by garbage collection.
        /// </summary>
        ~SolutionBuildListener()
        {
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposing && _buildEvents != null)
            {
                try
                {
                    _buildEvents.OnBuildDone -= OnBuildDone;
                    _buildEvents.OnBuildBegin -= OnBuildBegin;
                    _buildEvents.OnBuildProjConfigDone -= OnBuildProjConfigDone;
                }
                catch { }
            }
            _disposing = true;

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}