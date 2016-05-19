using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{
    class FooterScope : IDisposable
    {

        private Manager manager;

        public FooterScope(Manager manager)
        {
            this.manager = manager;
            manager.StartFooter();
        }

        public void Dispose()
        {
            manager.EndBlock();
        }

    }

}
