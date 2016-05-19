using System;
using System.IO;

namespace VisualStudio.ParsingSolution
{

    public class ScriptFileScope : IDisposable
    {

        private Manager manager;
        private Block block;

        internal ScriptFileScope(object manager, string name)
        {
            string _name = AppliPatternToFilname(name);
            this.manager = manager as Manager;
            block = this.manager.StartNewFile(_name);
        }

        internal ScriptFileScope(object manager, string name, NodeItemFolder folder)
        {
            string _name = AppliPatternToFilname(name);
            this.manager = manager as Manager;
            block = this.manager.StartNewFile(_name, folder);
        }

        internal ScriptFileScope(object manager, string name, NodeProject project)
        {
            string _name = AppliPatternToFilname(name);
            this.manager = manager as Manager;
            block = this.manager.StartNewFile(_name, project);
        }

        private string AppliPatternToFilname(string name)
        {

            string _name;

            string e = Path.GetExtension(name);
            string mask = ".generated" + e;

            if (!name.EndsWith(mask))
            {
                e = mask;
                _name = Path.ChangeExtension(name, e);
            }
            else
                _name = name;

            return _name;
        }

        public void Dispose()
        {
            manager.EndBlock();
        }

        public String Name { get { return this.block.Name; } }

        public string OutputPath { get { return this.block.OutputPath; } }

        public string Fullname { get { return this.block.Fullname; } }

        public EnvDTE.ProjectItem ParentProjectItem { get { return this.block.ParentProjectItem; } }

        public EnvDTE.Project ParentProject { get { return this.block.ParentProject; } }

    }


}