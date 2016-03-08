using System.IO;

namespace VisualStudio.ParsingSolution
{


    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class NodeFolderSolution : NodeSolutionItem
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeFolderSolution"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public NodeFolderSolution(EnvDTE.Project project)
            : base(project)
        {

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
                return project.FullName;
            }
        }

        /// <summary>
        /// Kind item
        /// </summary>
        public override KindItem KindItem { get { return KindItem.Folder; } }

    }


}
