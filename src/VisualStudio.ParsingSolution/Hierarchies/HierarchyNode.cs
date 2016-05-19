using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using EnvDTE;
using System.IO;

namespace VsxFactory.Modeling.VisualStudio
{
    [DebuggerDisplay("Name={Name}")]
    public class HierarchyNode : IDisposable
    {
        private static readonly Guid SolutionFolderGuid = new Guid("{6bb5f8f0-4483-11d3-8bcf-00c04f8ec28c}");

        #region NativeMethods class

        private sealed class NativeMethods
        {
            private NativeMethods() { }
            [StructLayout(LayoutKind.Sequential)]
            public struct SHFILEINFO
            {
                public IntPtr hIcon;
                public IntPtr iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                public string szTypeName;
            };

            public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
            public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010; // use passed dwFileAttribute
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            public const uint SHGFI_SMALLICON = 0x1; // 'Small icon

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
            [DllImport("comctl32.dll")]
            public extern static IntPtr ImageList_GetIcon(IntPtr himl, int i, uint flags);
            //[DllImport("comctl32.dll")]
            //public extern static int ImageList_GetImageCount(IntPtr himl);

            public static bool IsSameComObject(object obj1, object obj2)
            {
                bool flag;
                IntPtr zero = IntPtr.Zero;
                IntPtr pUnk = IntPtr.Zero;
                try
                {
                    if (obj1 != null)
                    {
                        zero = Marshal.GetIUnknownForObject(obj1);
                    }
                    if (obj2 != null)
                    {
                        pUnk = Marshal.GetIUnknownForObject(obj2);
                    }
                    flag = zero == pUnk;
                }
                finally
                {
                    if (zero != IntPtr.Zero)
                    {
                        Marshal.Release(zero);
                    }
                    if (pUnk != IntPtr.Zero)
                    {
                        Marshal.Release(pUnk);
                    }
                }
                return flag;
            }
        }

        #endregion

        /// <summary>
        /// Constructs a HierarchyNode at the solution root
        /// </summary>
        /// <param name="vsSolution"></param>
        // FXCOP: false positive.
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public HierarchyNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution)
            : this(vsSolution, Guid.Empty)
        {
        }

