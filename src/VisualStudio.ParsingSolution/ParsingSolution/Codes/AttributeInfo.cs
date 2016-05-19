using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class AttributeInfo
    {

        private CodeAttribute2 _attr;
        private List<AttributeArgumentInfo> _arguments;

        /// <summary>
        /// 
        /// </summary>
        public AttributeInfo(CodeAttribute2 attribute)
        {
            this._attr = attribute;
        }

        /// <summary>
        /// 
        /// </summary>
        public string FullName
        {
            get
            {

                try
                {
                    return _attr.FullName;
                }
                catch (Exception)
                {

                }

                return string.Empty;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<AttributeArgumentInfo> Arguments
        {
            get
            {
                if (_arguments == null)
                {
                    _arguments = new List<AttributeArgumentInfo>();
                    foreach (EnvDTE80.CodeAttributeArgument arg in _attr.Arguments.OfType<EnvDTE80.CodeAttributeArgument>())
                        _arguments.Add(ObjectFactory.Instance.CreateAttributeArgument(arg));
                }

                return _arguments;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string GetValue(string name)
        {

            AttributeArgumentInfo item = Arguments.Where(c => c.Name == name).FirstOrDefault();

            if (item != null)
                return item.Value;

            return string.Empty;

        }

        //public override bool Equals(object obj)
        //{
        //
        //if (_parameters == null)
        //{
        //var t = this.Parameters;
        //}
        //return obj != null && _attr.Name == ((CodeFunctionInfo)obj)._signature;
        //}
        //
        //public override int GetHashCode()
        //{
        //
        //if (_parameters == null)
        //{
        //var t = this.Parameters;
        //}
        //
        //return _signature.GetHashCode();
        //
        //}

    }


}
