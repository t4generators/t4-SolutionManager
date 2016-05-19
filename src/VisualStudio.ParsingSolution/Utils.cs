using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security;
using System.Security.Policy;
using System.Reflection;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Net;
using System.Windows.Forms;

namespace VsxFactory.Modeling.VisualStudio
{
        /// <summary>
        /// Classe utilitaire
        /// </summary>
    public sealed class Utils
    {
        private static string s_tempFolder;
        private const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const Int32 MAX_PATH = 260;

        /// <summary>
        /// Pathes the compact.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="maxChar">The max char.</param>
        /// <returns></returns>
        public static string PathCompact(string path, int maxChar)
        {
            StringBuilder str = new StringBuilder(MAX_PATH);
            if (NativeMethods.PathCompactPathEx(str, path, maxChar, 0))
            {
                return str.ToString();
            }
            return path;         
        }

        /// <summary>
        /// Pathes the common prefix.
        /// </summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns></returns>
        public static string PathCommonPrefix(string path1, string path2)
        {
            StringBuilder str = new StringBuilder(MAX_PATH);
            if (NativeMethods.PathCommonPrefix(path1, path2, str) > 0)
            {
                return str.ToString();
            }
            return null;
        }

        /// <summary>
        /// Get the relative path to folder.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>the relative path or null</returns>
        public static string PathRelativePathToFolder(string folderPath, string filePath)
        {
            StringBuilder str = new StringBuilder(MAX_PATH);
            if (NativeMethods.PathRelativePathTo(str, folderPath, FILE_ATTRIBUTE_DIRECTORY, filePath, 0))
            {
                return PathCanonicalize( str.ToString() );
            }
            return null;
        }

        /// <summary>
        /// Canonicalize a path
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static string PathCanonicalize(string path)
        {
            StringBuilder str = new StringBuilder(MAX_PATH);
            if (NativeMethods.PathCanonicalize(str, path))
            {
                return str.ToString();
            }
            return path;
        }

