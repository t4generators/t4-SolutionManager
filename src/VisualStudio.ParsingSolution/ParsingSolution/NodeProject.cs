using EnvDTE;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using VSLangProj;

namespace VisualStudio.ParsingSolution
{

    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeProject : NodeSolutionItem
    {

        public const string ContentProjectConst2 = "Project";
        public const string ContentProjectConst = "project";
        public const string ContentPathConst = "path";
        public const string ContentCompile = "Compile";
        public const string ContentReference = "Reference";
        public const string ContentProjectReference = "ProjectReference";
        public const string ContentContent = "Content";
        public const string ContentEmbeddedResource = "EmbeddedResource";
        public const string ContentResource = "Resource";
        public const string ContentClassName = "classname";
        private Microsoft.Build.Construction.ProjectRootElement _projectContent;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeProject"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public NodeProject(EnvDTE.Project project)
            : base(project)
        {


        }

        public Project Project { get { return this.project; } }

        public VSProject VSProject { get { return this.project.Object as VSProject; } }

        /// <summary>
        /// Add the file in the project
        /// </summary>
        /// <param name="folderPath">The folder path in the project</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="content">The content of the file.</param>
        public NodeItem AddFile(string folderPath, string name, string content)
        {

            NodeItemFolder folder = this.GetFolder(folderPath);
            var file = new FileInfo(Path.Combine(folder.LocalPath, name));
            if (!file.Exists)
            {

                var ar = System.Text.Encoding.UTF8.GetBytes(content);

                using (var stream = file.OpenWrite())
                {
                    stream.Write(ar, 0, ar.Length);
                    stream.Flush();
                }
            }

            return folder.AddFile(file.FullName);

        }

        /// <summary>
        /// Add the file in the project
        /// </summary>
        /// <param name="folderPath">The folder path in the project</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="content">The content of the file.</param>
        public NodeItem AddFile(string folderPath, string name, byte[] content)
        {

            NodeItemFolder folder = this.GetFolder(folderPath);
            var file = new FileInfo(Path.Combine(folder.LocalPath, name));
            if (!file.Exists)
            {
                using (var stream = file.OpenWrite())
                {
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }

            return folder.AddFile(file.FullName);

        }

        /// <summary>
        /// add a new file in the folder to the solution
        /// </summary>
        public NodeItem AddFile(FileInfo file)
        {
            return AddFile(file.FullName);
        }

        /// <summary>
        /// add a new file in the folder to the solution
        /// </summary>
        public NodeItem AddFile(string FullName)
        {
            return new NodeItem(this.project.ProjectItems.AddFromFile(FullName));
        }

        /// <summary>
        /// add a new files in the folder of the solution
        /// </summary>
        public NodeItem AddFiles(DirectoryInfo directory)
        {
            return AddFiles(directory.FullName);
        }

        /// <summary>
        /// add a new files in the folder of the solution
        /// </summary>
        public NodeItem AddFiles(string directoryFullName)
        {
            return new NodeItemFolder(this.project.ProjectItems.AddFromDirectory(directoryFullName));
        }

        /// <summary>
        /// resolve the folder path and return the last folder.
        /// if a part is missing. it is created 
        /// </summary>
        public NodeItemFolder GetFolder(string path)
        {

            string p = path.Replace("/", @"\");
            p = p.Trim();
            p = p.Trim('\\');

            string[] ar = p.Split('\\');

            return GetFolder(ar);

        }

        /// <summary>
        /// resolve the folder path and return the last folder.
        /// if a part is missing. it is created
        /// </summary>
        public NodeItemFolder GetFolder(string[] paths)
        {

            NodeItemFolder node = null;
            string n2 = Name;

            for (int i = 0; i < paths.Length; i++)
            {

                string n = paths[i];

                if (string.IsNullOrEmpty(n))
                    return null;

                if (node == null)
                    node = this.GetItem<NodeItemFolder>(c => c.Name.ToLower() == n.ToLower()).FirstOrDefault();
                else
                    node = node.GetFolder(n);

                if (node == null)
                {

                    var _path = Path.Combine(LocalPath, n);
                    DirectoryInfo dir = new DirectoryInfo(_path);
                    if (!dir.Exists)
                        dir.Create();

                    this.project.ProjectItems.AddFromDirectory(_path);

                    node = this.GetItem<NodeItemFolder>(c => c.Name.ToLower() == n.ToLower())
                               .FirstOrDefault();

                    if (node == null)
                        throw new Exception(String.Format("{0} can't be resolved in ", n, n2));

                }

                n2 = n;

            }

            return node;

        }

        public NodeItem GetNode(string FullName)
        {
            return GetItem<NodeItem>(c => c.Name == FullName).FirstOrDefault();
        }

        /// <summary>
        /// return the path of the specified file in the project
        /// </summary>
        /// <param name="FullName">The full name.</param>
        /// <returns></returns>
        public string GetProjectPath(string FullName)
        {

            var file = new FileInfo(FullPath);
            var project = new FileInfo(this.FullPath);

            if (file.Directory.FullName.Length >= project.Directory.FullName.Length)
                return file.Directory.FullName.Substring(this.FullPath.Length).Trim('\\').Trim();

            return string.Empty;

        }

        /// <summary>
        /// Creates the code type reference.
        /// </summary>
        /// <param name="typeFullname">The type fullname.</param>
        /// <returns></returns>
        public CodeTypeRef CreateCodeTypeRef(string typeFullname)
        {
            return this.project.CodeModel.CreateCodeTypeRef(typeFullname);
        }

        /// <summary>
        /// return the codetype from the type fullname
        /// </summary>
        public EnvDTE.CodeType CodeTypeFromFullName(string typeFullname)
        {
            return this.project.CodeModel.CodeTypeFromFullName(typeFullname);
        }

        /// <summary>
        /// return the datetime of the last access 
        /// </summary>
        public override DateTime GetLastAccessTime { get { return File.GetLastAccessTime(this.project.FullName); } }

        /// <summary>
        /// return the creation datetime 
        /// </summary>
        public override DateTime GetCreationTime { get { return File.GetCreationTime(this.project.FullName); } }

        /// <summary>
        /// return the datatime of the last write access
        /// </summary>
        public override DateTime GetLastWriteTime { get { return File.GetLastWriteTime(this.project.FullName); } }

        /// <summary>
        /// return kind item
        /// </summary>
        public override KindItem KindItem { get { return KindItem.Project; } }

        protected Microsoft.Build.Construction.ProjectRootElement GetProjectContent()
        {
            if (_projectContent == null)
                _projectContent = Microsoft.Build.Construction.ProjectRootElement.Open(this.FullName);
            return _projectContent;
        }

        protected IEnumerable<Microsoft.Build.Construction.ProjectItemElement> GetElements(string name)
        {

            Microsoft.Build.Construction.ProjectRootElement project = GetProjectContent();

            foreach (var item in project.ItemGroups)
                foreach (var item2 in item.Children)
                {
                    var t = item2 as Microsoft.Build.Construction.ProjectItemElement;
                    if (t != null)
                        if (t.ItemType == name)
                            yield return t;
                }
        }


        public IEnumerable<ReferenceAssembly> References
        {
            get
            {

                List<ReferenceAssembly> _result = new List<ReferenceAssembly>();
                var lst = GetElements(NodeProject.ContentReference).ToList();
                HashSet<string> _h = new HashSet<string>();
                foreach (Microsoft.Build.Construction.ProjectItemElement ass in lst)
                {
                    if (_h.Add(ass.Include) && !ass.Include.Contains("$") && !ass.Include.Contains(")"))
                    {
                        var a = new ReferenceAssembly(ass.Include, ass.FirstChild);
                        _result.Add(a);
                    }
                }

                return _result;
            }
        }



        public string AspNetDebugging
        {
            get { return FindProperty<string>("WebApplication.AspNetDebugging").Value; }
            set { FindProperty<string>("WebApplication.AspNetDebugging").Value = value; }
        }

        public string AspnetCompilerIISMetabasePath
        {
            get { return FindProperty<string>("WebApplication.AspnetCompilerIISMetabasePath").Value; }
            set { FindProperty<string>("WebApplication.AspnetCompilerIISMetabasePath").Value = value; }
        }

        public string OutputTypeEx
        {
            get { return FindProperty<string>("OutputTypeEx").Value; }
            set { FindProperty<string>("OutputTypeEx").Value = value; }
        }

        public string TargetFrameworkMoniker
        {
            get { return FindProperty<string>("TargetFrameworkMoniker").Value; }
            set { FindProperty<string>("TargetFrameworkMoniker").Value = value; }
        }

        public string ComVisible
        {
            get { return FindProperty<string>("ComVisible").Value; }
            set { FindProperty<string>("ComVisible").Value = value; }
        }

        public string EnableSecurityDebugging
        {
            get { return FindProperty<string>("EnableSecurityDebugging").Value; }
            set { FindProperty<string>("EnableSecurityDebugging").Value = value; }
        }

        public string OptionCompare
        {
            get { return FindProperty<string>("OptionCompare").Value; }
            set { FindProperty<string>("OptionCompare").Value = value; }
        }

        public string StartupObject
        {
            get { return FindProperty<string>("StartupObject").Value; }
            set { FindProperty<string>("StartupObject").Value = value; }
        }

        public string SSLEnabled
        {
            get { return FindProperty<string>("WebApplication.SSLEnabled").Value; }
            set { FindProperty<string>("WebApplication.SSLEnabled").Value = value; }
        }

        public string UseIIS
        {
            get { return FindProperty<string>("WebApplication.UseIIS").Value; }
            set { FindProperty<string>("WebApplication.UseIIS").Value = value; }
        }

        public string ManifestCertificateThumbprint
        {
            get { return FindProperty<string>("ManifestCertificateThumbprint").Value; }
            set { FindProperty<string>("ManifestCertificateThumbprint").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Trademark
        {
            get { return FindProperty<string>("Trademark").Value; }
            set { FindProperty<string>("Trademark").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Title
        {
            get { return FindProperty<string>("Title").Value; }
            set { FindProperty<string>("Title").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StartExternalUrl
        {
            get { return FindProperty<string>("WebApplication.StartExternalUrl").Value; }
            set { FindProperty<string>("WebApplication.StartExternalUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string IISUrl
        {
            get { return FindProperty<string>("WebApplication.IISUrl").Value; }
            set { FindProperty<string>("WebApplication.IISUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyOriginatorKeyFileType
        {
            get { return FindProperty<string>("AssemblyOriginatorKeyFileType").Value; }
            set { FindProperty<string>("AssemblyOriginatorKeyFileType").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FileName
        {
            get { return FindProperty<string>("FileName").Value; }
            set { FindProperty<string>("FileName").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string WebServer
        {
            get { return FindProperty<string>("WebServer").Value; }
            set { FindProperty<string>("WebServer").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyOriginatorKeyMode
        {
            get { return FindProperty<string>("AssemblyOriginatorKeyMode").Value; }
            set { FindProperty<string>("AssemblyOriginatorKeyMode").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyKeyContainerName
        {
            get { return FindProperty<string>("AssemblyKeyContainerName").Value; }
            set { FindProperty<string>("AssemblyKeyContainerName").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string WindowsAuthenticationEnabled
        {
            get { return FindProperty<string>("WebApplication.WindowsAuthenticationEnabled").Value; }
            set { FindProperty<string>("WebApplication.WindowsAuthenticationEnabled").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SecureUrl
        {
            get { return FindProperty<string>("WebApplication.SecureUrl").Value; }
            set { FindProperty<string>("WebApplication.SecureUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DevelopmentServerCommandLine
        {
            get { return FindProperty<string>("WebApplication.DevelopmentServerCommandLine").Value; }
            set { FindProperty<string>("WebApplication.DevelopmentServerCommandLine").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SQLDebugging
        {
            get { return FindProperty<string>("WebApplication.SQLDebugging").Value; }
            set { FindProperty<string>("WebApplication.SQLDebugging").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StartPageUrl
        {
            get { return FindProperty<string>("WebApplication.StartPageUrl").Value; }
            set { FindProperty<string>("WebApplication.StartPageUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultServerDirectoryListing
        {
            get { return FindProperty<string>("WebApplication.DefaultServerDirectoryListing").Value; }
            set { FindProperty<string>("WebApplication.DefaultServerDirectoryListing").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DevelopmentServerVPath
        {
            get { return FindProperty<string>("WebApplication.DevelopmentServerVPath").Value; }
            set { FindProperty<string>("WebApplication.DevelopmentServerVPath").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DevelopmentServerPort
        {
            get { return FindProperty<string>("WebApplication.DevelopmentServerPort").Value; }
            set { FindProperty<string>("WebApplication.DevelopmentServerPort").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ProjectType
        {
            get { return FindProperty<string>("ProjectType").Value; }
            set { FindProperty<string>("ProjectType").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ReferencePath
        {
            get { return FindProperty<string>("ReferencePath").Value; }
            set { FindProperty<string>("ReferencePath").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string IsUsingIISExpress
        {
            get { return FindProperty<string>("WebApplication.IsUsingIISExpress").Value; }
            set { FindProperty<string>("WebApplication.IsUsingIISExpress").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PreBuildEvent
        {
            get { return FindProperty<string>("PreBuildEvent").Value; }
            set { FindProperty<string>("PreBuildEvent").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AnonymousAuthenticationEnabled
        {
            get { return FindProperty<string>("WebApplication.AnonymousAuthenticationEnabled").Value; }
            set { FindProperty<string>("WebApplication.AnonymousAuthenticationEnabled").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SilverlightDebugging
        {
            get { return FindProperty<string>("WebApplication.SilverlightDebugging").Value; }
            set { FindProperty<string>("WebApplication.SilverlightDebugging").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StartCmdLineArguments
        {
            get { return FindProperty<string>("WebApplication.StartCmdLineArguments").Value; }
            set { FindProperty<string>("WebApplication.StartCmdLineArguments").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Copyright
        {
            get { return FindProperty<string>("Copyright").Value; }
            set { FindProperty<string>("Copyright").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ApplicationIcon
        {
            get { return FindProperty<string>("ApplicationIcon").Value; }
            set { FindProperty<string>("ApplicationIcon").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string CurrentDebugUrl
        {
            get { return FindProperty<string>("WebApplication.CurrentDebugUrl").Value; }
            set { FindProperty<string>("WebApplication.CurrentDebugUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ExcludedPermissions
        {
            get { return FindProperty<string>("ExcludedPermissions").Value; }
            set { FindProperty<string>("ExcludedPermissions").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RunPostBuildEvent
        {
            get { return FindProperty<string>("RunPostBuildEvent").Value; }
            set { FindProperty<string>("RunPostBuildEvent").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultTargetSchema
        {
            get { return FindProperty<string>("DefaultTargetSchema").Value; }
            set { FindProperty<string>("DefaultTargetSchema").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RootNamespace
        {
            get { return FindProperty<string>("RootNamespace").Value; }
            set { FindProperty<string>("RootNamespace").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string IsUsingCustomServer
        {
            get { return FindProperty<string>("WebApplication.IsUsingCustomServer").Value; }
            set { FindProperty<string>("WebApplication.IsUsingCustomServer").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ManifestTimestampUrl
        {
            get { return FindProperty<string>("ManifestTimestampUrl").Value; }
            set { FindProperty<string>("ManifestTimestampUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ManifestKeyFile
        {
            get { return FindProperty<string>("ManifestKeyFile").Value; }
            set { FindProperty<string>("ManifestKeyFile").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DebugSecurityZoneURL
        {
            get { return FindProperty<string>("DebugSecurityZoneURL").Value; }
            set { FindProperty<string>("DebugSecurityZoneURL").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Product
        {
            get { return FindProperty<string>("Product").Value; }
            set { FindProperty<string>("Product").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PostBuildEvent
        {
            get { return FindProperty<string>("PostBuildEvent").Value; }
            set { FindProperty<string>("PostBuildEvent").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OptionStrict
        {
            get { return FindProperty<string>("OptionStrict").Value; }
            set { FindProperty<string>("OptionStrict").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultHTMLPageLayout
        {
            get { return FindProperty<string>("DefaultHTMLPageLayout").Value; }
            set { FindProperty<string>("DefaultHTMLPageLayout").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DelaySign
        {
            get { return FindProperty<string>("DelaySign").Value; }
            set { FindProperty<string>("DelaySign").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OutputType
        {
            get { return FindProperty<string>("OutputType").Value; }
            set { FindProperty<string>("OutputType").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StartWorkingDirectory
        {
            get { return FindProperty<string>("WebApplication.StartWorkingDirectory").Value; }
            set { FindProperty<string>("WebApplication.StartWorkingDirectory").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DebugStartAction
        {
            get { return FindProperty<string>("WebApplication.DebugStartAction").Value; }
            set { FindProperty<string>("WebApplication.DebugStartAction").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string NeutralResourcesLanguage
        {
            get { return FindProperty<string>("NeutralResourcesLanguage").Value; }
            set { FindProperty<string>("NeutralResourcesLanguage").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OptionExplicit
        {
            get { return FindProperty<string>("OptionExplicit").Value; }
            set { FindProperty<string>("OptionExplicit").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OutputFileName
        {
            get { return FindProperty<string>("OutputFileName").Value; }
            set { FindProperty<string>("OutputFileName").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ServerExtensionsVersion
        {
            get { return FindProperty<string>("ServerExtensionsVersion").Value; }
            set { FindProperty<string>("ServerExtensionsVersion").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string NonSecureUrl
        {
            get { return FindProperty<string>("WebApplication.NonSecureUrl").Value; }
            set { FindProperty<string>("WebApplication.NonSecureUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public new string ToString
        {
            get { return FindProperty<string>("WebApplication.ToString").Value; }
            set { FindProperty<string>("WebApplication.ToString").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyGuid
        {
            get { return FindProperty<string>("AssemblyGuid").Value; }
            set { FindProperty<string>("AssemblyGuid").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string GenerateManifests
        {
            get { return FindProperty<string>("GenerateManifests").Value; }
            set { FindProperty<string>("GenerateManifests").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyVersion
        {
            get { return FindProperty<string>("AssemblyVersion").Value; }
            set { FindProperty<string>("AssemblyVersion").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Win32ResourceFile
        {
            get { return FindProperty<string>("Win32ResourceFile").Value; }
            set { FindProperty<string>("Win32ResourceFile").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Description
        {
            get { return FindProperty<string>("Description").Value; }
            set { FindProperty<string>("Description").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string URL
        {
            get { return FindProperty<string>("URL").Value; }
            set { FindProperty<string>("URL").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultClientScript
        {
            get { return FindProperty<string>("DefaultClientScript").Value; }
            set { FindProperty<string>("DefaultClientScript").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string NativeDebugging
        {
            get { return FindProperty<string>("WebApplication.NativeDebugging").Value; }
            set { FindProperty<string>("WebApplication.NativeDebugging").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TargetFramework
        {
            get { return FindProperty<string>("TargetFramework").Value; }
            set { FindProperty<string>("TargetFramework").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SignManifests
        {
            get { return FindProperty<string>("SignManifests").Value; }
            set { FindProperty<string>("SignManifests").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OfflineURL
        {
            get { return FindProperty<string>("OfflineURL").Value; }
            set { FindProperty<string>("OfflineURL").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string WebServerVersion
        {
            get { return FindProperty<string>("WebServerVersion").Value; }
            set { FindProperty<string>("WebServerVersion").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Publish
        {
            get { return FindProperty<string>("Publish").Value; }
            set { FindProperty<string>("Publish").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int AssemblyType
        {
            get { return FindProperty<int>("AssemblyType").Value; }
            set { FindProperty<int>("AssemblyType").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FullPath
        {
            get { return FindProperty<string>("FullPath").Value; }
            set { FindProperty<string>("FullPath").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string WebAccessMethod
        {
            get { return FindProperty<string>("WebAccessMethod").Value; }
            set { FindProperty<string>("WebAccessMethod").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string UseIISExpress
        {
            get { return FindProperty<string>("WebApplication.UseIISExpress").Value; }
            set { FindProperty<string>("WebApplication.UseIISExpress").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BrowseURL
        {
            get { return FindProperty<string>("WebApplication.BrowseURL").Value; }
            set { FindProperty<string>("WebApplication.BrowseURL").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string NTLMAuthentication
        {
            get { return FindProperty<string>("WebApplication.NTLMAuthentication").Value; }
            set { FindProperty<string>("WebApplication.NTLMAuthentication").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OverrideIISAppRootUrl
        {
            get { return FindProperty<string>("WebApplication.OverrideIISAppRootUrl").Value; }
            set { FindProperty<string>("WebApplication.OverrideIISAppRootUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OpenedURL
        {
            get { return FindProperty<string>("WebApplication.OpenedURL").Value; }
            set { FindProperty<string>("WebApplication.OpenedURL").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyKeyProviderName
        {
            get { return FindProperty<string>("AssemblyKeyProviderName").Value; }
            set { FindProperty<string>("AssemblyKeyProviderName").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TypeComplianceDiagnostics
        {
            get { return FindProperty<string>("TypeComplianceDiagnostics").Value; }
            set { FindProperty<string>("TypeComplianceDiagnostics").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Company
        {
            get { return FindProperty<string>("Company").Value; }
            set { FindProperty<string>("Company").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ActiveFileSharePath
        {
            get { return FindProperty<string>("ActiveFileSharePath").Value; }
            set { FindProperty<string>("ActiveFileSharePath").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyOriginatorKeyFile
        {
            get { return FindProperty<string>("AssemblyOriginatorKeyFile").Value; }
            set { FindProperty<string>("AssemblyOriginatorKeyFile").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StartWebServerOnDebug
        {
            get { return FindProperty<string>("WebApplication.StartWebServerOnDebug").Value; }
            set { FindProperty<string>("WebApplication.StartWebServerOnDebug").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AutoAssignPort
        {
            get { return FindProperty<string>("WebApplication.AutoAssignPort").Value; }
            set { FindProperty<string>("WebApplication.AutoAssignPort").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ApplicationManifest
        {
            get { return FindProperty<string>("ApplicationManifest").Value; }
            set { FindProperty<string>("ApplicationManifest").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AssemblyFileVersion
        {
            get { return FindProperty<string>("AssemblyFileVersion").Value; }
            set { FindProperty<string>("AssemblyFileVersion").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string AspnetVersion
        {
            get { return FindProperty<string>("AspnetVersion").Value; }
            set { FindProperty<string>("AspnetVersion").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FileSharePath
        {
            get { return FindProperty<string>("FileSharePath").Value; }
            set { FindProperty<string>("FileSharePath").Value = value; }
        }

        /// <summary>
        /// return the name of the assembly
        /// </summary>
        public string AssemblyName
        {
            get { return FindProperty<string>("AssemblyName").Value; }
            set { FindProperty<string>("AssemblyName").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string EditAndContinue
        {
            get { return FindProperty<string>("WebApplication.EditAndContinue").Value; }
            set { FindProperty<string>("WebApplication.EditAndContinue").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ServerDirectoryListing
        {
            get { return FindProperty<string>("WebApplication.ServerDirectoryListing").Value; }
            set { FindProperty<string>("WebApplication.ServerDirectoryListing").Value = value; }
        }

        /// <summary>
        /// return directory full name
        /// </summary>
        public virtual string LocalPath
        {
            get { return new FileInfo(this.project.FileName).Directory.FullName; }
        }

        /// <summary>
        /// return the default namespace
        /// </summary>
        public string DefaultNamespace
        {
            get { return FindProperty<string>("DefaultNamespace").Value; }
            set { FindProperty<string>("DefaultNamespace").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string LinkRepair
        {
            get { return FindProperty<string>("LinkRepair").Value; }
            set { FindProperty<string>("LinkRepair").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string StartExternalProgram
        {
            get { return FindProperty<string>("WebApplication.StartExternalProgram").Value; }
            set { FindProperty<string>("WebApplication.StartExternalProgram").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string IISAppRootUrl
        {
            get { return FindProperty<string>("WebApplication.IISAppRootUrl").Value; }
            set { FindProperty<string>("WebApplication.IISAppRootUrl").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TargetZone
        {
            get { return FindProperty<string>("TargetZone").Value; }
            set { FindProperty<string>("TargetZone").Value = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SignAssembly
        {
            get { return FindProperty<string>("SignAssembly").Value; }
            set { FindProperty<string>("SignAssembly").Value = value; }
        }

        ///// <summary>
        ///// return the reference object com
        ///// </summary>
        //public EnvDTE.Project Source
        //{
        //    get
        //    {
        //        return project;
        //    }
        //}

        /// <summary>
        /// full filename
        /// </summary>
        public virtual string FullName
        {
            get
            {
                return project.FullName;
            }
        }

        /// <summary>
        /// Gets a new node solution that reference the current project..
        /// </summary>
        /// <value>
        /// The solution.
        /// </value>
        public NodeSolution Solution { get { return new NodeSolution(this.project.DTE); } }

        /// <summary>
        /// Is the project hosting the Store actually referencing an assembly?
        /// </summary>
        /// <param name="assembly">Assembly for which we want to know if it is referenced by the 
        /// project hosting the model contained in the store
        /// </param>
        /// <returns><c>true</c> if the assembly is referenced by the project hosting the store, and <c>false</c> otherwise</returns>
        public bool IsReferencingAssembly(Assembly assembly)
        {
            Contract.Requires(assembly != null);
            return IsReferencingAssembly(assembly.GetName().Name);
        }

        /// <summary>
        /// Is the project hosting the Store actually referencing an assembly?
        /// </summary>
        /// <param name="assemblyName">Assembly name for which we want to know if it is referenced by the 
        /// project hosting the model contained in the store
        /// </param>
        /// <returns><c>true</c> if the assembly is referenced by the project hosting the store, and <c>false</c> otherwise</returns>
        public bool IsReferencingAssembly(string assemblyName)
        {
            // Add references.
            VSProject vsProject = this.project.Object as VSProject;
            return (vsProject.References.OfType<Reference>().Any(reference => reference.Name == assemblyName));

        }

        /// <summary>
        /// Ensures that the VS project hosting a modeling store references a given assembly
        /// </summary>
        /// <param name="assembly">Asssembly for which we want to ensure that it is referenced by the VS project hosting the <paramref name="store"/></param>
        public void EnsureReferencesAssembly(Assembly assembly)
        {
            Contract.Requires(assembly != null);
            EnsureReferencesAssembly(assembly.GetName().Name);
        }

        /// <summary>
        /// Ensures that the VS project hosting a modeling store references a given assembly (by name)
        /// </summary>
        /// <param name="assemblyName">Name of the asssembly for which we want to ensure that it is referenced by the VS project hosting the <paramref name="store"/></param>
        public void EnsureReferencesAssembly(string assemblyName)
        {

            // Add references.
            VSProject vsProject = project.Object as VSProject;

            if (!vsProject.References.OfType<Reference>().Any(reference => reference.Name == assemblyName))
                vsProject.References.Add(assemblyName);

        }

        /// <summary>
        /// Ensures that the VS project hosting a modeling store references a given assembly (by name)
        /// </summary>
        /// <param name="project">project instance</param>
        /// <returns></returns>
        public IEnumerable<Assembly> ReferencesAssemblies()
        {

            OutputWriter wr = new OutputWriter();

            VSProject vsProject = project.Object as VSProject;
            foreach (Reference item in vsProject.References)
            {

                // si la Source project n'est pas null c'est que le type est contenu dans la solution.
                //la charger va locker la librairie.
                if (item.SourceProject == null)
                {
                    var path = item.Path;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {

                        Assembly ass = null;

                        try
                        {
                            ass = Assembly.LoadFile(path);
                        }
                        catch (Exception e)
                        {
                            wr.WriteLine(e);
                        }

                        if (ass != null)
                            yield return ass;

                    }

                }
            }

        }

        /// <summary>
        /// return the list of the Referencings the projects.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NodeProject> ReferencingProjects()
        {

            VSProject vsProject = project.Object as VSProject;
            foreach (Reference item in vsProject.References)
            {
                // si la Source project n'est pas null c est que le type est contenu dans la solution.
                if (item.SourceProject != null)
                    yield return new NodeProject(item.SourceProject);
            }

        }

        /// <summary>
        /// Ensures that a VS project in the same solution as the project hosting a modeling store references a given project in the same solution (by name)
        /// </summary>
        /// <param name="uniqueProjectNameReferencing">Unique project name of the project that needs to reference <paramref name="uniqueProjectNameToReference"/>. Can be null, in that
        /// case the project is the project holding the model in the store</param>
        /// <param name="uniqueProjectNameToReference">Unique project nameof the project for which we want to ensure that it is referenced by the VS project hosting the <paramref name="store"/></param>
        public void EnsureProjectReferencesProject(string uniqueProjectNameReferencing, string uniqueProjectNameToReference)
        {

            // Get the referencing project
            NodeProject referencingProject = null;

            var sln = new NodeSolution(project.DTE);

            if (!string.IsNullOrWhiteSpace(uniqueProjectNameReferencing))
                referencingProject = sln.Projects.FirstOrDefault(p => p.Name == uniqueProjectNameReferencing);

            if (referencingProject == null)
                return;

            // Add reference to the other project if it is not already referenced
            VSProject vsProject = referencingProject.VSProject;
            if (vsProject.References.OfType<Reference>().FirstOrDefault(reference => reference.SourceProject != null && reference.SourceProject.UniqueName == uniqueProjectNameToReference) == null)
            {

                NodeProject otherProject = sln.Projects.FirstOrDefault(p => p.Name == uniqueProjectNameToReference);

                if (otherProject != null)
                    vsProject.References.AddProject(otherProject.Project);

            }

        }

    }

}
