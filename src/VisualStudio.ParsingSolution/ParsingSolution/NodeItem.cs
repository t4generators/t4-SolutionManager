using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VisualStudio.ParsingSolution.Projects.Codes;

namespace VisualStudio.ParsingSolution
{



    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeItem : NodeSolutionItem
    {

        protected readonly EnvDTE.ProjectItem s;
        private string path;
        private List<BaseInfo> classes;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeItem"/> class.
        /// </summary>
        /// <param name="s">The s.</param>
        public NodeItem(EnvDTE.ProjectItem s)
            : base()
        {

            this.s = s;
            this.path = s.FileNames[1] as string;
            this.path = this.path.TrimEnd('\\');
            //Debug.WriteLine("----------------------------");
            //foreach (Property item in s.Properties)
            //{
            //    Debug.WriteLine(item.Name);
            //}
            //Debug.WriteLine("----------------------------");


            //   Extension
            //   CustomToolOutput
            //   DateModified
            //   IsLink
            //   BuildAction
            //   SubType
            //   CopyToOutputDirectory
            //   IsSharedDesignTimeBuildInput
            //   ItemType
            //   IsCustomToolOutput
            //   HTMLTitle
            //   CustomTool
            //   URL
            //   Filesize
            //   CustomToolNamespace
            //   Author
            //   FullPath
            //   IsDependentFile
            //   IsDesignTimeBuildInput
            //   DateCreated
            //   LocalPath
            //   ModifiedBy

        }

        /// <summary>
        /// Gets the project item.
        /// </summary>
        /// <value>
        /// The project item.
        /// </value>
        public EnvDTE.ProjectItem ProjectItem
        {
            get
            {
                return this.s;
            }
        }

        /// <summary>
        /// Gets the get last access time of the file.
        /// </summary>
        /// <value>
        /// The get last access time.
        /// </value>
        public override DateTime GetLastAccessTime { get { return File.GetLastAccessTime(this.Filename); } }

        /// <summary>
        /// Gets the get creation time of the file.
        /// </summary>
        /// <value>
        /// The get creation time.
        /// </value>
        public override DateTime GetCreationTime { get { return File.GetCreationTime(this.Filename); } }

        /// <summary>
        /// Gets the get last write time of the file.
        /// </summary>
        /// <value>
        /// The get last write time.
        /// </value>
        public override DateTime GetLastWriteTime { get { return File.GetLastWriteTime(this.Filename); } }

        /// <summary>
        /// Gets the kind item.
        /// </summary>
        /// <value>
        /// The kind item.
        /// </value>
        public override KindItem KindItem { get { return KindItem.File; } }

        public string WebReferenceInterface
        {
            get { return FindProperty<string>("WebReferenceInterface").Value; }
            set { FindProperty<string>("WebReferenceInterface").Value = value; }
        }


        public string WebReference
        {
            get { return FindProperty<string>("WebReference").Value; }
            set { FindProperty<string>("WebReference").Value = value; }
        }


        public string URL
        {
            get { return FindProperty<string>("URL").Value; }
            set { FindProperty<string>("URL").Value = value; }
        }


        public string UrlBehavior
        {
            get { return FindProperty<string>("UrlBehavior").Value; }
            set { FindProperty<string>("UrlBehavior").Value = value; }
        }

        /// <summary>
        /// filename
        /// </summary>
        public string Filename
        {
            get { return this.path; }
        }

        public string LocalPath
        {
            get { return FindProperty<string>("LocalPath").Value; }
            set { FindProperty<string>("LocalPath").Value = value; }
        }


        public string DefaultNamespace
        {
            get { return FindProperty<string>("DefaultNamespace").Value; }
            set { FindProperty<string>("DefaultNamespace").Value = value; }
        }


        protected override void BuildProperties()
        {

            _properties = new Dictionary<string, NodeItemProperty>();

            string aa = s.Name;
            try
            {
                foreach (EnvDTE.Property item in s.Properties)
                    _properties.Add(item.Name, new NodeProperty<object>(s, item.Name));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// parser for iterate items of the solution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public override IEnumerable<T> GetItem<T>(Func<T, bool> filter)
        {

            if (s.ProjectItems != null)
                foreach (ProjectItem item in s.ProjectItems)
                {

                    NodeItem i = ProjectHelper.CreateNodeItem(item);

                    var f = i as T;
                    if (f != null)
                        if (filter == null || filter(f))
                            yield return f;

                    foreach (NodeSolutionItem i2 in i.GetItem<T>(filter))
                        yield return i2 as T;

                }

        }


        ///<summary>
        /// return the list of object contains in the file.
        ///</summary>
        public IEnumerable<BaseInfo> GetClassItems<T>(Func<T, bool> predicate)
            where T : BaseInfo
        {

            return GetClassItems().OfType<T>().Where(predicate);

        }


        ///<summary>
        /// return the list of object contains in the file.
        ///</summary>
        public IEnumerable<BaseInfo> GetClassItems<T>()
            where T : BaseInfo
        {

            return GetClassItems().OfType<T>();

        }

        ///<summary>
        /// return the list of objects contains in the file.
        ///</summary>
        public IEnumerable<BaseInfo> GetClassItems()
        {

            if (classes == null)
            {

                classes = new List<BaseInfo>();

                try
                {
                    if (s.FileCodeModel != null)
                    {

                        foreach (CodeClass2 code in s.FileCodeModel.CodeElements.OfType<CodeClass2>())
                            if (ObjectFactory.Instance.AcceptClass(code))
                                classes.Add(ObjectFactory.Instance.CreateClass(this, code));

                        foreach (CodeEnum code in s.FileCodeModel.CodeElements.OfType<CodeEnum>())
                            if (ObjectFactory.Instance.AcceptEnum(code))
                                classes.Add(ObjectFactory.Instance.CreateEnum(this, code));

                        foreach (CodeInterface2 code in s.FileCodeModel.CodeElements.OfType<CodeInterface2>())
                            if (ObjectFactory.Instance.AcceptInterface(code))
                                classes.Add(ObjectFactory.Instance.CreateInterface(this, code));

                        foreach (EnvDTE.CodeNamespace ns in s.FileCodeModel.CodeElements.OfType<EnvDTE.CodeNamespace>())
                        {

                            foreach (CodeClass2 code in ns.Members.OfType<CodeClass2>())
                                if (ObjectFactory.Instance.AcceptClass(code))
                                    classes.Add(ObjectFactory.Instance.CreateClass(this, code));

                            foreach (CodeEnum code in ns.Members.OfType<CodeEnum>())
                                if (ObjectFactory.Instance.AcceptEnum(code))
                                    classes.Add(ObjectFactory.Instance.CreateEnum(this, code));

                            foreach (CodeInterface2 code in ns.Members.OfType<CodeInterface2>())
                                if (ObjectFactory.Instance.AcceptInterface(code))
                                    classes.Add(ObjectFactory.Instance.CreateInterface(this, code));

                        }
                    }
                }
                catch
                {

                }

            }

            return classes;

        }

        /// <summary>
        /// return the namespaces list in the file
        /// </summary>
        public IEnumerable<EnvDTE.CodeNamespace> GetNamespaceItems()
        {

            List<EnvDTE.CodeNamespace> list = new List<EnvDTE.CodeNamespace>();

            foreach (EnvDTE.CodeNamespace codeNamespace in s.FileCodeModel.CodeElements.OfType<EnvDTE.CodeNamespace>())
                list.Add(codeNamespace);

            return list;

        }

        /// <summary>
        /// Kind item
        /// </summary>
        public override string Kind { get { return s.Kind; } }

        /// <summary>
        /// Name of the file in visual studio
        /// </summary>
        public override string Name
        {
            get { return s.Name; }
            set { s.Name = value; }
        }

        /// <summary>
        /// Name of the filename
        /// </summary>
        public string Name2 { get { return s.FileNames[0]; } }

        /// <summary>
        /// add a new file in the folder to the solution
        /// </summary>
        public NodeItem AddFile(FileInfo file)
        {
            return AddFile(file.FullName);
        }

        /// <summary>
        /// add a new file in the folder to the solution
        /// </summary>
        public NodeItem AddFile(string FullName)
        {
            return new NodeItem(this.s.ProjectItems.AddFromFile(FullName));
        }


        /// <summary>
        /// Add the file in the project
        /// </summary>
        /// <param name="folderPath">The folder path in the project</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="content">The content of the file.</param>
        public NodeItem AddFile(string name, string content)
        {

            var file = new FileInfo(Path.Combine(this.LocalPath, name));
            if (!file.Exists)
            {

                var ar = System.Text.Encoding.UTF8.GetBytes(content);

                using (var stream = file.OpenWrite())
                {
                    stream.Write(ar, 0, ar.Length);
                    stream.Flush();
                }
            }

            return this.AddFile(file.FullName);

        }

        /// <summary>
        /// Add the file in the project
        /// </summary>
        /// <param name="folderPath">The folder path in the project</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="content">The content of the file.</param>
        public NodeItem AddFile(string name, byte[] content)
        {

            var file = new FileInfo(Path.Combine(this.LocalPath, name));
            if (!file.Exists)
            {
                using (var stream = file.OpenWrite())
                {
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }

            return this.AddFile(file.FullName);

        }

    }


}
