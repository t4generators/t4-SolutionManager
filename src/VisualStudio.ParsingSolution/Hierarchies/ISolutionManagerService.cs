using System;
using VsxFactory.Modeling.VisualStudio.Synchronization;
namespace VsxFactory.Modeling.VisualStudio
{
    public interface ISolutionManagerService
    {
        event EventHandler<EventArgs> ConfigurationChanged;
        string ConfigurationName { get; }
        System.Collections.Generic.List<string> ConfigurationNames { get; }
        //VsxFactory.Modeling.VisualStudio.ProjectNode CreateProject(string SolutionFolderPath, string projectName, string template, string defaultNamespace, string solutionName);
        VsxFactory.Modeling.VisualStudio.SolutionNode CurrentSolution { get; }
        void DisableReferenceEventsOnProject(string projectName);
        void Dispose();
        void EnableReferenceEventsOnProject();
        VsxFactory.Modeling.VisualStudio.ProjectNode GetProjectDropped(System.Windows.Forms.IDataObject data);
        VsxFactory.Modeling.VisualStudio.HierarchyNode GetProjectItemDropped(System.Windows.Forms.IDataObject data);
        VsxFactory.Modeling.VisualStudio.SolutionNode GetSolution(IServiceProvider serviceProvider);
        bool IsDocumentInSolution(string relativeFileName);
        bool IsProjectDragged(System.Windows.Forms.IDataObject data);
        bool IsProjectItemDragged(System.Windows.Forms.IDataObject iDataObject);
        void OpenFile(string fileName);
        event EventHandler<ProjectChangedEventArg> ProjectChanged;
        IServiceProvider ServiceProvider { get; }
        event SolutionEventsHandler SolutionEvents;
        System.Collections.Generic.List<VsxFactory.Modeling.VisualStudio.ProjectNode> StartupProjects { get; }
        System.Collections.Generic.List<String> EnumerateTemplates(string languageName, string modelsTemplateFolder);
    }
}
