using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace VsxFactory.Modeling.VisualStudio
{
    public interface IVsSolutionExplorer
    {
        IVsSolution CurrentSolution { get; }
        void DisableReferenceEventsOnProject(string projectName);
        void EnableReferenceEventsOnProject();
        string GetExportedProjectTemplatesDir();
        IVsProjectItem SelectedItem { get; }
    }

    [ContractClass(typeof(IVsProjectItemContract))]
    public interface IVsProjectItem
    {
        string Path { get; }
        object ExternalObject { get; }
        IVsSolution Solution { get; }
    }

    [ContractClassFor(typeof(IVsProject))]
    public class IVsProjectItemContract : IVsProjectItem
    {
        public string Path
        {
            get {
                Contract.Ensures(Contract.Result<string>() != null);
                throw new NotImplementedException(); }
        }

        public object ExternalObject
        {
            get {
                Contract.Ensures(Contract.Result<object>() != null);
                throw new NotImplementedException(); }
        }

        public IVsSolution Solution
        {
            get {
                Contract.Ensures(Contract.Result<IVsSolution>() != null);
                throw new NotImplementedException(); }
        }
    }
 

    public interface IVsProjectFileItem : IVsProjectItem
    {
        IVsProject Project { get; }
        string Name { get; set; }
        void Remove();
        IVsProjectFileItem AddFromFile(string relativeFileName);
        IVsProjectFileItem AddFromTemplate(string templateName, string fileName);
        string GetContent();
        void ReplaceText(string result, Encoding encoding=null);
        string GetAttribute(string p);
        void SetAttribute(string p, string value);
        IEnumerable<IVsProjectFileItem> AllElements { get; }
        void Rename(string newName);
        bool GetAttributeAsBoolean(string name, bool defaultValue);
        T GetAttributeAsEnum<T>(string name, T codeGenerationEvent);
        void RunCustomTool();
    }

    public interface IVsItemContainer : IVsProjectItem
    {
        IEnumerable<IVsProjectItem> AllElements { get; }
        T FindByPath<T>(string fullName) where T : class, IVsProjectItem;
        T Find<T>(Func<T, bool> func) where T : class, IVsProjectItem;
    }

    public interface IVsFolder : IVsItemContainer
    {
        void AddItemAsLink(string fileName);
    }

    public interface IVsSolutionFolder : IVsItemContainer
    {

    }

    public interface IVsSolutionItems : IVsItemContainer 
    {
    }

    public interface IVsProjectReference : IVsProjectItem
    {
        bool IsProjectReference { get; }
        Guid ProjectGuid { get; }
        string Version { get; }
    }

    public interface IVsProject : IVsFolder
    {
        string Name { get; }
        Guid ProjectGuid { get; }
        string Namespace { get; }
        IVsProjectFileItem AddItem(string fullName);
        Modeling.VisualStudio.ProjectKind Kind { get; }
        string Language { get; }
        IVsProjectReference AddProjectReference(Guid projectGuid);
        IVsProjectReference AddReference(string assemblyName, string version = null);
        IVsProjectFileItem AddFromFile(string relativeFileName);
        IVsProjectFileItem AddItem(string fileName, byte[] content=null);
        IVsFolder FindOrCreateFolder(string folderName);
        IVsProjectFileItem AddFromTemplate(string templateName, string fileName, bool overwrite);
        void RenameItem(string oldRelativeFileName, string newRelativeFileName);
        void RemoveItem(string relativeFileName);
    }

    public interface IVsSolution : IVsItemContainer
    {
        IVsProjectFileItem AddTemplateWithReplaceParameters(string name, bool binaryContent, string templateFolder, System.Resources.ResourceManager resourceManager, params string[] parameters);
        IVsProjectFileItem AddItem(string name, byte[] content=null);
        string Name { get; }
        IVsSolutionExplorer SolutionExplorer { get; }
        IVsProject GetProject(Guid projectGuid);
        IVsProject GetProject(string uniqueName);
        IVsSolutionItems SolutionItems { get; }
        IVsProject CreateProject(string projectTemplatesFolder, string solutionFolder, string name, string assemblyName, string template, string defaultNamespace);
    }
}
