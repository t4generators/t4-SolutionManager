using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using VsxFactory.Modeling.VisualStudio;
using Microsoft.VisualStudio;
using System.IO;

namespace VsxFactory.Modeling
{
    /// <summary>
    /// 
    /// </summary>
    public class FileSavedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the item node.
        /// </summary>
        /// <value>The item node.</value>
        public HierarchyNode ItemNode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSavedEventArgs"/> class.
        /// </summary>
        /// <param name="node">The node.</param>
        public FileSavedEventArgs(HierarchyNode node)
        {
            ItemNode = node;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SolutionFilesChangeManager : IVsRunningDocTableEvents, IDisposable
    {
        private IVsRunningDocumentTable _runningDocumentTableService;
        private uint _rdtCookie;
        public List<ObserveRule> Rules { get; private set; }
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Occurs when [file saved].
        /// </summary>
        public event EventHandler<FileSavedEventArgs> FileSaved;

        public class ObserveRule
        {
            public string Pattern { get; private set; }

            public ObserveRule(string pattern)
            {
                Pattern = pattern;
            }

            public bool IsMatch(string fileName)
            {
                return Utils.IsMatchPattern(Pattern, fileName);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFilesChangeManager"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="patterns">The patterns.</param>
        public SolutionFilesChangeManager(IServiceProvider serviceProvider, params string[] patterns)
        {
            Guard.ArgumentNotNull(serviceProvider, "serviceProvider");
            Guard.ArgumentNotNull(patterns, "patterns");
            Rules = new List<ObserveRule>();
            foreach (var pattern in patterns)
            {
                Rules.Add(new ObserveRule(pattern));
            }
            _serviceProvider = serviceProvider;
            _runningDocumentTableService = serviceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            _runningDocumentTableService.AdviseRunningDocTableEvents(this, out _rdtCookie);
        }

        #region IDisposable Members
        private bool _disposed;
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            ErrorHandler.ThrowOnFailure(this._runningDocumentTableService.UnadviseRunningDocTableEvents(_rdtCookie));
            _disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="SolutionFilesChangeManager"/> is reclaimed by garbage collection.
        /// </summary>
        ~SolutionFilesChangeManager()
        {
            Dispose(false);
        }
        #endregion


        #region IVsRunningDocTableEvents Members

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            if( FileSaved == null)
                return VSConstants.S_OK;

            // TODO ne pas chercher en tant que service
            ISolutionManagerService solutionManager = _serviceProvider.GetService(typeof(ISolutionManagerService)) as ISolutionManagerService;
            if (solutionManager.CurrentSolution == null)
                return VSConstants.S_OK;

            uint pgrfRDTFlags;
            uint pdwReadLocks;
            uint pdwEditLocks;
            string pbstrMkDocument;
            IVsHierarchy ppHier;
            uint pitemid;
            IntPtr ppunkDocData; 
            _runningDocumentTableService.GetDocumentInfo(docCookie, out pgrfRDTFlags, out pdwReadLocks, out pdwEditLocks,
                out pbstrMkDocument, out ppHier, out pitemid, out ppunkDocData);
            
            var projectNode = new HierarchyNode(solutionManager.CurrentSolution.Solution, ppHier);
            if (projectNode != null)
            {
                var node = new HierarchyNode(projectNode, pitemid);
                if (node != null)
                {
                    foreach (var rule in Rules)
                    {
                        if (rule.IsMatch(node.Path))
                            FileSaved(this, new FileSavedEventArgs(node));
                    }
                }
            }
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
