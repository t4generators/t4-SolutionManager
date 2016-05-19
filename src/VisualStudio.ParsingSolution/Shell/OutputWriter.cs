using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;

namespace VisualStudio.ParsingSolution
{

    public class OutputWriter
    {

        #region Ctors

        public OutputWriter()
            : this(VSConstants.GUID_OutWindowGeneralPane, "Output")
        {
        
        }

        public OutputWriter(Guid customOutput, string customTitle)
        {
            generalPaneGuid = customOutput;
            _title = customTitle;
        }

        #endregion

        /// <summary>
        /// Writes the exception in the output windows.
        /// </summary>
        /// <param name="e">The e.</param>
        public void WriteLine(Exception e)
        {
            this.WriteLine(e.ToString());
        }

        /// <summary>
        /// Writes the specified message in the output windows.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        public void Write(string message, params object[] parameters)
        {

            IVsOutputWindowPane generalPane = GetPane();

            if (parameters != null && parameters.Length > 0)
                message = string.Format(message, parameters);

            generalPane.OutputString(message);
            generalPane.Activate();


        }

        /// <summary>
        /// Writes the message in the output windows.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        public void WriteLine(string message, params object[] parameters)
        {

            IVsOutputWindowPane generalPane = GetPane();

            if (parameters != null && parameters.Length > 0)
                message = string.Format(message, parameters);

            generalPane.OutputString(message + "\n");

            generalPane.Activate();

        }

        private IVsOutputWindowPane GetPane()
        {

            if (generalPane != null)
                return generalPane;

            outWindow.GetPane(ref generalPaneGuid, out generalPane);

            if (generalPane == null)
            {
                outWindow.CreatePane(ref generalPaneGuid, _title, 1, 1);
                outWindow.GetPane(ref generalPaneGuid, out generalPane);
            }

            return generalPane;

        }

        /// <summary>
        /// Gets or sets the _title.
        /// </summary>
        /// <value>
        /// The _title.
        /// </value>
        public string _title { get; set; }

        #region private

        IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
        Guid generalPaneGuid;
        IVsOutputWindowPane generalPane;

        #endregion

    }

}

