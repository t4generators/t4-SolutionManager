namespace VisualStudio.ParsingSolution
{

    public abstract class NodeItemBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeItemBase"/> class.
        /// </summary>
        public NodeItemBase()
        {

        }

        /// <summary>
        /// Gets the kind item.
        /// </summary>
        /// <value>
        /// The kind item.
        /// </value>
        public abstract KindItem KindItem { get; }

    }

}
