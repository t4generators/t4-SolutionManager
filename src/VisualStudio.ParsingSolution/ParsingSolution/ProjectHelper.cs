using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VisualStudio.ParsingSolution.Projects.Codes;

namespace VisualStudio.ParsingSolution
{

    public static class ProjectHelper
    {

        internal static Context _context;
        internal static EnvDTE.DTE Dte;
        internal static IServiceProvider serviceProvider;
        internal static NodeSolution _sln;
        internal static StringBuilder _generationEnvironment;

        /// <summary>
        /// 
        /// </summary>
        public static bool HasCsharpFile(NodeItem item)
        {
            if (item.KindItem == KindItem.File)
                return (Path.GetExtension(item.Filename) == ".cs");

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool HasVbFile(NodeItem item)
        {
            if (item.KindItem == KindItem.File)
                return (Path.GetExtension(item.Filename) == ".vb");

            return false;
        }

        #region Path

        /// <summary>
        /// Determines whether the specified source is located in the path.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static bool IsPath(EnvDTE.ProjectItem source, string path)
        {
            string path2 = string.Join(@"\", GetPath(source));
            return String.Equals(path, path2);
        }

        /// <summary>
        /// Gets the path of the specified source
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetPath(EnvDTE.ProjectItem source)
        {

            List<string> l = new List<string>();

            var s = source.Collection.Parent;

            EnvDTE.ProjectItem p1 = s as EnvDTE.ProjectItem;
            if (p1 != null)
                l.AddRange(GetPath(p1));

            else
            {
                EnvDTE.Project p2 = s as EnvDTE.Project;
                if (p2 != null)
                {
                    l.AddRange(GetPath(p2));
                }
                else
                {

                }
            }

            l.Add(source.Name);
            return l;
        }

        /// <summary>
        /// return the files of the project item
        /// </summary>
        public static IEnumerable<string> GetPath(EnvDTE.Project source)
        {
            List<string> l = new List<string>();

            var s = source.ParentProjectItem;

            EnvDTE.ProjectItem p1 = s as EnvDTE.ProjectItem;
            if (p1 != null)
            {
                l.AddRange(GetPath(p1));
                return l;
            }

            else
            {
                EnvDTE.Project p2 = s as EnvDTE.Project;
                if (p2 != null)
                {
                    l.AddRange(GetPath(p2));

                }
                else
                {

                }
            }

            l.Add(source.Name);
            return l;
        }

        #endregion

        #region Getfiles

        /// <summary>
        /// return the files of the project item
        /// </summary>
        public static IEnumerable<EnvDTE.ProjectItem> GetFiles(EnvDTE.ProjectItems source)
        {

            foreach (EnvDTE.ProjectItem item2 in source)
                foreach (var item3 in GetFiles(item2))
                    yield return item3;

        }

        /// <summary>
        /// return the files of the project item
        /// </summary>
        public static IEnumerable<EnvDTE.ProjectItem> GetFiles(EnvDTE.ProjectItem source)
        {

            yield return source;

            if (source.ProjectItems != null)
                foreach (EnvDTE.ProjectItem item2 in GetFiles(source.ProjectItems))
                    yield return item2;
        }

        /// <summary>
        /// return the files of the project item
        /// </summary>
        public static IEnumerable<EnvDTE.ProjectItem> GetFiles(EnvDTE.Project source)
        {

            foreach (EnvDTE.ProjectItem item in source.ProjectItems)
            {
                yield return item;

                if (item.ProjectItems != null)

                    foreach (EnvDTE.ProjectItem item2 in GetFiles(item.ProjectItems))
                        yield return item2;

            }


        }

        #endregion

        public static string GetNamespace(EnvDTE.ProjectItem source)
        {


            StringBuilder s1 = new StringBuilder();

            var s = source.Collection.Parent;

            EnvDTE.ProjectItem p1 = s as EnvDTE.ProjectItem;
            if (p1 != null)
                s1.Append(GetNamespace(p1));

            else
            {
                EnvDTE.Project p2 = s as EnvDTE.Project;
                if (p2 != null)
                {
                    s1.Append(p2.Properties.Item("DefaultNamespace").Value.ToString());
                }

            }

            if (s1.Length > 0)
                s1.Append(".");
            s1.Append(source.Name);
            return s1.ToString();

        }

        public static NodeItem CreateNodeItem(EnvDTE.ProjectItem s)
        {

            NodeItem fld = null;

            var t = s.FileNames[1] as string;

            if (t != null)
            {
                System.IO.FileInfo f = new System.IO.FileInfo(t);

                if (f.Exists && f.Extension.Length > 0)
                    fld = new NodeItem(s);
                else
                    fld = new NodeItemFolder(s);

            }

            return fld;

        }

        public class Context
        {

            public Context()
            {
                Classes = new List<KeyValuePair<string, BaseInfo>>();
                Projects = new List<KeyValuePair<string, NodeProject>>();

                ComputeClasses();
                ComputeProject();

            }

            public List<KeyValuePair<string, BaseInfo>> Classes { get; set; }
            public List<KeyValuePair<string, NodeProject>> Projects { get; set; }

            public ILookup<string, KeyValuePair<string, BaseInfo>> Indexeclasses { get; set; }

            /// <summary>
            /// return the class of the specified object 
            /// </summary>
            public IEnumerable<BaseInfo> ResolveType(TypeInfo type)
            {

                var t = type.Name;

                if (this.Indexeclasses.Contains(t))
                {

                    IEnumerable<KeyValuePair<string, BaseInfo>> results = this.Indexeclasses[t];

                    foreach (KeyValuePair<string, BaseInfo> k in results)
                        yield return k.Value;

                }

                yield break;

            }

            /// <summary>
            /// return the solution instance 
            /// </summary>
            public NodeSolution Solution()
            {
                return _sln;
            }

            void ComputeClasses()
            {

                NodeSolution sln = Solution();

                foreach (NodeItem file in sln.GetItem<NodeItem>())
                    foreach (BaseInfo cls in file.GetClassItems())
                        if (cls.IsCodeType)
                        {
                            string key = cls.FullName;
                            if (!string.IsNullOrEmpty(key))
                            {
                                var keyValuePair = new KeyValuePair<string, BaseInfo>(cls.FullName, cls);
                                Classes.Add(keyValuePair);
                            }
                        }

                Indexeclasses = Classes.ToLookup(cls => cls.Key);

            }

            void ComputeProject()
            {

                NodeSolution sln = Solution();

                foreach (NodeProject file in sln.GetItem<NodeProject>())
                {
                    if (!string.IsNullOrEmpty(file.Source.FullName))
                    {
                        var f = new FileInfo(file.Source.FullName).Directory.FullName;
                        var keyValuePair = new KeyValuePair<string, NodeProject>(f, file);
                        Projects.Add(keyValuePair);
                    }
                }

            }

            /// <summary>
            /// return the project for the full path project name specified
            /// </summary>
            public NodeProject GetProjectByName(string fullpathItem)
            {

                foreach (KeyValuePair<string, NodeProject> item in Projects)
                    if (fullpathItem == item.Key)
                        return item.Value;

                return null;
            }

            /// <summary>
            /// return all script objects from parsable code
            /// </summary>
            public IEnumerable<BaseInfo> GetObjects()
            {

                foreach (KeyValuePair<string, BaseInfo> k in Classes)
                    yield return k.Value;

            }

            /// <summary>
            /// return all object from parsable code that inherit from the specidied object
            /// <summary>
            public IEnumerable<BaseInfo> GetObjectsInheritFrom(string fullname)
            {
                foreach (KeyValuePair<string, BaseInfo> k in Classes)
                {
                    if (k.Value.IsClass)
                    {
                        ClassInfo itemC = k.Value as ClassInfo;
                        if (itemC.IsDerivedFrom(fullname))
                            yield return k.Value;
                    }
                    else if (k.Value.IsInterface)
                    {
                        InterfaceInfo itemI = k.Value as InterfaceInfo;
                        if (itemI.IsDerivedFrom(fullname))
                            yield return k.Value;
                    }
                }
            }

        }

        public static Context GetContext()
        {

            return _context;
            
        }

        /// <summary>
        /// initialize the solution managemeent.
        /// Note : the script attribute "hostspecific" must be "true"
        /// </summary>
        public static void InitializeSolution(IServiceProvider host, StringBuilder generationEnvironment)
        {

            _generationEnvironment = generationEnvironment;
            serviceProvider = host;

            if (serviceProvider != null)
                Dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(EnvDTE.DTE));

            if (Dte == null)
                throw new Exception("T4 can only execute through the Visual Studio host");

            _sln = new NodeSolution(Dte);

            _context = new Context();

        }

        public static GenericArguments ParseGenericArguments(string type, string path, int absoluteCharOffset)
        {

            GenericArguments generics = new GenericArguments();

            var p = type.IndexOf('<');
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            if (p > -1)
            {
                for (int i = p + 1; i < type.Length; i++)
                {

                    var c = type[i];

                    switch (c)
                    {

                        case '>':
                        case ',':
                            generics.Add(s.ToString().Trim());
                            s.Clear();
                            break;

                        default:
                            s.Append(c);
                            break;
                    }

                }
            }


            string txt = LoadText(path, absoluteCharOffset);
            System.Text.RegularExpressions.MatchCollection matchs = r.Matches(txt);

            foreach (System.Text.RegularExpressions.Match item in matchs)
            {
                string t = item.Value.Substring("where".Length).Trim();
                GenericArgument gen = generics.Resolve(t);
                if (gen != null)
                {
                    t = txt.Substring(item.Index + item.Length + 1).Trim();
                    var m2 = r.Match(t);
                    if (m2.Success)
                        t = t.Substring(0, m2.Index);
                    var constraints = t.Split(',');
                    foreach (string constraint in constraints)
                        gen.AddConstraint(constraint.Trim());
                }
            }

            return generics;

        }

        private static string LoadText(string path, int absoluteCharOffset)
        {

            var file = new FileInfo(path);
            byte[] arr = new byte[file.Length];

            using (FileStream stream = file.OpenRead())
                stream.Read(arr, 0, arr.Length);

            System.Text.StringBuilder s = new StringBuilder(System.Text.Encoding.UTF8.GetString(arr).Replace("\n", ""));
            StringBuilder s2 = new StringBuilder(10000);

            char txt = (char)0;
            for (int i = absoluteCharOffset; i < s.Length; i++)
            {

                var c = s[i];

                if (txt == (char)0)
                {
                    if (c == '"' || c == '\'')
                    {
                        txt = c;
                        continue;
                    }

                    if (c == '{' || c == ';')
                        break;

                    s2.Append(c);

                }
                else if (c == txt)
                {
                    txt = (char)0;
                    continue;
                }

            }

            return s2.ToString();

        }

        private const string pattern = @"where\s[^:]*";
        private static System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(pattern);

    }

}
