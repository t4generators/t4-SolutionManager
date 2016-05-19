using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsxFactory.Modeling.VisualStudio;
using Microsoft.VisualStudio.CSharp.Services.Language;
using EnvDTE;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    public static class CodeRefactoringHelper
    {
        /// <summary>
        /// Renames the specified service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="oldFullName">Old full name</param>
        /// <param name="newName">New simply name.</param>
        public static void Rename(IServiceProvider serviceProvider, string oldFullName, string newName)
        {
            Guard.ArgumentNotNull(serviceProvider, "serviceProvider");
            Guard.ArgumentNotNullOrEmptyString(newName, "newName");

            if (String.IsNullOrEmpty(oldFullName))
                return;

            CodeType codeElement = null;
            SolutionManagerService solutionManager = new SolutionManagerService(serviceProvider);
            if (solutionManager.CurrentSolution == null)
                return;

            foreach (var prj in solutionManager.CurrentSolution.AllProjects)
            {
                codeElement = prj.Project.CodeModel.CodeTypeFromFullName(oldFullName);
                if (codeElement != null && codeElement.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject)
                {
                    try
                    {
                        var refactoring = codeElement.ProjectItem.FileCodeModel as ICSCodemodelRefactoring;
                        if (refactoring != null)
                        {
                            refactoring.RenameNoUI(codeElement as CodeElement, newName, true, true, true);
                            break;
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
