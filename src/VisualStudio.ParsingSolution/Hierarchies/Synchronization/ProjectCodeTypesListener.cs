using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.ComponentModel;
using System.Threading;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    public class ProjectCodeTypesListener : IDisposable, IProjectCodeTypesListener
    {
        private ISolutionManagerService _solutionManagerService;
        /// <summary>
        /// known code types for a given project list
        /// </summary>
        private Dictionary<string, KnownCodeTypes> _knownCodeTypes;

        private IServiceProvider _serviceProvider;
        private ProjectItemsEvents _projectItemsEvents;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ProjectCodeTypesListener(IServiceProvider serviceProvider)
        {
            Guard.ArgumentNotNull(serviceProvider, "serviceprovider");
            _serviceProvider = serviceProvider;

            // TODO ne pas chercher en tant que service
            _solutionManagerService = serviceProvider.GetService(typeof(ISolutionManagerService)) as ISolutionManagerService;
            Guard.AssumeNotNull(_solutionManagerService != null, "_solutionManagerService");

            _knownCodeTypes = new Dictionary<string, KnownCodeTypes>();

            _solutionManagerService.SolutionEvents += new SolutionEventsHandler(OnSolutionEvents);
            _solutionManagerService.ProjectChanged += new EventHandler<ProjectChangedEventArg>(OnSolutionItemsChanged);
        }

        /// <summary>
        /// Get all the know types
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EnvDTE.CodeType> AllKnownTypes
        {
            get
            {
                foreach (var knownCodeType in _knownCodeTypes)
                {
                    foreach (var knownType in knownCodeType.Value.AllKnownTypes)
                    {
                        try
                        {
                            var n = knownType.Name; // Test si type existe
                        }
                        catch {
                            /* deleted type */
                            continue;
                        }
                        yield return knownType;
                    }
                }
            }
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="SolutionReferencesListener"/> is reclaimed by garbage collection.
        /// </summary>
        ~ProjectCodeTypesListener()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            UnregisterAllProjects();

            _solutionManagerService.SolutionEvents -= new SolutionEventsHandler(OnSolutionEvents);
            _solutionManagerService.ProjectChanged -= new EventHandler<ProjectChangedEventArg>(OnSolutionItemsChanged);           
        }
        #endregion

        /// <summary>
        /// When project items are renamed, tell it
        /// </summary>
        /// <param name="ProjectItem"></param>
        /// <param name="OldName"></param>
        void projectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Project Item {0} renamed", projectItem.get_FileNames(1)));

            //var projectNode = _solutionManagerService.CurrentSolution.GetProject(projectItem.ContainingProject.UniqueName);
            //RegisterProject(projectNode);
        }

        /// <summary>
        /// When a project item has been added
        /// </summary>
        /// <param name="ProjectItem"></param>
        void projectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Project Item {0} added", projectItem.get_FileNames(1)));

            //var projectNode = _solutionManagerService.CurrentSolution.GetProject(projectItem.ContainingProject.UniqueName);
            //KnownCodeTypes kct;
            //if (_knownCodeTypes.TryGetValue(projectNode.UniqueName, out kct))
            //{
            //    System.Diagnostics.Debug.WriteLine(String.Format("Project Item {0} added", projectItem.get_FileNames(1)));
            //    kct.TakeIntoAccount(projectNode.Project);
            //}
            //else
            //{
            //    RegisterProject(projectNode);
            //}
        }

        /// <summary>
        /// When a project item is removed, Types might be removed
        /// </summary>
        /// <param name="ProjectItem"></param>
        void projectItemsEvents_ItemRemoved(ProjectItem projectItem)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Project Item {0} removed", projectItem.get_FileNames(1)));

            var projectNode = _solutionManagerService.CurrentSolution.GetProject(projectItem.ContainingProject.UniqueName);
            KnownCodeTypes kct;
            if (_knownCodeTypes.TryGetValue(projectNode.UniqueName, out kct))
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Project Item {0} removed", projectItem.get_FileNames(1)));
                kct.RemoveObsoleteTypes();
            }
        }

        /// <summary>
        /// Suppression de tous les évents
        /// </summary>
        public void UnregisterAllProjects()
        {
            // UnSubscribe to Misc Files events
            if (_projectItemsEvents != null)
            {
                this._projectItemsEvents.ItemRenamed -= new _dispProjectItemsEvents_ItemRenamedEventHandler(projectItemsEvents_ItemRenamed);
                this._projectItemsEvents.ItemRemoved -= new _dispProjectItemsEvents_ItemRemovedEventHandler(projectItemsEvents_ItemRemoved);
                this._projectItemsEvents.ItemAdded -= new _dispProjectItemsEvents_ItemAddedEventHandler(projectItemsEvents_ItemAdded);
                this._projectItemsEvents = null;
            }

            foreach (var kct in _knownCodeTypes.Values)
                kct.Dispose();
            _knownCodeTypes = new Dictionary<string, KnownCodeTypes>();
        }

        /// <summary>
        /// Fermeture de la solution. On fait le mènage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnSolutionEvents(object sender, SolutionEventArgs e)
        {
            if (e.Closing)
            {
                UnregisterAllProjects();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="project"></param>
        public void RegisterProject(ProjectNode project)
        {
            Guard.ArgumentNotNull(project, "project");

            if (project.Project == null)
                return;
            System.Diagnostics.Debug.WriteLine(String.Format("project {0} registered", project.Name));

            // Subscribe to Misc Files events
            if (_projectItemsEvents == null)
            {
                EnvDTE.DTE dte = project.Project.DTE;
                this._projectItemsEvents = ((EnvDTE80.Events2)dte.Events).ProjectItemsEvents;
                this._projectItemsEvents.ItemRenamed += new _dispProjectItemsEvents_ItemRenamedEventHandler(projectItemsEvents_ItemRenamed);
                this._projectItemsEvents.ItemRemoved += new _dispProjectItemsEvents_ItemRemovedEventHandler(projectItemsEvents_ItemRemoved);
                this._projectItemsEvents.ItemAdded += new _dispProjectItemsEvents_ItemAddedEventHandler(projectItemsEvents_ItemAdded);
            }

            UnregisterProject(project.Project);

            var knownCodeType = KnownCodeTypes.FromProject(project.Project);
            if (knownCodeType != null)
            {
                _knownCodeTypes.Add(project.UniqueName, knownCodeType);
                knownCodeType.OnAttributeAdded += new AttributeAdded(knownCodeType_OnAttributeAdded);
                knownCodeType.OnAttributeChanged += new AttributeChanged(knownCodeType_OnAttributeChanged);
                knownCodeType.OnAttributeRemoved += new AttributeRemoved(knownCodeType_OnAttributeRemoved);
                knownCodeType.OnBaseTypeChanged += new BaseTypeChanged(knownCodeType_OnBaseTypeChanged);
                knownCodeType.OnElementPropertiesChanged += new ElementPropertiesChanged(knownCodeType_OnElementPropertiesChanged);
                knownCodeType.OnFieldAdded += new FieldAdded(knownCodeType_OnFieldAdded);
                knownCodeType.OnFieldRemoved += new MemberRemoved(knownCodeType_OnFieldRemoved);
                knownCodeType.OnFieldRenamed += new FieldRenamed(knownCodeType_OnFieldRenamed);
                knownCodeType.OnMethodAdded += new MethodAdded(knownCodeType_OnMethodAdded);
                knownCodeType.OnMethodRemoved += new MemberRemoved(knownCodeType_OnMethodRemoved);
                knownCodeType.OnMethodRenamed += new MethodRenamed(knownCodeType_OnMethodRenamed);
                knownCodeType.OnNestedTypeAdded += new NestedTypeAdded(knownCodeType_OnNestedTypeAdded);
                knownCodeType.OnParameterAdded += new ParameterAdded(knownCodeType_OnParameterAdded);
                knownCodeType.OnParameterRemoved += new ParameterRemoved(knownCodeType_OnParameterRemoved);
                knownCodeType.OnParameterRenamed += new ParameterRenamed(knownCodeType_OnParameterRenamed);
                knownCodeType.OnPropertyAdded += new PropertyAdded(knownCodeType_OnPropertyAdded);
                knownCodeType.OnPropertyRemoved += new MemberRemoved(knownCodeType_OnPropertyRemoved);
                knownCodeType.OnTypeAdded += new TypeAdded(knownCodeType_OnTypeAdded);
                knownCodeType.OnTypeRemoved += new TypeRemoved(knownCodeType_OnTypeRemoved);
                knownCodeType.OnTypeRenamed += new TypeRenamed(knownCodeType_OnTypeRenamed);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="project"></param>
        public void UnregisterProject(ProjectNode project)
        {
            if( project != null)
                UnregisterProject(project.Project);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="project"></param>
        private void UnregisterProject(EnvDTE.Project project)
        {
            KnownCodeTypes knownCodeType;
            if (_knownCodeTypes.TryGetValue(project.UniqueName, out knownCodeType))
            {                
                _knownCodeTypes.Remove(project.UniqueName);

                knownCodeType.OnAttributeAdded -= new AttributeAdded(knownCodeType_OnAttributeAdded);
                knownCodeType.OnAttributeChanged -= new AttributeChanged(knownCodeType_OnAttributeChanged);
                knownCodeType.OnAttributeRemoved -= new AttributeRemoved(knownCodeType_OnAttributeRemoved);
                knownCodeType.OnBaseTypeChanged -= new BaseTypeChanged(knownCodeType_OnBaseTypeChanged);
                knownCodeType.OnElementPropertiesChanged -= new ElementPropertiesChanged(knownCodeType_OnElementPropertiesChanged);
                knownCodeType.OnFieldAdded -= new FieldAdded(knownCodeType_OnFieldAdded);
                knownCodeType.OnFieldRemoved -= new MemberRemoved(knownCodeType_OnFieldRemoved);
                knownCodeType.OnFieldRenamed -= new FieldRenamed(knownCodeType_OnFieldRenamed);
                knownCodeType.OnMethodAdded -= new MethodAdded(knownCodeType_OnMethodAdded);
                knownCodeType.OnMethodRemoved -= new MemberRemoved(knownCodeType_OnMethodRemoved);
                knownCodeType.OnMethodRenamed -= new MethodRenamed(knownCodeType_OnMethodRenamed);
                knownCodeType.OnNestedTypeAdded -= new NestedTypeAdded(knownCodeType_OnNestedTypeAdded);
                knownCodeType.OnParameterAdded -= new ParameterAdded(knownCodeType_OnParameterAdded);
                knownCodeType.OnParameterRemoved -= new ParameterRemoved(knownCodeType_OnParameterRemoved);
                knownCodeType.OnParameterRenamed -= new ParameterRenamed(knownCodeType_OnParameterRenamed);
                knownCodeType.OnPropertyAdded -= new PropertyAdded(knownCodeType_OnPropertyAdded);
                knownCodeType.OnPropertyRemoved -= new MemberRemoved(knownCodeType_OnPropertyRemoved);
                knownCodeType.OnTypeAdded -= new TypeAdded(knownCodeType_OnTypeAdded);
                knownCodeType.OnTypeRemoved -= new TypeRemoved(knownCodeType_OnTypeRemoved);
                knownCodeType.OnTypeRenamed -= new TypeRenamed(knownCodeType_OnTypeRenamed);
                knownCodeType.Dispose();
            }
        }

        /// <summary>
        /// When a project is removed, empty the dictionary.
        /// </summary>
        /// <param name="project">Removed project</param>
        void OnSolutionItemsChanged(object sender, ProjectChangedEventArg e)
        {
            if (e.Action == EventAction.ProjectRemoved)
            {
                EnvDTE.Project project = new ProjectNode(e.Project).Project;
                UnregisterProject(project);
            }
            if (e.Action == EventAction.ProjectAdded)
            {
                var prj = new ProjectNode(e.Project);
                RegisterProject(prj);
            }
            if (e.Action == EventAction.ProjectChanged)
            {
                // TODO changement de nom de projet
            }
            if (e.Action == EventAction.SolutionLoaded)
            {
                // On laisse la main au modéle           
            }
        }

        #region Events

        void knownCodeType_OnAttributeRemoved(EnvDTE.CodeElement parent, string attributeName)
        {
            if (OnAttributeRemoved != null)
                OnAttributeRemoved(parent, attributeName);
        }

        void knownCodeType_OnTypeRenamed(string oldFullName, EnvDTE.CodeType type)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Type {0} renamed to {1}", oldFullName, type.FullName));

            if (OnTypeRenamed != null)
                OnTypeRenamed(oldFullName, type);
        }

        void knownCodeType_OnTypeRemoved(string fullName)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Type {0} removed", fullName));

            if (OnTypeRemoved != null)
                OnTypeRemoved(fullName);
        }

        void knownCodeType_OnTypeAdded(EnvDTE.CodeType newType)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Type {0} added",newType.FullName));

            if (OnTypeAdded != null)
                OnTypeAdded(newType);
        }

        void knownCodeType_OnPropertyRemoved(EnvDTE.CodeType type, string memberNameOrSignature)
        {
            if (OnPropertyRemoved != null)
                OnPropertyRemoved(type, memberNameOrSignature);
        }

        void knownCodeType_OnPropertyAdded(EnvDTE.CodeType parent, EnvDTE.CodeProperty newProperty)
        {
            if (OnPropertyAdded != null)
                OnPropertyAdded(parent, newProperty);
        }

        void knownCodeType_OnParameterRenamed(EnvDTE80.CodeFunction2 method, int parameterIndex, EnvDTE.CodeParameter parameter)
        {
            if (OnParameterRenamed != null)
                OnParameterRenamed(method, parameterIndex, parameter);
        }

        void knownCodeType_OnParameterRemoved(EnvDTE80.CodeFunction2 method, string parameterName)
        {
            if (OnParameterRemoved != null)
                OnParameterRemoved(method, parameterName);
        }

        void knownCodeType_OnParameterAdded(EnvDTE80.CodeFunction2 method, EnvDTE.CodeParameter parameter)
        {
            if (OnParameterAdded != null)
                OnParameterAdded(method, parameter);
        }

        void knownCodeType_OnNestedTypeAdded(EnvDTE.CodeClass parent, EnvDTE.CodeType newNestedType)
        {
            if (OnNestedTypeAdded != null)
                OnNestedTypeAdded(parent, newNestedType);
        }

        void knownCodeType_OnMethodRenamed(EnvDTE.CodeType type, EnvDTE80.CodeFunction2 method)
        {
            if (OnMethodRenamed != null)
                OnMethodRenamed(type, method);
        }

        void knownCodeType_OnMethodRemoved(EnvDTE.CodeType type, string memberNameOrSignature)
        {
            if (OnMethodRemoved != null)
                OnMethodRemoved(type, memberNameOrSignature);
        }

        void knownCodeType_OnMethodAdded(EnvDTE.CodeType parent, EnvDTE80.CodeFunction2 newMethod)
        {
            if (OnMethodAdded != null)
                OnMethodAdded(parent, newMethod);
        }

        void knownCodeType_OnFieldRenamed(EnvDTE.CodeType type, EnvDTE.CodeVariable field)
        {
            if (OnFieldRenamed != null)
                OnFieldRenamed(type, field);
        }

        void knownCodeType_OnFieldRemoved(EnvDTE.CodeType type, string memberNameOrSignature)
        {
            if (OnFieldRemoved != null)
                OnFieldRemoved(type, memberNameOrSignature);
        }

        void knownCodeType_OnFieldAdded(EnvDTE.CodeType parent, EnvDTE.CodeVariable newField)
        {
            if (OnFieldAdded != null)
                OnFieldAdded(parent, newField);
        }

        void knownCodeType_OnElementPropertiesChanged(EnvDTE.CodeElement element)
        {
            if (OnElementPropertiesChanged != null)
                OnElementPropertiesChanged(element);
        }

        void knownCodeType_OnBaseTypeChanged(EnvDTE.CodeType type)
        {
            if (OnBaseTypeChanged != null)
                OnBaseTypeChanged(type);
        }

        void knownCodeType_OnAttributeChanged(EnvDTE80.CodeAttribute2 attribute)
        {
            if (OnAttributeChanged != null)
                OnAttributeChanged(attribute);
        }

        void knownCodeType_OnAttributeAdded(EnvDTE80.CodeAttribute2 attribute)
        {
            if (OnAttributeAdded != null)
                OnAttributeAdded(attribute);
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

        #endregion
    }
}
