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

            switch (this._item.ReadWrite)
            {
                case vsCMPropertyKind.vsCMPropertyKindReadWrite:
                    this.CanRead = true;
                    this.CanWrite = true;
                    break;
                case vsCMPropertyKind.vsCMPropertyKindReadOnly:
                    this.CanRead = true;
                    this.CanWrite = false;
                    break;
                case vsCMPropertyKind.vsCMPropertyKindWriteOnly:
                    this.CanRead = false;
                    this.CanWrite = true;
                    break;
            }

            List<ParamInfo> _parameters = new List<ParamInfo>();

            int index = 0;
            foreach (var p in this._item.Parameters.OfType<CodeParameter2>())
            {
                ParamInfo pinfo = ObjectFactory.Instance.CreateParameter(this, p, index++, p.DocComment);
                _parameters.Add(pinfo);
            }

            this.Parameters = _parameters;
            

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
        /// Gets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public override IEnumerable<AttributeInfo> Attributes
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
        /// Gets a value indicating whether this instance can read.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can read; otherwise, <c>false</c>.
        /// </value>
        public bool CanRead { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance can write.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can write; otherwise, <c>false</c>.
        /// </value>
        public bool CanWrite { get; private set; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IEnumerable<ParamInfo> Parameters { get; private set; }


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
