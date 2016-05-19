using System.Collections.Generic;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// collection of generic argument 
    /// </summary>
    public class GenericArguments : IEnumerable<GenericArgument>
    {

        private Dictionary<string, GenericArgument> _list;

        /// <summary>
        /// contructor
        /// </summary>
        public GenericArguments()
        {
            _list = new Dictionary<string, GenericArgument>();
        }

        /// <summary>
        /// add an item
        /// </summary>
        public GenericArgument Add(string typeName)
        {
            var g = new GenericArgument(typeName);
            _list.Add(typeName, g);
            return g;
        }

        /// <summary>
        /// resolve an item by typeName
        /// </summary>
        public GenericArgument Resolve(string typeName)
        {
            GenericArgument result;
            _list.TryGetValue(typeName, out result);
            return result;
        }


        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerator<GenericArgument> GetEnumerator()
        {
            return this._list.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this._list.Values.GetEnumerator();
        }

    }


}
