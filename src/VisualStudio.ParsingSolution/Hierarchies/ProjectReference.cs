using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace VsxFactory.Modeling.VisualStudio
{
    /// <summary>
    /// 
    /// </summary>
    public enum ReferenceType
    {
        /// <summary>
        /// Projet Visual Studio
        /// </summary>
        VSProject,
        /// <summary>
        /// Assemblie
        /// </summary>
        Assembly,
        /// <summary>
        /// 
        /// </summary>
        Artifact
    }

    /// <summary>
    /// 
    /// </summary>
    public class ProjectReference 
    {
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return StrongName.GetHashCode();
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the source project.
        /// </summary>
        /// <value>The source project.</value>
        public string SourceProjectUniqueName { get; set; }

        /// <summary>
        /// Gets or sets the name of the referenced project unique.
        /// </summary>
        /// <value>The name of the referenced project unique.</value>
        public string ReferencedProjectUniqueName { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string FullPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        public ReferenceType Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the strong.
        /// </summary>
        /// <value>The name of the strong.</value>
        public string StrongName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [copy local].
        /// </summary>
        /// <value><c>true</c> if [copy local]; otherwise, <c>false</c>.</value>
        public bool CopyLocal { get; set; }
    }

    public static class ProjectReferenceHelper
    {
        /// <summary>
        /// Creates the visual studio reference from reference.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="pReference">The p reference.</param>
        /// <returns></returns>
        internal static ProjectReference CreateVisualStudioReferenceFromReference(SolutionNode solution, VSLangProj.Reference pReference)
        {
            if (pReference == null || (pReference.SourceProject == null && String.IsNullOrEmpty(pReference.Path)))
                return null;

            string strongName = null;
            if (pReference.StrongName)
            {
                string culture = pReference.Culture;
                if (String.IsNullOrEmpty(culture))
                    culture = "neutral";
                strongName = String.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}", pReference.Name, pReference.Version, culture, pReference.PublicKeyToken);
            }
            else
                strongName = String.Format("{0}, Version={1}", pReference.Name, pReference.Version);

            return CreateVSReference(solution, pReference.ContainingProject, pReference.SourceProject, pReference.Name, pReference.Path, pReference.Version, strongName, pReference.CopyLocal);
        }

        /// <summary>
        /// Creates the VS reference.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="containingProject">The containing project.</param>
        /// <param name="referencedProject">The referenced project.</param>
        /// <param name="name">The name.</param>
        /// <param name="fullPath">The full path.</param>
        /// <param name="version">The version.</param>
        /// <param name="strongName">Name of the strong.</param>
        /// <param name="copyLocal">if set to <c>true</c> [copy local].</param>
        /// <returns></returns>
        private static ProjectReference CreateVSReference(SolutionNode solution, EnvDTE.Project containingProject, EnvDTE.Project referencedProject, string name, string fullPath, string version, string strongName, bool copyLocal)
        {
            Guard.ArgumentNotNull(solution, "solution");
            Guard.ArgumentNotNullOrEmptyString(name, "name");
            Guard.ArgumentNotNullOrEmptyString(strongName, "strongName");

            // Référence système implicite 
            if (name.StartsWith("mscor", StringComparison.OrdinalIgnoreCase))
                return null;

            ProjectReference rf = new ProjectReference();
            rf.SourceProjectUniqueName = containingProject.UniqueName;
            rf.Name = name.ToLower();
            rf.CopyLocal = copyLocal;

            if (referencedProject != null)
            {
                rf.Type = ReferenceType.VSProject;
                rf.ReferencedProjectUniqueName = referencedProject.UniqueName;
                return rf;
            }

            rf.Type = ReferenceType.Assembly;
            rf.Version = new Version( version );
            rf.StrongName = strongName;
            rf.FullPath = fullPath;
            return rf;
        }

        /// <summary>
        /// Creates the visual studio reference from reference.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="reference">The reference.</param>
        /// <returns></returns>
        internal static ProjectReference CreateVisualStudioReferenceFromReference(SolutionNode solution, VsWebSite.AssemblyReference reference)
        {
            if (reference == null || (reference.ReferencedProject == null && String.IsNullOrEmpty(reference.FullPath)))
                return null;

            string strongName = reference.Name;
            string version = "1.0.0.0";
            try
            {
                strongName = reference.StrongName;
                AssemblyName an = new AssemblyName(strongName);
                version = an.Version.ToString();
            }
            catch
            {
                if (!String.IsNullOrEmpty(strongName))
                {
                    string[] parts = strongName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        strongName = parts[0].Trim();
                        if (parts.Length > 1)
                        {
                            string[] parts2 = parts[1].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts2.Length > 1)
                                version = parts2[1].Trim();
                        }
                        strongName += ", Version=" + version;
                    }
                }
            }
            return CreateVSReference(solution, reference.ContainingProject, reference.ReferencedProject, reference.Name, reference.FullPath, version, strongName, reference.ReferenceKind == VsWebSite.AssemblyReferenceType.AssemblyReferenceClientProject);
        }
    }
}
