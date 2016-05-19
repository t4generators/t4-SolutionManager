namespace VisualStudio.ParsingSolution
{


    [System.Diagnostics.DebuggerDisplay("{Value}")]
    public class GenericNodeProperty<T> : INodeProperty<T>
    {

        NodeItemProperty _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericNodeProperty{T}"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        public GenericNodeProperty(NodeItemProperty item)
        {
            this._instance = item;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value
        {
            get
            {
                if (this._instance != null && this._instance.PropertyObject != null)
                    return (T)this._instance.PropertyObject.Value;
                return default(T);
            }
            set
            {
                if (this._instance != null && this._instance.PropertyObject != null)
                    this._instance.PropertyObject.Value = Value;
            }
        }

    }


}
