using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using VSLangProj;

namespace VisualStudio.ParsingSolution
{

    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeSolution : NodeSolutionItem
    {

        private EnvDTE.Solution solution;
        private EnvDTE.DTE dte;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dte">The DTE.</param>
        public NodeSolution(EnvDTE.DTE dte) : base()
        {
            if (dte == null)
                this.dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            else
            this.dte = dte;
            this.solution = this.dte.Solution;
            this.BuildProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeSolution"/> class.
        /// </summary>
        public NodeSolution()
            : base()
        {
            EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            this.dte = dte;
            this.solution = dte.Solution;
            this.BuildProperties();
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public EnvDTE.Solution Source { get { return this.solution; } }

        /// <summary>
        /// The vs view kind code
        /// </summary>
        public const string vsViewKindCode = "{7651A701-06E5-11D1-8EBD-00A0C90F26EA}";

        /// <summary>
        /// return the fullname of the specified path
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public string ResolvePath(string path)
        {

            ProjectItem projectItem = this.solution.FindProjectItem(path);

            if (projectItem == null)
                throw new Exception(string.Format("the path '{0}' can't be resolved", path));

            // If the .tt file is not opened, open it
            if (projectItem.Document == null)
                projectItem.Open(vsViewKindCode);

            var file = projectItem.FileNames[1];

            return file;

        }

        /// <summary>
        /// Gets the kind item.
        /// </summary>
        /// <value>
        /// The kind item.
        /// </value>
        public override KindItem KindItem { get { return KindItem.Solution; } }

        /// <summary>
        /// return the name of the solution
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name
        {
            get { return FindProperty<string>("Name").Value; }
            set { FindProperty<string>("Name").Value = value; }
        }

        /// <summary>
        /// Gets or sets the extender catid.
        /// </summary>
        /// <value>
        /// The extender catid.
        /// </value>
        public string ExtenderCATID
        {
            get { return FindProperty<string>("ExtenderCATID").Value; }
            set { FindProperty<string>("ExtenderCATID").Value = value; }
        }

        /// <summary>
        /// Gets or sets the project dependencies.
        /// </summary>
        /// <value>
        /// The project dependencies.
        /// </value>
        public string ProjectDependencies
        {
            get { return FindProperty<string>("ProjectDependencies").Value; }
            set { FindProperty<string>("ProjectDependencies").Value = value; }
        }

        /// <summary>
        /// Gets or sets the extender.
        /// </summary>
        /// <value>
        /// The extender.
        /// </value>
        public string Extender
        {
            get { return FindProperty<string>("Extender").Value; }
            set { FindProperty<string>("Extender").Value = value; }
        }

        /// <summary>
        /// Gets or sets the active configuration.
        /// </summary>
        /// <value>
        /// The active configuration.
        /// </value>
        public string ActiveConfig
        {
            get { return FindProperty<string>("ActiveConfig").Value; }
            set { FindProperty<string>("ActiveConfig").Value = value; }
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path
        {
            get { return FindProperty<string>("Path").Value; }
            set { FindProperty<string>("Path").Value = value; }
        }

        /// <summary>
        /// Gets or sets the extender names.
        /// </summary>
        /// <value>
        /// The extender names.
        /// </value>
        public string ExtenderNames
        {
            get { return FindProperty<string>("ExtenderNames").Value; }
            set { FindProperty<string>("ExtenderNames").Value = value; }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description
        {
            get { return FindProperty<string>("Description").Value; }
            set { FindProperty<string>("Description").Value = value; }
        }

        /// <summary>
        /// Gets or sets the startup project.
        /// </summary>
        /// <value>
        /// The startup project.
        /// </value>
        public string StartupProject
        {
            get { return FindProperty<string>("StartupProject").Value; }
            set { FindProperty<string>("StartupProject").Value = value; }
        }


        /// <summary>
        /// Gets the get DTE.
        /// </summary>
        /// <value>
        /// The get DTE.
        /// </value>
        public EnvDTE.DTE GetDTE { get { return dte; } }

        /// <summary>
        /// Gets the projects.
        /// </summary>
        /// <value>
        /// The projects.
        /// </value>
        public IEnumerable<NodeProject> Projects
        {
            get
            {
                return GetItem<NodeProject>(/*c => c.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}"*/);
            }
        }

        /// <summary>
        /// Gets the projects.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NodeProject> GetProjects()
        {
            return GetItem<NodeProject>(/*c => c.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}"*/);
        }

        /// <summary>
        /// Gets the projects.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public IEnumerable<NodeProject> GetProjects(Func<NodeProject, bool> filter)
        {
            return GetItem<NodeProject>(filter)/*.Where(c => c.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")*/;
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public override IEnumerable<T> GetItem<T>(Func<T, bool> filter)
        {

            var prjs = this.solution.Projects;

            if (prjs != null)
            {
                foreach (EnvDTE.Project project in prjs)
                {

                    NodeSolutionItem fld = null;

                    if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                        fld = new NodeFolderSolution(project);

                    else if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")  // is not the same that last test
                        fld = new NodeFolderSolution(project);

                    else if (project.Kind == "{EA6618E8-6E24-4528-94BE-6889FE16485C}")
                        fld = new NodeVirtualFolder(project);

                    else
                        fld = new NodeProject(project);

                    var f = fld as T;
                    if (f != null)
                        if (filter == null || filter(f))
                            yield return f;

                    foreach (T item in fld.GetItem<T>(filter))
                        yield return item;

                }
            }
        }


        /// <summary>
        /// Builds the properties.
        /// </summary>
        protected override void BuildProperties()
        {
            _properties = new Dictionary<string, NodeItemProperty>();

            foreach (EnvDTE.Property item in solution.Properties)
                _properties.Add(item.Name, new NodeProperty<object>(this.solution, item.Name));

        }

    }

}
