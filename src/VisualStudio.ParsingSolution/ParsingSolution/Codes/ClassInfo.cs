using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class ClassInfo : BaseInfo
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassInfo"/> class.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="item"></param>
        protected ClassInfo(BaseInfo parent, CodeElement2 item)
            : base(parent, item)
        {


        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassInfo"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="item">The item.</param>
        public ClassInfo(NodeItem parent, CodeClass2 item)
            : base(null, item as CodeElement2)
        {

            this.Parent = parent;
            this.item = item;
            this.IsClass = true;

            this.Access = ObjectFactory.Convert(this.item.Access);

            this.IsAbstract = this.item.IsAbstract;

            this.IsShared = this.item.IsShared;
            this.IsGeneric = this.item.IsGeneric;
            this.Namespace = item.Namespace.FullName;
            this.DocComment = this.item.DocComment;

            IsPublic = this.IsPublic_Impl(this.item.Access);
            IsPrivate = this.IsPrivate_Impl(this.item.Access);
            IsProtected = this.IsProtected_Impl(this.item.Access);
            IsFamilyOrProtected = this.IsFamilyOrProtected_Impl(this.item.Access);

            this.IsStatic = false;
            this.IsStruct = false;

            //this.item.Children
            //this.item.DerivedTypes
            //this.item.InfoLocation
            //this.item.InheritanceKind
            //this.item.Kind = vsCMElement.
            //this.item.Parent
            //this.item.PartialClasses
            //this.item.Parts

        }


        /// <summary>
        /// List the generics arguments if the class is generic 
        /// </summary>
        public override GenericArguments GenericArguments
        {
            get
            {
                if (this._genericArguments == null)
                {
                    if (this.IsGeneric)
                        this._genericArguments = ProjectHelper.ParseGenericArguments(this.item.FullName, this.Parent.LocalPath, this.item.StartPoint.AbsoluteCharOffset);
                    else
                        this._genericArguments = new GenericArguments();
                }
                return _genericArguments;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected readonly CodeClass2 item;
        private GenericArguments _genericArguments;
        private List<CodeFunctionInfo> _methods;
        private List<CodePropertyInfo> _properties;
        private List<CodeEventInfo> _events;
        private List<AttributeInfo> _attributes;
        private List<CodeConstructorInfo> _constructors;
        private List<CodeFieldInfo> _fields;

        /// <summary>
        /// 
        /// </summary>
        public NodeItem Parent { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public CMAccess Access { get; private set; }

        /// <summary>
        /// Is generic
        /// </summary>
        public bool IsGeneric { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsShared { get; private set; }

        /// <summary>
        /// return true if the class is derived from
        /// </summary>
        public bool IsDerivedFrom(string fullName)
        {
            return this.item.IsDerivedFrom[fullName];
        }

        /// <summary>
        /// return the base class
        /// </summary>
        /// <returns></returns>
        public override ClassInfo GetBase()
        {

            foreach (EnvDTE.CodeElement item in this.item.Bases)
            {
                if (item.Kind == EnvDTE.vsCMElement.vsCMElementClass)
                {
                    NodeItem parent = new NodeItem(this.item.ProjectItem);
                    EnvDTE80.CodeClass2 i = item as EnvDTE80.CodeClass2;
                    ClassInfo _result = ObjectFactory.Instance.CreateClass(parent, i);
                    return _result;
                }
            }

            return null;

        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodeConstructorInfo> GetConstructors()
        {

            if (_constructors == null)
            {

                _constructors = new List<CodeConstructorInfo>();
                CodeClass2 i = item;

                while (i != null)
                {

                    var _members1 = i.Members.OfType<CodeFunction2>()
                        .Where(f => f.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                        .ToList();

                    var _members = _members1.Where(f => AcceptConstructor(f))
                        .Select(c => ObjectFactory.Instance.CreateConstructor(this, c))
                        .Where(d => d != null)
                        .ToList();

                    _constructors.AddRange(_members);

                    if (i.Bases.Count == 0)
                        break;

                    i = i.Bases.Item(1) as CodeClass2;

                    if (i == null || !AcceptAncestor(i.Namespace.FullName, i.Name))
                        break;

                }

                InitializeConstructors(_constructors);

            }

            return _constructors;

        }


        /// <summary>
        /// return the list of derived interface.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<InterfaceInfo> ImplementedInterfaces()
        {

            foreach (EnvDTE.CodeElement item in this.item.Bases)
            {
                if (item.Kind == EnvDTE.vsCMElement.vsCMElementInterface)
                {
                    NodeItem parent = new NodeItem(this.item.ProjectItem);
                    EnvDTE80.CodeInterface2 i = item as EnvDTE80.CodeInterface2;
                    InterfaceInfo _result = ObjectFactory.Instance.CreateInterface(parent, i);
                    yield return _result;
                }
            }

        }

        /// <summary>
        /// Gets the methods.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodeFunctionInfo> GetMethods()
        {

            if (_methods == null)
            {

                _methods = new List<CodeFunctionInfo>();
                CodeClass2 i = item;

                while (i != null)
                {

                    var _members = i.Members.OfType<CodeFunction2>()
                        .Where(f => f.FunctionKind != vsCMFunction.vsCMFunctionConstructor && AcceptMethod(f))
                        .Select(c => ObjectFactory.Instance.CreateMethod(this, c))
                        .Where(d => d != null)
                        .ToList();

                    _methods.AddRange(_members);

                    if (i.Bases.Count == 0)
                        break;

                    i = i.Bases.Item(1) as CodeClass2;

                    if (i == null || !AcceptAncestor(i.Namespace.FullName, i.Name))
                        break;

                }

                InitializeMethods(_methods);

            }

            return _methods;

        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodePropertyInfo> GetProperties()
        {

            if (_properties == null)
            {

                _properties = new List<CodePropertyInfo>();
                CodeClass2 i = item;

                while (i != null)
                {

                    var _members = i.Members.OfType<CodeProperty2>()
                        .Where(f => AcceptProperty(f))
                        .Select(c => ObjectFactory.Instance.CreateProperty(this, c))
                        .Where(d => d != null)
                        .ToList();

                    _properties.AddRange(_members);

                    if (i.Bases.Count == 0)
                        break;

                    i = i.Bases.Item(1) as CodeClass2;

                    if (i == null || !AcceptAncestor(i.Namespace.FullName, i.Name))
                        break;

                }

                InitializeProperties(_properties);

            }

            return _properties;

        }

        /// <summary>
        /// Gets the events.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodeEventInfo> GetEvents()
        {

            if (_events == null)
            {

                _events = new List<CodeEventInfo>();

                CodeClass2 i = item;

                while (i != null)
                {

                    var _members = i.Members.OfType<EnvDTE80.CodeEvent>()
                            .Where(f => AcceptEvent(f))
                            .Select(c => ObjectFactory.Instance.CreateEvent(this, c))
                            .Where(d => d != null)
                            .ToList();

                    _events.AddRange(_members);

                    if (i.Bases.Count == 0)
                        break;

                    i = i.Bases.Item(1) as CodeClass2;

                    if (i == null || !AcceptAncestor(i.Namespace.FullName, i.Name))
                        break;

                }

                InitializeEvents(_events);

            }

            return _events;

        }

        public IEnumerable<CodeFieldInfo> GetFields()
        {

            if (_fields == null)
            {

                _fields = new List<CodeFieldInfo>();

                CodeClass2 i = item;

                while (i != null)
                {

                    var _members = i.Members.OfType<EnvDTE80.CodeElement2>()
                            .Where(f => AcceptField(f))
                            .Select(c => ObjectFactory.Instance.CreateField(this, c))
                            .Where(d => d != null)
                            .ToList();

                    _fields.AddRange(_members);

                    if (i.Bases.Count == 0)
                        break;

                    i = i.Bases.Item(1) as CodeClass2;

                    if (i == null || !AcceptAncestor(i.Namespace.FullName, i.Name))
                        break;

                }

                InitializeFields(_fields);

            }

            return _fields;

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
                        _attributes = ObjectFactory.GetAttributes(item.Attributes);
                    }
                    catch (Exception)
                    {
                        _attributes = new List<AttributeInfo>();
                    }

                    InitializeAttributes(_attributes);

                }
                return _attributes;
            }
        }

        /// <summary>
        /// Accepts the ancestor.
        /// </summary>
        /// <param name="_namespace">The _namespace.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        protected virtual bool AcceptAncestor(string _namespace, string name)
        {
            return true;
        }

        /// <summary>
        /// Accepts the event.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        protected virtual bool AcceptField(EnvDTE80.CodeElement2 e)
        {
            return true;
        }

        /// <summary>
        /// Accepts the event.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        protected virtual bool AcceptEvent(EnvDTE80.CodeEvent e)
        {
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        protected virtual bool AcceptMethod(CodeFunction2 method)
        {
            return method.FunctionKind == vsCMFunction.vsCMFunctionFunction;
        }

        protected virtual bool AcceptConstructor(CodeFunction2 method)
        {
            return method.Name == this.Name 
                && method.FunctionKind == vsCMFunction.vsCMFunctionConstructor;
        }

        protected virtual bool AcceptDestructor(CodeFunction2 method)
        {
            return method.FunctionKind == vsCMFunction.vsCMFunctionDestructor;
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual bool AcceptProperty(CodeProperty2 property)
        {
            return true;
        }

    }

}
