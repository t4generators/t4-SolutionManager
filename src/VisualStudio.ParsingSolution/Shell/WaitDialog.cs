using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualStudio.ParsingSolution
{

    /// <summary>
    /// show the visual studio wait form dialog
    /// </summary>
    public class WaitDialog : IDisposable
    {

        private IVsThreadedWaitDialog _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitDialog"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="text">The text.</param>
        public WaitDialog(IServiceProvider serviceProvider, string caption, string text)
        {
            _service = serviceProvider.GetService(typeof(SVsThreadedWaitDialog)) as IVsThreadedWaitDialog;
            if (_service != null)
            {
                _service.StartWaitDialog(caption, text, null, 0, null, null);
            }

        }

        /// <summary>
        /// Updates the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>false if the user has canceled the operation</returns>
        public bool UpdateText(string text)
        {
            if (_service != null)
            {
                int pfCancelled;
                _service.GiveTimeSlice(text, null, 1, out pfCancelled);
                return pfCancelled == 0;
            }
            return true;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public void Dispose()
        {
            if (_service != null)
            {
                int pfCancelled = 0;
                _service.EndWaitDialog(ref pfCancelled);
            }
        }
    }
}
