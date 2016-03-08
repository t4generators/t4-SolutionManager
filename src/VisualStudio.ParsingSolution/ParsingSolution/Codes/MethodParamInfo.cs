using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    [System.Diagnostics.DebuggerDisplay("parameter {Name}")]
    /// <summary>
    /// 
    /// </summary>
    public class MethodParamInfo
    {

        private IEnumerable<AttributeInfo> _attributes;
        private CodeParameter2 item;
        private CodeFunctionInfo parent;

        /// <summary>
        /// constructor
        /// </summary>
        public MethodParamInfo(CodeFunctionInfo parent, CodeParameter2 item, string comment)
        {
            this.parent = parent;
            this.item = item;
            this.Comment = comment;
            Name = item.Name;
            Type = TypeInfo.Create(item.Type);
            DefaultValue = item.DefaultValue;
        }

        /// <summary>
        /// name of the parameter
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// type of the parameter
        /// </summary>
        public TypeInfo Type { get; private set; }

        /// <summary>
        /// default value if not specified in the arguments
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// comment of the parameter
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Attributes of the parameter
        /// </summary>
        public IEnumerable<AttributeInfo> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    try
                    {
                        _attributes = ObjectFactory.GetAttributes(item.Attributes);
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

        protected virtual void InitializeAttributes(List<AttributeInfo> attributes)
        {

        }

        protected IEnumerable<AttributeInfo> GetAttributes(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).ToList();
        }

        protected AttributeInfo GetAttribute(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
        }

        protected void ForAttributes(string attributeType, Action<AttributeInfo> act)
        {
            foreach (AttributeInfo attr in ObjectFactory.GetAttributes(Attributes, attributeType))
                act(attr);
        }

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
