//***************************************************************************
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the MICROSOFT VISUAL STUDIO 2010
//    VISUALIZATION AND MODELING SOFTWARE DEVELOPMENT KIT license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using VSLangProj;
using System.Collections.Generic;
//using Microsoft.VisualStudio.Modeling;

namespace VisualStudio.ParsingSolution.Shell
{

    /// <summary>
    /// Helper class enabling to make the link between a Store and the VS Project that hosts the model stored in this Store. 
    /// It provides operations againt projects and files in a way that is relative to the model file, and without any reference
    /// to DTE concepts, hence simplifying the authoring
    /// </summary>
    public static class StoreHostingProject
    {

        #region Adding a project

        /// <summary>
        /// Ensuures that a project of a given name exists in the solution
        /// </summary>
        /// <param name="project">project instance</param>
        /// <param name="relativePath">Relative path where to create the new project if necessary (ending in the project name with .csproj)</param>
        /// <param name="sourcePath">Source path of the template of the project</param>
        /// <param name="updateAssemblyNameAndNamespace">Should we update the assembly name and namespace of the new project.</param>
        /// <remarks>
        /// Suppose you want to create add a new project named "MyProject.csproj" from a template (vs vsTemplate located in a sub folder of the location of the extension,
        /// and you want to have similar namespaces:
        /// <code>
        ///    StoreHostingProject.EnsureNamedProjectExistsInDslSolution(dsl.Store, "MyProject.csproj"
        ///                                          , Path.Combine(Path.GetDirectoryName(typeof(ATypeInMyExtension).Assembly.Location), @"Templates\MyProject\MyTemplate.vstemplate")
        ///                                          , true
        ///                                          );
        /// </code>
        /// </remarks>
        public static void EnsureNamedProjectExistsInDslSolution(Project project, string relativePath, string sourcePath, bool updateAssemblyNameAndNamespace)
        {
            // Verify that the relative path ends with csproj
            if (Path.GetExtension(relativePath) != ".csproj")
            {
                throw new ArgumentException("relativePath should be relative path of the .csproj file to create with respect to the solution, hence ending in .csproj", "relativePath");
            }

            Solution solution = project.DTE.Solution;
            Project newProject = solution.Projects.OfType<Project>().FirstOrDefault(p => p.UniqueName == relativePath);
            if (newProject != null)
                return;

            string projectDirectory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(project.FullName)), Path.GetFileNameWithoutExtension(relativePath));
            string projectPath = Path.Combine(projectDirectory, Path.GetFileName(relativePath));
            string projectSimpleName = Path.GetFileNameWithoutExtension(relativePath);

            // The project exist but is not in the solution: let's just add it.
            if (File.Exists(projectPath))
                solution.AddFromFile(projectPath, false);

            // The project does not exist: create it from a template
            else
            {

                newProject = project.DTE.Solution.AddFromTemplate(sourcePath, projectDirectory, Path.GetFileName(relativePath), false);

                // Well known workaround for C# and VB projects, AddFromTemplate returns null
                newProject = solution.Projects.OfType<Project>().FirstOrDefault(p => p.Name == projectSimpleName);

                // Update the assembly name and namespace if necessary
                if (updateAssemblyNameAndNamespace)
                {
                    newProject.Properties.Item("AssemblyName").Value = project.Properties.Item("AssemblyName").Value.ToString().Replace("." + project.Name, "." + projectSimpleName);
                    newProject.Properties.Item("DefaultNamespace").Value = project.Properties.Item("DefaultNamespace").Value.ToString() + "." + projectSimpleName;
                }

            }
        }

        #endregion

        #region Adding a file or a link to a file

        /// <summary>
        /// Ensures that a file is present in the project
        /// </summary>
        /// <param name="project">project instance</param>
        /// <param name="relativePath">relative path where the file should be located</param>
        /// <param name="sourcePath">Path of the file to copy if not already present in the solution</param>
        /// <example>
        /// if you have a file Adapter.tt, added to the VSIX, of type Content, and copied if newer, in a folder Temmplates of the extension project, you can add
        /// it to the GeneratedCode folder of the DSL by the following code:
        /// <code>
        ///    StoreHostingProject.EnsureFileInProject(dsl.Store, @"GeneratedCode\Adapter.tt",
        ///                                            Path.Combine(Path.GetDirectoryName(typeof(MyExtensionAuthoring).Assembly.Location), @"Templates\Adapter.tt"));
        /// </code>
        /// </example>
        public static void EnsureFileCopiedInProject(Project project, string relativePath, string sourcePath)
        {

            Contract.Requires(project != null);
            Contract.Requires(relativePath != null);
            Contract.Requires(sourcePath != null);

            string[] pathSegments = relativePath.Split('\\');

            ProjectItems parent = project.ProjectItems;

            // Find the folder (or create it if necessary)
            for (int i = 0; i < pathSegments.Length - 1; ++i)
            {

                ProjectItem folder = parent.OfType<ProjectItem>().FirstOrDefault(projectItem => projectItem.Name == pathSegments[i]);

                if (folder == null)
                    folder = parent.AddFolder(pathSegments[i]);

                parent = folder.ProjectItems;
            }

            // Find the file and create it if necessary
            ProjectItem file = parent.OfType<ProjectItem>().FirstOrDefault(projectItem => projectItem.Name == pathSegments[pathSegments.Length - 1]);
            if (file == null)
            {
                string fileDirectory = Path.Combine(Path.GetDirectoryName(project.FullName), Path.GetDirectoryName(relativePath));
                string filePath = Path.Combine(fileDirectory, Path.GetFileName(relativePath));

                // Case where the file is already there, but not added to the project
                if (File.Exists(filePath))
                    parent.AddFromFile(filePath);

                else
                    parent.AddFromFileCopy(sourcePath);

            }
        }

        /// <summary>
        /// Ensures a link on a file is created in a project
        /// </summary>
        /// <param name="project">project instance</param>
        /// <param name="uniqueProjectName">Unique project name of the project to which to add a link a a file</param>
        /// <param name="relativePathOfFileToCreate">Relative path to the link to create in the project described by <paramref name="relativePathOfFileToCreate"/></param>
        /// <param name="originalFileToLink">Path to the original file to link</param>
        public static void EnsureFileLinkInProject(Project project, string uniqueProjectName, string relativePathOfFileToCreate, string originalFileToLink)
        {

            Contract.Requires(project != null);
            Contract.Requires(relativePathOfFileToCreate != null);
            Contract.Requires(originalFileToLink != null);

            if (!string.IsNullOrWhiteSpace(uniqueProjectName))
                project = project.DTE.Solution.Projects.OfType<Project>().FirstOrDefault(p => p.UniqueName == uniqueProjectName);

            if (project == null)
                return;

            string[] pathSegments = relativePathOfFileToCreate.Split('\\');

            ProjectItems parent = project.ProjectItems;

            // Find the folder (or create it if necessary)
            for (int i = 0; i < pathSegments.Length - 1; ++i)
            {

                ProjectItem folder = parent.OfType<ProjectItem>().FirstOrDefault(projectItem => projectItem.Name == pathSegments[i]);

                if (folder == null)
                    folder = parent.AddFolder(pathSegments[i]);

                parent = folder.ProjectItems;

            }

            // Find the file and create a link on the originalFileToLink it if necessary
            ProjectItem file = parent.OfType<ProjectItem>().FirstOrDefault(projectItem => projectItem.Name == pathSegments[pathSegments.Length - 1]);
            if (file == null)
                parent.AddFromFile(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.FullName), originalFileToLink)));

        }
        
        #endregion
    }


}
