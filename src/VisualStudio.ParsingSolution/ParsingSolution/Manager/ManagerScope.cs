using Microsoft.VisualStudio.TextTemplating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{

    /// <summary>
    /// Manager class records the various blocks so it can split them up
    /// </summary>
    public class ManagerScope : IDisposable
    {

        private static Manager manager;

        /// <summary>
        /// constructor
        /// </summary>
        public ManagerScope()
        {
            manager = Manager.Create(ProjectHelper.serviceProvider as ITextTemplatingEngineHost, ProjectHelper._generationEnvironment);
        }


        public void SetFilterToDelete(Func<FileInfo, bool> filterToDelete)
        {

            manager.filterToDelete = filterToDelete;

        }

        /// <summary>
        /// 
        /// </summary>
        public IDisposable StartHeader()
        {
            return new HeaderScope(manager);
        }

        /// <summary>
        /// 
        /// </summary>
        public IDisposable StartFooter()
        {
            return new FooterScope(manager);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            manager.Process(true);
        }

        /// <summary>
        /// 
        /// </summary>
        public ScriptFileScope NewFile(string filename)
        {
            return new ScriptFileScope(manager, filename);
        }

        /// <summary>
        /// 
        /// </summary>
        public ScriptFileScope NewFile(string filename, NodeItemFolder folder)
        {
            return new ScriptFileScope(manager, filename, folder);
        }

        /// <summary>
        /// 
        /// </summary>
        public ScriptFileScope NewFile(string filename, NodeProject project)
        {
            return new ScriptFileScope(manager, filename, project);
        }

        /// <summary>
        /// 
        /// </summary>
        public NodeProject GetCurrentProject()
        {
            var p = manager.GetCurrentProject();
            NodeProject result = new NodeProject(p);
            return result;
            //var sln = ProjectHelper.GetContext().Solution();
            //NodeProject prj = sln.GetProjects(c => c.Name == p.Name).FirstOrDefault();
            //return prj;
        }

        /// <summary>
        /// 
        /// </summary>
        public NodeProject GetProject(string projectName)
        {
            var sln = ProjectHelper.GetContext().Solution();
            NodeProject prj = sln.GetProjects(c => c.Name == projectName).FirstOrDefault();
            return prj;
        }

    }

}
