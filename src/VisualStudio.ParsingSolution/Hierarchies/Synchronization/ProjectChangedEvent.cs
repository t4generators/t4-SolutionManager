using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    /// <summary>
    /// 
    /// </summary>
    public enum EventAction
    {
        /// <summary>
        /// 
        /// </summary>
        SolutionLoaded,
        /// <summary>
        /// 
        /// </summary>
        ProjectRemoved,
        /// <summary>
        /// 
        /// </summary>
        ProjectAdded,
        /// <summary>
        /// 
        /// </summary>
        ProjectChanged,
        /// <summary>
        /// 
        /// </summary>
        ReferenceChanged,
        /// <summary>
        /// 
        /// </summary>
        ReferenceAdded,
        /// <summary>
        /// 
        /// </summary>
        ReferenceRemoved
    }

    /// <summary>
    /// 
    /// </summary>
    public class ProjectChangedEventArg : EventArgs
    {
        public HierarchyNode Project { get; set; }
        public EventAction Action { get; set; }
        public ProjectReference ProjectReference { get; set; }
    }
}
