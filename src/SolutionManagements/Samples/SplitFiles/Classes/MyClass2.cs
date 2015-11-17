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
    public class MyClass2<T>
        where T : MyClass1, new()
    {

        public T Property1 { get; set; }

    }
}
