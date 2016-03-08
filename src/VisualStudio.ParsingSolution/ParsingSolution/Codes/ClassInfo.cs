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

            //this.item.Children
            //this.item.DerivedTypes
            //this.item.InfoLocation
            //this.item.InheritanceKind
            //this.item.Kind = vsCMElement.
            //this.item.Parent
            //this.item.PartialClasses
            //this.item.Parts

        }

        private GenericArguments _genericArguments;

        /// <summary>
        /// List the generics arguments if the class is generic 
        /// </summary>
        public GenericArguments GenericArguments
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
        private List<CodeFunctionInfo> _methods;
        private List<CodePropertyInfo> _properties;
        private List<CodeEventInfo> _events;
        private List<AttributeInfo> _attributes;
        private ClassInfo _base;

        /// <summary>
        /// 
        /// </summary>
        public NodeItem Parent { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public CMAccess Access { get; private set; }

        /// <summary>
        /// Is abstract
        /// </summary>
        public bool IsAbstract { get; private set; }

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
        public ClassInfo GetBase()
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
                        .Where(f => AcceptMethod(f))
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

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
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
        /// Gets the attributes.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <returns></returns>
        protected IEnumerable<AttributeInfo> GetAttributes(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).ToList();
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
        /// Determines whether the specified a is public.
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsPublic(EnvDTE.vsCMAccess a)
        {
            return a == EnvDTE.vsCMAccess.vsCMAccessPublic;
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool IsPrivate(EnvDTE.vsCMAccess a)
        {
            return a == EnvDTE.vsCMAccess.vsCMAccessPrivate;
        }

        /// <summary>
        /// Determines whether the specified a is protected.
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsProtected(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessProtected) == EnvDTE.vsCMAccess.vsCMAccessProtected;
        }

        /// <summary>
        /// Determines whether the specified a is family.
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsFamily(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessProject) == EnvDTE.vsCMAccess.vsCMAccessProject;
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool IsFamilyOrProtected(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected) == EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeEvents(List<CodeEventInfo> events)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual bool AcceptMethod(CodeFunction2 method)
        {
            return method.FunctionKind == vsCMFunction.vsCMFunctionFunction;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeMethods(List<CodeFunctionInfo> methods)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual bool AcceptProperty(CodeProperty2 property)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeProperties(List<CodePropertyInfo> properties)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeAttributes(List<AttributeInfo> attributes)
        {

        }


    }

}
