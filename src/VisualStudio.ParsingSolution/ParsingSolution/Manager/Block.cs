using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{

    public class Block
    {
        public String Name;
        public int Start, Length;
        public string OutputPath;
        public string Fullname;
        public EnvDTE.ProjectItem ParentProjectItem;
        public EnvDTE.Project ParentProject;
    }

}