        /// <devdoc>
        /// Please use this "approved" method to compare file names.
        /// </devdoc>
        public static bool IsSamePath(string file1, string file2)
        {
            if (file1 == null || file1.Length == 0)
            {
                return (file2 == null || file2.Length == 0);
            }

            Uri uri1 = null;
            Uri uri2 = null;

            try
            {
                if (!Uri.TryCreate(file1, UriKind.Absolute, out uri1) || !Uri.TryCreate(file2, UriKind.Absolute, out uri2))
                {
                    if (Uri.TryCreate(file1, UriKind.Relative, out uri1) && Uri.TryCreate(file2, UriKind.Relative, out uri2))
                    {
                        return uri1 != null && uri2 != null && 0 == String.Compare(uri1.OriginalString, uri2.OriginalString, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }

                if (uri1 != null && uri1.IsFile && uri2 != null && uri2.IsFile)
                {
                    return 0 == String.Compare(uri1.LocalPath, uri2.LocalPath, StringComparison.OrdinalIgnoreCase);
                }

                return file1 == file2;
            }
            catch (UriFormatException e)
            {
                Trace.WriteLine("Exception " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Répertoire de travail
        /// </summary>
        /// <returns></returns>
        public static string GetTempFolderBase()
        {
            if (s_tempFolder == null)
            {
                s_tempFolder = Path.Combine(Path.GetTempPath(), VsxFactoryConstants.Name);
                Directory.CreateDirectory(s_tempFolder);
            }
            return s_tempFolder;
        }

        /// <summary>
        /// Retourne un nouveau répertoire temporaire dans le répertoire de travail
        /// </summary>
        /// <returns></returns>
        public static string GetTemporaryFolder()
        {
            string tempFile = Path.GetTempFileName(); // Génere un nouveau nom
            DeleteFile(tempFile);
            return Path.Combine(GetTempFolderBase(), Path.GetFileName(tempFile));
        }

        /// <summary>
        /// Retourne un nouveau chemin temporaire dans le répertoire de travail
        /// </summary>
        /// <param name="relativeModelFileName">Nom du fichier</param>
        /// <returns></returns>
        public static string GetTemporaryFileName(string relativeModelFileName)
        {
            string targetFileName = Path.Combine(GetTemporaryFolder(), relativeModelFileName);
            return targetFileName;
        }

        /// <summary>
        /// Teste la validité d'un nom de variable
        /// </summary>
        /// <param name="name">Nom à vérifier</param>
        /// <param name="composedName">Indique si le nom peut-être composé (avec des .)</param>
        /// <returns></returns>
        public static bool IsValidName(string name, bool composedName)
        {
            if (String.IsNullOrEmpty(name))
                return false;
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                if (!Char.IsLetterOrDigit(ch) && ch != '_' && !(ch == '.' && composedName))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Normalise un nom de variable en respectant les conventions d'écriture. Par défaut, il est en
        /// Pascal Casing
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string NormalizeName(string name)
        {
            if (String.IsNullOrEmpty(name))
                return "_";


            StringBuilder sb = new StringBuilder();
            if (Char.IsDigit(name[0]))
            {
                sb.Append("_");
            }

            // Séparateur de nom :
            //  - Tout autre car que lettre ou chiffre
            //  - Une majuscule précédée d'une minuscule ou d'une autre majuscule (avec un max de 3) si le 
            //    mot initial contient au moins une minuscule.

            bool containsLowerChars = false;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsLower(name, i))
                {
                    containsLowerChars = true;
                    break;
                }
            }

            bool newWord = true;
            char prevChar = ' ';
            int upperCount = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                // Est ce que c'est un séparateur
                if (char.IsLetterOrDigit(ch))
                {
                    if (!newWord && containsLowerChars)
                    {
                        if (char.IsUpper(ch) && char.IsLower(prevChar))
                        {
                            upperCount = 0;
                            newWord = true;
                        }
                        else if (char.IsUpper(ch) && char.IsUpper(prevChar) && upperCount < 3)
                        {
                            upperCount++;
                            newWord = true;
                        }
                        else
                            upperCount = 0;
                    }
                    prevChar = ch;

                    if (newWord)
                    {
                        ch = char.ToUpper(ch);
                    }
                    else
                    {
                        ch = char.ToLower(ch);
                    }

                    newWord = false;
                    sb.Append(ch);
                }
                else
                {
                    newWord = true;
                    if (ch == '_')
                        sb.Append(ch);
                }
            }
            name = sb.ToString();
            //    name = this.LegalizeKeywords(name);
            return name;
        }

        /// <summary>
        /// S"assure qu'une chaine ne fera pas plus d'une certaine longueur. La tronque si nécessaire
        /// et rajoute le prefixe '...'.
        /// </summary>
        /// <param name="str">Chaine à tronquer</param>
        /// <param name="maxLength">Longueur maximun souhaité</param>
        /// <returns>Chaine d'une longueur maximun de 'maxLength'</returns>
        public static string StripString(string str, int maxLength)
        {
            if (str.Length <= maxLength)
                return str;

            // On se positionne au maximun et on remonte jusqu'à trouver un séparateur.
            for (int i = str.Length - (maxLength - 3); i < str.Length; i++)
            {
                if (str[i] == Path.DirectorySeparatorChar)
                {
                    return String.Concat("...", str.Substring(i));
                }
            }

            return String.Concat("...", str.Substring(str.Length - (maxLength - 3)));
        }

        /// <summary>
        /// Copie le contenu d'un répertoire (non récursif) dans un autre
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="targetFolder"></param>
        /// <param name="filePattern"></param>
        public static int CopyFiles(string sourceFolder, string targetFolder, string filePattern)
        {
            if (!Directory.Exists(sourceFolder))
                return -1;

            int cx = 0;
            Directory.CreateDirectory(targetFolder);

            DirectoryInfo di = new DirectoryInfo(sourceFolder);
            foreach (FileInfo fi in di.GetFiles(filePattern))
            {
                string destFile = Path.Combine(targetFolder, fi.Name);
                CopyFile(fi.FullName, destFile);
                cx++;
            }
            return cx;
        }

        /// <summary>
        /// Copie un fichier en s'assurant que le répertoire destination existe et que le
        /// fichier destination peut-être écrasé si il existe.
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        public static void CopyFile(string sourceFileName, string destFileName)
        {
            if (String.IsNullOrEmpty(sourceFileName) || !File.Exists(sourceFileName))
                return;
            DeleteFile(destFileName);
            string folder = Path.GetDirectoryName(destFileName);
            Directory.CreateDirectory(folder);
            File.Copy(sourceFileName, destFileName, true);
        }

        /// <summary>
        /// Unsets the read only.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public static void UnsetReadOnly(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                    File.SetAttributes(fileName, FileAttributes.Normal);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Suppression d'un fichier
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public static void DeleteFile(string fileName)
        {
            if (fileName != null && File.Exists(fileName))
            {
                try
                {
                    File.SetAttributes(fileName, FileAttributes.Normal);
                    File.Delete(fileName);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Deletes the files.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="pattern">The pattern.</param>
        public static void DeleteFiles(string folder, string pattern)
        {
            foreach (string fileName in Utils.SearchFile(folder, pattern))
            {
                DeleteFile(fileName);
            }
        }

        /// <summary>
        /// Files the date equals.
        /// </summary>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        internal static bool FileDateEquals(string targetFileName, string fileName)
        {
            return File.GetLastWriteTime(targetFileName) == File.GetLastWriteTime(fileName);
        }

        /// <summary>
        /// Copies the directory.
        /// </summary>
        /// <param name="sourcefolder">The sourcefolder.</param>
        /// <param name="targetFolder">The target folder.</param>
        public static void CopyDirectory(string sourcefolder, string targetFolder)
        {
            if (!Directory.Exists(sourcefolder))
                return;

            DirectoryInfo di = new DirectoryInfo(sourcefolder);
            CopyDirectoryInternal(di, targetFolder);
        }

        /// <summary>
        /// Copies the directory internal.
        /// </summary>
        /// <param name="di">The di.</param>
        /// <param name="targetFolder">The target folder.</param>
        private static void CopyDirectoryInternal(DirectoryInfo di, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);

            foreach (DirectoryInfo subdir in di.GetDirectories())
            {
                CopyDirectoryInternal(subdir, Path.Combine(targetFolder, subdir.Name));
            }
            foreach (FileInfo file in di.GetFiles())
            {
                try
                {
                    string targetFileName = Path.Combine(targetFolder, file.Name);
                    file.CopyTo(targetFileName, true);
                    File.SetAttributes(targetFileName, FileAttributes.Normal);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Supprime un répertoire avec son contenu (y compris ses sous-répertoires)
        /// </summary>
        /// <param name="path"></param>
        public static void RemoveDirectory(string path, bool throwError=true)
        {
            if (!Directory.Exists(path))
                return;

            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                Directory.Delete(path, true);
            }
            catch
            {
                if (throwError)
                    throw;
            }
        }

        /// <summary>
        /// Vérifie si un fichier et vérrouillé (read only)
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// 	<c>true</c> if [is file locked] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFileLocked(string path)
        {
            return
                path != null && File.Exists(path) &&
                (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }

        /// <summary>
        /// Moves the directory.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public static void MoveDirectory(string source, string target)
        {
            if (!Directory.Exists(source))
                return;
            if (Directory.Exists(target))
                RemoveDirectory(target);
            File.SetAttributes(source, FileAttributes.Normal);
            Directory.Move(source, target);
        }

        /// <summary>
        /// Swaps two instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance1">The instance1.</param>
        /// <param name="instance2">The instance2.</param>
        public static void Swap<T>(ref T instance1, ref T instance2)
        {
            T tmp = instance1;
            instance1 = instance2;
            instance2 = tmp;
        }

        public static IEnumerable<String> GetFilesWithExactExtension(string path, string filterExtension, bool recursive)
        {
            int extLength = filterExtension.Length - filterExtension.IndexOf('.');
            foreach (var file in Directory.GetFiles(path, filterExtension, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                if (Path.GetExtension(file).Length == extLength)
                    yield return file;
            }
        }

        //public static AppDomain CreateAppDomain(string applicationBase, Assembly assembly)
        //{
        //    AppDomainSetup info = new AppDomainSetup();

        //    // Répertoire de base
        //    info.ApplicationBase = applicationBase;
        //    info.ConfigurationFile = string.Empty;

        //    SecurityZone zone = SecurityZone.MyComputer;

        //    // Set up the Evidence
        //    Evidence baseEvidence = AppDomain.CurrentDomain.Evidence;
        //    Evidence evidence = new Evidence(baseEvidence);
        //    evidence.AddAssembly(assembly.FullName);
        //    evidence.AddHostEvidence(new Zone(zone));

        //    return AppDomain.CreateDomain(VsxFactoryConstants.Name + " domain", evidence, info);

        //}

        /// <summary>
        /// Recherche d'un fichier dans un répertoire et ses sous-répertoires
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="filterPattern">The filter pattern.</param>
        /// <returns></returns>
        public static IEnumerable<string> SearchFile(string folderPath, string filterPattern)
        {
            List<string> result = new List<string>();

            DirectoryInfo di = new DirectoryInfo(folderPath);
            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles(filterPattern))
                    result.Add(fi.FullName);

                foreach (DirectoryInfo child in di.GetDirectories())
                {
                    result.AddRange(SearchFile(child.FullName, filterPattern));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the VS install dir.
        /// </summary>
        /// <value>The VS install dir.</value>
        private static string _vsInstallDir;
        public static string VSInstallDir
        {
            get
            {
                if (_vsInstallDir == null)
                {
                    using (RegistryKey key1 = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\VisualStudio"))
                    {
                        if (key1 != null)
                        {
                            using (RegistryKey key2 = key1.OpenSubKey(VsxFactoryConstants.VisualStudioVersion))
                            {
                                if (key2 != null)
                                {
                                    _vsInstallDir = (string)key2.GetValue("InstallDir");
                                }
                            }
                        }
                    }
                }
                return _vsInstallDir;
            }
        }

        /// <summary>
        /// Prompts the yes no.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static bool PromptYesNo(IServiceProvider serviceProvider, string message)
        {
            Microsoft.VisualStudio.Shell.Interop.IVsUIShell shell = serviceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell)) as Microsoft.VisualStudio.Shell.Interop.IVsUIShell;
            return Microsoft.VisualStudio.Shell.VsShellUtilities.PromptYesNo(message, "Warning", Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_QUERY, shell);
        }

        /// <summary>
        /// Shows a message box.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        /// <param name="helpKeyword">The help keyword.</param>
        /// <returns></returns>
        public static DialogResult ShowMessageBox(IServiceProvider serviceProvider, string title, string text, string helpKeyword)
        {
            return ShowMessageBox(serviceProvider, title, text, helpKeyword, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows a message box.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        /// <param name="helpKeyword">The help keyword.</param>
        /// <param name="buttons">The buttons.</param>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public static DialogResult ShowMessageBox(IServiceProvider serviceProvider, string title, string text, string helpKeyword, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            Guard.ArgumentNotNull(serviceProvider, "serviceProvider");
            Guard.ArgumentNotNullOrEmptyString(title, "title");
            Guard.ArgumentNotNullOrEmptyString(text, "text");

            IVsUIShell uiShellService = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

            int pnResult = 1;

            if (uiShellService != null)
            {
                Guid empty = Guid.Empty;
                OLEMSGBUTTON msgbtn = (OLEMSGBUTTON)buttons;
                OLEMSGICON msgicon = OLEMSGICON.OLEMSGICON_INFO;

                switch (icon)
                {
                    case MessageBoxIcon.Question:
                        msgicon = OLEMSGICON.OLEMSGICON_QUERY;
                        break;

                    case MessageBoxIcon.Exclamation:
                        msgicon = OLEMSGICON.OLEMSGICON_WARNING;
                        break;

                    case MessageBoxIcon.Asterisk:
                        msgicon = OLEMSGICON.OLEMSGICON_INFO;
                        break;

                    case MessageBoxIcon.None:
                        msgicon = OLEMSGICON.OLEMSGICON_NOICON;
                        break;

                    case MessageBoxIcon.Hand:
                        msgicon = OLEMSGICON.OLEMSGICON_CRITICAL;
                        break;
                }

                uiShellService.ShowMessageBox(
                    0,
                    ref empty,
                    title,
                    string.IsNullOrEmpty(text) ? null : text,
                    helpKeyword,
                    0,
                    msgbtn,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    msgicon,
                    0,
                    out pnResult);
            }

            return (DialogResult)pnResult;
        }

        public static void MakeWebCall(IServiceProvider serviceProvider, Uri url, Action call)
        {
            Guard.ArgumentNotNull(serviceProvider, "serviceProvider");
            Guard.ArgumentNotNull(url, "url");
            Guard.ArgumentNotNull(call, "call");
            __VsWebProxyState proxyState = __VsWebProxyState.VsWebProxyState_NoCredentials;
            MakeWebCall(serviceProvider, url.ToString(), ref proxyState, call);
        }

        /// <summary>
        /// Makes the web call.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="webCallUrl">The web call URL.</param>
        /// <param name="proxyState">State of the proxy.</param>
        /// <param name="webCall">The web call.</param>
        private static void MakeWebCall(IServiceProvider serviceProvider, string webCallUrl, ref __VsWebProxyState proxyState, Action webCall)
        {
            var proxy = serviceProvider.GetService(typeof(SVsWebProxy)) as IVsWebProxy;
            if (proxy != null)
            {
                uint oldProxyState = (uint)proxyState;
                int errorCode = proxy.PrepareWebProxy(webCallUrl, oldProxyState, out oldProxyState, Convert.ToInt32(false));
                if (errorCode != 0)
                {
                    throw Marshal.GetExceptionForHR(errorCode);
                }
                proxyState = (__VsWebProxyState)oldProxyState;
            }
            if (proxyState != __VsWebProxyState.VsWebProxyState_Abort)
            {
                webCall();
            }
        }

        /// <summary>
        /// Determines whether [is match pattern] [the specified pattern].
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        /// 	<c>true</c> if [is match pattern] [the specified pattern]; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsMatchPattern(string patterns, string fileName)
        {
            Guard.ArgumentNotNullOrEmptyString(patterns, "pattern");
            if (string.IsNullOrEmpty(fileName))
                return false;

            foreach (var p in patterns.Split(new char[] {';'},  StringSplitOptions.RemoveEmptyEntries ))
            {
                var pattern = p.Trim();
                if (pattern == "*.*")
                    return true;

                if (Path.IsPathRooted(pattern))
                {
                    if (Utils.IsSamePath(fileName, pattern))
                        return true;
                }
                else
                {
                    if (pattern.StartsWith("*.") && String.Compare(Path.GetExtension(fileName), pattern.Substring(1), StringComparison.InvariantCultureIgnoreCase) == 0)
                        return true;
                    if (String.Compare(Path.GetFileName(fileName), pattern, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return true;
                    if (pattern.EndsWith(".*") && String.Compare(Path.GetFileNameWithoutExtension(fileName), pattern.Substring(0, pattern.Length - 2), StringComparison.InvariantCultureIgnoreCase) == 0)
                        return true;
                }
            }
            return false;
        }
    }
}
