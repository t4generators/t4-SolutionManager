using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitFiles.Classes
{
    
    public interface IContract1
    {

        string Property1 { get; set; }

        void Method1(string text);

        string Method2();

    }
}
