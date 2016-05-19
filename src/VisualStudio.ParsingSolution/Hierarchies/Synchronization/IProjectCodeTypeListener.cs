using System;
using VsxFactory.Modeling.VisualStudio;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    public interface IProjectCodeTypesListener
    {
        void UnregisterAllProjects();
        void RegisterProject(ProjectNode project);
        void UnregisterProject(ProjectNode project);
        System.Collections.Generic.IEnumerable<EnvDTE.CodeType> AllKnownTypes { get; }
        void Dispose();
        event AttributeAdded OnAttributeAdded;
        event AttributeChanged OnAttributeChanged;
        event AttributeRemoved OnAttributeRemoved;
        event BaseTypeChanged OnBaseTypeChanged;
        event ElementPropertiesChanged OnElementPropertiesChanged;
        event FieldAdded OnFieldAdded;
        event MemberRemoved OnFieldRemoved;
        event FieldRenamed OnFieldRenamed;
        event MethodAdded OnMethodAdded;
        event MemberRemoved OnMethodRemoved;
        event MethodRenamed OnMethodRenamed;
        event NestedTypeAdded OnNestedTypeAdded;
        event ParameterAdded OnParameterAdded;
        event ParameterRemoved OnParameterRemoved;
        event ParameterRenamed OnParameterRenamed;
        event PropertyAdded OnPropertyAdded;
        event MemberRemoved OnPropertyRemoved;
        event TypeAdded OnTypeAdded;
        event TypeRemoved OnTypeRemoved;
        event TypeRenamed OnTypeRenamed;
    }
}
