using EnvDTE;
using EnvDTE80;
using System;

namespace VisualStudio.ParsingSolution.Projects.Codes
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("field {FullName}")]
    public class CodeFieldInfo : CodeMemberInfo
    {

        private CodeElement _item;
        private TypeInfo _type;

        /// <summary>
        /// 
        /// </summary>
        public CodeFieldInfo(BaseInfo parent, CodeElement item, TypeInfo type)
            : base(parent, item as CodeElement2)
        {
            this._item = item;
            this.Access = CMAccess.Public; // ObjectFactory.Convert(this._item.Access);
            this._type = type;
        }

        /// <summary>
        /// 
        /// </summary>
        public override TypeInfo Type
        {
            get
            {
                return _type;
            }
        }


        public object Value
        {
            get
            {
                try
                {

                    if (this._item.Kind == vsCMElement.vsCMElementVariable)
                    {
                        EnvDTE80.CodeVariable2 variable = this._item as EnvDTE80.CodeVariable2;
                        if (variable != null && variable.IsConstant && variable.InitExpression != null)
                            return variable.InitExpression;
                    }
                }
                catch (Exception e)
                {

                }

                return string.Empty;

            }
        }

    }


}
