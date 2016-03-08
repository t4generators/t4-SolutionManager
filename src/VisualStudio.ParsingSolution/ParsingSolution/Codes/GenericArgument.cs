using System.Collections.Generic;

namespace VisualStudio.ParsingSolution.Projects.Codes
{
    /// <summary>
    /// generic argument informations
    /// </summary>
    public class GenericArgument
    {

        private List<string> _constraints;

        /// <summary>
        /// constructor
        /// </summary>
        public GenericArgument(string typeName)
        {
            _constraints = new List<string>();
            this.Name = typeName;
        }


        public bool HasConstraint { get; private set; }


        /// <summary>
        /// Add constraint
        /// </summary>
        public void AddConstraint(string constraint)
        {
            if (!string.IsNullOrEmpty(constraint))
            {

                HasConstraint = true;

                if (constraint == "new()")
                    this.HasEmptyConstructor = true;

                else if (constraint == "new()")
                    this.IsClass = true;

                else
                    _constraints.Add(constraint);
            }
        }

        /// <summary>
        /// name of the generic
        /// </summary>
        public string Name { get; private set; }


        /// <summary>
        /// the type maust have an empty contructor accessible
        /// </summary>
        public bool HasEmptyConstructor { get; private set; }

        /// <summary>
        /// the type is a class
        /// </summary>
        public bool IsClass { get; private set; }

        /// <summary>
        /// list of contraints
        /// </summary>
        public IEnumerable<string> Constraints { get { return _constraints; } }

    }


}