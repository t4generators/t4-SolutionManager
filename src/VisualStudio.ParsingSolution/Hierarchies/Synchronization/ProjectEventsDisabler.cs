using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    public sealed class ProjectEventsDisabler : IDisposable
    {
        private bool _disposing;
        private readonly IVsSolutionExplorer _solutionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectEventsDisabler"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public ProjectEventsDisabler(IVsSolutionExplorer solutionManager, string projectName)
        {
            Guard.ArgumentNotNull(solutionManager, "solutionManager");
            Guard.ArgumentNotNullOrEmptyString(projectName, "projectName");

            _solutionManager = solutionManager;
            _solutionManager.DisableReferenceEventsOnProject(projectName);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ProjectEventsDisabler"/> is reclaimed by garbage collection.
        /// </summary>
        ~ProjectEventsDisabler()
        {
            Dispose();
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposing)
            {
                _disposing = true;
                if (_solutionManager != null)
                    _solutionManager.EnableReferenceEventsOnProject();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
