using EnvDTE;
using System.Collections.Generic;
using EnvDTE80;
using System;
using Microsoft.VisualStudio.Modeling;

// Code de Jean-Marc Prieur 

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    /// <summary>
    /// Signature for methods interested in the fact a type was added to the known types
    /// (not a nested type. For nested types <see cref="NestedTypeAdded"/>)
    /// </summary>
    /// <param name="newType">Newly added type</param>
    public delegate void TypeAdded(CodeType newType);

    /// <summary>
    ///  Signature for methods interested in the fact a type was renamed
    /// </summary>
    /// <param name="oldFullName">Full type name the type had before renaming</param>
    /// <param name="type">Type that was renamed (hence its name or fullname)</param>
    public delegate void TypeRenamed(string oldFullName, CodeType type);

    /// <summary>
    /// Signature for methods interested in the fact a nested type was added to a class (for
    /// types added directly to a namespace <see cref="TypeAdded"/>)
    /// </summary>
    /// <param name="parent">Class to which a nested type was added</param>
    /// <param name="newNestedType">Type which was newly added to the parent class.</param>
    public delegate void NestedTypeAdded(CodeClass parent, CodeType newNestedType);

    /// <summary>
    /// Signature for methods interested in the fact a field was added to a type (class, enum)
    /// </summary>
    /// <param name="parent">Type to which a field was added</param>
    /// <param name="newField">Newly added field</param>
    public delegate void FieldAdded(CodeType parent, CodeVariable newField);

    /// <summary>
    /// Signature for methods interested in the fact a property was added to a type (class, interface)
    /// </summary>
    /// <param name="parent">Type to which a property was added</param>
    /// <param name="newProperty">Newly added property</param>
    public delegate void PropertyAdded(CodeType parent, CodeProperty newProperty);

    /// <summary>
    /// Signature for methods interested in the fact a method was added to a type (class, interface)
    /// </summary>
    /// <param name="parent">Type to which a method was added</param>
    /// <param name="newMethod">Newly added method</param>
    public delegate void MethodAdded(CodeType parent, CodeFunction2 newMethod);

    /// <summary>
    /// Signature for methods interested in the fact a type was removed.
    /// </summary>
    /// <param name="fullName">Fully qualified name of the type that was removed</param>
    public delegate void TypeRemoved(string fullName);

    /// <summary>
    /// Signature for methods interested in the fact a memvber was removed.
    /// </summary>
    /// <param name="type">Type to which the member was removed</param>
    /// <param name="memberNameOrSignature">Name of the removed member, or unique signature
    /// in the case of methods.</param>
    public delegate void MemberRemoved(CodeType type, string memberNameOrSignature);

    /// <summary>
    /// Signature for methods interested in the fact a parameter was added to a method
    /// </summary>
    /// <param name="method">Method to which the parameter was added</param>
    /// <param name="parameter">Parameter which was added</param>
    public delegate void ParameterAdded(CodeFunction2 method, CodeParameter parameter);

    /// <summary>
    /// Signature for methods interested in the fact a parameter was removed
    /// </summary>
    /// <param name="method">Method to which the parameter was removed</param>
    /// <param name="parameterName">Name of the parameter which was removed</param>
    public delegate void ParameterRemoved(CodeFunction2 method, string parameterName);

    /// <summary>
    /// Signature for methods interested in the fact a parameter was renamed
    /// </summary>
    /// <param name="method">Method to which the parameter was renamed</param>
    /// <param name="parameterIndex">Index (0 to method.Pameters.Count-1) of the parameter that was renamed</param>
    /// <param name="parameter">Renamed parameter (hence its new name)</param>
    public delegate void ParameterRenamed(CodeFunction2 method, int parameterIndex, CodeParameter parameter);

    /// <summary>
    /// Signature for methods interested in the fact a field was renamed
    /// </summary>
    /// <param name="type">Type owning that field (class, struct, enum)</param>
    /// <param name="field">Renamed field</param>
    public delegate void FieldRenamed(CodeType type, CodeVariable field);

    /// <summary>
    /// Signature for methods interested in the fact a method was renamed
    /// </summary>
    /// <param name="type">Type owning that field (class, interface, struct)</param>
    /// <param name="method">Renamed method</param>
    public delegate void MethodRenamed(CodeType type, CodeFunction2 method);

    /// <summary>
    /// Signature for methods interested in the fact some property of a CodeElement changed
    /// </summary>
    /// <param name="element">Code Element which properties changed</param>
    public delegate void ElementPropertiesChanged(CodeElement element);

    /// <summary>
    ///  Signature for methods interested in the fact the base type of a type changed
    /// </summary>
    /// <param name="type">Type which base type changed (base class for CodeClass2, and Base
    /// interface for CodeInterface)</param>
    public delegate void BaseTypeChanged(CodeType type);

    /// <summary>
    /// Signature for methods interested in the fact an attribute was added to a code element
    /// </summary>
    /// <param name="attribute">Attribute that was added (its Parent is the Code Element to
    /// which it was added)</param>
    public delegate void AttributeAdded(CodeAttribute2 attribute);

    /// <summary>
    /// Signature for methods interested in the fact an attribute was removed
    /// </summary>
    /// <param name="parent">Code element owning the attribute</param>
    /// <param name="attributeName">Name of the attribute that was removed</param>
    public delegate void AttributeRemoved(CodeElement parent, string attributeName);

    /// <summary>
    /// Signature for methods interested in the fact an attribute changed
    /// </summary>
    /// <param name="attribute">New value for the attribute</param>
    public delegate void AttributeChanged(CodeAttribute2 attribute);

    /// <summary>
    /// Class managing known types
    /// </summary>
    public class KnownCodeTypes : IDisposable
    {
        private Project _project;
        private bool _initialized;

        /// <summary>
        /// Code model events subscribed to
        /// </summary>
        public Dictionary<string, CodeModelEvents> MonitoredNamespace { get; private set; }

        /// <summary>
        /// Get the KnownCodeTypes associated to a project.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static KnownCodeTypes FromProject(Project project)
        {
            if (project.CodeModel == null)
                return null;

            KnownCodeTypes knownCodeTypes = new KnownCodeTypes(project);
            return knownCodeTypes;
        }

        /// <summary>
        /// Constructor. Has an argument which is the fullname of the base class / interface for all
        /// types to be taken into account
        /// </summary>
        public KnownCodeTypes(Project project)
        {
            this.MonitoredNamespace = new Dictionary<string, CodeModelEvents>();
            _project = project;
            TakeIntoAccount(project);
        }

        /// <summary>
        /// Takes the types of a code model into accounrt
        /// </summary>
        /// <param name="codeModel"></param>
        internal void TakeIntoAccount(Project project)
        {
            var codeModel = project.CodeModel;
            if (codeModel == null)
                return;

            // Subscribe to code model events
            foreach (CodeElement element in codeModel.CodeElements)
            {
                CodeNamespace ns = element as CodeNamespace;
                if (ns != null )
                {
                    // Take types into account
//                    if (TakeTypesIntoAccount(codeModel.CodeElements) && !MonitoredNamespace.ContainsKey(ns.Name))
                    if (TakeIntoAccount(ns, false) && !MonitoredNamespace.ContainsKey(ns.Name))
                    {
                        CodeModelEvents ev = ((codeModel.DTE as DTE2).Events as Events2).get_CodeModelEvents(element);
                        ev.ElementAdded += new _dispCodeModelEvents_ElementAddedEventHandler(events_ElementAdded);
                        ev.ElementChanged += new _dispCodeModelEvents_ElementChangedEventHandler(events_ElementChanged);
                        ev.ElementDeleted += new _dispCodeModelEvents_ElementDeletedEventHandler(events_ElementDeleted);
                        MonitoredNamespace.Add(ns.Name, ev);
                    }
                }
            }
       }

        /// <summary>
        /// Clears the knowledge of the types
        /// </summary>
        public void Clear()
        {
            UnsubscribeCodeModelEvents();

            // And clear type knowledge
            typesByFullName.Clear();
            typesByName.Clear();
        }

        private void UnsubscribeCodeModelEvents()
        {
            // unsubscribe to code model events
            foreach (CodeModelEvents ev in MonitoredNamespace.Values)
            {
                ev.ElementAdded -= new _dispCodeModelEvents_ElementAddedEventHandler(events_ElementAdded);
                ev.ElementChanged -= new _dispCodeModelEvents_ElementChangedEventHandler(events_ElementChanged);
                ev.ElementDeleted -= new _dispCodeModelEvents_ElementDeletedEventHandler(events_ElementDeleted);
            }

            MonitoredNamespace.Clear();
        }

        /// <summary>
        /// Get the types which simple name is provided as an argument
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CodeType[] GetNamedTypes(string name)
        {
            if (typesByName.ContainsKey(name))
                return typesByName[name];
            else
                return new CodeType[0];
        }

        /// <summary>
        /// Get all the know types
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodeType> AllKnownTypes
        {
            get
            {
                return typesByFullName.Values;
            }
        }

        /// <summary>
        /// Get the type which fullname is provided
        /// </summary>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public CodeType GetFullNamedType(string fullname)
        {
            if (typesByFullName.ContainsKey(fullname))
                return typesByFullName[fullname];
            else
                return null;
        }

        /// <summary>
        /// Is the type known
        /// </summary>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public bool IsKnown(string fullname)
        {
            string fullTypeName = fullname;
            while (fullTypeName.EndsWith("[]"))
                fullTypeName = fullTypeName.Substring(0, fullTypeName.Length - 2);
            return (GetFullNamedType(fullTypeName) != null);
        }

        /// <summary>
        /// Get the short type name (no namespace) for a CodeType
        /// </summary>
        /// <param name="codeType">CodeType the short type name of which we want</param>
        /// <returns>The name (including nesting classes if nested) but without namespace</returns>
        public static string GetShortTypeName(CodeType codeType)
        {
            // Base types
            string fullname = SimplifyForCSharp(codeType.FullName);
            if (fullname != codeType.FullName)
                return fullname;

            // Other cases
            else
            {
                string name = codeType.Name;
                if (codeType.Parent is CodeType)
                    name = GetShortTypeName((codeType.Parent) as CodeType) + "." + name;
                return name;
            }
        }


        /// <summary>
        /// Get the short type name (no namespace) for CodeRef (might have arrays)
        /// </summary>
        /// <param name="codeTypeRef">CodeTypeRef the short type name of which we want</param>
        /// <returns>The name (including nesting classes if nested) but without namespace</returns>
        public static string GetShortTypeName(CodeTypeRef codeTypeRef)
        {
            string suffix = string.Empty;
            CodeTypeRef inner = codeTypeRef;
            while (inner.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                suffix = "[" + new string(',', inner.Rank - 1) + "]" + suffix;
                inner = inner.ElementType;
            }
            if (inner.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                return GetShortTypeName(inner.CodeType) + suffix;
            else
                return inner.AsFullName + suffix;
        }



        /// <summary>
        /// Get the full type name (no namespace) for CodeRef (might have arrays)
        /// </summary>
        /// <param name="codeTypeRef">CodeTypeRef the full type name of which we want</param>
        /// <returns>The full name of the type</returns>
        public static string GetFullTypeName(CodeTypeRef codeTypeRef)
        {
            string suffix = string.Empty;
            CodeTypeRef inner = codeTypeRef;
            while (inner.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
            {
                suffix = "[" + new string(',', inner.Rank - 1) + "]" + suffix;
                inner = inner.ElementType;
            }
            if (inner.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                return inner.CodeType.FullName + suffix;
            else
                return inner.AsString + suffix;
        }



        /// <summary>
        /// Get the short type name (no namespace) for CodeRef (might have arrays)
        /// </summary>
        /// <param name="codeTypeRef">CodeTypeRef the short type name of which we want</param>
        /// <returns>The name (including nesting classes if nested) but without namespace</returns>
        public static string GetNamespaceOf(CodeTypeRef codeTypeRef)
        {
            CodeTypeRef inner = codeTypeRef;
            while (inner.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
                inner = inner.ElementType;
            if (inner.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                return inner.CodeType.Namespace.FullName;
            else
                return string.Empty;
        }


        /// <summary>
        /// Simplify fulltype name for C# language
        /// </summary>
        internal static string SimplifyForCSharp(string fullTypeName)
        {
            switch (fullTypeName)
            {
                case "System.Double":
                case "Double":
                    return "double";

                case "System.Single":
                case "Single":
                    return "float";
                case "System.Int32":
                case "Int32":
                    return "int";

                case "System.Int16":
                case "Int16":
                    return "short";

                case "System.Char":
                case "Char":
                    return "char";

                case "System.Byte":
                case "Byte":
                    return "byte";


                case "System.String":
                case "String":
                    return "string";
                default:
                    return fullTypeName;
            }
        }

        /// <summary>
        /// Event fired when a type is added to a namespace
        /// </summary>
        public event TypeAdded OnTypeAdded;

        /// <summary>
        /// Event fired when a type is added as a nested class to a class
        /// </summary>
        public event NestedTypeAdded OnNestedTypeAdded;

        /// <summary>
        /// Event fired when a type has been removed
        /// </summary>
        public event TypeRemoved OnTypeRemoved;

        /// <summary>
        /// Event fired when a field was added to a class or enumeration
        /// </summary>
        public event FieldAdded OnFieldAdded;

        /// <summary>
        /// Event fired when a property was added to a type
        /// </summary>
        public event PropertyAdded OnPropertyAdded;

        /// <summary>
        /// Event fired when a method was added to a type
        /// </summary>
        public event MethodAdded OnMethodAdded;

        /// <summary>
        /// Event fired when a parameter was added to a method
        /// </summary>
        public event ParameterAdded OnParameterAdded;

        /// <summary>
        /// Event fired when a field was removed from a class or enumeration
        /// </summary>
        public event MemberRemoved OnFieldRemoved;

        /// <summary>
        /// Event fired when a property was removed from a type
        /// </summary>
        public event MemberRemoved OnPropertyRemoved;

        /// <summary>
        /// Event fired when a method was removed from a type
        /// </summary>
        public event MemberRemoved OnMethodRemoved;

        /// <summary>
        /// Event fired when a parameter was removed from a method
        /// </summary>
        public event ParameterRemoved OnParameterRemoved;

        /// <summary>
        /// Event fired when a type was renamed (or its namespace was renamed)
        /// </summary>
        public event TypeRenamed OnTypeRenamed;

        /// <summary>
        /// Event fired when a parameter of a method was renamed
        /// </summary>
        public event ParameterRenamed OnParameterRenamed;

        /// <summary>
        /// Event fired when a field was renamed
        /// </summary>
        public event FieldRenamed OnFieldRenamed;

        /// <summary>
        /// Event fired when a method was renamed
        /// </summary>
        public event MethodRenamed OnMethodRenamed;

        /// <summary>
        /// Event fired when the base type (class) for a type changed
        /// </summary>
        public event BaseTypeChanged OnBaseTypeChanged;

        /// <summary>
        /// Event fired when the properties of an element changed (exluding the name, namespace, etc ...)
        /// already processed by the other events.
        /// </summary>
        public event ElementPropertiesChanged OnElementPropertiesChanged;

        /// <summary>
        /// Event fired when an attribute is added (see its Parent for element holding this attribute)
        /// </summary>
        public event AttributeAdded OnAttributeAdded;

        /// <summary>
        /// Event fired when an attribute is removed
        /// </summary>
        public event AttributeRemoved OnAttributeRemoved;

        /// <summary>
        /// Event fired when an attribute is changed
        /// </summary>
        public event AttributeChanged OnAttributeChanged;

        /// <summary>
        /// Take the types into account (recursively)
        /// </summary>
        /// <param name="codeElements"></param>
        private bool TakeTypesIntoAccount(CodeElements codeElements)
        {
            bool flag = false;

            // Find types
            foreach (CodeElement element in codeElements)
            {
                switch (element.Kind)
                {
                    case vsCMElement.vsCMElementNamespace:
                        flag |= TakeIntoAccount(element as CodeNamespace, false);
                        break;
                    case vsCMElement.vsCMElementClass:
                    case vsCMElement.vsCMElementDelegate:
                    case vsCMElement.vsCMElementEnum:
                    case vsCMElement.vsCMElementInterface:
                    case vsCMElement.vsCMElementStruct:
                        flag |= TakeIntoAccount(element as CodeType, false);
                        break;
                    default:
                        break;
                }
            }
            return flag;
        }


        ///// <summary>
        ///// When project items are renamed, tell it
        ///// </summary>
        ///// <param name="ProjectItem"></param>
        ///// <param name="OldName"></param>
        //void SolutionItemsEvents_ItemRenamed(ProjectItem ProjectItem, string OldName)
        //{
        //    // Case where of non source files (for example model files are renamed !)
        //    if (ProjectItem.FileCodeModel == null)
        //        return;

        //    foreach (CodeElement element in ProjectItem.FileCodeModel.CodeElements)
        //        if (element is CodeNamespace)
        //            ProcessNamespace(element as CodeNamespace, delegate(CodeType t) { if (OnElementPropertiesChanged != null) OnElementPropertiesChanged(t as CodeElement); });
        //        else if (element is CodeType)
        //        {
        //            if (OnElementPropertiesChanged != null)
        //                OnElementPropertiesChanged(element as CodeElement);
        //        }
        //}

        ///// <summary>
        ///// When a project item has been added
        ///// </summary>
        ///// <param name="ProjectItem"></param>
        //void projectItemsEvents_ItemAdded(ProjectItem ProjectItem)
        //{
        //    if (ProjectItem.FileCodeModel != null)
        //        TakeTypesIntoAccount(ProjectItem.FileCodeModel.CodeElements);
        //}


        ///// <summary>
        ///// When a project item is removed, Types might be removed
        ///// </summary>
        ///// <param name="ProjectItem"></param>
        //void projectItemsEvents_ItemRemoved(ProjectItem ProjectItem)
        //{
        //    RemoveObsoleteTypes();
        //}

        /// <summary>
        /// Removes from the knowledge the types that are no longer valid
        /// </summary>
        internal void RemoveObsoleteTypes()
        {
            List<string> toRemove = new List<string>();
            bool flag = false;
            foreach (string fullTypeName in typesByFullName.Keys)
            {
                CodeType type = typesByFullName[fullTypeName];
                try
                {
                    string name = type.Name; // will have an exception for renamed type
                }
                catch (Exception)
                {
                    flag = true;
                    toRemove.Add(fullTypeName);
                }
            }

            if (!flag)
                return;

            foreach (string fullTypeName in toRemove)
            {
                CodeType type = typesByFullName[fullTypeName];
                string name = GetTypeName(fullTypeName);
                typesByName.Remove(name, type);
                typesByFullName.Remove(fullTypeName);
                if (OnTypeRemoved != null)
                    OnTypeRemoved(fullTypeName);
                System.Diagnostics.Debug.WriteLine(String.Format("Type {0} removed", fullTypeName));
            }

            if (typesByFullName.Count == 0)
            {
                UnsubscribeCodeModelEvents();
            }
        }

        /// <summary>
        /// Process an action on namespace
        /// </summary>
        /// <param name="codeNamespace"></param>
        /// <param name="action"></param>
        private void ProcessNamespace(CodeNamespace codeNamespace, Action<CodeType> action)
        {
            foreach (CodeElement element in codeNamespace.Members)
                if (element is CodeNamespace)
                    ProcessNamespace(element as CodeNamespace, action);
                else if (element is CodeType)
                    action(element as CodeType);
        }

        /// <summary>
        /// Last action in code model events. Used in protection agains multi call with same parameters
        /// </summary>
        enum LastAction
        {
            None,
            Add,
            Delete,
            Change
        }
        LastAction lastAction = LastAction.None;

        /// <summary>
        /// Last parent in code model events
        /// </summary>
        object lastParent = null;

        /// <summary>
        /// Last element in code model events
        /// </summary>
        object lastElement = null;

        /// <summary>
        /// Processing of code element deleted
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="element"></param>
        void events_ElementDeleted(object Parent, CodeElement element)
        {
            // Protection agains multi call with same parameters
            if ((lastAction == LastAction.Delete) && (lastParent == Parent) && (lastElement == element))
                return;
            else
            {
                lastParent = Parent;
                lastElement = element;
                lastAction = LastAction.Delete;
            }

            if (element.IsCodeType)
            {
                CodeType codeType = element as CodeType;
                CodeElement parent = Parent as CodeElement;

                // Get full name
                string fullname;
                if (parent == null)
                    fullname = codeType.Name;
                else fullname = parent.FullName + "." + codeType.Name;
                if (typesByFullName.ContainsKey(fullname))
                {
                    typesByFullName.Remove(fullname);
                    typesByName.Remove(codeType.Name, codeType);
                    if (OnTypeRemoved != null)
                        OnTypeRemoved(fullname);
                }
            }
            else if (element is CodeVariable)
            {
                CodeVariable field = element as CodeVariable;
                CodeType parent = Parent as CodeType;
                if ((OnFieldRemoved != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnFieldRemoved(parent, element.Name);
            }
            else if (element is CodeProperty)
            {
                CodeProperty property = element as CodeProperty;
                CodeType parent = Parent as CodeType;
                if ((OnPropertyRemoved != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnPropertyRemoved(parent, element.Name);
            }
            else if (element is CodeFunction2)
            {
                CodeFunction2 m = element as CodeFunction2;
                CodeType parent = Parent as CodeType;
                if ((OnMethodRemoved != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnMethodRemoved(parent, m.get_Prototype((int)(vsCMPrototype.vsCMPrototypeUniqueSignature)));
            }
            else if (element is CodeParameter)
            {
                CodeParameter p = element as CodeParameter;
                CodeFunction2 m = Parent as CodeFunction2;
                CodeType parent = m.Parent as CodeType;
                if ((OnParameterRemoved != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnParameterRemoved(m, element.Name);
            }
            else if (element is CodeNamespace)
            {
                string fullname;
                if ((Parent as CodeElement) != null)
                    fullname = (Parent as CodeElement).FullName + "." + element.Name + ".";
                else
                    fullname = element.Name + ".";
                List<string> toRemove = new List<string>();
                foreach (string typeFullName in typesByFullName.Keys)
                    if (typeFullName.StartsWith(fullname))
                        toRemove.Add(typeFullName);

                foreach (string typeFullName in toRemove)
                {
                    CodeType codeType = typesByFullName[typeFullName];
                    string name = GetTypeName(typeFullName);
                    typesByFullName.Remove(typeFullName);
                    typesByName.Remove(name, codeType);
                    if (OnTypeRemoved != null)
                        OnTypeRemoved(typeFullName);
                }
            }
            else if (element is CodeAttribute2)
            {
                if (OnAttributeRemoved != null)
                    OnAttributeRemoved(Parent as CodeElement, element.Name);
            }

            RemoveObsoleteTypes();
        }

        /// <summary>
        /// Get the shortest name from a full type name
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <returns></returns>
        public static string GetTypeName(string typeFullName)
        {
            string[] components = typeFullName.Split('.');
            string name = components[components.Length - 1];
            return name;
        }

        /*
          /// <summary>
          /// Last change kind (protected agains multi-calls)
          /// </summary>
          vsCMChangeKind lastChange = (vsCMChangeKind)(-1);
        */

        /// <summary>
        /// Processes code element changes
        /// </summary>
        /// <param name="element"></param>
        /// <param name="Change"></param>
        void events_ElementChanged(CodeElement element, vsCMChangeKind Change)
        {
            /*
               if ((lastAction == LastAction.Change) && (lastChange == Change) && (lastElement == element))
                return;
               else
               {
                lastChange = Change;
                lastElement = element;
                lastAction = LastAction.Change;
               }
             */
            switch (Change)
            {
                case vsCMChangeKind.vsCMChangeKindArgumentChange:
                    if (element is CodeAttribute2)
                    {
                        CodeAttribute2 attribute = element as CodeAttribute2;
                        if (OnAttributeChanged != null)
                            OnAttributeChanged(attribute);
                    }
                    break;
                case vsCMChangeKind.vsCMChangeKindBaseChange:
                    if (element is CodeType)
                    {
                        CodeType codeType = element as CodeType;

                        // Only fire event when a valid base changed
                        if ((codeType.Bases.Count > 0) && (codeType.Bases.Item(1) is CodeType))
                            if (OnBaseTypeChanged != null)
                                OnBaseTypeChanged(element as CodeType);
                    }
                    break;

                case vsCMChangeKind.vsCMChangeKindRename:
                    // Change type name
                    if (element is CodeType)
                    {
                        CodeType codeType = element as CodeType;

                        foreach (string fullTypeName in typesByFullName.Keys)
                        {
                            CodeType type = typesByFullName[fullTypeName];
                            try
                            {
                                string name = type.Name; // will have an exception for renamed type
                            }
                            catch (Exception)
                            {
                                string name = GetTypeName(fullTypeName);
                                typesByName.Remove(name, codeType);
                                typesByFullName.Remove(fullTypeName);
                                typesByFullName.Add(codeType.FullName, codeType);
                                typesByName.Add(codeType.Name, codeType);
                                if (OnTypeRenamed != null)
                                    OnTypeRenamed(fullTypeName, codeType);
                                break; // Must be the last instruction ! (no loop otherwise modified collection !)
                            }
                        }
                    }
                    else if (element is CodeParameter)
                    {
                        CodeParameter parameter = element as CodeParameter;
                        CodeFunction2 function = parameter.Parent as CodeFunction2;
                        int index = 0;
                        foreach (CodeParameter p in function.Parameters)
                            if (p == parameter)
                            {
                                if (OnParameterRenamed != null)
                                    OnParameterRenamed(function, index, parameter);
                                break;
                            }
                    }
                    else if (element is CodeNamespace)
                    {
                        CodeNamespace ns = element as CodeNamespace;
                        List<string> toReplace = new List<string>();

                        // Find obsolete types
                        foreach (string fullTypeName in typesByFullName.Keys)
                        {
                            CodeType type = typesByFullName[fullTypeName];
                            try
                            {
                                string oldNs = type.Namespace.FullName;
                            }
                            catch (Exception)
                            {
                                toReplace.Add(fullTypeName);
                            }
                        }

                        // And replace them
                        foreach (string fullTypeName in toReplace)
                        {
                            // Removes obsolete types
                            string name = GetTypeName(fullTypeName);
                            CodeType type = typesByFullName[fullTypeName];
                            typesByName.Remove(name, type);
                            typesByFullName.Remove(fullTypeName);

                            // Find new one
                            type = ns.Members.Item(name) as CodeType;

                            // And add-it
                            typesByFullName.Add(type.FullName, type);
                            typesByName.Add(type.Name, type);

                            // Notify renaming
                            if (OnTypeRenamed != null)
                                OnTypeRenamed(fullTypeName, type);
                        }
                    }
                    else if (element is CodeVariable)
                    {
                        CodeVariable field = element as CodeVariable;
                        CodeType type = field.Parent as CodeType;
                        if (OnFieldRenamed != null)
                            OnFieldRenamed(type, field);
                    }
                    else if (element is CodeFunction2)
                    {
                        CodeFunction2 m = element as CodeFunction2;
                        CodeType type = m.Parent as CodeType;
                        if (OnMethodRenamed != null)
                            OnMethodRenamed(type, m);
                    }
                    break;

                case vsCMChangeKind.vsCMChangeKindSignatureChange:
                case vsCMChangeKind.vsCMChangeKindTypeRefChange:
                case vsCMChangeKind.vsCMChangeKindUnknown:
                    if (OnElementPropertiesChanged != null)
                        OnElementPropertiesChanged(element);
                    break;
                default:
                    if (OnElementPropertiesChanged != null)
                        OnElementPropertiesChanged(element);
                    break;
            }

        }


        /// <summary>
        /// Processes code element added
        /// </summary>
        /// <param name="element"></param>
        void events_ElementAdded(CodeElement element)
        {
            if ((lastAction == LastAction.Add) && (lastElement == element))
                return;
            else
            {
                lastAction = LastAction.Add;
                lastElement = element;
            }


            if (element is CodeType)
            {
                CodeType codeType = element as CodeType;
                if (!typesByFullName.ContainsKey(codeType.FullName))
                {
                    typesByFullName.Add(codeType.FullName, codeType);
                    typesByName.Add(codeType.Name, codeType);
                    if (codeType.Parent is CodeNamespace)
                    {
                        if (OnTypeAdded != null)
                            OnTypeAdded(codeType);
                    }
                    else if (codeType.Parent is CodeClass)
                    {
                        if (OnNestedTypeAdded != null)
                            OnNestedTypeAdded(codeType.Parent as CodeClass, codeType);
                    }
                }
            }
            else if (element is CodeVariable)
            {
                CodeVariable field = element as CodeVariable;
                CodeType parent = field.Parent as CodeType;
                if ((OnFieldAdded != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnFieldAdded(parent, field);
            }
            else if (element is CodeProperty)
            {
                CodeProperty property = element as CodeProperty;
                CodeType parent = property.Parent as CodeType;
                if ((OnPropertyAdded != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnPropertyAdded(parent, property);
            }
            else if (element is CodeFunction2)
            {
                CodeFunction2 m = element as CodeFunction2;
                CodeType parent = m.Parent as CodeType;
                if ((OnMethodAdded != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnMethodAdded(parent, m);
            }
            else if (element is CodeParameter)
            {
                CodeParameter p = element as CodeParameter;
                CodeFunction2 m = p.Parent as CodeFunction2;
                CodeType parent = m.Parent as CodeType;
                if ((OnParameterAdded != null) && (typesByFullName.ContainsKey(parent.FullName)))
                    OnParameterAdded(m, p);
                else if (OnElementPropertiesChanged != null)
                    OnElementPropertiesChanged(m as CodeElement);
            }
            else if (element is CodeNamespace)
                TakeIntoAccount(element as CodeNamespace, true);
            else if (element is CodeAttribute2)
            {
                CodeAttribute2 attribute = element as CodeAttribute2;
                if (OnAttributeAdded != null)
                    OnAttributeAdded(attribute);
            }
        }

        /// <summary>
        /// Take a namespace into account
        /// </summary>
        /// <param name="codeNamespace"></param>
        /// <param name="notify"></param>
        private bool TakeIntoAccount(CodeNamespace codeNamespace, bool notify)
        {
            if (codeNamespace.Name == "MS" || codeNamespace.Name == "Microsoft" || codeNamespace.Name == "System")
                return false;

            bool flag = false;
            try
            {
                foreach (CodeElement element in codeNamespace.Members)
                {
                    if (element.IsCodeType)
                        flag |= TakeIntoAccount(element as CodeType, notify);
                    else if (element.Kind == vsCMElement.vsCMElementNamespace)
                        flag |= TakeIntoAccount(element as CodeNamespace, notify);
                }
            }
            catch { }

            return flag;
        }


        /// <summary>
        /// Take a type into account
        /// </summary>
        /// <param name="type"></param>
        /// <param name="notify"></param>
        private bool TakeIntoAccount(CodeType type, bool notify)
        {
            bool flag = false;
            if (type.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject && !typesByFullName.ContainsKey(type.FullName))
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Type {0} registered", type.FullName));

                flag = true;
                // Register fullname, name
                typesByFullName.Add(type.FullName, type);
                typesByName.Add(type.Name, type);
                System.Diagnostics.Debug.WriteLine(String.Format("Type {0} added", type.FullName));

                // And event short name as name for nested types
                if (type.Parent is CodeType)
                {
                    string shortTypeName = GetShortTypeName(type);
                    if (!typesByName.ContainsKey(shortTypeName))
                        typesByName.Add(shortTypeName, type);
                }

                // And notify addition for classes
                if ((notify) && (type is CodeClass))
                {
                    if ((type.Parent is CodeNamespace) || (type.Parent is CodeModel) || (type.Parent is FileCodeModel))
                    {
                        if (OnTypeAdded != null)
                            OnTypeAdded(type);
                    }
                    else if (type.Parent is CodeClass)
                    {
                        if (OnNestedTypeAdded != null)
                            OnNestedTypeAdded(type.Parent as CodeClass, type);
                    }
                }
            }

            // Nested types
            if (type is CodeClass)
                foreach (CodeElement nested in GetMembers(type))
                    if (nested is CodeType)
                        TakeIntoAccount(nested as CodeType, notify);
            
            return flag;
        }


        /// <summary>
        /// Get all the members of a type (considering there might be partial classes)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IEnumerable<CodeElement> GetMembers(CodeType type)
        {
            // The members might be spread accross several files in the case of partial classes
            // or partial interfaces
            if (type is CodeClass2)
            {
                CodeClass2 codeClass = type as CodeClass2;

                // Case of a partial class
                if (codeClass.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
                {
                    foreach (CodeClass2 partialClass in codeClass.PartialClasses)
                        foreach (CodeElement e in partialClass.Members)
                            yield return e;
                }

                // Case of a non-partial class
                else
                    foreach (CodeElement e in codeClass.Members)
                        yield return e;
            }


            else if (type is CodeInterface2)
            {
                CodeInterface2 codeInterface = type as CodeInterface2;

                // Case of a partial interface
                if (codeInterface.DataTypeKind == vsCMDataTypeKind.vsCMDataTypeKindPartial)
                {
                    foreach (CodeInterface2 partialInterface in codeInterface.Parts)
                        foreach (CodeElement e in partialInterface.Members)
                            yield return e;
                }

                // Case of a non partial interface
                else
                    foreach (CodeElement e in codeInterface.Members)
                        yield return e;
            }
            else
                foreach (CodeElement e in type.Members)
                    yield return e;
        }

        /// <summary>
        /// Types by full names
        /// </summary>
        Dictionary<string, CodeType> typesByFullName = new Dictionary<string, CodeType>();

        /// <summary>
        /// Types by name
        /// </summary>
        MultiMap<string, CodeType> typesByName = new MultiMap<string, CodeType>();

        #region IDisposable Members

        ~KnownCodeTypes()
        {
            Dispose(false);
        }

        private bool _disposed;
        public void Dispose(bool disposing)
        {
            if (_disposed)
                return;
             Clear();
             _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
