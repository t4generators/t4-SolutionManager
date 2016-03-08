namespace VisualStudio.ParsingSolution
{


    [System.Diagnostics.DebuggerDisplay("{Name}: {Value}")]
    public class NodeProperty<T> : NodeItemProperty, INodeProperty<T>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeProperty{T}"/> class.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="name">The name.</param>
        public NodeProperty(object o, string name)
            : base(o, name)
        {

        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value
        {
            get { return PropertyObject != null ? (T)PropertyObject.Value : default(T); }
            set { PropertyObject.Value = value; }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="NodeProperty{T}"/> to <see cref="T"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator T(NodeProperty<T> property) { return property.Value; }

    }


}
