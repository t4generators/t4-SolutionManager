/***************************************************************************

Copyright (c) 2008 Microsoft Corporation. All rights reserved.

***************************************************************************/

using System;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace VisualStudio.ParsingSolution
{
    /// <summary>
    /// Class to interact with the VS error list window
    /// </summary>
    public class VSErrorsListWindow
    {
        #region Fields
        IServiceProvider _serviceProvider;
        int _errorCount;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorListWindow"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public VSErrorsListWindow(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }
        #endregion

        #region Properties
        private ErrorListProvider errorListProvider;

        /// <summary>
        /// Gets the error list provider.
        /// </summary>
        /// <value>The error list provider.</value>
        protected ErrorListProvider ErrorListProvider
        {
            get
            {
                if (errorListProvider == null)
                {
                    errorListProvider = new ErrorListProvider(this._serviceProvider);
                }

                return errorListProvider;
            }
        }
        #endregion

        #region Public Implementation
        /// <summary>
        /// Shows the errors.
        /// </summary>
        public void Show()
        {
            this.ErrorListProvider.Show();
        }

        /// <summary>
        /// Gets the errors count.
        /// </summary>
        /// <value>The errors count.</value>
        public bool HasErrors
        {
            get
            {
                return _errorCount > 0;
            }
        }

        /// <summary>
        /// Writes the error.
        /// </summary>
        /// <param name="message">The message.</param>
        public void WriteError(string message, EventLogEntryType type)
        {
            // Pas de duplication du message
            foreach (ErrorTask task in ErrorListProvider.Tasks)
            {
                if (task.Text == message)
                    return;
            }

            ErrorTask errorTask = new ErrorTask();

            TaskErrorCategory errorCategory = TaskErrorCategory.Error;
            switch (type)
            {
                case EventLogEntryType.Error:
                case EventLogEntryType.FailureAudit:
                    errorCategory = TaskErrorCategory.Error;
                    _errorCount++;
                    break;
                case EventLogEntryType.Information:
                case EventLogEntryType.SuccessAudit:
                    errorCategory = TaskErrorCategory.Message;
                    break;
                case EventLogEntryType.Warning:
                    errorCategory = TaskErrorCategory.Warning;
                    break;
            }

            errorTask.CanDelete = false;
            errorTask.ErrorCategory = errorCategory;
            errorTask.Text = message;

            this.ErrorListProvider.Tasks.Add(errorTask);
            Show();
        }

        /// <summary>
        /// Clears the errors.
        /// </summary>
        public void ClearErrors()
        {
            this.ErrorListProvider.Tasks.Clear();
            _errorCount = 0;
        }

        #endregion
    }
}
