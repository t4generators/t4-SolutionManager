using System;

namespace VisualStudio.ParsingSolution
{


    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeItemProperty
    {

        private bool? undefine = null;
        private EnvDTE.Property property;
        private EnvDTE.Properties properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeItemProperty"/> class.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="name">The name.</param>
        public NodeItemProperty(object o, string name)
        {
            Name = name;

            if ((o as EnvDTE.Solution) != null)
                this.properties = (o as EnvDTE.Solution).Properties;

            else if ((o as EnvDTE.Project) != null)
                this.properties = (o as EnvDTE.Project).Properties;

            else if ((o as EnvDTE.ProjectItem) != null)
                this.properties = (o as EnvDTE.ProjectItem).Properties;

        }

        private EnvDTE.Property SetValue()
        {

            try
            {
                property = properties.Item(Name);
                undefine = true;
                return property;
            }
            catch (Exception)
            {
                undefine = false;
            }

            return null;

        }

        /// <summary>
        /// Gets the property object.
        /// </summary>
        /// <value>
        /// The property object.
        /// </value>
        public EnvDTE.Property PropertyObject { get { return undefine != null ? property : SetValue(); } }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="NodeItemProperty"/> is undefine.
        /// </summary>
        /// <value>
        ///   <c>true</c> if undefine; otherwise, <c>false</c>.
        /// </value>
        private bool Undefine { get { return undefine != null ? (bool)undefine : (PropertyObject == null); } }

    }

}