        /// <summary>
        /// Constructs a HierarchyNode given the unique string identifier
        /// </summary>
        /// <param name="vsSolution"></param>
        /// <param name="projectUniqueName"></param>
        // FXCOP: false positive.
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public HierarchyNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, string projectUniqueName)
        {
            Guard.ArgumentNotNull(vsSolution, "vsSolution");
            Guard.ArgumentNotNullOrEmptyString(projectUniqueName, "projectUniqueName");

            Microsoft.VisualStudio.Shell.Interop.IVsHierarchy rootHierarchy = null;
            if (projectUniqueName.StartsWith("{") && projectUniqueName.EndsWith("}"))
            {
                projectUniqueName = projectUniqueName.Substring(1, projectUniqueName.Length - 2);
            }
            if (projectUniqueName.Length == Guid.Empty.ToString().Length && projectUniqueName.Split('-').Length == 5)
            {
                Guid projectGuid = new Guid(projectUniqueName);
                int hr = vsSolution.GetProjectOfGuid(ref projectGuid, out rootHierarchy);
                Marshal.ThrowExceptionForHR(hr);
            }
            else
            {
                int hr = vsSolution.GetProjectOfUniqueName(projectUniqueName, out rootHierarchy);
                Marshal.ThrowExceptionForHR(hr);
            }
            Init(vsSolution, rootHierarchy, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Constructs a HierarchyNode given the projectGuid
        /// </summary>
        /// <param name="vsSolution"></param>
        /// <param name="projectGuid"></param>
        // FXCOP: False positive
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public HierarchyNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, Guid projectGuid)
        {
            Guard.ArgumentNotNull(vsSolution, "vsSolution");

            IVsHierarchy rootHierarchy = null;
            int hr = vsSolution.GetProjectOfGuid(ref projectGuid, out rootHierarchy);
            if (rootHierarchy == null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, VsxFactory.Modeling.Properties.Resources.ProjectDoesNotExist, projectGuid.ToString("b")),
                    Marshal.GetExceptionForHR(hr));
            }
            Init(vsSolution, rootHierarchy, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Constructs a hierarchy node at the root level of hierarchy
        /// </summary>
        /// <param name="vsSolution"></param>
        /// <param name="hierarchy"></param>
        // FXCOP: False positive
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public HierarchyNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, IVsHierarchy hierarchy)
        {
            Guard.ArgumentNotNull(vsSolution, "vsSolution");

            Init(vsSolution, hierarchy, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Constructs a hierarchy node
        /// </summary>
        /// <param name="vsSolution"></param>
        /// <param name="hierarchy"></param>
        /// <param name="itemId"></param>
        // FXCOP: False positive
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public HierarchyNode(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, IVsHierarchy hierarchy, uint itemId)
        {
            Guard.ArgumentNotNull(vsSolution, "vsSolution");
            Init(vsSolution, hierarchy, itemId);
        }

        /// <summary>
        /// Builds a child HierarchyNode from the parent node
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childId"></param>
        // FXCOP: False positive
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public HierarchyNode(HierarchyNode parent, uint childId)
        {
            Guard.ArgumentNotNull(parent, "parent");

            Init(parent.solution, parent.hierarchy, childId);
        }

        /// <summary>
        /// Queries the type T to the internal hierarchy object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetObject<T>()
            where T : class
        {
            return (hierarchy as T);
        }

        /// <summary>
        /// Returns true if this a root node of another node
        /// </summary>
        public bool IsRoot
        {
            get { return (VSConstants.VSITEMID_ROOT == itemId); }
        }

        /// <summary>
        /// Casts to project node.
        /// </summary>
        /// <returns></returns>
        public ProjectNode CastToProjectNode()
        {
            System.Diagnostics.Contracts.Contract.Ensures(System.Diagnostics.Contracts.Contract.Result<ProjectNode>() != null);
            return new ProjectNode(this);
        }

        /// <summary>
        /// Name of this node
        /// </summary>
        public string Name
        {
            get { return GetProperty<string>(__VSHPROPID.VSHPROPID_Name); }
            set { SetProperty(__VSHPROPID.VSHPROPID_Name, value); }
        }

        /// <summary>
        /// Document cookie
        /// </summary>
        public uint DocCookie
        {
            get { return GetProperty<uint>(__VSHPROPID.VSHPROPID_ItemDocCookie); }
        }

        /// <summary>
        /// Name of this node
        /// </summary>
        public string CanonicalName
        {
            get
            {
                string name = string.Empty;
                int hr = hierarchy.GetCanonicalName(itemId, out name);
                Marshal.ThrowExceptionForHR(hr);
                if (name != null)
                {
                    return name;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the unique string that identifies this node in the solution
        /// </summary>
        public string UniqueName
        {
            get
            {
                string uniqueName = string.Empty;
                int hr = solution.GetUniqueNameOfProject(hierarchy, out uniqueName);
                Marshal.ThrowExceptionForHR(hr);
                return uniqueName;
            }
        }

        /// <summary>
        /// Returns the Project GUID
        /// </summary>
        public Guid ProjectGuid
        {
            get { return GetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid); }
            set { SetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, value); }
        }

        /// <summary>
        /// Returns true if the current node is the solution root
        /// </summary>
        public bool IsSolution
        {
            get
            {
                return (Parent == null);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is solution folder.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is solution folder; otherwise, <c>false</c>.
        /// </value>
        public bool IsFolder
        {
            get
            {
                Project p = this.ExtObject as Project;
                return p != null && (
                    p.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder ||
                    p.Kind == EnvDTE.Constants.vsProjectKindSolutionItems);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is solution items folder.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is solution items folder; otherwise, <c>false</c>.
        /// </value>
        public bool IsSolutionItemsFolder
        {
            get
            {
                Project p = this.ExtObject as Project;
                return p != null && p.Name == SolutionNode.SolutionItemsName;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is project.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is project; otherwise, <c>false</c>.
        /// </value>
        public bool IsProject
        {
            get
            {
                Project p = this.ExtObject as Project;
                return p != null && !IsFolder && !IsSolutionItemsFolder && !IsSolution;
            }
        }

        /// <summary>
        /// Returns the TypeGUID
        /// </summary>
        public Guid TypeGuid
        {
            get
            {
                // If the root node is a solution, then there is no TypeGuid
                if (IsSolution)
                {
                    return Guid.Empty;
                }
                else
                {
                    return GetGuidProperty(__VSHPROPID.VSHPROPID_TypeGuid, this.ItemId);
                }
            }
        }

        /// <summary>
        /// Icon handle of the node
        /// </summary>
        public IntPtr IconHandle
        {
            get { return new IntPtr(GetProperty<int>(__VSHPROPID.VSHPROPID_IconHandle)); }
        }

        /// <summary>
        /// Icon index of the node
        /// </summary>
        public int IconIndex
        {
            get { return GetProperty<int>(__VSHPROPID.VSHPROPID_IconIndex); }
        }

        /// <summary>
        /// True if the Icon index of the node is valid
        /// </summary>
        public bool HasIconIndex
        {
            get { return HasProperty(__VSHPROPID.VSHPROPID_IconIndex); }
        }

        /// <summary>
        /// StateIcon index of the node
        /// </summary>
        public int StateIconIndex
        {
            get { return GetProperty<int>(__VSHPROPID.VSHPROPID_StateIconIndex); }
        }

        /// <summary>
        /// OverlayIcon index of the node
        /// </summary>
        public int OverlayIconIndex
        {
            get { return GetProperty<int>(__VSHPROPID.VSHPROPID_OverlayIconIndex); }
        }

        /// <summary>
        /// Imagelist Handle
        /// </summary>
        public IntPtr ImageListHandle
        {
            get { return new IntPtr(GetProperty<int>(__VSHPROPID.VSHPROPID_IconImgList)); }
        }

        private string iconKey;
        /// <summary>
        /// Returns the Key to index icons in an image collection
        /// </summary>
        public string IconKey
        {
            get
            {
                if (iconKey == null)
                {
                    if (HasIconIndex)
                    {
                        iconKey = TypeGuid.ToString("b", CultureInfo.InvariantCulture) + "." + IconIndex.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (IsValidFullPathName(SaveName))
                    {
                        iconKey = new FileInfo(SaveName).Extension;
                    }
                    else
                    {
                        iconKey = string.Empty;
                    }
                }
                return iconKey;
            }
        }

        /// <summary>
        /// item id
        /// </summary>
        private uint itemId;

        protected internal uint ItemId
        {
            get { return itemId; }
        }

        /// <summary>
        /// hierarchy object
        /// </summary>
        private IVsHierarchy hierarchy;

        protected IVsHierarchy Hierarchy
        {
            get { return hierarchy; }
        }

        /// <summary>
        /// Solution service
        /// </summary>
        private Microsoft.VisualStudio.Shell.Interop.IVsSolution solution;

        internal Microsoft.VisualStudio.Shell.Interop.IVsSolution Solution
        {
            get { return solution; }
        }

        protected bool IsValidFullPathName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            int i = fileName.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
            if (i == -1)
            {
                return IsValidFileName(fileName);
            }
            else
            {
                string pathPart = fileName.Substring(0, i + 1);
                if (IsValidPath(pathPart))
                {
                    string filePart = fileName.Substring(i + 1);
                    return IsValidFileName(filePart);
                }
            }
            return false;
        }

        protected bool IsValidPath(string pathPart)
        {
            if (string.IsNullOrEmpty(pathPart))
            {
                return true;
            }
            foreach (char c in System.IO.Path.GetInvalidPathChars())
            {
                if (pathPart.IndexOf(c) != -1)
                {
                    return false;
                }
            }
            return true;
        }

        protected bool IsValidFileName(string filePart)
        {
            if (string.IsNullOrEmpty(filePart))
            {
                return false;
            }
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                if (filePart.IndexOf(c) != -1)
                {
                    return false;
                }
            }
            return true;
        }


        private Icon icon;
        /// <summary>
        /// Returns the icon of the node
        /// </summary>
        public Icon Icon
        {
            get
            {
                if (icon == null)
                {
                    if (ImageListHandle != IntPtr.Zero && HasIconIndex)
                    {
                        IntPtr hIcon = NativeMethods.ImageList_GetIcon(ImageListHandle, IconIndex, 0);
                        icon = Icon.FromHandle(hIcon);
                    }
                    else if (IconHandle != IntPtr.Zero)
                    {
                        icon = Icon.FromHandle(IconHandle);
                    }
                    else if (IsValidFullPathName(SaveName))
                    {
                        // The following comes from http://support.microsoft.com/kb/319350/
                        NativeMethods.SHFILEINFO shinfo = new NativeMethods.SHFILEINFO();
                        NativeMethods.SHGetFileInfo(
                            new FileInfo(SaveName).Extension,
                            NativeMethods.FILE_ATTRIBUTE_NORMAL,
                            ref shinfo, (uint)Marshal.SizeOf(shinfo),
                            NativeMethods.SHGFI_USEFILEATTRIBUTES | NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON);
                        if (shinfo.hIcon != IntPtr.Zero)
                        {
                            icon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
                        }
                    }
                }
                return icon;
            }
        }

        /// <summary>
        /// Returns true is there is al least one child under this node
        /// </summary>
        public bool HasChildren
        {
            get
            {
                return (FirstChildId != VSConstants.VSITEMID_NIL);
            }
        }

        /// <summary>
        /// Returns the item id of the first child
        /// </summary>
        public uint FirstChildId
        {
            get
            {
                //Get the first child node of the current hierarchy being walked
                // NOTE: to work around a bug with the Solution implementation of VSHPROPID_FirstChild,
                // we keep track of the recursion level. If we are asking for the first child under
                // the Solution, we use VSHPROPID_FirstVisibleChild instead of _FirstChild. 
                // In VS 2005 and earlier, the Solution improperly enumerates all nested projects
                // in the Solution (at any depth) as if they are immediate children of the Solution.
                // Its implementation _FirstVisibleChild is correct however, and given that there is
                // not a feature to hide a SolutionFolder or a Project, thus _FirstVisibleChild is 
                // expected to return the identical results as _FirstChild.
                return GetItemId(GetProperty<object>(IsSolution ? __VSHPROPID.VSHPROPID_FirstVisibleChild : __VSHPROPID.VSHPROPID_FirstChild));
            }
        }
        /// <summary>
        /// Gets the next child id from the passed childId
        /// </summary>
        /// <param name="childId"></param>
        /// <returns></returns>
        public uint GetNextChildId(uint childId)
        {
            object nextChild = null;
            // NOTE: to work around a bug with the Solution implementation of VSHPROPID_NextSibling,
            // we keep track of the recursion level. If we are asking for the next sibling under
            // the Solution, we use VSHPROPID_NextVisibleSibling instead of _NextSibling. 
            // In VS 2005 and earlier, the Solution improperly enumerates all nested projects
            // in the Solution (at any depth) as if they are immediate children of the Solution.
            // Its implementation   _NextVisibleSibling is correct however, and given that there is
            // not a feature to hide a SolutionFolder or a Project, thus _NextVisibleSibling is 
            // expected to return the identical results as _NextSibling.
            hierarchy.GetProperty(childId,
                    (int)(IsSolution ? __VSHPROPID.VSHPROPID_NextVisibleSibling : __VSHPROPID.VSHPROPID_NextSibling),
                    out nextChild);
            return GetItemId(nextChild);
        }

        /// <summary>
        /// Returns the file name of the hierarcynode
        /// </summary>
        public string SaveName
        {
            get { return GetProperty<string>(__VSHPROPID.VSHPROPID_SaveName); }
        }

        /// <summary>
        /// Returns the project directory
        /// </summary>
        public virtual string ProjectDir
        {
            get { return GetProperty<string>(__VSHPROPID.VSHPROPID_ProjectDir); }
        }

        /// <summary>
        /// Returns the full path
        /// </summary>
        /// <returns></returns>
        public string Path
        {
            get
            {
                string path = string.Empty;
                if(hierarchy is Microsoft.VisualStudio.Shell.Interop.IVsProject)
                {
                    int hr = ((Microsoft.VisualStudio.Shell.Interop.IVsProject)hierarchy).GetMkDocument(itemId, out path);
                    //Marshal.ThrowExceptionForHR(hr);
                    return path;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the relative path.
        /// </summary>
        /// <value>The relative path.</value>
        public string RelativePath
        {
            get
            {
                if (IsRoot)
                    return ProjectDir;

                if (Parent != null)
                {
                    if (Parent.RelativePath != null)
                        return System.IO.Path.Combine(Parent.RelativePath, Name);
                    if (this.Path != null)
                        return this.Path;
                }

                return Name;
            }
        }

        /// <summary>
        /// Returns the extensibility object
        /// </summary>
        public object ExtObject
        {
            get { return GetProperty<object>(__VSHPROPID.VSHPROPID_ExtObject); }
        }

        /// <summary>
        /// Gets the GUID property.
        /// </summary>
        /// <param name="propId">The prop id.</param>
        /// <param name="itemid">The itemid.</param>
        /// <returns></returns>
        private Guid GetGuidProperty(__VSHPROPID propId, uint itemid)
        {
            Guid guid = Guid.Empty;
            int hr = hierarchy.GetGuidProperty(itemid, (int)propId, out guid);
            Marshal.ThrowExceptionForHR(hr);
            return guid;
        }

        /// <summary>
        /// Sets the GUID property.
        /// </summary>
        /// <param name="propId">The prop id.</param>
        /// <param name="itemid">The itemid.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private Guid SetGuidProperty(__VSHPROPID propId, uint itemid, Guid value)
        {
            int hr = hierarchy.SetGuidProperty(itemid, (int)propId, ref value);
            Marshal.ThrowExceptionForHR(hr);
            return value;
        }

        /// <summary>
        /// Gets the GUID property.
        /// </summary>
        /// <param name="propId">The prop id.</param>
        /// <returns></returns>
        private Guid GetGuidProperty(__VSHPROPID propId)
        {
            return GetGuidProperty(propId, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Sets the GUID property.
        /// </summary>
        /// <param name="propId">The prop id.</param>
        /// <returns></returns>
        private void SetGuidProperty(__VSHPROPID propId, Guid value)
        {
            SetGuidProperty(propId, VSConstants.VSITEMID_ROOT, value);
        }

        /// <summary>
        /// Determines whether the specified prop id has property.
        /// </summary>
        /// <param name="propId">The prop id.</param>
        /// <returns>
        /// 	<c>true</c> if the specified prop id has property; otherwise, <c>false</c>.
        /// </returns>
        private bool HasProperty(__VSHPROPID propId)
        {
            object value = null;
            int hr = hierarchy.GetProperty(this.itemId, (int)propId, out value);
            if (hr != VSConstants.S_OK || value == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <param name="propId">The prop id.</param>
        /// <param name="value">The value.</param>
        public void SetProperty(__VSHPROPID propId, object value)
        {
            int hr = hierarchy.SetProperty(ItemId, (int)propId, value);
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propId">The prop id.</param>
        /// <param name="itemid">The itemid.</param>
        /// <returns></returns>
        protected T GetProperty<T>(__VSHPROPID propId, uint itemid)
        {
            object value = null;
            int hr = hierarchy.GetProperty(itemid, (int)propId, out value);
            if (hr != VSConstants.S_OK || value == null)
            {
                return default(T);
            }
            return (T)value;
        }

        public T GetProperty<T>(__VSHPROPID2 propId)
        {
            object value = null;
            int hr = hierarchy.GetProperty(this.ItemId, (int)propId, out value);
            if (hr != VSConstants.S_OK || value == null)
            {
                return default(T);
            }
            return (T)value;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propId">The prop id.</param>
        /// <returns></returns>
        public T GetProperty<T>(__VSHPROPID propId)
        {
            return GetProperty<T>(propId, this.itemId);
        }

        /// <summary>
        /// Gets the item id.
        /// </summary>
        /// <param name="pvar">The pvar.</param>
        /// <returns></returns>
        private static uint GetItemId(object pvar)
        {
            if (pvar == null) return VSConstants.VSITEMID_NIL;
            if (pvar is int) return (uint)(int)pvar;
            if (pvar is uint) return (uint)pvar;
            if (pvar is short) return (uint)(short)pvar;
            if (pvar is ushort) return (uint)(ushort)pvar;
            if (pvar is long) return (uint)(long)pvar;
            return VSConstants.VSITEMID_NIL;
        }

        /// <summary>
        /// Finds the specified func.
        /// </summary>
        /// <param name="func">The func.</param>
        /// <returns></returns>
        public HierarchyNode Find(Predicate<HierarchyNode> func)
        {
            foreach (HierarchyNode child in this.Children)
            {
                if (func(child))
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Recursive enumeration 
        /// </summary>
        public IEnumerable<HierarchyNode> AllElements
        {
            get
            {
                // Optimisation iterateur recursif voir  http://blogs.msdn.com/b/toub/archive/2004/10/31/250303.aspx
                Stack<HierarchyNode> stack = new Stack<HierarchyNode>();
                stack.Push(this);
                while (stack.Count > 0)
                {
                    var root = stack.Pop();
                    foreach (HierarchyNode child in root.Children)
                    {
                        yield return child;
                        if( child.HasChildren)
                            stack.Push(child);
                    }
                }
            }
        }

        // FXCOP: False positive
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "child")]
        public void ForEach(Action<HierarchyNode> func)
        {
            foreach (HierarchyNode child in this.Children)
            {
                func(child);
            }
        }

        /// <summary>
        /// Recursives for each.
        /// </summary>
        /// <param name="func">The func.</param>
        public void RecursiveForEach(Action<HierarchyNode> func)
        {
            func(this);
            foreach (HierarchyNode child in this.Children)
            {
                child.RecursiveForEach(func);
            }
        }

        /// <summary>
        /// Recursives the find.
        /// </summary>
        /// <param name="func">The func.</param>
        /// <returns></returns>
        public HierarchyNode RecursiveFind(Predicate<HierarchyNode> func)
        {
            if (func(this))
            {
                return this;
            }
            foreach (HierarchyNode child in this.Children)
            {
                HierarchyNode found = child.RecursiveFind(func);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Recursives the find by path.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public HierarchyNode RecursiveFindByPath(string fileName)
        {
            return RecursiveFind(n => Utils.IsSamePath(n.Path, fileName));
        }

        /// <summary>
        /// Find a node by its name.
        /// </summary>
        /// <param name="name">The name (may contains * )</param>
        /// <returns></returns>
        public HierarchyNode FindByName(string name)
        {
            return Find(delegate(HierarchyNode node)
            {
                if (name.StartsWith("*."))
                    return System.IO.Path.GetExtension(node.Name).Equals(name.Substring(1), StringComparison.InvariantCultureIgnoreCase);
                if (name.EndsWith(".*"))
                    return System.IO.Path.GetFileNameWithoutExtension(node.Name).Equals(name.Substring(0, name.Length - 2), StringComparison.InvariantCultureIgnoreCase);
                return node.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        /// <summary>
        /// Find a node recursively by its name.
        /// </summary>
        /// <param name="name">The name (may contains * )</param>
        /// <returns></returns>
        public HierarchyNode RecursiveFindByName(string name)
        {
            return RecursiveFind(delegate(HierarchyNode node)
            {
                if (name.StartsWith("*."))
                    return System.IO.Path.GetExtension(node.Name).Equals(name.Substring(1), StringComparison.InvariantCultureIgnoreCase);
                if (name.EndsWith(".*"))
                    return System.IO.Path.GetFileNameWithoutExtension(node.Name).Equals(name.Substring(0, name.Length - 2), StringComparison.InvariantCultureIgnoreCase);
                return node.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        /// <summary>
        /// Gets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public HierarchyNode Parent
        {
            get
            {
                if (!IsRoot)
                {
                    return new HierarchyNode(solution, hierarchy);
                }
                else
                {
                    IVsHierarchy vsHierarchy = GetProperty<IVsHierarchy>(__VSHPROPID.VSHPROPID_ParentHierarchy, VSConstants.VSITEMID_ROOT);
                    if (vsHierarchy == null)
                    {
                        return null;
                    }

                    HierarchyNode tempNode = new HierarchyNode(solution, vsHierarchy);
                    if (tempNode.IsSolution)
                        return new SolutionNode(solution);
                    if (tempNode.IsProject)
                        return new ProjectNode(tempNode);
                    return tempNode;
                }
            }
        }

        /// <summary>
        /// Removes this instance.
        /// </summary>
        public virtual void Remove()
        {
            Debug.Assert(Parent != null);
            try
            {
                ((HierarchyNode)Parent).RemoveItem(itemId);
            }
            catch { }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="vsItemId">The vs item id.</param>
        /// <returns></returns>
        private bool RemoveItem(uint vsItemId)
        {
            var vsProject = hierarchy as Microsoft.VisualStudio.Shell.Interop.IVsProject2;
            if (vsProject == null)
            {
                return false;
            }
            try
            {
                int result = 0;
                int hr = vsProject.RemoveItem(0, vsItemId, out result);
                return (hr == VSConstants.S_OK && result == 1);
            }
            catch
            {
                return false;
            }
        }

        #region IDisposable Members

        private bool disposed;

        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }
                // Release unmanaged resources. If disposing is false, 
                // only the following code is executed.

                // Note that this is not thread safe.
                // Another thread could start disposing the object
                // after the managed resources are disposed,
                // but before the disposed flag is set to true.
                // If thread safety is necessary, it must be
                // implemented by the client.

            }
            disposed = true;
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method 
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~HierarchyNode()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion

        /// <summary>
        /// Gets the children (not recursive)
        /// </summary>
        /// <value>The children.</value>
        public IEnumerable<HierarchyNode> Children
        {
            get { return new HierarchyNodeCollection(this); }
        }

        /// <summary>
        /// Inits the specified vs solution.
        /// </summary>
        /// <param name="vsSolution">The vs solution.</param>
        /// <param name="vsHierarchy">The vs hierarchy.</param>
        /// <param name="vsItemId">The vs item id.</param>
        private void Init(Microsoft.VisualStudio.Shell.Interop.IVsSolution vsSolution, IVsHierarchy vsHierarchy, uint vsItemId)
        {
            this.solution = vsSolution;
            int hr = VSConstants.E_FAIL;
            if (vsHierarchy == null)
            {
                Guid emptyGuid = Guid.Empty;
                hr = this.solution.GetProjectOfGuid(ref emptyGuid, out this.hierarchy);
                Marshal.ThrowExceptionForHR(hr);
            }
            else
            {
                this.hierarchy = vsHierarchy;
            }
            this.itemId = vsItemId;

            IntPtr nestedHierarchyObj;
            uint nestedItemId;
            Guid hierGuid = typeof(IVsHierarchy).GUID;

            // Check first if this node has a nested hierarchy. If so, then there really are two 
            // identities for this node: 1. hierarchy/itemid 2. nestedHierarchy/nestedItemId.
            // We will convert this node using the inner nestedHierarchy/nestedItemId identity.
            hr = this.hierarchy.GetNestedHierarchy(this.itemId, ref hierGuid, out nestedHierarchyObj, out nestedItemId);
            if (VSConstants.S_OK == hr && IntPtr.Zero != nestedHierarchyObj)
            {
                IVsHierarchy nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHierarchyObj) as IVsHierarchy;
                Marshal.Release(nestedHierarchyObj); // we are responsible to release the refcount on the out IntPtr parameter
                if (nestedHierarchy != null)
                {
                    this.hierarchy = nestedHierarchy;
                    this.itemId = nestedItemId;
                }
            }
        }

        /// <summary>
        /// Gets the relative path from project.
        /// </summary>
        /// <returns></returns>
        public string GetRelativePathFromProject()
        {
            HierarchyNode project = new HierarchyNode(solution, this.UniqueName);
            return Utils.PathRelativePathToFolder(project.ProjectDir, RelativePath);
        }

        #region HierarchyNode Members


        /// <summary>
        /// Gets the containing project.
        /// </summary>
        /// <returns></returns>
        public ProjectNode GetContainingProject()
        {
            if (this.IsProject)
                return this.CastToProjectNode();
            HierarchyNode parent = this.Parent;
            while (parent != null && !parent.IsProject)
                parent = parent.Parent;
            return parent != null ? parent.CastToProjectNode() : null;
        }

        #endregion

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        public virtual bool Save()
        {
            return solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, null, DocCookie) == VSConstants.S_OK;
        }

        public virtual void Rename(string name)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");
            EnsureCheckout();
            ProjectItem pi = this.ExtObject as ProjectItem;
            if (pi != null)
                pi.Name = name;
        }

        private static List<IVsWindowFrame> GetFramesForDocument(IServiceProvider site, object docData)
        {
            IEnumWindowFrames frames;
            List<IVsWindowFrame> list = new List<IVsWindowFrame>();
            IVsRunningDocumentTable service = site.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            IVsUIShell shell = site.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if ((shell == null) || (service == null))
            {
                return list;
            }
            ErrorHandler.ThrowOnFailure(shell.GetDocumentWindowEnum(out frames));
            IVsWindowFrame[] frameArray = new IVsWindowFrame[16];
            while (true)
            {
                uint num;
                ErrorHandler.ThrowOnFailure(frames.Next((uint)frameArray.Length, frameArray, out num));
                if (num == 0)
                {
                    return list;
                }
                for (int i = 0; i < num; i++)
                {
                    object obj2;
                    IVsWindowFrame item = frameArray[i];
                    obj2 = ErrorHandler.ThrowOnFailure(item.GetProperty(-4004, out obj2));
                    if (NativeMethods.IsSameComObject(obj2, docData))
                    {
                        list.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the doc data.
        /// </summary>
        /// <param name="viewKind">Kind of the view.(Guid.Empty to not force to open the document) </param>
        /// <returns></returns>
        public object GetDocData(Guid viewKind)
        {
            uint docCookie;
            IVsRunningDocumentTable service = ServiceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            IntPtr zero = IntPtr.Zero;
            docCookie = 0;

            object objectForIUnknown = null;
            try
            {
                IVsHierarchy hierarchy;
                uint num;
                for (int nb = 0; nb < 2; nb++)
                {
                    if (ErrorHandler.Succeeded(service.FindAndLockDocument((uint)Microsoft.VisualStudio.Shell.Interop._VSRDTFLAGS.RDT_NoLock, this.Path, out hierarchy, out num, out zero, out docCookie)) && (zero != IntPtr.Zero))
                    {
                        objectForIUnknown = Marshal.GetObjectForIUnknown(zero);
                        break;
                    }
                    else if (nb == 0)
                    {
                        if (viewKind == Guid.Empty)
                            return null;
                        this.ProjectItem.Open(viewKind.ToString());
                    }
                }
            }
            finally
            {
                if (zero != IntPtr.Zero)
                {
                    Marshal.Release(zero);
                }
            }
            return objectForIUnknown;
        }

        /// <summary>
        /// Gets the project item.
        /// </summary>
        /// <value>The project item.</value>
        public ProjectItem ProjectItem
        {
            get { return this.ExtObject as EnvDTE.ProjectItem; }
        }

        /// <summary>
        /// Runs the custom tool.
        /// </summary>
        public void RunCustomTool()
        {
            var pi = this.ProjectItem;
            if (pi != null)
            {
                var pi2 = pi.Object as VSLangProj.VSProjectItem;
                if (pi2 != null)
                    pi2.RunCustomTool();
            }
        }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public virtual IServiceProvider ServiceProvider
        {
            get
            {
                EnvDTE.DTE dte = this.ProjectItem.DTE;
                return new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
            }
        }

        /// <summary>
        /// Ensures the checkout.
        /// </summary>
        public void EnsureCheckout()
        {
            // SCC
            IVsQueryEditQuerySave2 scc = ServiceProvider.GetService(typeof(SVsQueryEditQuerySave)) as IVsQueryEditQuerySave2;
            if (scc != null)
            {
                uint result;
                uint info;
                string[] files = new string[1];
                files[0] = this.Path;
                scc.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_ForceEdit_NoPrompting, 1, files, null, null, out result, out info);
            }
            Utils.UnsetReadOnly(this.Path);
        }

        public void SetAttribute(string name, string value)
        {
            IVsBuildPropertyStorage hierarchy = this.hierarchy as IVsBuildPropertyStorage;
            if (hierarchy != null)
            {
                ErrorHandler.ThrowOnFailure(hierarchy.SetItemAttribute(this.itemId, name, value));
            }
        }

        public bool GetAttributeAsBoolean(string name, bool defaultValue)
        {
            try
            {
                var v = GetAttribute(name);
                return String.IsNullOrWhiteSpace(v) ? defaultValue : Boolean.Parse(v);
            }
            catch
            {
                return defaultValue;
            }
        }

        public T GetAttributeAsEnum<T>(string name, T defaultValue)
        {
            try
            {
                var v = GetAttribute(name);
                return String.IsNullOrWhiteSpace(v) ? defaultValue : (T)Enum.Parse(typeof(T), v);
            }
            catch
            {
                return defaultValue;
            }
        }

        public string GetAttribute(string name)
        {
            try
            {
                string str;
                IVsBuildPropertyStorage hierarchy = this.hierarchy as IVsBuildPropertyStorage;
                if ((hierarchy != null) && ErrorHandler.Succeeded(hierarchy.GetItemAttribute(this.itemId, name, out str)))
                {
                    return str;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

    }
}
