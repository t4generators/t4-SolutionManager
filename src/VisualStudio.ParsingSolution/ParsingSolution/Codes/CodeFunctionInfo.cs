using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Method {FullName}")]
    public class CodeFunctionInfo : CodeMemberInfo
    {

        protected CodeFunction2 _item;
        private string _signature;
        private Dictionary<string, string> parameters = new Dictionary<string, string>();
        private TypeInfo _returnType;
        private IEnumerable<MethodParamInfo> _parameters;
        private IEnumerable<AttributeInfo> _attributes;

        /// <summary>
        /// 
        /// </summary>
        public CodeFunctionInfo(BaseInfo parent, CodeFunction2 method)
            : base(parent, method as CodeElement2)
        {

            // Can be null when an custom ActionResult has no ctor
            if (method == null)
                return;

            _item = method;
            Summary = string.Empty;
            _signature = method.Name;
            this.IsGeneric = method.IsGeneric;

            try
            {
                BuildComment(_item.DocComment);
            }
            catch (Exception)
            {

            }

            this.Access = ObjectFactory.Convert(this._item.Access);

            Parameters.ToList();

        }

        private void BuildComment(string docs)
        {
            try
            {

                if (!string.IsNullOrEmpty(docs))
                {

                    System.Xml.Linq.XElement _element;

                    var comment = System.Xml.Linq.XElement.Parse(_item.DocComment);

                    if ((_element = comment.Element("summary")) != null)
                        Summary = _element.Value;

                    if ((_element = comment.Element("param")) != null)
                    {
                        var p = _element.Attribute("name");
                        if (p != null)
                            parameters.Add(p.Value, _element.Value);
                    }
                }
            }
            catch
            {
            }
        }

        public bool IsGeneric { get; private set; }


        private GenericArguments _genericArguments;

        /// <summary>
        /// List the generics arguments if the method is generic 
        /// </summary>
        public GenericArguments GenericArguments
        {
            get
            {
                if (this._genericArguments == null)
                {
                    if (this.IsGeneric)
                    {

                        string localPath = string.Empty;
                        ClassInfo cls = this.Parent as ClassInfo;
                        if (cls != null)
                            localPath = cls.Parent.LocalPath;
                        else
                        {
                            InterfaceInfo cls2 = this.Parent as InterfaceInfo;
                            if (cls2 != null)
                                localPath = cls2.Parent.LocalPath;
                        }

                        this._genericArguments = ProjectHelper.ParseGenericArguments(this._item.FullName, localPath, this._item.StartPoint.AbsoluteCharOffset);

                    }
                    else
                        this._genericArguments = new GenericArguments();
                }
                return _genericArguments;
            }
        }

        /// <summary>
        /// result type of the method
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
        /// Attributes of the method
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

        /// <summary>
        /// Parameters of the method
        /// </summary>
        public IEnumerable<MethodParamInfo> Parameters
        {
            get
            {

                if (_parameters == null)
                {
                    var _Parameters = new List<MethodParamInfo>();
                    // Process all the parameters
                    foreach (CodeParameter2 p in _item.Parameters.OfType<CodeParameter2>())
                    {

                        string parameterComment = string.Empty;
                        parameters.TryGetValue(p.Name, out parameterComment);

                        _Parameters.Add(ObjectFactory.Instance.CreatParameter(this, p, parameterComment));
                        _signature += "," + p.Type.AsString;
                    }

                    _parameters = _Parameters;

                }

                return _parameters;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool Equals(object obj)
        {

            if (_parameters == null)
            {
                var t = this.Parameters;
            }
            return obj != null && _signature == ((CodeFunctionInfo)obj)._signature;
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {

            return _signature;

        }

        /// <summary>
        /// 
        /// </summary>
        public override int GetHashCode()
        {

            if (_parameters == null)
            {
                var t = this.Parameters;
            }

            return _signature.GetHashCode();

        }

    }



}
