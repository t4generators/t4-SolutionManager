using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;

namespace VisualStudio.ParsingSolution
{


    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public abstract class NodeSolutionItem : NodeItemBase
    {

        protected EnvDTE.Project project;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeSolutionItem"/> class.
        /// </summary>
        public NodeSolutionItem()
            : base()
        {

        }


        protected Dictionary<string, NodeItemProperty> _properties = null;
        /// <summary>
        /// Finds the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        protected INodeProperty<T> FindProperty<T>(string p)
        {
            if (_properties == null)
                BuildProperties();

            NodeItemProperty result;
            _properties.TryGetValue(p, out result);

            return new GenericNodeProperty<T>(result);

        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public Dictionary<string, NodeItemProperty> Properties { get { return _properties; } }

        protected virtual void BuildProperties()
        {
            _properties = new Dictionary<string, NodeItemProperty>();
            if (project != null)
            {
                try
                {
                    foreach (Property item in project.Properties)
                        _properties.Add(item.Name, new NodeProperty<object>(this.project, item.Name));
                }
                catch { }

            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeSolutionItem"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public NodeSolutionItem(EnvDTE.Project project)
            : base()
        {
            this.project = project;
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEnumerable<T> GetItem<T>()
            where T : NodeSolutionItem
        {
            foreach (T item in GetItem<T>((Func<T, bool>)null))
                yield return item;
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public virtual IEnumerable<T> GetItem<T>(Func<T, bool> filter)
            where T : NodeSolutionItem
        {

            if (project == null)
                yield break;

            var items = project.ProjectItems;
            if (items != null)
            {
                foreach (EnvDTE.ProjectItem s in project.ProjectItems)
                {

                    NodeSolutionItem fld = null;

                    EnvDTE.Project proj = s.SubProject as EnvDTE.Project;

                    if (proj != null && !string.IsNullOrEmpty(proj.FullName))
                        fld = new NodeProject(proj);

                    else if (s.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}" && proj != null)
                        fld = new NodeFolderSolution(s.SubProject);

                    else if (s.Kind == "{66A26722-8FB5-11D2-AA7E-00C04F688DDE}" && proj !=null)
                        fld = new NodeFolderSolution(s.SubProject);

                    else if (s.Kind == "{EA6618E8-6E24-4528-94BE-6889FE16485C}" && proj != null)
                        fld = new NodeVirtualFolder(s as EnvDTE.Project);

                    //if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}" && proj != null)
                    //    fld = new NodeFolderSolution(project);

                    //else if (project.Kind == "{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}" && proj != null)  // is not the same that last test
                    //    fld = new NodeFolderSolution(project);

                    else
                        fld = ProjectHelper.CreateNodeItem(s);


                    if (fld != null)
                    {
                        var f = fld as T;
                        if (f != null)
                            if (filter == null || filter(f))
                                yield return f;

                        foreach (NodeSolutionItem i2 in fld.GetItem<T>(filter))
                            yield return i2 as T;
                    }

                }

            }

        }

        /// <summary>
        /// return the kind of the object
        /// </summary>
        /// <value>
        /// The kind.
        /// </value>
        public virtual string Kind
        {
            get
            {
                return project.Kind;
            }
        }


        /// <summary>
        /// get or set the name of the project
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public virtual string Name
        {
            get
            {
                return project.Name;
            }
            set
            {
                project.Name = value;
            }
        }

        /// <summary>
        /// Gets the get last access time of the file.
        /// </summary>
        /// <value>
        /// The get last access time.
        /// </value>
        public virtual DateTime GetLastAccessTime { get { return File.GetLastAccessTime(this.project.FullName); } }

        /// <summary>
        /// Gets the get creation time of the file.
        /// </summary>
        /// <value>
        /// The get creation time.
        /// </value>
        public virtual DateTime GetCreationTime { get { return File.GetCreationTime(this.project.FullName); } }

        /// <summary>
        /// Gets the get last write time of the file.
        /// </summary>
        /// <value>
        /// The get last write time.
        /// </value>
        public virtual DateTime GetLastWriteTime { get { return File.GetLastWriteTime(this.project.FullName); } }

    }

}
