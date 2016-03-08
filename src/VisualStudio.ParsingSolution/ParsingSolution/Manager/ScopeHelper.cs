using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{

    public static class ScopeHelper
    {

        /// <summary>
        /// 
        /// </summary>
        public static ManagerScope StartManager()
        {
            ManagerScope m = new ManagerScope();
            return m;
        }

    }
}
