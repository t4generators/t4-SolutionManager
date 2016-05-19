using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VsxFactory.Modeling.VisualStudio
{
    public enum ProjectKind
    {
        NotDefined = 0,
        WindowsApplication = 1,
        WebApplication = 2,
        WebSite = 4, 
        Database = 8
    }
}
