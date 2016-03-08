using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 
/// </summary>
namespace VisualStudio.ParsingSolution.Projects.Codes
{
    /// <summary>
    /// Property of class
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("property {FullName}")]
    public class CodePropertyInfo : CodeMemberInfo
    {

        private CodeProperty2 _item;
        private TypeInfo _returnType;
        private IEnumerable<AttributeInfo> _attributes;

        /// <summary>
        /// constructor
        /// </summary>
        public CodePropertyInfo(BaseInfo parent, CodeProperty2 item)
            : base(parent, item as CodeElement2)
        {
            this._item = item;
            this.Access = ObjectFactory.Convert(this._item.Access);
        }

        /// <summary>
        /// 
        /// </summary>
        public override TypeInfo Type
        {
            get
            {
                if (_returnType == null)
                    _returnType = TypeInfo.Create(_item.Type);
                return _returnType;
            }
        }
        /// <summary>
        /// 
        /// </summary>

        public IEnumerable<AttributeInfo> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    try
                    {
                        _attributes = ObjectFactory.GetAttributes(_item.Attributes);
                    }
                    catch (Exception)
                    {
                        _attributes = new List<AttributeInfo>();
                    }

                    InitializeAttributes(_attributes as List<AttributeInfo>);

                }
                return _attributes;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected IEnumerable<AttributeInfo> GetAttributes(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        protected AttributeInfo GetAttribute(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        protected void ForAttributes(string attributeType, Action<AttributeInfo> act)
        {
            foreach (AttributeInfo attr in ObjectFactory.GetAttributes(Attributes, attributeType))
                act(attr);
        }

        /// <summary>
        /// 
        /// </summary>
        protected string GetArgumentFromAttribute(string attributeType, string argumentName)
        {
            AttributeInfo attr = ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
            if (attr != null)
            {
                AttributeArgumentInfo arg = attr.Arguments.FirstOrDefault(a => a.Name == argumentName);
                if (arg != null)
                    return arg.Value;
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        protected string GetArgumentFromAttribute(string attributeType, int indexArgument)
        {
            AttributeInfo attr = ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
            if (attr != null)
            {
                AttributeArgumentInfo arg = (attr.Arguments as List<AttributeArgumentInfo>)[indexArgument];
                if (arg != null)
                    return arg.Value;
            }
            return string.Empty;
        }

    }

}
