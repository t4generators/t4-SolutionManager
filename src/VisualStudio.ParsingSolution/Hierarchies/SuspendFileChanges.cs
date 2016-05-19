using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace VsxFactory.Modeling
{
    internal class SuspendFileChanges
    {
        // Fields
        private string documentFileName;
        private IVsDocDataFileChangeControl fileChangeControl;
        private bool isSuspending;
        private IServiceProvider site;

        // Methods
        internal SuspendFileChanges(IServiceProvider site, string document)
        {
            this.site = site;
            this.documentFileName = document;
        }

        public void Resume()
        {
            if (this.isSuspending)
            {
                IVsFileChangeEx service = this.site.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
                if (service != null)
                {
                    this.isSuspending = false;
                    ErrorHandler.ThrowOnFailure(service.IgnoreFile(0, this.documentFileName, 0));
                    if (this.fileChangeControl != null)
                    {
                        ErrorHandler.ThrowOnFailure(this.fileChangeControl.IgnoreFileChanges(0));
                    }
                }
            }
        }

        public void Suspend()
        {
            if (!this.isSuspending)
            {
                IntPtr zero = IntPtr.Zero;
                try
                {
                    IVsRunningDocumentTable service = this.site.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
                    if (service != null)
                    {
                        IVsHierarchy hierarchy;
                        uint num;
                        uint num2;
                        ErrorHandler.ThrowOnFailure(service.FindAndLockDocument(0, this.documentFileName, out hierarchy, out num, out zero, out num2));
                        if ((num2 != 0) && (zero != IntPtr.Zero))
                        {
                            IVsFileChangeEx ex = this.site.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
                            if (ex != null)
                            {
                                this.isSuspending = true;
                                ErrorHandler.ThrowOnFailure(ex.IgnoreFile(0, this.documentFileName, 1));
                                if (zero != IntPtr.Zero)
                                {
                                    IVsPersistDocData data = null;
                                    object objectForIUnknown = Marshal.GetObjectForIUnknown(zero);
                                    if (objectForIUnknown is IVsPersistDocData)
                                    {
                                        data = (IVsPersistDocData)objectForIUnknown;
                                        if (data is IVsDocDataFileChangeControl)
                                        {
                                            this.fileChangeControl = (IVsDocDataFileChangeControl)data;
                                            if (this.fileChangeControl != null)
                                            {
                                                ErrorHandler.ThrowOnFailure(this.fileChangeControl.IgnoreFileChanges(1));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (InvalidCastException exception)
                {
                    Trace.WriteLine("Exception" + exception.Message);
                }
                finally
                {
                    if (zero != IntPtr.Zero)
                    {
                        Marshal.Release(zero);
                    }
                }
            }
        }
    }
}
