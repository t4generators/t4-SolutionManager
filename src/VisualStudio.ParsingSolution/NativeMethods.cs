using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace VsxFactory.Modeling.VisualStudio
{
    public static class NativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] uint dwAttrFrom,
             [In] string pszTo,
             [In] uint dwAttrTo
        );
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PathCanonicalize([Out] StringBuilder dst, string src);
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern Int32 PathCommonPrefix(
            [In] String pszFile1,
            [In] String pszFile2,
            [Out] StringBuilder pszPath
            );
    }
}
