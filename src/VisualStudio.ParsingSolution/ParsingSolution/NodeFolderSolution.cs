using System;
using System.IO;
using System.Linq;

namespace VisualStudio.ParsingSolution
{


    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeFolderSolution : NodeSolutionItem
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeFolderSolution"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public NodeFolderSolution(EnvDTE.Project project)
            : base(project)
        {

            this.Solution = new NodeSolution();

        }

        /// <summary>
        /// Appends the file the folder.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <param name="name">The name.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public string AppendFile(string solutionPath, string name, string content)
        {

            var dir = new System.IO.DirectoryInfo(System.IO.Path.Combine(this.Solution.Folder, ".Solutionfolders", solutionPath));
            var file = new FileInfo(Path.Combine(dir.FullName, name));

            if (!file.Directory.Exists)
                file.Directory.Create();

            if (file.Exists)
                file.Delete();

            var ar = System.Text.Encoding.UTF8.GetBytes(content);

            using (var stream = file.OpenWrite())
            {
                stream.Write(ar, 0, ar.Length);
                stream.Flush();
            }

            AddFile(file);

            return file.FullName;

        }

        /// <summary>
        /// Appends the file the folder.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <param name="name">The name.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public string AppendFile(string solutionPath, string name, byte[] content)
        {

            var dir = new System.IO.DirectoryInfo(System.IO.Path.Combine(this.Solution.Folder, ".Solutionfolders", solutionPath));
            var file = new FileInfo(Path.Combine(dir.FullName, name));

            if (!file.Directory.Exists)
                file.Directory.Create();

            if (file.Exists)
                file.Delete();

            using (var stream = file.OpenWrite())
            {
                stream.Write(content, 0, content.Length);
                stream.Flush();
            }

            AddFile(file);

            return file.FullName;

        }

        /// <summary>
        /// add a new file in the folder of the solution
        /// </summary>
        public NodeItem AddFile(FileInfo file)
        {
            return AddFile(file.FullName);
        }

        /// <summary>
        /// add a new file in the folder of the solution
        /// </summary>
        public NodeItem AddFile(string FullName)
        {
            return new NodeItem(this.project.ProjectItems.AddFromFile(FullName));
        }

        /// <summary>
        /// add a new files in the folder to the solution
        /// </summary>
        public NodeItem AddFiles(DirectoryInfo directory)
        {
            return AddFiles(directory.FullName);
        }

        /// <summary>
        /// add a new files in the folder to the solution
        /// </summary>
        public NodeItem AddFiles(string directoryFullName)
        {
            return new NodeItemFolder(this.project.ProjectItems.AddFromDirectory(directoryFullName));
        }

        /// <summary>
        /// return the fullname of the file
        /// </summary>
        public virtual string FullName
        {
            get
            {
                return project.Name;
            }
        }

        /// <summary>
        /// Kind item
        /// </summary>
        public override KindItem KindItem { get { return KindItem.Folder; } }

        public NodeSolution Solution { get; private set; }

        public NodeFolderSolution GetSolutionFolder(string path)
        {

            string p = path.Replace("/", @"\");
            p = p.Trim();
            p = p.Trim('\\');

            string[] ar = p.Split('\\');

            return GetSolutionFolder(ar);

        }

        public NodeFolderSolution GetSolutionFolder(string[] paths)
        {

            NodeFolderSolution node = null;
            var sln2 = (this.project.DTE.Solution as EnvDTE80.Solution2);


            for (int i = 0; i < paths.Length; i++)
            {

                string n = paths[i];

                if (string.IsNullOrEmpty(n))
                    return null;

                if (node == null)
                    node = this.GetItem<NodeFolderSolution>(c => c.Name.ToLower() == n.ToLower()).FirstOrDefault();
                else
                    node = node.GetSolutionFolder(n);

                if (node == null)
                {
                    var x1 = (this.project.Object as EnvDTE80.SolutionFolder);
                    var x2 = x1.AddSolutionFolder(n);
                    node = new NodeFolderSolution(x2);
                }

            }

            return node;

        }

    }


}
