using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution
{

    class HeaderScope : IDisposable
    {

        private Manager manager;

        public HeaderScope(Manager manager)
        {
            this.manager = manager;
            manager.StartHeader();
        }

        public void Dispose()
        {
            manager.EndBlock();
        }

    }

}
