using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    /// <summary>
    /// 
    /// </summary>
    public delegate void SolutionEventsHandler(object sender, SolutionEventArgs e);

    /// <summary>
    /// 
    /// </summary>
    public class SolutionEventArgs : EventArgs
    {
        private bool _close;

        /// <summary>
        /// Gets a value indicating whether this <see cref="SolutionEventArgs"/> is closing.
        /// </summary>
        /// <value><c>true</c> if closing; otherwise, <c>false</c>.</value>
        public bool Closing { get { return _close; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionEventArgs"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="close">if set to <c>true</c> [close].</param>
        public SolutionEventArgs(bool close)
        {
            _close = close;
        }
    }
}
