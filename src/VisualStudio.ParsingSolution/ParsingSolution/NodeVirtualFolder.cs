namespace VisualStudio.ParsingSolution
{

    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class NodeVirtualFolder : NodeSolutionItem
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeVirtualFolder"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public NodeVirtualFolder(EnvDTE.Project project)
            : base(project)
        {

        }

        /// <summary>
        /// return the fullname of the virtual filder
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        public virtual string FullName
        {
            get
            {
                return project.FullName;
            }
        }

        /// <summary>
        /// Gets the kind item.
        /// </summary>
        /// <value>
        /// The kind item.
        /// </value>
        public override KindItem KindItem { get { return KindItem.VirtualFolder; } }

    }

}
