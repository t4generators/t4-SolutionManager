﻿using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Namespace}.{Name}")]
    public class InterfaceInfo : BaseInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public InterfaceInfo(NodeItem parent, CodeInterface2 item)
            : base(null, item as CodeElement2)
        {

            this.Parent = parent;
            this.item = item;
            this.IsInterface = true;
            this.Access = ObjectFactory.Convert(this.item.Access);
            this.IsAbstract = true;
            this.IsShared = false;
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

        protected readonly CodeInterface2 item;

        private List<CodeFunctionInfo> _methods;
        private List<CodePropertyInfo> _properties;
        private List<CodeEventInfo> _events;
        private List<AttributeInfo> _attributes;
        private GenericArguments _genericArguments;

        /// <summary>
        /// List the generics arguments if the interface is generic 
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
        public NodeItem Parent { get; private set; }

        /// <summary>
        /// Gets the access.
        /// </summary>
        /// <value>
        /// The access.
        /// </value>
        public CMAccess Access { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is generic.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is generic; otherwise, <c>false</c>.
        /// </value>
        public bool IsGeneric { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is shared.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is shared; otherwise, <c>false</c>.
        /// </value>
        public bool IsShared { get; private set; }

        /// <summary>
        /// return true if the current interface is derived from the specified type
        /// </summary>
        public bool IsDerivedFrom(string fullName)
        {
            return this.item.IsDerivedFrom[fullName];
        }

        /// <summary>
        /// return the list of derived interface.
        /// </summary>
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
        /// return the list of the methods.
        /// </summary>
        public IEnumerable<CodeFunctionInfo> GetMethods()
        {

            if (_methods == null)
            {

                _methods = new List<CodeFunctionInfo>();
                CodeInterface2 i = item;

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

                    i = i.Bases.Item(1) as CodeInterface2;

                    if (i == null || !AcceptAncestor(i.Namespace.FullName, i.Name))
                        break;

                }

                InitializeMethods(_methods);

            }

            return _methods;

        }


        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<CodePropertyInfo> GetProperties()
        {

            if (_properties == null)
            {

                _properties = new List<CodePropertyInfo>();
                CodeInterface2 i = item;

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

                    i = i.Bases.Item(1) as CodeInterface2;

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

                CodeInterface2 i = item;

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

                    i = i.Bases.Item(1) as CodeInterface2;

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
        /// 
        /// </summary>
        protected virtual bool AcceptAncestor(string _namespace, string name)
        {
            return true;
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
        
        /// <summary>
        /// 
        /// </summary>
        protected virtual bool AcceptProperty(CodeProperty2 property)
        {
            return true;
        }

    }

}
