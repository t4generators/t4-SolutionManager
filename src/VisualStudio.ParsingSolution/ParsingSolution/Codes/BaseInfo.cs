using EnvDTE80;
using System;

namespace VisualStudio.ParsingSolution.Projects.Codes
{


    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class BaseInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public BaseInfo(BaseInfo parent, CodeElement2 item)
        {
            this.Name = item.Name;
            this.FullName = item.FullName;
            this.IsCodeType = item.IsCodeType;
            this.Root = parent;
            this.Source = item;

            try
            {
                this.Location = new LocationInfo(item.StartPoint, item.EndPoint);
            }
            catch (Exception)
            {
                this.Location = new LocationInfo(null, null);
            }

        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        protected CodeElement2 Source { get; private set; }

        /// <summary>
        /// Namespace
        /// </summary>
        public string Namespace { get; protected set; }

        /// <summary>
        /// Code documentation
        /// </summary>
        public string DocComment { get; protected set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        /// <value>
        /// The summary.
        /// </value>
        public string Summary { get; protected set; }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        public string FullName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is code type.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is code type; otherwise, <c>false</c>.
        /// </value>
        public bool IsCodeType { get; private set; }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        public BaseInfo Root { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enum.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is enum; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnum { get; protected set; }


        /// <summary>
        /// Gets or sets a value indicating whether this instance is interface.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is interface; otherwise, <c>false</c>.
        /// </value>
        public bool IsInterface { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is class.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is class; otherwise, <c>false</c>.
        /// </value>
        public bool IsClass { get; protected set; }

        /// <summary>
        /// Gets the location of the code.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public LocationInfo Location { get; private set; }

    }




}
