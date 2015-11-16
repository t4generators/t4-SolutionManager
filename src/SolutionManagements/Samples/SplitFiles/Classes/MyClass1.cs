using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitFiles.Classes
{


    /// <summary>
    /// MyComment for MyClass1 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    public class MyClass1
    {

        public void Method1(string text)
        {


        }

        public string Method2()
        {
            return "test";
        }

        public string Property1 { get; set; }

        public event EventHandler MyEvent1;

    }
}
