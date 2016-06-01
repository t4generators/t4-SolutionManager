using EnvDTE80;
using System.Collections.Generic;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class CodeMemberInfo : BaseInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public CodeMemberInfo(BaseInfo parent, CodeElement2 item)
            : base(parent, item)
        {
            Parent = parent;            
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract TypeInfo Type { get; }

        /// <summary>
        /// 
        /// </summary>
        public CMAccess Access { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public BaseInfo Parent { get; private set; }

    }

}
