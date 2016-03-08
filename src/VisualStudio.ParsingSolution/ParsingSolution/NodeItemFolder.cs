using System;
using System.IO;
using System.Linq;

namespace VisualStudio.ParsingSolution
{

    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeItemFolder : NodeItem
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeItemFolder"/> class.
        /// </summary>
        /// <param name="s">The s.</param>
        public NodeItemFolder(EnvDTE.ProjectItem s)
            : base(s)
        {

        }

        /// <summary>
        /// Gets the kind item.
        /// </summary>
        /// <value>
        /// The kind item.
        /// </value>
        public override KindItem KindItem { get { return KindItem.Folder; } }

        /// <summary>
        /// resolve the folder path and return the last folder of the path.
        /// if a part is missing. it is created
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public NodeItemFolder GetFolder(string path)
        {

            string p = path.Replace("/", @"\");
            p = p.Trim();
            p = p.Trim('\\');

            string[] ar = p.Split('\\');

            return GetFolder(ar);

        }

        /// <summary>
        /// Gets the folder.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public NodeItemFolder GetFolder(string[] paths)
        {

            NodeItemFolder node = null;
            string n2 = Name;

            for (int i = 0; i < paths.Length; i++)
            {

                string n = paths[i];

                if (string.IsNullOrEmpty(n))
                    return null;

                node = this.GetItem<NodeItemFolder>(c => c.Name == n).FirstOrDefault();

                if (node == null)
                {

                    var _path = Path.Combine(LocalPath, n);
                    DirectoryInfo dir = new DirectoryInfo(_path);

                    if (!dir.Exists)
                        dir.Create();

                    this.s.ProjectItems.AddFromDirectory(_path);

                    node = this.GetItem<NodeItemFolder>(c => c.Name == n).FirstOrDefault();

                    if (node == null)
                        throw new Exception(String.Format("{0} can't be resolved in ", n, n2));

                }

                n2 = n;

            }

            return node;

        }

    }
}
