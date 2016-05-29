using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            this.Folder = new FileInfo(this.Path).Directory.FullName;

        }

        public NodeFolderSolution GetSolutionFolder(string path)
        {

            string p = path.Replace("/", @"\");
            p = p.Trim();
            p = p.Trim('\\');

            string[] ar = p.Split('\\');

            return GetSolutionFolder(ar);

        }

        public NodeFolderSolution GetSolutionFolder(string[] paths)
        {

            NodeFolderSolution node = null;

            var sln2 = (this.dte.Solution as EnvDTE80.Solution2);

            for (int i = 0; i < paths.Length; i++)
            {

                string n = paths[i];

                if (string.IsNullOrEmpty(n))
                    return null;

                if (node == null)
                    node = this.GetItem<NodeFolderSolution>(c => c.Name.ToLower() == n.ToLower()).FirstOrDefault();
                else
                    node = node.GetSolutionFolder(n);

                if (node == null)
                {
                    var x = sln2.AddSolutionFolder(n);
                    node = new NodeFolderSolution(x);
                }

            }

            return node;

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
            this.Folder = new FileInfo(this.Path).Directory.FullName;
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
        /// Gets the folder storing the solution.
        /// </summary>
        /// <value>
        /// The folder.
        /// </value>
        public string Folder { get; private set; }

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
                foreach (EnvDTE.Project project in prjs)    // 
                {

                    NodeSolutionItem fld = null;

                    if (project.Kind == "{67294A52-A4F0-11D2-AA88-00C04F688DDE}")
                        fld = new NodeFolderSolution(project);

                    if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                        fld = new NodeFolderSolution(project);

                    else if (project.Kind == "{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}")  // is not the same that last test
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


/*

Project Type Description	                Project Type Guid
Windows (C#)	                            {FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}
Windows (VB.NET)	                        {F184B08F-C81C-45F6-A57F-5ABD9991F28F}
Windows (Visual C++)	                    {8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}
Web Application	                            {349C5851-65DF-11DA-9384-00065B846F21}
Web Site	                                {E24C65DC-7377-472B-9ABA-BC803B73C61A}
Distributed System	                        {F135691A-BF7E-435D-8960-F99683D2D49C}
Windows Communication Foundation (WCF)	    {3D9AD99F-2412-4246-B90B-4EAA41C64699}
Windows Presentation Foundation (WPF)	    {60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}
Visual Database Tools	                    {C252FEB5-A946-4202-B1D4-9916A0590387}
Database	                                {A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}
Database (other project types)	            {4F174C21-8C12-11D0-8340-0000F80270F8}
Test	                                    {3AC096D0-A1C2-E12C-1390-A8335801FDAB}
Legacy (2003) Smart Device (C#)	            {20D4826A-C6FA-45DB-90F4-C717570B9F32}
Legacy (2003) Smart Device (VB.NET)	        {CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}
Smart Device (C#)	                        {4D628B5B-2FBC-4AA6-8C16-197242AEB884}
Smart Device (VB.NET)	                    {68B1623D-7FB9-47D8-8664-7ECEA3297D4F}
Solution Folder	                            {66A26720-8FB5-11D2-AA7E-00C04F688DDE}
Workflow (C#)	                            {14822709-B5A1-4724-98CA-57A101D1B079}
Workflow (VB.NET)	                        {D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}
Deployment Merge Module	                    {06A35CCD-C46D-44D5-987B-CF40FF872267}
Deployment Cab	                            {3EA9E505-35AC-4774-B492-AD1749C4943A}
Deployment Setup	                        {978C614F-708E-4E1A-B201-565925725DBA}
Deployment Smart Device Cab	                {AB322303-2255-48EF-A496-5904EB18DA55}
Visual Studio Tools for Applications (VSTA)	{A860303F-1F3F-4691-B57E-529FC101A107}
Visual Studio Tools for Office (VSTO)	    {BAA0C2D2-18E2-41B9-852F-F413020CAA33}
Visual J#	                                {E6FDF86B-F3D1-11D4-8576-0002A516ECE8}
SharePoint Workflow	                        {F8810EC1-6754-47FC-A15F-DFABD2E3FA90}
XNA (Windows)	                            {6D335F3A-9D43-41b4-9D22-F6F17C4BE596}
XNA (XBox)	                                {2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}
XNA (Zune)	                                {D399B71A-8929-442a-A9AC-8BEC78BB2433}
SharePoint (VB.NET)	                        {EC05E597-79D4-47f3-ADA0-324C4F7C7484}
SharePoint (C#)	                            {593B0543-81F6-4436-BA1E-4747859CAAE2}
Silverlight	                                {A1591282-1198-4647-A2B1-27E5FF5F6F3B} 
*/
