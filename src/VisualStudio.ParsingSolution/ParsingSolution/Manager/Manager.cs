using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{


    public class Manager
    {

        // Manager.tt from Damien Guard: http://damieng.com/blog/2009/11/06/multiple-outputs-from-t4-made-easy-revisited

        private Block currentBlock;
        private List<Block> files = new List<Block>();
        private Block footer = new Block();
        private Block header = new Block();
        private ITextTemplatingEngineHost host;
        private StringBuilder template;
        protected List<Block> generatedFileNames = new List<Block>();
        public Func<FileInfo, bool> filterToDelete = file => file.Name.EndsWith(".generated" + file.Extension);
        protected EnvDTE.DTE dte;
        protected EnvDTE.ProjectItem templateProjectItem;
        protected EnvDTE.Project ContainingProject;

        /// <summary>
        /// 
        /// </summary>
        public static Manager Create(ITextTemplatingEngineHost host, StringBuilder template)
        {

            return (host is IServiceProvider)
                ? new VSManager(host, template)
                : new Manager(host, template);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool FileOkToWrite(String fileName)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public Block StartNewFile(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            CurrentBlock = new Block { Name = name, OutputPath = Path.GetDirectoryName(host.TemplateFile), ParentProjectItem = templateProjectItem, ParentProject = null };
            return CurrentBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        public Block StartNewFile(String name, NodeProject project)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            CurrentBlock = new Block { Name = name, OutputPath = Path.GetDirectoryName(project.FullPath), ParentProjectItem = templateProjectItem, ParentProject = project.Project };
            return CurrentBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        public Block StartNewFile(String name, NodeItemFolder folder)
        {

            if (folder == null)
                throw new ArgumentNullException("folder");

            if (name == null)
                throw new ArgumentNullException("name");

            if (folder is NodeItemFolder)
            {
                EnvDTE.ProjectItem ProjectItem = folder.ProjectItem;
            }

            CurrentBlock = new Block { Name = name, OutputPath = folder.LocalPath, ParentProjectItem = folder.ProjectItem, ParentProject = null };
            return CurrentBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        public EnvDTE.Project GetCurrentProject()
        {
            return ContainingProject;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartFooter()
        {
            CurrentBlock = footer;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartHeader()
        {
            CurrentBlock = header;
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndBlock()
        {
            if (CurrentBlock == null)
                return;
            CurrentBlock.Length = template.Length - CurrentBlock.Start;
            if (CurrentBlock != header && CurrentBlock != footer)
                files.Add(CurrentBlock);
            currentBlock = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Process(bool split)
        {
            if (split)
            {
                EndBlock();
                String headerText = template.ToString(header.Start, header.Length);
                String footerText = template.ToString(footer.Start, footer.Length);
                files.Reverse();
                foreach (Block block in files)
                {
                    block.Fullname = Path.Combine(block.OutputPath, block.Name);

                    StringBuilder sb = new StringBuilder(template.Length);
                    sb.Append(headerText);
                    sb.Append(template.ToString(block.Start, block.Length));
                    sb.Append(footerText);

                    generatedFileNames.Add(block);

                    if (!Directory.Exists(block.OutputPath))
                        Directory.CreateDirectory(block.OutputPath);

                    CreateFile(block.Fullname, sb.ToString());
                    template.Remove(block.Start, block.Length);
                }
            }
        }

        protected virtual void CreateFile(String fileName, String content)
        {
            if (IsFileContentDifferent(fileName, content))
                File.WriteAllText(fileName, content);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual String GetCustomToolNamespace(String fileName)
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual String DefaultProjectNamespace
        {
            get { return null; }
        }

        protected bool IsFileContentDifferent(String fileName, String newContent)
        {
            return !(File.Exists(fileName) && File.ReadAllText(fileName) == newContent);
        }

        private Manager(ITextTemplatingEngineHost host, StringBuilder template)
        {
            this.host = host;
            this.template = template;
        }

        private Block CurrentBlock
        {
            get { return currentBlock; }
            set
            {
                if (CurrentBlock != null)
                    EndBlock();
                if (value != null)
                    value.Start = template.Length;
                currentBlock = value;
            }
        }

        private class VSManager : Manager
        {
            private Action<String> checkOutAction;
            private Action<Func<FileInfo, bool>, IEnumerable<Block>> projectSyncAction;
            private IVsQueryEditQuerySave2 queryEditSave;

            /// <summary>
            /// 
            /// </summary>
            public override String DefaultProjectNamespace
            {
                get
                {
                    return templateProjectItem.ContainingProject.Properties.Item("DefaultNamespace").Value.ToString();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public override String GetCustomToolNamespace(string fileName)
            {
                return dte.Solution.FindProjectItem(fileName).Properties.Item("CustomToolNamespace").Value.ToString();
            }

            /// <summary>
            /// 
            /// </summary>
            public override void Process(bool split)
            {
                base.Process(split);
                projectSyncAction.EndInvoke(projectSyncAction.BeginInvoke(filterToDelete, generatedFileNames, null, null));
            }

            /// <summary>
            /// 
            /// </summary>
            public override bool FileOkToWrite(String fileName)
            {
                CheckoutFileIfRequired(fileName);
                return base.FileOkToWrite(fileName);
            }

            protected override void CreateFile(String fileName, String content)
            {
                try
                {
                    if (IsFileContentDifferent(fileName, content))
                    {
                        CheckoutFileIfRequired(fileName);
                        File.WriteAllText(fileName, content);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message + @"\t for the file : " + fileName, e);
                }
            }

            internal VSManager(ITextTemplatingEngineHost host, StringBuilder template)
                : base(host, template)
            {

                var hostServiceProvider = (IServiceProvider)host;

                if (hostServiceProvider == null)
                    throw new ArgumentNullException("Could not obtain IServiceProvider");

                dte = (EnvDTE.DTE)hostServiceProvider.GetService(typeof(EnvDTE.DTE));
                if (dte == null)
                    throw new ArgumentNullException("Could not obtain DTE from host");

                checkOutAction = (String fileName) => dte.SourceControl.CheckOutItem(fileName);
                projectSyncAction = (Func<FileInfo, bool> filterToDelete, IEnumerable<Block> fileNames) => ProjectSync(filterToDelete, fileNames);
                queryEditSave = (IVsQueryEditQuerySave2)hostServiceProvider.GetService(typeof(SVsQueryEditQuerySave));

                this.templateProjectItem = this.dte.Solution.FindProjectItem(host.TemplateFile);
                this.ContainingProject = this.templateProjectItem.ContainingProject;

            }

            private static void ProjectSync(Func<FileInfo, bool> filterToDelete, IEnumerable<Block> keepFileNames)
            {

                var keepFileNameSet = new HashSet<String>(keepFileNames.Select(c => c.Fullname).ToList());

                foreach (Block block in keepFileNames)
                {

                    string filename1 = string.Empty;
                    var projectFiles = new Dictionary<String, EnvDTE.ProjectItem>();

                    //System.Diagnostics.Debugger.Break();

                    if (block.ParentProject == null)
                    {

                        filename1 = block.ParentProjectItem.get_FileNames(0);
                        var originalFilePrefix = Path.GetFileNameWithoutExtension(filename1) + ".";

                        // On enumere le contenu du repertoire
                        foreach (EnvDTE.ProjectItem projectItem in block.ParentProjectItem.ProjectItems)
                        {
                            string filename = projectItem.get_FileNames(0);
                            projectFiles.Add(filename, projectItem);
                        }

                        // Remove unused items from the project
                        foreach (var pair in projectFiles)
                            if (!keepFileNameSet.Contains(pair.Key) && !(Path.GetFileNameWithoutExtension(pair.Key) + ".").StartsWith(originalFilePrefix))
                                if (filterToDelete(new FileInfo(pair.Key)))
                                    pair.Value.Delete();

                        // Add missing files to the project
                        foreach (String fileName in keepFileNameSet)
                            if (!projectFiles.ContainsKey(fileName))
                                block.ParentProjectItem.ProjectItems.AddFromFile(fileName);

                    }
                    else
                    {

                        filename1 = filename1 = new FileInfo(block.ParentProject.FileName).Directory.FullName;
                        var originalFilePrefix = Path.GetFileNameWithoutExtension(filename1) + ".";

                        // On enumere le contenu du repertoire
                        foreach (EnvDTE.ProjectItem projectItem in block.ParentProject.ProjectItems)
                        {
                            string filename = projectItem.get_FileNames(0);
                            projectFiles.Add(filename, projectItem);
                        }

                        // Remove unused items from the project
                        foreach (var pair in projectFiles)
                            if (!keepFileNameSet.Contains(pair.Key) && !(Path.GetFileNameWithoutExtension(pair.Key) + ".").StartsWith(originalFilePrefix))
                                if (filterToDelete(new FileInfo(pair.Key)))
                                    pair.Value.Delete();

                        // Add missing files to the project
                        foreach (String fileName in keepFileNameSet)
                            if (!projectFiles.ContainsKey(fileName))
                                block.ParentProject.ProjectItems.AddFromFile(fileName);

                    }

                }

            }

            private void CheckoutFileIfRequired(String fileName)
            {
                if (queryEditSave != null)
                {
                    uint pfEditVerdict;
                    queryEditSave.QuerySaveFile(fileName, 0, null, out pfEditVerdict);
                }
                else
                {
                    var sc = dte.SourceControl;
                    if (sc != null && sc.IsItemUnderSCC(fileName) && !sc.IsItemCheckedOut(fileName))
                        checkOutAction.EndInvoke(checkOutAction.BeginInvoke(fileName, null, null));
                }
            }
        }

    }

}
