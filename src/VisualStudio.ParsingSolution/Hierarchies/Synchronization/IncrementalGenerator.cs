using System;
using System.Collections.Generic;
using System.IO;

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Modeling;
using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using System.Text;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    /// <summary>
    /// Code generator which acts by incremental modification of the CodeModel. This
    /// code generator capabilities are to :
    /// <list>
    /// <item>Find a named nested Class, nested Interface</item>
    /// <item>Find a method or a constructor of given signature</item>
    /// </list>
    /// </summary>
    public class IncrementalGenerator
    {
        /// <summary>
        /// Find a property of a class from its name
        /// </summary>
        /// <param name="classe">class in which we are looking for a property</param>
        /// <param name="name">Name of the property to find</param>
        /// <returns>a <c>CodeProperty</c> if a property of given name exists in the class
        /// or otherwise <c>null</c></returns>
        public static CodeProperty FindProperty(CodeClass2 classe, string name)
        {
            foreach (CodeElement element in GetAllMembers(classe))
                if ((element.Kind == vsCMElement.vsCMElementProperty) && (element.Name == name))
                    return element as CodeProperty;
            return null;
        }



        /// <summary>
        /// Find the first member of given name
        /// </summary>
        /// <param name="codeClass2"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static CodeElement FindFirstMember(CodeClass2 codeClass2, string name)
        {
            foreach (CodeElement element in codeClass2.Members)
                if (element.Name == name)
                    return element;
            return null;
        }


        /// <summary>
        /// Get the type of a member (return type for functions)
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static CodeTypeRef GetMemberType(CodeElement member)
        {
            CodeFunction method = member as CodeFunction;
            if (method != null)
                return method.Type;

            CodeVariable field = member as CodeVariable;
            if (field != null)
                return field.Type;

            CodeProperty property = member as CodeProperty;
            if (property != null)
                return property.Type;

            throw new NotImplementedException("GetMemberType(CodeElement member) is only implemented for CodeFunction, CodeVariable, and CodeProperty");
        }


        /// <summary>
        /// Ensure a member (Field or property) has the correct type
        /// </summary>
        /// <param name="member">Interface / Class member to ensure the Type of</param>
        /// <param name="fullTypeName">Fully qualified name of the type that the field / property should have</param>
        public static void EnsureMemberType(CodeElement member, string fullTypeName)
        {
            if (member.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return;

            Project project = member.ProjectItem.ContainingProject;
            KnownCodeTypes knownCodeTypes = KnownCodeTypes.FromProject(project);
            if (knownCodeTypes == null)
                return;

            // Get new and previous type
            CodeTypeRef currentMemberType = GetMemberType(member);
            try
            {
                if (currentMemberType.AsFullName == fullTypeName)
                    return;
            }
            catch (Exception)
            {
                // If a using is missing, .AsFullName may throw an exception !
            }

            CodeType type = knownCodeTypes.GetFullNamedType(fullTypeName);
            bool generatedUsing;
            CodeTypeRef typeRef;
            if (type != null)
            {
                if (knownCodeTypes.GetNamedTypes(type.Name).Length == 1)
                {
                    typeRef = project.CodeModel.CreateCodeTypeRef(KnownCodeTypes.SimplifyForCSharp(type.Name));
                    generatedUsing = true;
                }
                else
                {
                    typeRef = project.CodeModel.CreateCodeTypeRef(type.FullName);
                    generatedUsing = false;
                }
            }
            else
            {
                generatedUsing = false;
                typeRef = project.CodeModel.CreateCodeTypeRef(fullTypeName);
            }

            if (member is CodeVariable)
            {
                CodeVariable field = member as CodeVariable;
                if (field.Type.AsString != typeRef.AsString) // Do not override same code (no void modifications in undo buffer of source file)
                {
                    if ((generatedUsing) && (type.Namespace.FullName != (field.Parent as CodeType).Namespace.FullName)) // No using if using types from the same namespace
                        EnsureUsings(field.ProjectItem, type.Namespace.FullName);
                    field.Type = typeRef;
                }
            }
            else if (member is CodeProperty)
            {
                CodeProperty property = member as CodeProperty;
                if (property.Type.AsString != typeRef.AsString) // Do not override same code (no void modifications in undo buffer of source file)
                {
                    if ((generatedUsing) && (type.Namespace.FullName != property.Parent.Namespace.FullName))
                        EnsureUsings(property.ProjectItem, type.Namespace.FullName);
                    property.Type = typeRef;
                }
            }
            else
                throw new NotImplementedException("EnsureCodeElementType() only implemented for CodeVariable and CodeProperty for the moment");
        }


        /// <summary>
        /// Find a field of given name in a class
        /// </summary>
        /// <param name="classe">Class in which to look for the field</param>
        /// <param name="name">name of the field to look for</param>
        /// <returns></returns>
        public static CodeVariable2 FindField(CodeClass2 classe, string name)
        {
            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementVariable)
                {
                    CodeVariable variable = element as CodeVariable;
                    if ((variable.Name == name) /*&& (variable.Type.AsString == type)*/)
                        return variable as CodeVariable2;
                }
            return null;
        }

        /// <summary>
        /// Find a method by its name (if there are several, get the first found)
        /// </summary>
        /// <param name="classe">class in which we are looking for a named method</param>
        /// <param name="name">Name of the method to find</param>
        /// <returns>a <c>CodeFunction2</c> if a method of given name exists in the class
        /// or otherwise <c>null</c></returns>
        public static CodeFunction2 FindFirstMethod(CodeClass2 classe, string name)
        {
            foreach (CodeElement element in GetAllMembers(classe))
                if ((element.Name == name) && (element.Kind == vsCMElement.vsCMElementFunction))
                    return element as CodeFunction2;
            return null;
        }

        /// <summary>
        /// Find the first method of an interface having a given name.
        /// </summary>
        /// <param name="iface">CodeInterface we look a method of</param>
        /// <param name="name">Name of the method to find.</param>
        /// <returns>The first method of the interface having the given name</returns>
        public static CodeFunction2 FindFirstMethod(CodeInterface iface, string name)
        {
            foreach (CodeElement element in GetAllMembers(iface))
                if ((element.Name == name) && (element.Kind == vsCMElement.vsCMElementFunction))
                    return element as CodeFunction2;
            return null;
        }


        /// <summary>
        /// Get the correct signature for the method. There are some case where f.get_Prototype((int)(vsCMPrototype.vsCMPrototypeType | vsCMPrototype.vsCMPrototypeParamTypes))
        /// does not return the right result (not fully qualified parameter types)
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string GetCorrectSignature(CodeFunction2 f)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                // Return type
                sb.Append(f.Type.AsString + " ");
                sb.Append(f.Name + " (");
                List<string> parameterString = new List<string>();
                for (int i = 1; i <= f.Parameters.Count; ++i)
                    parameterString.Add((f.Parameters.Item(i) as CodeParameter).Type.AsString);
                sb.Append(string.Join(", ", parameterString.ToArray()));
                sb.Append(")");
            }
            catch (Exception)
            {
                sb.Append(f.get_Prototype((int)(vsCMPrototype.vsCMPrototypeType | vsCMPrototype.vsCMPrototypeParamTypes)));
            }
            return sb.ToString();
        }


        /// <summary>
        /// Find a method of given signature
        /// </summary>
        /// <param name="codeType">Type (class, interface) holding a method)</param>
        /// <param name="methodSignature">String that will be compared to the 
        /// <c>function.get_Prototype((int)(vsCMPrototype.vsCMPrototypeParamTypes|vsCMPrototype.vsCMPrototypeType))</c> 
        /// of the function members of type <paramref name="codeType"/></param>
        /// <returns></returns>
        public static CodeFunction2 FindMethod(CodeType codeType, string methodSignature)
        {
            methodSignature = methodSignature.Replace(" (", "(");
            foreach (CodeElement member in GetAllMembers(codeType))
            {
                CodeFunction2 function = member as CodeFunction2;
                if ((function != null) && (GetCorrectSignature(function).Replace(" (", "(") == methodSignature))
                    //    if ((function != null) && (function.get_Prototype((int)(vsCMPrototype.vsCMPrototypeParamTypes | vsCMPrototype.vsCMPrototypeType)).Replace(" (", "(") == methodSignature))
                    return function;
            }
            return null;
        }

        /// <summary>
        /// Find a method of given signature
        /// </summary>
        /// <param name="codeClass">Class holding a method</param>
        /// <param name="methodSignature">String that will be compared to the 
        /// <c>function.get_Prototype((int)(vsCMPrototype.vsCMPrototypeParamTypes|vsCMPrototype.vsCMPrototypeType))</c> 
        /// of the function members of type <paramref name="codeType"/></param>
        /// <returns></returns>
        public static CodeFunction2 FindMethod(CodeClass2 codeClass, string methodSignature)
        {
            return FindMethod(codeClass as CodeType, methodSignature);
        }

        /// <summary>
        /// Find a method of given signature
        /// </summary>
        /// <param name="codeInterface">Interface holding a method</param>
        /// <param name="methodSignature">String that will be compared to the 
        /// <c>function.get_Prototype((int)(vsCMPrototype.vsCMPrototypeParamTypes|vsCMPrototype.vsCMPrototypeType))</c> 
        /// of the function members of type <paramref name="codeType"/></param>
        /// <returns></returns>
        public static CodeFunction2 FindMethod(CodeInterface codeInterface, string methodSignature)
        {
            return FindMethod(codeInterface as CodeType, methodSignature);
        }


        /// <summary>
        /// Find a method by its name and signature
        /// </summary>
        /// <param name="classe">class in which we are looking for a named method of given signature</param>
        /// <param name="name">Name of the method to find</param>
        /// <param name="signature">Signature of the method to look for</param>
        /// <param name="nonStaticOnly">requests only non static methods</param>
        /// <returns>a <c>CodeFunction2</c> if a method of given name and signature exists in the class
        /// or otherwise <c>null</c></returns>
        public static CodeFunction2 FindMethod(CodeClass2 classe, string name, string signature, bool nonStaticOnly)
        {
            int begin = signature.IndexOf('(');
            int end = signature.IndexOf(')');
            string parameterSignature;
            if ((begin >= 0) && (end >= 0))
                parameterSignature = signature.Substring(begin + 1, end - begin - 1);
            else
                parameterSignature = signature;

            // Parameter signature -> parameter types (with correct processing of no parameters)
            parameterSignature = parameterSignature.Replace(" ", "");
            string[] parameterTypes;
            if (string.IsNullOrEmpty(parameterSignature))
                parameterTypes = new string[0];
            else
                parameterTypes = parameterSignature.Replace(" ", "").Split(',');

            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    CodeFunction2 method = element as CodeFunction2;
                    if (method.Name != name)
                        continue;
                    if (method.IsShared && nonStaticOnly)
                        continue;
                    if (method.Parameters.Count != parameterTypes.Length)
                        continue;
                    bool ok = true;
                    for (int i = 0; i < parameterTypes.Length; ++i)
                        if (!(method.Parameters.Item(i + 1) as CodeParameter).Type.AsString.EndsWith(parameterTypes[i]))
                        {
                            ok = false;
                            break;
                        }
                    if (ok)
                        return method;
                }
            return null;
        }


        /// <summary>
        /// Find a constructor of given parameters in the class
        /// </summary>
        /// <param name="classe">class in which we are looking for a named method of given signature</param>
        /// <param name="parameterTypes">types of the parameters to look for</param>
        /// <returns>a <c>CodeFunction2</c> if a constructor of given signature exists in the class
        /// or otherwise <c>null</c></returns>
        public static CodeFunction2 FindConstructor(CodeClass2 classe, params string[] parameterTypes)
        {
            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    // Must be a constructor
                    CodeFunction2 constructor = element as CodeFunction2;
                    if (constructor.FunctionKind != vsCMFunction.vsCMFunctionConstructor)
                        continue;

                    // With the right number of parameters
                    if (constructor.Parameters.Count != parameterTypes.Length)
                        continue;

                    // And the parameter types must match
                    int i = 0;
                    foreach (CodeParameter parameter in constructor.Parameters)
                        if (constructor.Type.AsString != parameterTypes[i])
                            continue;
                        else
                            i++;

                    // At that point we have found the constructor
                    return constructor;
                }
            return null;
        }


        /// <summary>
        /// Find the fist attribute on a class, which has a given full type name
        /// </summary>
        /// <param name="class">A code class</param>
        /// <param name="attributeFullTypeName">The fully qualified name of an attribute that the
        /// class might hold</param>
        /// <returns>Attribute found</returns>
        public static CodeAttribute2 FindFirstAttribute(CodeClass @class, string attributeFullTypeName)
        {
            return FindFirstAttribute(@class as CodeType, attributeFullTypeName);
        }

        /// <summary>
        /// Return the first attribute of given full type name
        /// </summary>
        /// <param name="class">Class holding the attribute</param>
        /// <param name="attributeFullTypeName">Full type Name of the attribute to look for</param>
        /// <returns>The attribute, if found, otherwise <c>null</c></returns>
        public static CodeAttribute2 FindFirstAttribute(CodeType @class, string attributeFullTypeName)
        {
            return FindFirstAttribute(attributeFullTypeName, @class.Attributes);
        }


        /// <summary>
        /// Return the first attribute of given full type name
        /// </summary>
        /// <param name="method">Method holding the attribute</param>
        /// <param name="attributeFullTypeName">Full type Name of the attribute to look for</param>
        /// <returns>The attribute, if found, otherwise <c>null</c></returns>
        public static CodeAttribute2 FindFirstAttribute(CodeFunction2 method, string attributeFullTypeName)
        {
            return FindFirstAttribute(attributeFullTypeName, method.Attributes);
        }

        /// <summary>
        /// Return the first attribute of given full type name
        /// </summary>
        /// <param name="variable">Field holding the attribute</param>
        /// <param name="attributeFullTypeName">Full type Name of the attribute to look for</param>
        /// <returns>The attribute, if found, otherwise <c>null</c></returns>
        public static CodeAttribute2 FindFirstAttribute(CodeVariable variable, string attributeFullTypeName)
        {
            return FindFirstAttribute(attributeFullTypeName, variable.Attributes);
        }

        /// <summary>
        /// Return the first attribute of given full type name
        /// </summary>
        /// <param name="property">Property holding the attribute</param>
        /// <param name="attributeFullTypeName">Full type Name of the attribute to look for</param>
        /// <returns>The attribute, if found, otherwise <c>null</c></returns>
        public static CodeAttribute2 FindFirstAttribute(CodeProperty property, string attributeFullTypeName)
        {
            return FindFirstAttribute(attributeFullTypeName, property.Attributes);
        }

        /// <summary>
        /// Return the first attribute of given full type name
        /// </summary>
        /// <param name="e">CodeElement holding the attribute</param>
        /// <param name="attributeFullTypeName">Full type Name of the attribute to look for</param>
        /// <returns>The attribute, if found, otherwise <c>null</c></returns>
        public static CodeAttribute2 FindFirstAttribute(CodeElement e, string attributeFullTypeName)
        {
            if (e is CodeVariable)
                return FindFirstAttribute(e as CodeVariable, attributeFullTypeName);
            else if (e is CodeProperty)
                return FindFirstAttribute(e as CodeProperty, attributeFullTypeName);
            else if (e is CodeType)
                return FindFirstAttribute(e as CodeType, attributeFullTypeName);
            else
                throw new NotImplementedException("FindFistAttribute is only supported for CodeVariable, CodeProperty, CodeType");
        }

        /// <summary>
        /// Find the first attribute of given fully qualified type name among a collection of attributes.
        /// </summary>
        /// <param name="attributeFullTypeName">fully qualified name of a type which is an attribute we are searching</param>
        /// <param name="attributes">Collections of attributes</param>
        /// <returns>The first attribute which fully qualified name if provided (ending with Attribute or not)</returns>
        private static CodeAttribute2 FindFirstAttribute(string attributeFullTypeName, CodeElements attributes)
        {
            CodeAttribute2 attribute = null;
            foreach (CodeElement element in attributes)
                if (element is CodeAttribute2)
                {
                    attribute = element as CodeAttribute2;


                    // If the using necessary for the attribute is not present, then, the attribute.Fullname
                    // will fail.
                    string fullname;
                    try
                    {
                        fullname = attribute.FullName;
                    }
                    catch (Exception)
                    {
                        fullname = attribute.Name;
                    }


                    // First try with full name including trailing "Attribute"
                    if (fullname == attributeFullTypeName)
                        return attribute;

                    // Second try with full name excluding trailing "Attribute"
                    // Apparently the CodeModel does considers the full name does not have it !!!
                    if (attributeFullTypeName.EndsWith("Attribute"))
                        if (fullname == attributeFullTypeName.Substring(0, attributeFullTypeName.Length - "Attribute".Length))
                            return attribute;
                }
            return null;
        }

        /// <summary>
        /// Find a nested class of given name
        /// </summary>
        /// <param name="codeClass">class in which we are looking for a nested class</param>
        /// <param name="name">Name of the nested class to find</param>
        /// <returns>a <c>CodeClass2</c> if a nested class of given name exists in the class
        /// or otherwise <c>null</c></returns>
        public static CodeClass2 FindNestedClass(CodeClass2 codeClass, string name)
        {
            foreach (CodeElement element in GetAllMembers(codeClass))
                if ((element.Name == name) && (element.Kind == vsCMElement.vsCMElementClass))
                    return element as CodeClass2;
            return null;
        }


        /// <summary>
        /// Find a nested interface of given name
        /// </summary>
        /// <param name="codeClass">class in which we are looking for a nested interface</param>
        /// <param name="name">Name of the nested interface to find</param>
        /// <returns>a <c>CodeInterface</c> if a nested interface of given name exists in the class
        /// or otherwise <c>null</c></returns>
        public static CodeInterface FindNestedInterface(CodeClass2 codeClass, string name)
        {
            foreach (CodeElement element in GetAllMembers(codeClass))
                if ((element.Name == name) && (element.Kind == vsCMElement.vsCMElementInterface))
                    return element as CodeInterface;
            return null;
        }


        /// <summary>
        /// Get the base class fully qualified name of the form baseClass&lt;T1, T2&gt;
        /// </summary>
        /// <param name="class">Class for which we want the base class under the form of an
        /// instanciation of a generic class with 2 generic parameters</param>
        /// <param name="T1">First generic parameter</param>
        /// <param name="T2">Second generic parameter</param>
        /// <returns></returns>
        public static string GetGeneric2BaseClass(CodeClass2 @class, out string T1, out string T2)
        {
            T1 = T2 = string.Empty;

            if (@class.Bases.Count > 0)
            {
                CodeClass2 baseClass = @class.Bases.Item(1) as CodeClass2;
                string baseClassName = baseClass.FullName;
                int begin = baseClassName.IndexOf('<') + 1;
                int middle = baseClassName.IndexOf(',');
                int end = baseClassName.IndexOf('>') - 1;
                if ((begin >= 1) && (middle >= 0) && (end >= 0))
                {
                    T1 = baseClassName.Substring(begin, middle - begin);
                    T2 = baseClassName.Substring(middle + 1, end - middle);
                    return baseClassName.Substring(0, begin - 1);
                }
            }
            return string.Empty;
        }



        /// <summary>
        /// Ensures a class has a base class of the form baseClass&lt;T1, T2&gt;
        /// </summary>
        /// <param name="classe">class we want to ensure the base class of</param>
        /// <param name="generic2BaseClass">Base class (generic fully qualified name)</param>
        /// <param name="T1">First generic parameter</param>
        /// <param name="T2">Second generic parameter</param>
        public static void EnsureGeneric2BaseClass(CodeClass2 classe, string generic2BaseClass, string T1, string T2)
        {
            CodeModel codeModel = classe.ProjectItem.ContainingProject.CodeModel as CodeModel;
            CodeType firstGenericParameter = codeModel.CodeTypeFromFullName(T1) as CodeType;
            CodeType secondGenericParameter = codeModel.CodeTypeFromFullName(T2) as CodeType;
            CodeType baseClass = codeModel.CodeTypeFromFullName(generic2BaseClass) as CodeType;
            if ((baseClass != null) && (firstGenericParameter != null) && (secondGenericParameter != null))
            {
                // Do not change things are already are correct, so test for change !
                string currentBaseClassFullName = classe.Bases.Item(1).FullName;
                string baseClassFullName = baseClass.FullName.Substring(0, baseClass.FullName.IndexOf('<')) + "<" + firstGenericParameter.FullName + "," + secondGenericParameter.FullName + ">";
                if (currentBaseClassFullName != baseClassFullName)
                {
                    string baseName = KnownCodeTypes.GetShortTypeName(baseClass) + "<" + KnownCodeTypes.GetShortTypeName(firstGenericParameter) + "," + KnownCodeTypes.GetShortTypeName(secondGenericParameter) + ">";
                    EnsureUsings(classe, baseClass.Namespace.FullName, firstGenericParameter.Namespace.FullName, secondGenericParameter.Namespace.FullName);
                    EnsureBaseClass(classe, baseName);
                }
            }
        }


        /// <summary>
        /// Ensures the project exists
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public Project EnsureProject(Solution solution, string name)
        {
            // Returns the project of given name, if it exists
            for (int i = 1; i <= solution.Projects.Count; ++i)
            {
                Project project = solution.Projects.Item(i);
                if (project.Name == name)
                    return project;
            }

            Solution2 solution2 = solution as Solution2;
            string projectTemplate = solution2.GetProjectTemplate("ClassLibrary.zip", "CSharp");
            string projectDirectory = Path.GetDirectoryName(solution.FileName) + Path.DirectorySeparatorChar + name;
            string projectFileName = projectDirectory + Path.DirectorySeparatorChar + name + ".csproj";
            Project createdProject;
            if (File.Exists(projectFileName))
                createdProject = solution.AddFromFile(projectFileName, true);
            else
                createdProject = solution.AddFromTemplate(projectTemplate, projectDirectory, name, true);
            if (createdProject != null)
                return createdProject;
            else
                for (int i = 1; i <= solution.Projects.Count; ++i)
                {
                    Project project = solution.Projects.Item(i);
                    if (project.Name == name)
                        return project;
                }
            return null;
        }


        /// <summary>
        /// Ensures a source file exists
        /// </summary>
        /// <param name="project"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public ProjectItem EnsureSourcefile(Project project, string name)
        {
            if (project == null)
                return null;

            ProjectItem projectItem = project.DTE.Solution.FindProjectItem(name);
            if (projectItem == null)
            {
                string filename = Path.GetDirectoryName(project.FileName)
                 + Path.DirectorySeparatorChar
                 + name;
                StreamWriter w = File.CreateText(filename);
                w.Close();
                projectItem = project.ProjectItems.AddFromFile(filename);
            }
            return projectItem;
        }



        /// <summary>
        /// Creates a namespace in a project item file if necessary
        /// </summary>
        /// <param name="projectItem">ProjectItem file</param>
        /// <param name="name">Name of the namespace to create if it doest not exist</param>
        /// <returns>The namespace named <c>name</c></returns>
        static public CodeNamespace EnsureNamespace(ProjectItem projectItem, string name)
        {
            if (projectItem == null)
                return null;

            FileCodeModel fileCodeModel = projectItem.FileCodeModel;
            if (fileCodeModel == null)
                return null;

            foreach (CodeElement element in fileCodeModel.CodeElements)
                if ((element is CodeNamespace) && (element.Name == name))
                    return element as CodeNamespace;

            return fileCodeModel.AddNamespace(name, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentNamespace"></param>
        /// <param name="name"></param>
        static public CodeNamespace EnsureNamespace(CodeNamespace parentNamespace, string name)
        {
            if (parentNamespace == null)
                return null;

            foreach (CodeElement element in parentNamespace.Members)
                if ((element is CodeNamespace) && (element.Name == name))
                    return element as CodeNamespace;

            return parentNamespace.AddNamespace(name, null);
        }

        /// <summary>
        /// Adds a class to the namespace
        /// </summary>
        /// <param name="aNamespace">Namespace</param>
        /// <param name="name">Name of the class to ensure existence</param>
        /// <returns>The named class</returns>
        static public CodeClass EnsureClass(CodeNamespace aNamespace, string name)
        {
            if (aNamespace == null)
                return null;

            foreach (CodeElement element in aNamespace.Members)
                if ((element is CodeClass) && (element.Name == name))
                    return element as CodeClass;

            return aNamespace.AddClass(name, null, null, null, vsCMAccess.vsCMAccessDefault);
        }


        /// <summary>
        /// Ensures the existance of the interface
        /// </summary>
        /// <param name="aNamespace">A namespace</param>
        /// <param name="name">Name of the interface to create</param>
        /// <returns>An interface</returns>
        public static CodeInterface EnsureInterface(CodeNamespace aNamespace, string name)
        {
            if (aNamespace == null)
                return null;

            foreach (CodeElement element in aNamespace.Members)
                if ((element is CodeInterface) && (element.Name == name))
                    return element as CodeInterface;

            return aNamespace.AddInterface(name, null, null, vsCMAccess.vsCMAccessDefault);
        }


        /// <summary>
        /// Ensures the nested class is created
        /// </summary>
        /// <param name="classe"></param>
        /// <param name="name"></param>
        /// <param name="baseClass"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public static CodeClass2 EnsureNestedClass(CodeClass2 classe, string name, string baseClass, vsCMAccess visibility)
        {
            CodeClass2 nestedClass = FindNestedClass(classe, name);
            if (nestedClass == null)
                nestedClass = classe.AddClass(name, -1, baseClass, null, visibility) as CodeClass2;
            else
                EnsureBaseClass(nestedClass, baseClass);
            return nestedClass;
        }


        /// <summary>
        /// Ensures the nested interface
        /// </summary>
        /// <param name="classe">Class in which a nested interface should be looked for or created</param>
        /// <param name="name">Name of the nested interface to look for or create</param>
        /// <param name="baseInterfaceName">Full type name of the base interface (if any)</param>
        /// <param name="visibility">Visibility of the interface</param>
        /// <param name="isNew">Should the interface be 'new'</param>
        /// <returns></returns>
        public static CodeInterface EnsureNestedInterface(CodeClass2 classe, string name, string baseInterfaceName, vsCMAccess visibility, bool isNew)
        {
            CodeInterface baseInterface = classe.ProjectItem.ContainingProject.CodeModel.CodeTypeFromFullName(baseInterfaceName) as CodeInterface;
            if (baseInterface == null)
                return null;
            CodeInterface nestedInterface = FindNestedInterface(classe, name);
            if (nestedInterface == null)
            {
                classe.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint().Insert("public " + (isNew ? "new " : "") + "interface " + name + " : " + KnownCodeTypes.GetShortTypeName(baseInterface as CodeType) + "\r\n{\r\n}");
                nestedInterface = FindNestedInterface(classe, name);
                nestedInterface.Access = visibility;
            }
            else
            {
                if (nestedInterface.Bases.Count > 0)
                {
                    CodeInterface firstBase = null;
                    try
                    {
                        object theFirstBase = nestedInterface.Bases.Item(1);
                        firstBase = theFirstBase as CodeInterface;
                        if (firstBase == null)
                        {
                            nestedInterface.RemoveBase(theFirstBase);
                            nestedInterface.AddBase(KnownCodeTypes.GetShortTypeName(baseInterface as CodeType), 0);
                        }
                        else
                            if (firstBase.FullName != baseInterface.FullName)
                            {
                                nestedInterface.RemoveBase(firstBase);
                                nestedInterface.AddBase(KnownCodeTypes.GetShortTypeName(baseInterface as CodeType), 0);
                            }
                    }

                    // the first interface might be wrong (type does not exist)
                    catch (Exception)
                    {
                        try
                        {
                            nestedInterface.RemoveBase(1);
                        }
                        catch (Exception)
                        {
                        }
                        nestedInterface.AddBase(KnownCodeTypes.GetShortTypeName(baseInterface as CodeType), 0);
                    }

                }
            }
            return nestedInterface;
        }



        /// <summary>
        /// Ensure that a class derives from a given base class
        /// </summary>
        /// <param name="class"></param>
        /// <param name="baseClass"></param>
        public static void EnsureBaseClass(CodeClass2 @class, string baseClass)
        {
            CodeModel codeModel = @class.ProjectItem.ContainingProject.CodeModel as CodeModel;
            CodeClass2 codeClass = codeModel.CodeTypeFromFullName(baseClass) as CodeClass2;
            //   if (codeClass == null)
            //    return;

            if (@class.Bases.Count == 0)
            {
                if (codeClass != null)
                    @class.AddBase(KnownCodeTypes.GetShortTypeName(codeClass as CodeType), 0);
                else
                    @class.AddBase(baseClass, 0);
                EnsureUsings(@class.ProjectItem.FileCodeModel as FileCodeModel2, codeClass.Namespace.FullName);
            }
            else
            {
                object theBaseClass = @class.Bases.Item(1);
                CodeClass2 baseClassAsCodeClass2 = theBaseClass as CodeClass2;
                if ((baseClassAsCodeClass2 == null) || (baseClassAsCodeClass2.FullName != baseClass))
                {
                    try
                    {
                        @class.RemoveBase(theBaseClass);
                    }
                    catch (Exception)
                    {
                    }
                    if (codeClass != null)
                        @class.AddBase(KnownCodeTypes.GetShortTypeName(codeClass as CodeType), 0);
                    else
                        @class.AddBase(baseClass, 0);
                }
                if (codeClass != null)
                    EnsureUsings(@class.ProjectItem.FileCodeModel as FileCodeModel2, codeClass.Namespace.FullName);
            }
        }

        /// <summary>
        /// Ensures that a class implements a given interface
        /// </summary>
        /// <param name="classe"></param>
        /// <param name="interfaceFullName"></param>
        public static void EnsureClassImplementsInterface(CodeClass2 classe, string interfaceFullName)
        {
            // Interface not known
            CodeInterface iface = classe.ProjectItem.ContainingProject.CodeModel.CodeTypeFromFullName(interfaceFullName) as CodeInterface;
            if (iface == null)
                return;

            // Already implemented
            foreach (CodeInterface implemented in classe.ImplementedInterfaces)
                if (implemented.FullName == iface.FullName)
                    return;

            // Interface implementation
            EnsureUsings(classe.ProjectItem.FileCodeModel as FileCodeModel2, iface.Namespace.FullName);
            CodeInterface implementedInterface = classe.AddImplementedInterface(KnownCodeTypes.GetShortTypeName(iface as CodeType), -1);
        }

        /// <summary>
        /// Ensures that a class does no longer implement an interface
        /// </summary>
        /// <param name="codeClass"></param>
        /// <param name="interfaceFullName"></param>
        public static void EnsureClassDoesNotImplementsInterface(CodeClass2 codeClass, string interfaceFullName)
        {
            // Interface not known
            CodeInterface iface = codeClass.ProjectItem.ContainingProject.CodeModel.CodeTypeFromFullName(interfaceFullName) as CodeInterface;
            if (iface == null)
                return;

            // Already implemented
            foreach (CodeInterface implemented in codeClass.ImplementedInterfaces)
                if (implemented.FullName == iface.FullName)
                {
                    codeClass.RemoveInterface(implemented);
                    return;
                };
        }


        /// <summary>
        /// Ensures an interface inherits from another interface
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="baseInterfaceFullName"></param>
        public static void EnsureBaseInterface(CodeInterface @interface, string baseInterfaceFullName)
        {
            CodeModel codeModel = @interface.ProjectItem.ContainingProject.CodeModel as CodeModel;
            CodeInterface codeInterface = codeModel.CodeTypeFromFullName(baseInterfaceFullName) as CodeInterface;
            if ((codeInterface == null) || (codeInterface.FullName != baseInterfaceFullName))
                return;
            if (@interface.Bases.Count == 0)
            {
                @interface.AddBase(KnownCodeTypes.GetShortTypeName(codeInterface as CodeType), 0);
                EnsureUsings(@interface.ProjectItem.FileCodeModel as FileCodeModel2, codeInterface.Namespace.FullName);
            }
            else
            {
                object theBaseInterface = @interface.Bases.Item(1);
                try
                {
                    @interface.RemoveBase(theBaseInterface);
                }
                catch (Exception)
                {
                }
                @interface.AddBase(KnownCodeTypes.GetShortTypeName(codeInterface as CodeType), 0);
                EnsureUsings(@interface.ProjectItem.FileCodeModel as FileCodeModel2, codeInterface.Namespace.FullName);
            }

        }



        /// <summary>
        /// Ensures a class holds an attribute, with specified arguments.
        /// </summary>
        /// <param name="classe">Class which should hold the attribute</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="arguments">Arguments of the attribute</param>
        /// <returns>The attribute preexistant or newly added</returns>
        public static CodeAttribute EnsureAttribute(CodeClass2 classe, string attributeName, params string[] arguments)
        {
            return EnsureAttribute(classe as CodeType, attributeName, arguments);
        }


        /// <summary>
        /// Ensures an interface holds an attribute, with specified arguments.
        /// </summary>
        /// <param name="iface">Interface which should hold the attribute</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="arguments">Arguments of the attribute</param>
        /// <returns>The attribute preexistant or newly added</returns>
        public static CodeAttribute EnsureAttribute(CodeInterface iface, string attributeName, params string[] arguments)
        {
            return EnsureAttribute(iface as CodeType, attributeName, arguments);
        }

        /// <summary>
        /// Ensures that the attribute is added to the class
        /// </summary>
        /// <param name="classe">Class on which to create the attribute</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="arguments">Arguments of the attribute</param>
        /// <example>
        /// <code>
        ///    EnsureAttribute(classe, "ModelInformation", 
        ///                 new string[] { "Provider", concept.Provider, "Domain", concept.Domain }
        ///                 );
        /// </code>
        /// </example>
        public static CodeAttribute EnsureAttribute(CodeType classe, string attributeName, params string[] arguments)
        {
            if (classe == null)
                return null;

            if (classe.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;

            Dictionary<string, string> argumentDictionary = new Dictionary<string, string>();
            for (int i = 0; i < arguments.Length; i += 2)
                argumentDictionary.Add(arguments[i], arguments[i + 1]);

            CodeAttribute2 attribute = null;
            foreach (CodeElement element in classe.Attributes)
            {
                if (element.Name == attributeName)
                {
                    attribute = element as CodeAttribute2;
                    break;
                }
            }
            if (attribute == null)
                attribute = classe.AddAttribute(attributeName, "", -1) as CodeAttribute2;

            foreach (CodeAttributeArgument argument in attribute.Arguments)
            {
                string argumentName = argument.Name;
                if (argumentDictionary.ContainsKey(argumentName))
                {
                    string newValue = argumentDictionary[argumentName];
                    if (argument.Value != newValue)
                        argument.Value = newValue;
                    argumentDictionary.Remove(argumentName);
                }
            }
            foreach (string remainingArgumentName in argumentDictionary.Keys)
                attribute.AddArgument(argumentDictionary[remainingArgumentName], remainingArgumentName, -1);
            return attribute;
        }


        /// <summary>
        /// Ensures that the attribute is added to the method
        /// </summary>
        /// <param name="codeMethod">Method on which to create the attribute</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="arguments">Arguments of the attribute</param>
        /// <example>
        /// <code>
        ///    EnsureAttribute(method, "ModelInformation", 
        ///                 "Provider", concept.Provider, "Domain", concept.Domain }
        ///                 );
        /// </code>
        /// </example>
        public static CodeAttribute EnsureAttribute(CodeFunction2 codeMethod, string attributeName, params string[] arguments)
        {
            if (codeMethod == null)
                return null;

            if (codeMethod.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;

            Dictionary<string, string> argumentDictionary = new Dictionary<string, string>();
            for (int i = 0; i < arguments.Length; i += 2)
                argumentDictionary.Add(arguments[i], arguments[i + 1]);

            CodeAttribute2 attribute = null;
            foreach (CodeElement element in codeMethod.Attributes)
                if (element.Name == attributeName)
                    attribute = element as CodeAttribute2;
            if (attribute == null)
                attribute = codeMethod.AddAttribute(attributeName, "", -1) as CodeAttribute2;

            foreach (CodeAttributeArgument argument in attribute.Arguments)
            {
                string argumentName = argument.Name;
                if (argumentDictionary.ContainsKey(argumentName))
                {
                    string newValue = argumentDictionary[argumentName];
                    if (argument.Value != newValue)
                        argument.Value = newValue;
                    argumentDictionary.Remove(argumentName);
                }
            }
            foreach (string remainingArgumentName in argumentDictionary.Keys)
                attribute.AddArgument(argumentDictionary[remainingArgumentName], remainingArgumentName, -1);
            return attribute;
        }


        /// <summary>
        /// Ensures the existance of an attribute on a field.
        /// </summary>
        /// <param name="field">Field on which to add the attribute.</param>
        /// <param name="attributeName">Name of the attribute to add.</param>
        /// <param name="arguments">Arguments of the attribute</param>
        /// <returns>The attribute if it could be added</returns>
        public static CodeAttribute EnsureAttribute(CodeVariable field, string attributeName, params string[] arguments)
        {
            if (field == null)
                return null;

            if (field.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;

            Dictionary<string, string> argumentDictionary = new Dictionary<string, string>();
            for (int i = 0; i < arguments.Length; i += 2)
                argumentDictionary.Add(arguments[i], arguments[i + 1]);

            CodeAttribute2 attribute = null;
            foreach (CodeElement element in field.Attributes)
                if (element.Name == attributeName)
                    attribute = element as CodeAttribute2;
            if (attribute == null)
                attribute = field.AddAttribute(attributeName, "", -1) as CodeAttribute2;

            foreach (CodeAttributeArgument argument in attribute.Arguments)
            {
                string argumentName = argument.Name;
                if (argumentDictionary.ContainsKey(argumentName))
                {
                    string newValue = argumentDictionary[argumentName];
                    if (argument.Value != newValue)
                        argument.Value = newValue;
                    argumentDictionary.Remove(argumentName);
                }
            }
            foreach (string remainingArgumentName in argumentDictionary.Keys)
                attribute.AddArgument(argumentDictionary[remainingArgumentName], remainingArgumentName, -1);
            return attribute;
        }



        /// <summary>
        /// Ensures the existance of an attribute on a property.
        /// </summary>
        /// <param name="property">Property on which to add the attribute.</param>
        /// <param name="attributeName">Name of the attribute to add.</param>
        /// <param name="arguments">Arguments of the attribute</param>
        /// <returns>The attribute if it could be added</returns>
        public static CodeAttribute EnsureAttribute(CodeProperty property, string attributeName, params string[] arguments)
        {
            if (property == null)
                return null;

            if (property.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;

            Dictionary<string, string> argumentDictionary = new Dictionary<string, string>();
            for (int i = 0; i < arguments.Length; i += 2)
                argumentDictionary.Add(arguments[i], arguments[i + 1]);

            CodeAttribute2 attribute = null;
            foreach (CodeElement element in property.Attributes)
                if (element.Name == attributeName)
                    attribute = element as CodeAttribute2;
            if (attribute == null)
                attribute = property.AddAttribute(attributeName, "", -1) as CodeAttribute2;

            foreach (CodeAttributeArgument argument in attribute.Arguments)
            {
                string argumentName = argument.Name;
                if (argumentDictionary.ContainsKey(argumentName))
                {
                    string newValue = argumentDictionary[argumentName];
                    if (argument.Value != newValue)
                        argument.Value = newValue;
                    argumentDictionary.Remove(argumentName);
                }
            }
            foreach (string remainingArgumentName in argumentDictionary.Keys)
                attribute.AddArgument(argumentDictionary[remainingArgumentName], remainingArgumentName, -1);
            return attribute;
        }

        /// <summary>
        /// Ensures that the class is created
        /// </summary>
        /// <param name="codeModel">CodeModel</param>
        /// <param name="sourcePath">Source in which class is created</param>
        /// <param name="theNamespace">Namespace</param>
        /// <param name="className">Class name</param>
        /// <param name="baseClass">Base class</param>
        /// <param name="comment">Comments</param>
        /// <param name="template">Template code from which the class can be created (can be null or empty)</param>
        public static CodeClass2 EnsureClass(CodeModel codeModel, string sourcePath, string theNamespace, string className, string baseClass, string comment, string template)
        {
            CodeClass2 classe = codeModel.CodeTypeFromFullName(theNamespace + "." + className) as CodeClass2;

            // There are four cases :
            if (classe != null)
            {

                // - 1st case : The class exists, but is external to the project : we are done
                if (classe.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal)
                    return classe;

                // - 2nd case :  The class exists and is in the right file : we are done

                // - 3rd case : The class exists in the project, but is not in the right file : we must relocate it
                if ((classe.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject) && (classe.StartPoint.Parent.Parent.FullName != sourcePath))
                {
                    // For this :
                    // - we save the code
                    CodeNamespace enclosingNamespace = classe.Parent as CodeNamespace;
                    FileCodeModel2 fileCodeModel = classe.ProjectItem.FileCodeModel as FileCodeModel2;
                    EditPoint editPoint = classe.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes).CreateEditPoint();
                    TextPoint endOfClass = classe.GetEndPoint(vsCMPart.vsCMPartBody);
                    string code = editPoint.GetText(endOfClass);

                    // - We delete the class from its current file
                    editPoint.Delete(endOfClass);

                    // - We recreate it in its new file
                    classe = EnsureClass(codeModel, sourcePath, theNamespace, className, baseClass, comment, template);

                    // - And we replace the code by the previous one (in order to keep the customizations)
                    editPoint = classe.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes).CreateEditPoint();
                    endOfClass = classe.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);
                    editPoint.ReplaceText(endOfClass, code, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);

                    // If namespace that used to contain the class in the previous file is now empty, we remove it
                    if ((enclosingNamespace != null) && (enclosingNamespace.Members.Count == 0))
                        RemoveNamespace(enclosingNamespace);
                }

                // Ensure the base class is correct
                if (!string.IsNullOrEmpty(baseClass))
                    if (classe.Bases.Count == 1)
                        if (classe.Bases.Item(1).Name != (baseClass = baseClass.Replace(" ", "")))
                        {
                            classe.RemoveBase(classe.Bases.Item(1));
                            classe.AddBase(baseClass, 0);
                        }

                // Ensure the comment is correct
                if (!string.IsNullOrEmpty(comment))
                    classe.DocComment = GetComment(comment);


                return classe;
            }


              // - 3rd case : The class does not exists : we must create it
            else
            {
                if (!string.IsNullOrEmpty(theNamespace))
                {
                    CodeNamespace ns = EnsureNamespace(codeModel, theNamespace, sourcePath);

                    // Create the class
                    try
                    {
                        if (!string.IsNullOrEmpty(template))
                        {
                            ns.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint().Insert(template);
                            classe = codeModel.CodeTypeFromFullName(theNamespace + "." + className) as CodeClass2;
                        }
                        else
                            classe = ns.AddClass(className, 0, new object[] { baseClass }, new object[0], EnvDTE.vsCMAccess.vsCMAccessPublic) as CodeClass2;
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        string message = ex.Message;
                    }
                }
                else
                {
                    classe = codeModel.AddClass(className, sourcePath, 0, new object[] { baseClass }, new object[0], EnvDTE.vsCMAccess.vsCMAccessPublic) as CodeClass2;
                }

                if (classe != null)
                    classe.DocComment = GetComment(comment);
                return classe;
            }
        }

        private static void RemoveNamespace(CodeNamespace enclosingNamespace)
        {
            object parent = enclosingNamespace.Parent;
            if (parent is CodeNamespace)
                (parent as CodeNamespace).Remove(enclosingNamespace);
            else if (parent is FileCodeModel)
                (parent as FileCodeModel).Remove(enclosingNamespace);
        }

        /// <summary>
        /// Get the comments
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static string GetComment(string comment)
        {
            return "<doc>\r\n<summary>\r\n" + comment + "\r\n</summary>\r\n</doc>";
        }


        /// <summary>
        /// Ensures that the namespace exists in the required file
        /// </summary>
        /// <param name="codeModel"></param>
        /// <param name="theNamespace"></param>
        /// <param name="sourcePath"></param>
        private static CodeNamespace EnsureNamespace(CodeModel codeModel, string theNamespace, string sourcePath)
        {
            foreach (CodeElement element in codeModel.CodeElements)
                if ((element.Kind == vsCMElement.vsCMElementNamespace)
                 && element.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject
                 && theNamespace.StartsWith(element.FullName))
                    if (theNamespace != element.FullName)
                        return EnsureNamespace(element as CodeNamespace, theNamespace, sourcePath);
                    else if (sourcePath == element.ProjectItem.get_FileNames(1))
                        return element as CodeNamespace;

            // Not found
            ProjectItem projectItem = ((codeModel.DTE as DTE2).Solution.FindProjectItem(sourcePath));
            if ((projectItem != null) && (File.Exists(sourcePath)) && (projectItem.FileCodeModel != null))
                return projectItem.FileCodeModel.AddNamespace(theNamespace, -1);
            else
                return codeModel.AddNamespace(theNamespace, sourcePath, 0);

        }



        private static CodeNamespace EnsureNamespace(CodeNamespace ns, string theNamespace, string sourcePath)
        {
            if (ns.FullName == theNamespace)
                return ns;
            else foreach (CodeElement element in ns.Members)
                    if ((element.Kind == vsCMElement.vsCMElementNamespace) && theNamespace.StartsWith(element.FullName))
                        return EnsureNamespace(element as CodeNamespace, theNamespace, sourcePath);

            return ns.AddNamespace(theNamespace, theNamespace.Substring(ns.FullName.Length + 1, -1));
        }

        /// <summary>
        /// Find a specified constructor (and create it if necessary)
        /// </summary>
        /// <param name="classe">Class in which the constructor should be looked for or created if necessary</param>
        /// <param name="parameters">Parameters of the constructor (name, type)*</param>
        /// <param name="visibility">Vibility for the constructor</param>
        /// <param name="baseDelegation">Code of delegation to base(...) or this(...)</param>
        /// <returns></returns>
        public static CodeFunction2 EnsureConstructor(CodeClass2 classe, string[] parameters, vsCMAccess visibility, string baseDelegation)
        {
            CodeFunction2 constructeur = null;

            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    CodeFunction2 function = element as CodeFunction2;
                    if (function.FunctionKind != vsCMFunction.vsCMFunctionConstructor)
                        continue;
                    if (function.Parameters.Count != parameters.Length / 2)
                        continue;
                    int i = 0;
                    foreach (CodeParameter parameter in function.Parameters)
                        if (parameter.Type.AsString != parameters[i])
                            continue;
                        else
                            i += 2;

                    // At that point we have found the constructor
                    return function;
                }

            // We have not found the constructor
            constructeur = classe.AddFunction(classe.Name, vsCMFunction.vsCMFunctionConstructor, null, null, visibility, null) as CodeFunction2;
            for (int i = 0; i < parameters.Length; i += 2)
                constructeur.AddParameter(parameters[i], parameters[i + 1], parameters.Length);

            if (baseDelegation.Contains("base"))
            {
                EditPoint2 point = constructeur.GetStartPoint(vsCMPart.vsCMPartHeader).CreateEditPoint() as EditPoint2;
                string s = point.GetText(constructeur.GetStartPoint(vsCMPart.vsCMPartBody));
                if (!s.Contains("base("))
                    point.CharRight(s.IndexOf(')') + 1);
                point.Insert(" : " + baseDelegation);
            }
            return constructeur;
        }




        /// <summary>
        /// Ensures the interface has a method with given signature
        /// </summary>
        /// <param name="codeInterface">Interface which should have a method of given signature</param>
        /// <param name="signature">Signature of the method to ensure</param>
        /// <returns>The method (preexisting or newly created)</returns>
        public static CodeFunction2 EnsureMethod(CodeInterface codeInterface, string signature)
        {
            if (codeInterface == null)
                return null;

            CodeFunction2 method = FindMethod(codeInterface, signature);

            if (method == null)
            {
                if (codeInterface.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                    return null;

                int indexBeginningOfParameters = signature.IndexOf('(');
                int indexEndOfParametes = signature.LastIndexOf(')');
                if ((indexBeginningOfParameters == -1) && (indexEndOfParametes == -1))
                {
                    indexBeginningOfParameters = signature.Length + 1;
                }
                else
                    if ((indexBeginningOfParameters == -1) || (indexEndOfParametes == -1))
                        throw new ArgumentException(string.Format("Method signature '{0}' is incorrect", signature));

                int indexFirstSpace = signature.IndexOf(' ');
                string name;
                string type;
                if ((indexFirstSpace == -1) || (indexFirstSpace > indexBeginningOfParameters))
                {
                    type = "void";
                    name = signature.Substring(0, indexBeginningOfParameters - 1).Trim();
                }
                else
                {
                    type = signature.Substring(0, indexFirstSpace).Trim();
                    name = signature.Substring(indexFirstSpace + 1, indexBeginningOfParameters - 1 - indexFirstSpace).Trim();
                }
                method = codeInterface.AddFunction(name, vsCMFunction.vsCMFunctionFunction, type, -1, vsCMAccess.vsCMAccessDefault) as CodeFunction2;
                if (indexEndOfParametes != -1)
                {

                    string parameterString = signature.Substring(indexBeginningOfParameters + 1, indexEndOfParametes - 1 - indexBeginningOfParameters);
                    string[] parameters = parameterString.Split(',');
                    if (!string.IsNullOrEmpty(parameterString))
                    {
                        int parameterIndex = 0;
                        foreach (string parameter in parameters)
                        {
                            string aParameterString = parameter.Trim();
                            string[] parts = aParameterString.Split(' ');
                            string parameterType;
                            string parameterName;
                            if (parts.Length >= 2)
                            {
                                parameterType = parts[0];
                                parameterName = parts[1];
                            }
                            else if (parts.Length > 0)
                            {
                                parameterType = parts[0];
                                parameterName = "p" + (parameterIndex + 1).ToString();
                            }
                            else
                                throw new ArgumentException(string.Format("Method signature '{0}' is incorrect (parameter #{1})", signature, parameterIndex));
                            method.AddParameter(parameterName, parameterType, parameterIndex);
                            parameterIndex++;
                        }
                    }
                }
            }
            return method;
        }


        /// <summary>
        /// Update a method from its prototype
        /// </summary>
        /// <param name="knownCodeTypes">Known code types</param>
        /// <param name="codeFunction">Method to update</param>
        /// <param name="newPrototype">new prototype</param>
        public static void UpdateMethodPrototype(KnownCodeTypes knownCodeTypes, CodeFunction codeFunction, string newPrototype)
        {
            string name;
            string returnType;
            List<KeyValuePair<string, string>> parameters;
            MethodLabel.ParsePrototype(newPrototype, out name, out returnType, out parameters);

            // Remove parameters if there are too many
            while (codeFunction.Parameters.Count > parameters.Count)
                codeFunction.RemoveParameter(codeFunction.Parameters.Count);

            // Update parameters up to function's parameter count.
            for (int i = 0; i < codeFunction.Parameters.Count; ++i)
            {
                bool replaceType = false;
                CodeParameter codeParameter = codeFunction.Parameters.Item(i + 1) as CodeParameter;
                CodeType newCodeType = null;
                if (knownCodeTypes != null)
                {
                    CodeType[] types = knownCodeTypes.GetNamedTypes(parameters[i].Value);
                    if (types.Length == 1)
                    {
                        replaceType = (codeParameter.Type.AsString != types[0].FullName);
                        newCodeType = types[0];
                    }
                    else
                    {
                        replaceType = codeParameter.Type.AsString != parameters[i].Value;
                    }
                }
                else
                    replaceType = codeParameter.Type.AsString != parameters[i].Value;

                if (replaceType)
                {
                    if (newCodeType.Namespace.FullName != (codeFunction.Parent as CodeType).Namespace.FullName)
                        EnsureUsings(codeFunction.ProjectItem, newCodeType.Namespace.FullName);
                    if (newCodeType != null)
                        codeParameter.Type = codeFunction.ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(newCodeType.Name);
                    else
                        codeParameter.Type = codeFunction.ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(parameters[i].Value);
                }

                // Refactor rename parameter
                if (codeParameter.Name != parameters[i].Key)
                    (codeParameter as CodeElement2).RenameSymbol(parameters[i].Key);
            }

            // Add parameters if necessary
            while (codeFunction.Parameters.Count < parameters.Count)
            {
                int n = codeFunction.Parameters.Count;
                CodeParameter codeParameter = codeFunction.AddParameter(parameters[n].Key, parameters[n].Value, n);
            }

            if (codeFunction.Type.AsString != returnType)
                codeFunction.Type = codeFunction.ProjectItem.ContainingProject.CodeModel.CreateCodeTypeRef(returnType);
        }

        /// <summary>
        /// Find a specified constructor (and create it if necessary)
        /// </summary>
        /// <param name="classe">Class in which the constructor should be looked for or created if necessary</param>
        /// <param name="name">Name of the method</param>
        /// <param name="parameters">Parameters of the constructor (name, type)*</param>
        /// <param name="visibility">Vibility for the constructor</param>
        /// <returns></returns>
        public static CodeFunction2 EnsureMethod(CodeClass2 classe, string name, string[] parameters, vsCMAccess visibility)
        {
            CodeFunction2 method = null;
            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    CodeFunction2 function = element as CodeFunction2;
                    if (function.Name != name)
                        continue;
                    if (function.Parameters.Count != parameters.Length / 2)
                        continue;
                    int i = 0;
                    foreach (CodeParameter parameter in function.Parameters)
                        if (parameter.Type.AsString != parameters[i])
                            continue;
                        else
                            i += 2;

                    // At that point we have found the constructor
                    return function;
                }

            // We have not found the constructor
            method = classe.AddFunction(name, (name == classe.Name) ? vsCMFunction.vsCMFunctionConstructor : vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, Type.Missing, visibility, Type.Missing) as CodeFunction2;
            for (int i = 0; i < parameters.Length; i += 2)
                method.AddParameter(parameters[i], parameters[i + 1], parameters.Length);
            return method;
        }


        /// <summary>
        /// Find a specified field (and create it if necessary)
        /// </summary>
        /// <param name="classe">Class in which the field should be looked for or created</param>
        /// <param name="name">Name of the field</param>
        /// <param name="type">type name for the field to create</param>
        /// <returns></returns>
        public static CodeVariable EnsureField(CodeClass2 classe, string name, string type)
        {
            CodeVariable field = null;
            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementVariable)
                {
                    CodeVariable variable = element as CodeVariable;
                    if ((variable.Name == name) /*&& (variable.Type.AsString == type)*/)
                    {
                        classe.RemoveMember(variable);
                        break;
                    }
                }
            // We have not found the constructor
            if ((!string.IsNullOrEmpty(name)) && (!string.IsNullOrEmpty(type)))
                field = classe.AddVariable(name, type, -1, vsCMAccess.vsCMAccessPrivate, null);
            return field;
        }


        /// <summary>
        /// Ensure the existence of the property
        /// </summary>
        /// <param name="classe"></param>
        /// <param name="name"></param>
        /// <param name="fullTypeName"></param>
        public static CodeProperty EnsureProperty(CodeClass2 classe, string name, string fullTypeName)
        {
            CodeProperty property = null;
            foreach (CodeElement element in GetAllMembers(classe))
                if (element.Kind == vsCMElement.vsCMElementProperty)
                {
                    CodeProperty p = element as CodeProperty;
                    if ((p.Name == name) /*&& (variable.Type.AsString == type)*/)
                        return p;
                }

            if ((!string.IsNullOrEmpty(name)) && (!string.IsNullOrEmpty(fullTypeName)))
                property = classe.AddProperty(name, name, fullTypeName, -1, vsCMAccess.vsCMAccessPrivate, null);
            return property;

        }

        /// <summary>
        /// Ensures a field exists in a class (with its description)
        /// </summary>
        /// <param name="codeClass">Class in which the field should be looked for or created</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="fullTypeName">type name for the field to create</param>
        /// <param name="Description">Comment to provide to the field</param>
        /// <returns></returns>
        public static CodeVariable2 EnsureField(CodeClass2 codeClass, string fieldName, string fullTypeName, string Description)
        {
            CodeVariable2 field = EnsureField(codeClass, fieldName, fullTypeName) as CodeVariable2;
            if (field != null)
                field.DocComment = GetComment(Description);
            return field;
        }

        /// <summary>
        /// Ensures a property exists
        /// </summary>
        /// <param name="classe"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="access"></param>
        /// <param name="getCode"></param>
        public static CodeProperty EnsureGetProperty(CodeClass2 classe, string name, string type, vsCMAccess access, string getCode)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            CodeProperty property = FindProperty(classe, name);
            if (property == null)
            {
                property = classe.AddProperty(name, "", type, -1, access, null);

                EditPoint editPoint = property.Getter.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                editPoint.Delete(property.Getter.GetEndPoint(vsCMPart.vsCMPartBody));
                if (getCode.StartsWith("throw") || getCode.StartsWith("return"))
                    editPoint.Insert("    " + getCode + ";\r\n");
                else
                    editPoint.Insert("    return " + getCode + ";\r\n");
                //    editPoint.Indent(property.Getter.GetEndPoint(vsCMPart.vsCMPartBody), 1);
            }
            else
                GetPropertySetCode(property, getCode);
            return property;
        }


        /// <summary>
        /// Add a value if it is not already in the collection
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="collection">Collection</param>
        /// <param name="value">Value to add</param>
        private static void AddUnique<T>(List<T> collection, T value)
        {
            if (!collection.Contains(value))
                collection.Add(value);
        }

        /// <summary>
        /// Ensure that the following "usings" are in the source code
        /// </summary>
        /// <param name="fileCodeModel"></param>
        /// <param name="usings"></param>
        public static void EnsureUsings(List<string> usings, FileCodeModel2 fileCodeModel)
        {
            foreach (CodeElement codeElement in fileCodeModel.CodeElements)
                if (codeElement.Kind == vsCMElement.vsCMElementImportStmt)
                {
                    CodeImport import = codeElement as CodeImport;
                    while (usings.Contains(import.Namespace))
                        usings.Remove(import.Namespace);
                }
            foreach (string import in usings)
                if (!string.IsNullOrEmpty(import))
                {
                    EditPoint2 editPoint = fileCodeModel.CodeElements.Item(1).StartPoint.CreateEditPoint() as EditPoint2;
                    editPoint.Insert("using " + import + ";\r\n");
                }
        }

        /// <summary>
        /// Ensures namespaces are referenced
        /// </summary>
        /// <param name="class">Class which file should contain the usings</param>
        /// <param name="usings">Namespaces to reference</param>
        public static void EnsureUsings(CodeClass2 @class, params string[] usings)
        {
            List<string> l = new List<string>();
            for (int i = usings.Length - 1; i >= 0; --i)
                if (!l.Contains(usings[i]))
                    l.Add(usings[i]);
            EnsureUsings(l, @class.ProjectItem.FileCodeModel as FileCodeModel2);
        }

        /// <summary>
        /// Ensure using directives are added at the beginning of given file
        /// </summary>
        /// <param name="projectItem">Project item (file) to which the usings should be added</param>
        /// <param name="usings">Name for the namespaces used in the using directives to add</param>
        public static void EnsureUsings(ProjectItem projectItem, params string[] usings)
        {
            EnsureUsings(projectItem.FileCodeModel as FileCodeModel2, usings);
        }

        /// <summary>
        /// Ensure using directives are added at the beginning of given file
        /// </summary>
        /// <param name="fileCodeModel">CodeModel for a file to which the usings should be added</param>
        /// <param name="usings">Name for the namespaces used in the using directives to add</param>
        public static void EnsureUsings(FileCodeModel2 fileCodeModel, params string[] usings)
        {
            List<string> l = new List<string>();
            for (int i = usings.Length - 1; i >= 0; --i)
                if (!l.Contains(usings[i]))
                    l.Add(usings[i]);
            EnsureUsings(l, fileCodeModel);
        }

        /// <summary>
        /// Ensures the using for namespaces
        /// </summary>
        /// <param name="usings"></param>
        /// <param name="fileCodeModel"></param>
        public static void EnsureUsingsForNamespaces(List<string> usings, FileCodeModel fileCodeModel)
        {
            foreach (CodeElement codeElement in fileCodeModel.CodeElements)
                if (codeElement.Kind == vsCMElement.vsCMElementImportStmt)
                {
                    CodeImport import = codeElement as CodeImport;
                    while (usings.Contains(import.Namespace))
                        usings.Remove(import.Namespace);
                }
            foreach (string import in usings)
                if (!string.IsNullOrEmpty(import))
                {
                    EditPoint2 editPoint = fileCodeModel.CodeElements.Item(1).StartPoint.CreateEditPoint() as EditPoint2;
                    if (fileCodeModel.Language == "{B5E9BD34-6D3E-4B5D-925E-8A43B79820B4}") // C#
                        editPoint.Insert("using " + import + ";\r\n");
                    else // if (fileCodeModel.Language == "VB")
                        editPoint.Insert("imports " + import + "\r\n");
                }

        }


        /// <summary>
        /// Removes (if body is empty) or comments (if not) a method
        /// </summary>
        /// <param name="function"></param>
        public static void RemoveOrCommentMethod(CodeFunction2 function)
        {
            if (function == null)
                return;

            // Code interface
            if (function.Parent is CodeInterface)
                (function.Parent as CodeInterface).RemoveMember(function);

            // Parent is a code class
            else if (function.Parent is CodeClass2)
            {
                CodeClass2 classe = function.Parent as CodeClass2;
                string text = function.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint().GetText(function.GetEndPoint(vsCMPart.vsCMPartBody));
                if (!string.IsNullOrEmpty(text))
                    text = text.Trim();
                if (string.IsNullOrEmpty(text))
                    classe.RemoveMember(function);
                else
                    CommentCodeElement(function as CodeElement);
            }
        }


        /// <summary>
        /// Removes or comment the data member (CodeVariable or CodeProperty)
        /// </summary>
        /// <param name="codeDataMember"></param>
        public static void RemoveOrCommentDataMember(CodeElement codeDataMember)
        {
            if (codeDataMember == null)
                return;

            if (codeDataMember is CodeVariable)
                ((codeDataMember as CodeVariable).Parent as CodeType).RemoveMember(codeDataMember);
            else if (codeDataMember is CodeProperty)
                CommentCodeElement(codeDataMember);
            else if (codeDataMember is CodeClass2)
                CommentCodeElement(codeDataMember);
        }

        /// <summary>
        /// Comments a code element
        /// </summary>
        /// <param name="elt"></param>
        public static void CommentCodeElement(CodeElement elt)
        {
            try
            {
                TextPoint begin = elt.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);
                TextPoint end = elt.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);
                begin.Parent.Selection.StartOfDocument(false);

                // Comment the XML comment preceding the member
                int previousLine = begin.Line;
                while (previousLine > 0)
                {
                    previousLine--;
                    begin.Parent.Selection.GotoLine(previousLine, false);
                    begin.Parent.Selection.SelectLine();
                    if (begin.Parent.Selection.Text.Trim().StartsWith("///"))
                        begin.Parent.Selection.Insert("// ", (int)vsInsertFlags.vsInsertFlagsInsertAtStart);
                    else
                        break;
                }

                // Comment the member
                for (int line = begin.Line; line <= end.Line; ++line)
                {
                    begin.Parent.Selection.GotoLine(line, false);
                    begin.Parent.Selection.Insert("// ", (int)vsInsertFlags.vsInsertFlagsInsertAtStart);
                }
            }
            catch (Exception)
            {
            }
        }



        /// <summary>
        /// Get the text of the delegation of a constructor (base(...) or this(....))
        /// </summary>
        /// <param name="constructor">Constructor we want the delegatio code of</param>
        public static string GetDelegationCode(CodeFunction2 constructor)
        {
            EditPoint begin = constructor.GetStartPoint(vsCMPart.vsCMPartHeader).CreateEditPoint();
            string s = constructor.GetStartPoint(vsCMPart.vsCMPartHeader).CreateEditPoint().GetText(constructor.GetStartPoint(vsCMPart.vsCMPartBody));
            EditPoint end = constructor.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint(); ;
            begin.CharRight(s.IndexOf(')') + 1);
            s = begin.GetText(end);
            return s;
        }

        /// <summary>
        /// Replace the text of the delegation of a constructor (base(...) or this(....)) by the
        /// provided code
        /// </summary>
        /// <param name="constructeur">Constructor</param>
        /// <param name="delegationText">New delegation text</param>
        public static void ReplaceDelegationCode(CodeFunction2 constructeur, string delegationText)
        {
            EditPoint begin = constructeur.GetStartPoint(vsCMPart.vsCMPartHeader).CreateEditPoint();
            string s = constructeur.GetStartPoint(vsCMPart.vsCMPartHeader).CreateEditPoint().GetText(constructeur.GetStartPoint(vsCMPart.vsCMPartBody));
            EditPoint end = constructeur.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint(); ;
            begin.CharRight(s.IndexOf(')') + 1);
            //   end.CharRight(s.Length - 1);
            s = begin.GetText(end);
            if (string.IsNullOrEmpty(delegationText))
                begin.ReplaceText(end, "\r\n {\r\n", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
            else
                begin.ReplaceText(end, "\r\n : " + delegationText + "\r\n  {\r\n", (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
        }


        /// <summary>
        /// Get the fields
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CodeVariable2> GetFields(CodeClass2 codeClass, bool nonStaticOnly)
        {
            if (codeClass.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
            {
                foreach (CodeClass2 partialClass in codeClass.PartialClasses)
                    foreach (CodeVariable2 v in GetFieldsForPartial(partialClass, nonStaticOnly))
                        yield return v;
            }
            else
                foreach (CodeVariable2 v in GetFieldsForPartial(codeClass, nonStaticOnly))
                    yield return v;
        }



        /// <summary>
        /// Get the fields and properties of the class
        /// </summary>
        /// <param name="codeClass"></param>
        /// <returns></returns>
        public static IEnumerable<CodeElement> GetFieldsAndProperties(CodeClass2 codeClass)
        {
            foreach (CodeElement e in IncrementalGenerator.GetAllMembers(codeClass))
                if ((e.Kind == vsCMElement.vsCMElementProperty) || (e.Kind == vsCMElement.vsCMElementVariable))
                    yield return e;
        }


        /// <summary>
        /// Get the fields for the partial class implementation only. If the class is not partial GetFieldsForPartial() and
        /// GetFields() return the same enumeration.
        /// </summary>
        /// <param name="codeClass"></param>
        /// <param name="nonStaticOnly"></param>
        /// <returns></returns>
        public static IEnumerable<CodeVariable2> GetFieldsForPartial(CodeClass2 codeClass, bool nonStaticOnly)
        {
            foreach (CodeElement member in codeClass.Members)
                if (member is CodeVariable2)
                {
                    CodeVariable2 variable = member as CodeVariable2;
                    if ((!nonStaticOnly) || (!variable.IsShared))
                        yield return member as CodeVariable2;
                }
        }



        /// <summary>
        /// Get the fields of the class
        /// </summary>
        /// <param name="codeClass">Class we are looking for the fields</param>
        /// <param name="nonStaticOnly">Only get non static fields</param>
        /// <returns></returns>
        public static IEnumerable<CodeProperty> GetProperties(CodeClass2 codeClass, bool nonStaticOnly)
        {
            if (codeClass.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
            {
                foreach (CodeClass2 partialClass in codeClass.PartialClasses)
                    foreach (CodeProperty v in GetPropertiesForPartial(partialClass, nonStaticOnly))
                        yield return v;
            }
            else
                foreach (CodeProperty v in GetPropertiesForPartial(codeClass, nonStaticOnly))
                    yield return v;
        }



        /// <summary>
        /// Get the fields for the partial class implementation only. If the class is not partial GetFieldsForPartial() and
        /// GetFields() return the same enumeration.
        /// </summary>
        /// <param name="codeClass"></param>
        /// <param name="nonStaticOnly"></param>
        /// <returns></returns>
        public static IEnumerable<CodeProperty> GetPropertiesForPartial(CodeClass2 codeClass, bool nonStaticOnly)
        {
            foreach (CodeElement member in codeClass.Members)
                if (member is CodeProperty)
                {
                    CodeProperty variable = member as CodeProperty;
                    bool shared = false;
                    if (variable is CodeProperty2)
                        shared = (variable as CodeProperty2).IsShared;
                    if ((!nonStaticOnly) || (!shared))
                        yield return member as CodeProperty;
                }
        }



        /// <summary>
        /// Get the methods of the class
        /// </summary>
        /// <param name="codeClass">Class we are looking for the fields</param>
        /// <param name="nonStaticOnly">Only get non static fields</param>
        /// <returns></returns>
        public static IEnumerable<CodeFunction2> GetMethods(CodeClass2 codeClass, bool nonStaticOnly)
        {
            if (codeClass.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
            {
                foreach (CodeClass2 partialClass in codeClass.PartialClasses)
                    foreach (CodeFunction2 v in GetMethodsForPartial(partialClass, nonStaticOnly))
                        yield return v;
            }
            else
                foreach (CodeFunction2 v in GetMethodsForPartial(codeClass, nonStaticOnly))
                    yield return v;
        }


        /// <summary>
        /// Get the methods of an interface
        /// </summary>
        /// <param name="codeInterface">Interface for which we require the methods</param>
        /// <returns>Methods of the interface</returns>
        public static IEnumerable<CodeFunction2> GetMethods(CodeInterface codeInterface)
        {
            foreach (CodeElement e in GetAllMembers(codeInterface))
                if (e is CodeFunction2)
                    yield return e as CodeFunction2;

        }

        /// <summary>
        /// Get the mehods for the partial class implementation only. If the class is not partial GetFieldsForPartial() and
        /// GetFields() return the same enumeration.
        /// </summary>
        /// <param name="codeClass"></param>
        /// <param name="nonStaticOnly"></param>
        /// <returns></returns>
        public static IEnumerable<CodeFunction2> GetMethodsForPartial(CodeClass2 codeClass, bool nonStaticOnly)
        {
            foreach (CodeElement member in codeClass.Members)
                if (member is CodeFunction2)
                {
                    CodeFunction2 method = member as CodeFunction2;
                    if ((!nonStaticOnly) || (!method.IsShared))
                        yield return method;
                }
        }


        /// <summary>
        /// Get all the members of the class, even if this is a partial class
        /// </summary>
        /// <param name="classe"></param>
        /// <returns></returns>
        public static IEnumerable<CodeElement> GetAllMembers(CodeClass2 classe)
        {
            if (classe.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
            {
                foreach (CodeClass2 partial in classe.PartialClasses)
                    foreach (CodeElement member in partial.Members)
                        yield return member;
            }
            else
                foreach (CodeElement member in classe.Members)
                    yield return member;
        }


        /// <summary>
        /// Get all members for a code Interface (even if it is partial)
        /// </summary>
        public static IEnumerable<CodeElement> GetAllMembers(CodeInterface iface)
        {
            if (iface is CodeInterface2)
                foreach (CodeElement member in GetAllMembers(iface as CodeInterface2))
                    yield return member;
            else
                foreach (CodeElement member in iface.Members)
                    yield return member;
        }

        /// <summary>
        /// Get all members for any code type (even if it is partial)
        /// </summary>
        public static IEnumerable<CodeElement> GetAllMembers(CodeType codeType)
        {
            if (codeType is CodeClass2)
                return GetAllMembers(codeType as CodeClass2);
            else if (codeType is CodeInterface2)
                return GetAllMembers(codeType as CodeInterface2);
            else
                return GetAllMembersForOtherCodeType(codeType);
        }

        private static IEnumerable<CodeElement> GetAllMembersForOtherCodeType(CodeType codeType)
        {
            foreach (CodeElement member in codeType.Members)
                yield return member;
        }

        /// <summary>
        /// Get all the members of the interface (including when it has partial implementation)
        /// </summary>
        public static IEnumerable<CodeElement> GetAllMembers(CodeInterface2 iface)
        {
            if (iface.DataTypeKind == vsCMDataTypeKind.vsCMDataTypeKindPartial)
            {
                foreach (CodeInterface2 partial in iface.Parts)
                    foreach (CodeElement member in partial.Members)
                        yield return member;
            }
            else
                foreach (CodeElement member in iface.Members)
                    yield return member;
        }



        /// <summary>
        /// Get all the members of the interface, including if it has partial representation, and
        /// including base interfaces members
        /// </summary>
        public static IEnumerable<CodeElement> GetAllMembersRecursively(CodeInterface2 codeInterface)
        {
            foreach (CodeInterface baseInterface in codeInterface.Bases)
                foreach (CodeElement member in GetAllMembersRecursively(baseInterface as CodeInterface2))
                    yield return member;
            foreach (CodeElement member in GetAllMembers(codeInterface))
                yield return member;
        }


        /// <summary>
        /// Signature for a method sending the id of a model element or a code element
        /// </summary>
        /// <typeparam name="T">ModelElement or code element derived type</typeparam>
        /// <param name="instance">instance we want the id of</param>
        public delegate object IdGetter<T>(T instance);

        /// <summary>
        /// Signature for a method creating a new T from a Store
        /// </summary>
        /// <typeparam name="T">ModelElement derived type</typeparam>
        /// <param name="codeElement">Code element</param>
        /// <typeparam name="U">Code element type (from EnvDTE)</typeparam>
        /// <param name="store">Store in which to create a new T</param>
        /// <returns>a new T</returns>
        public delegate T ElementCreator<T, U>(Store store, U codeElement) where T : ModelElement;

        /// <summary>
        /// Signature for methods updating a model element T from a code element U
        /// </summary>
        /// <typeparam name="T">Model element derived type</typeparam>
        /// <typeparam name="U">Code element type</typeparam>
        /// <param name="modelElement">Model element instance</param>
        /// <param name="codeElement">Code element instance</param>
        public delegate void ElementUpdater<T, U>(T modelElement, U codeElement);

        /// <summary>
        /// Updates a model role from a CodeElement collection
        /// </summary>
        /// <typeparam name="T">Model element Type (must derive from ModelElement)</typeparam>
        /// <typeparam name="U">CodeElement type must be a DTE type assignable to a CodeElement</typeparam>
        /// <param name="Role">Model role</param>
        /// <param name="members">CodeElement members</param>
        /// <param name="GetId">Delegate providing the id of a model element of type T</param>
        /// <param name="GetMemberId">Delegate providing the id of a model element of type U</param>
        /// <param name="CreateModelElementFromCodeElement">Method creating an element of type T</param>
        /// <param name="UpdateModelElementFromCodeElement">Method updating an element of type T from a U</param>
        public static void UpdateModelRoleFromCodeElements<T, U>(LinkedElementCollection<T> Role, IEnumerable<U> members,
         IdGetter<T> GetId,
         IdGetter<U> GetMemberId,
         ElementCreator<T, U> CreateModelElementFromCodeElement,
         ElementUpdater<T, U> UpdateModelElementFromCodeElement)
            where T : ModelElement
            where U : class
        {
            // Update the tno fields
            // ---------------------
            Dictionary<object, T> modelElementById = new Dictionary<object, T>();
            foreach (T modelElement in Role)
            {
                object id = GetId(modelElement);
                if (!modelElementById.ContainsKey(id))
                    modelElementById.Add(id, modelElement);
            }
            foreach (CodeElement element in members)
                if (element is U)
                {
                    U codeElement = element as U;
                    T modelElement;

                    // case where the tnoFieldVariable is already represented by a TnoField
                    string variableName = GetMemberId(codeElement).ToString();
                    if (modelElementById.ContainsKey(variableName))
                    {
                        modelElement = modelElementById[variableName];
                        modelElementById.Remove(variableName); // Will have been processed !
                    }
                    else
                    {
                        modelElement = CreateModelElementFromCodeElement(Role.SourceElement.Store, codeElement);
                        Role.Add(modelElement);
                    }

                    UpdateModelElementFromCodeElement(modelElement, codeElement);
                }

            // Remove unprocessed TnoField
            foreach (T modelElement in modelElementById.Values)
                modelElement.Delete();
        }

        /// <summary>
        /// Updates a model role from a CodeElement collection
        /// </summary>
        /// <typeparam name="T">Model element Type (must derive from ModelElement)</typeparam>
        /// <typeparam name="U">CodeElement type must be a DTE type assignable to a CodeElement</typeparam>
        /// <param name="Role">Model role</param>
        /// <param name="members">CodeElement members</param>
        /// <param name="domainPropertyId">Domain property providing the id of a model element of type T</param>
        /// <param name="GetMemberId">Delegate providing the id of a model element of type U</param>
        /// <param name="domainModelClassId">Guid of the DomainElementType to create</param>
        /// <param name="UpdateModelElementFromCodeElement">Method updating an element of type T from a U</param>
        public static void UpdateModelRoleFromCodeElements<T, U>(LinkedElementCollection<T> Role, IEnumerable<U> members,
         Guid domainPropertyId,
         IdGetter<U> GetMemberId,
         Guid domainModelClassId, /*ElementCreator<T> CreateModelElementFromCodeElement*/
         ElementUpdater<T, U> UpdateModelElementFromCodeElement)
            where T : ModelElement
            where U : class
        {
            // Update the tno fields
            // ---------------------
            Dictionary<object, T> modelElementById = new Dictionary<object, T>();
            Store store = Role.SourceElement.Store;
            foreach (T modelElement in Role)
            {
                object id = store.DomainDataDirectory.FindDomainProperty(domainPropertyId).GetValue(modelElement);
                if (!modelElementById.ContainsKey(id))
                    modelElementById.Add(id, modelElement);
            }
            foreach (CodeElement element in members)
                if (element is U)
                {
                    U codeElement = element as U;
                    T modelElement;

                    // case where the tnoFieldVariable is already represented by a TnoField
                    object codeElementId = GetMemberId(codeElement);
                    string variableName = codeElementId.ToString();
                    if (modelElementById.ContainsKey(variableName))
                    {
                        modelElement = modelElementById[variableName];
                        modelElementById.Remove(variableName); // Will have been processed !
                    }
                    else
                    {
                        DomainPropertyInfo propertyInfo = store.DomainDataDirectory.FindDomainProperty(domainPropertyId);
                        if (propertyInfo.Kind != DomainPropertyKind.Calculated)
                            modelElement = store.ElementFactory.CreateElement(domainModelClassId, new PropertyAssignment(domainPropertyId, codeElementId)) as T;
                        else
                            modelElement = store.ElementFactory.CreateElement(domainModelClassId) as T;
                        // modelElement = CreateModelElementFromCodeElement(Role.SourceElement.Store);
                        Role.Add(modelElement);
                    }

                    UpdateModelElementFromCodeElement(modelElement, codeElement);
                    // modelElementById.Remove(variableName);
                }

            // Remove unprocessed TnoField
            foreach (T modelElement in modelElementById.Values)
                modelElement.Delete();
        }

        /// <summary>
        /// Get the public constructors of a class
        /// </summary>
        public static IEnumerable<CodeFunction2> GetPublicConstructors(CodeClass2 codeClass2, bool nonStaticOnly)
        {
            if (codeClass2.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
                foreach (CodeClass2 partialClass in codeClass2.PartialClasses)
                {
                    foreach (CodeFunction2 constructor in GetPublicConstructorsForPartial(partialClass, nonStaticOnly))
                        yield return constructor;
                }
            else
                foreach (CodeFunction2 constructor in GetPublicConstructorsForPartial(codeClass2, nonStaticOnly))
                    yield return constructor;
        }


        /// <summary>
        /// Get the public constructors of a partial class implementation.
        /// </summary>
        public static IEnumerable<CodeFunction2> GetPublicConstructorsForPartial(CodeClass2 codeClass2, bool nonStaticOnly)
        {
            foreach (CodeElement element in codeClass2.Members)
                if (element is CodeFunction2)
                {
                    CodeFunction2 codeFunction = element as CodeFunction2;
                    if ((codeFunction.Access == vsCMAccess.vsCMAccessPublic) && (codeFunction.FunctionKind == vsCMFunction.vsCMFunctionConstructor))
                        if ((!nonStaticOnly) || (!codeFunction.IsShared))
                            yield return codeFunction;
                }
        }

        /// <summary>
        /// Get the public constructors of a class
        /// </summary>
        public static IEnumerable<CodeFunction2> GetPublicOrProtectedConstructors(CodeClass2 codeClass2, bool nonStaticOnly)
        {
            if (codeClass2.ClassKind == vsCMClassKind.vsCMClassKindPartialClass)
                foreach (CodeClass2 partialClass in codeClass2.PartialClasses)
                {
                    foreach (CodeFunction2 constructor in GetPublicOrProtectedConstructorsForPartial(partialClass, nonStaticOnly))
                        yield return constructor;
                }
            else
                foreach (CodeFunction2 constructor in GetPublicOrProtectedConstructorsForPartial(codeClass2, nonStaticOnly))
                    yield return constructor;
        }


        /// <summary>
        /// Get the public constructors of a partial class implementation.
        /// </summary>
        public static IEnumerable<CodeFunction2> GetPublicOrProtectedConstructorsForPartial(CodeClass2 codeClass2, bool nonStaticOnly)
        {
            foreach (CodeElement element in codeClass2.Members)
                if (element is CodeFunction2)
                {
                    CodeFunction2 codeFunction = element as CodeFunction2;
                    if ((codeFunction.Access == vsCMAccess.vsCMAccessPublic
                         || codeFunction.Access == vsCMAccess.vsCMAccessProtected
                         || codeFunction.Access == vsCMAccess.vsCMAccessProjectOrProtected
                         || codeFunction.Access == vsCMAccess.vsCMAccessAssemblyOrFamily) && (codeFunction.FunctionKind == vsCMFunction.vsCMFunctionConstructor))
                        if ((!nonStaticOnly) || (!codeFunction.IsShared))
                            yield return codeFunction;
                }
        }


        /// <summary>
        /// Get the parameters of the function (strong typed)
        /// </summary>
        /// <param name="codeFunction"></param>
        /// <returns></returns>
        public static IEnumerable<CodeParameter2> GetParameters(CodeFunction2 codeFunction)
        {
            foreach (CodeParameter2 parameter in codeFunction.Parameters)
                yield return parameter;
        }

        /// <summary>
        /// Get a method's prototype
        /// </summary>
        public static string GetPrototype(CodeFunction2 method)
        {
            string prototype = method.Name + '(';
            bool already = false;
            foreach (CodeParameter2 parameter in method.Parameters)
            {
                if (already)
                    prototype += ',';
                prototype += parameter.Type.AsString;
                already = true;
            }
            prototype += ')';
            return prototype;
        }

        /// <summary>
        /// Regular expression used to find a field encapsulted in a property in property code
        /// </summary>
        static Regex getReturningField = new Regex(@"get\s*{\s*return\s+(?<Field>\w+)\s*;\s*}"
         , RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


        /// <summary>
        /// Get the field (if any) encapsulated by a property
        /// </summary>
        /// <param name="property"></param>
        /// <returns>A code variable if the property encapsulates the field. And null otherwise</returns>
        public static string GetPropertyGetCode(CodeProperty property)
        {
            // Cannot get this information for properties for which the source code is not available
            if (property.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;

            try // Source might be incorrect !
            {
                EditPoint begin = property.StartPoint.CreateEditPoint();
                string propertyText = begin.GetText(property.EndPoint);
                int beginIndex = propertyText.IndexOf("return ");
                int endIndex = propertyText.LastIndexOf('}');
                if (endIndex != -1)
                    endIndex = propertyText.Substring(0, endIndex).LastIndexOf('}');
                if ((beginIndex != -1) && (endIndex != -1))
                {
                    beginIndex += "return ".Length;
                    return propertyText.Substring(beginIndex, endIndex - beginIndex - 2);
                }
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the code of the get method of the property
        /// </summary>
        /// <param name="property">Property we want to set the get_ method code of</param>
        /// <param name="getMethodText">Code for the get method</param>
        public static void GetPropertySetCode(CodeProperty property, string getMethodText)
        {
            // Cannot get this information for properties for which the source code is not available
            if (property.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return;

            try // Source might be incorrect !
            {
                EditPoint begin = property.StartPoint.CreateEditPoint();
                string propertyText = begin.GetText(property.EndPoint);
                int beginIndex = propertyText.IndexOf('{');
                int endIndex = propertyText.LastIndexOf('}');
                if ((beginIndex != -1) && (endIndex != -1))
                {
                    string newText = propertyText.Substring(0, beginIndex) + "{\r\n  set {" + getMethodText + "}\r\n " + propertyText.Substring(endIndex);
                    if (newText != propertyText)
                        begin.ReplaceText(property.EndPoint, newText, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
                }
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// Get the field (if any) encapsulated by a property
        /// </summary>
        /// <param name="property"></param>
        /// <returns>A code variable if the property encapsulates the field. And null otherwise</returns>
        public static CodeVariable2 GetEncapsulatedField(CodeProperty property)
        {
            string code = GetPropertyGetCode(property);
            if (!string.IsNullOrEmpty(code))
                return FindField(property.Parent as CodeClass2, code);
            else
                return null;
        }


        /// <summary>
        /// Does the type have an attribute of given fullname
        /// </summary>
        /// <param name="codeType">CodeType</param>
        /// <param name="name">Name for the attribute that we look for. (whether or NOT fully qualified)</param>
        /// <returns><c>true</c> if the attribute exists on the type, and <c>false</c> otherwise</returns>
        public static bool HasAttribute(CodeType codeType, string name)
        {
            if (codeType.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject)
            {
                foreach (CodeAttribute attribute in codeType.Attributes)
                {
                    try
                    {
                        if (attribute.FullName == name)
                            return true;
                        if (attribute.FullName == name + "Attribute")
                            return true;
                    }
                    catch (Exception)
                    {
                        if ((attribute.Name == name) || (attribute.Name == name + "Attribute") || (attribute.Name + "Attribute" == name))
                            return true;
                    }
                }
                return false;
            }
            else
            {
                object parent = codeType.Parent;
                Dictionary<string, Type> knownTypes;

                try
                {
                    // Get up to the containing namespace
                    while (parent is CodeNamespace)
                        parent = (parent as CodeNamespace).Parent;

                    // Since we are external, the parent is now a CodeModel
                    CodeModel codeModel = parent as CodeModel;

                    // And its parent is a Project
                    Project project = codeModel.Parent;

                    knownTypes = VsHelper.GetAvailableTypes(project, typeof(object), false, true);
                }
                catch (Exception)
                {
                    knownTypes = VsHelper.GetAvailableTypes(codeType.DTE.ActiveDocument.ProjectItem.ContainingProject, typeof(object), true, true);
                }

                if (knownTypes.ContainsKey(codeType.FullName))
                {
                    Type type = knownTypes[codeType.FullName];
                    foreach (Attribute a in type.GetCustomAttributes(true))
                        if ((a.GetType().FullName == name))
                            return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does the property have the attribute
        /// </summary>
        /// <param name="property"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasAttribute(CodeProperty property, string name)
        {
            if (property.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject)
            {
                foreach (CodeAttribute attribute in property.Attributes)
                {
                    if (attribute.Name == name)
                        return true;
                }
            }
            else
            {
                CodeClass parentClass = property.Parent;

                // Get parent class's parent
                object parent = parentClass.Parent;
                Dictionary<string, Type> knownTypes;

                try
                {
                    // Get up to the containing namespace
                    while (parent is CodeNamespace)
                        parent = (parent as CodeNamespace).Parent;

                    // Since we are external, the parent is now a CodeModel
                    CodeModel codeModel = parent as CodeModel;

                    // And its parent is a Project
                    Project project = codeModel.Parent;

                    knownTypes = VsHelper.GetAvailableTypes(project, typeof(object), true, true);
                }
                catch (Exception)
                {
                    knownTypes = VsHelper.GetAvailableTypes(property.DTE.ActiveDocument.ProjectItem.ContainingProject, typeof(object), true, true);
                }

                if (knownTypes.ContainsKey(property.Parent.FullName))
                {
                    Type parentType = knownTypes[parentClass.FullName];
                    PropertyInfo propertyInfo = parentType.GetProperty(property.Name);
                    foreach (Attribute a in propertyInfo.GetCustomAttributes(true))
                        if ((a.GetType().Name == name + "Attribute"))
                            return true;
                }

            }
            return false;
        }


        /// <summary>
        /// Does the property have the attribute
        /// </summary>
        /// <param name="property"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasAttribute(CodeVariable property, string name)
        {
            if (property.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject)
            {
                foreach (CodeAttribute attribute in property.Attributes)
                {
                    if (attribute.Name == name)
                        return true;
                }
            }
            else
            {
                // Get parent class's parent
                object parent = (property.Parent as CodeType).Parent;
                Dictionary<string, Type> knownTypes;

                try
                {

                    // Get up to the containing namespace
                    while (parent is CodeNamespace)
                        parent = (parent as CodeNamespace).Parent;

                    // Since we are external, the parent is now a CodeModel
                    CodeModel codeModel = parent as CodeModel;

                    // And its parent is a Project
                    Project project = codeModel.Parent;

                    knownTypes = VsHelper.GetAvailableTypes(project, typeof(object), true, true);
                }
                catch (Exception)
                {
                    knownTypes = VsHelper.GetAvailableTypes(property.DTE.ActiveDocument.ProjectItem.ContainingProject, typeof(object), true, true);
                }

                if (knownTypes.ContainsKey((property.Parent as CodeType).FullName))
                {
                    Type parentType = knownTypes[(property.Parent as CodeType).FullName];
                    PropertyInfo propertyInfo = parentType.GetProperty(property.Name);
                    foreach (Attribute a in propertyInfo.GetCustomAttributes(true))
                        if ((a.GetType().Name == name + "Attribute"))
                            return true;
                }

            }
            return false;
        }


        /// <summary>
        /// Doest the CodeElement have an attribute
        /// </summary>
        /// <param name="codeElement"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool HasAttribute(CodeElement codeElement, string name)
        {
            if (codeElement is CodeProperty)
                return HasAttribute(codeElement as CodeProperty, name);
            else if (codeElement is CodeVariable)
                return HasAttribute(codeElement as CodeVariable, name);
            else if (codeElement is CodeType)
                return HasAttribute(codeElement as CodeType, name);
            else
                throw new NotImplementedException("HasAttribute(CodeElement,string) is only supported for CodeProperty, CodeVariable, and CodeType");
        }

        /// <summary>
        /// Does the property have the attribute
        /// </summary>
        /// <param name="property"></param>
        /// <param name="name">Fully qualified name of the attribute</param>
        /// <returns></returns>
        public static bool HasAttribute(CodeFunction2 property, string name)
        {
            if (property.InfoLocation == vsCMInfoLocation.vsCMInfoLocationProject)
            {
                foreach (CodeAttribute attribute in property.Attributes)
                {
                    if (attribute.FullName == name)
                        return true;
                }
            }
            else
            {
                // Get parent class's parent
                object parent = property.Parent;
                Dictionary<string, Type> knownTypes;

                try
                {

                    // Get up to the containing namespace
                    while (parent is CodeNamespace)
                        parent = (parent as CodeNamespace).Parent;

                    // Since we are external, the parent is now a CodeModel
                    CodeModel codeModel = parent as CodeModel;

                    // And its parent is a Project
                    Project project = codeModel.Parent;

                    knownTypes = VsHelper.GetAvailableTypes(project, typeof(object), true, true);
                }
                catch (Exception)
                {
                    knownTypes = VsHelper.GetAvailableTypes(property.DTE.ActiveDocument.ProjectItem.ContainingProject, typeof(object), true, true);
                }

                if (knownTypes.ContainsKey((property.Parent as CodeType).FullName))
                {
                    Type parentType = knownTypes[(property.Parent as CodeType).FullName];
                    MethodInfo propertyInfo = parentType.GetMethod(property.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (Attribute a in propertyInfo.GetCustomAttributes(true))
                        if ((a.GetType().FullName == name))
                            return true;
                }

            }
            return false;
        }


        /// <summary>
        /// Ensure the interface has the attribute if and only if the condition stands.
        /// </summary>
        /// <param name="codeInterface">Interface on which to add an attribute</param>
        /// <param name="attributeFullTypeName">Fully qualified name of the attribute</param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static CodeAttribute EnsureAttributeIff(CodeInterface codeInterface, string attributeFullTypeName, bool condition)
        {
            return EnsureAttributeIff(codeInterface as CodeType, attributeFullTypeName, condition);
        }


        /// <summary>
        /// Ensure the class has the attribute if and only if the condition stands.
        /// </summary>
        /// <param name="codeClass">Class we want to add an attribute if a condition stands</param>
        /// <param name="attributeFullTypeName">Attribute to ensure</param>
        /// <param name="condition">Condition</param>
        /// <returns>The attribute added.</returns>
        public static CodeAttribute EnsureAttributeIff(CodeClass2 codeClass, string attributeFullTypeName, bool condition)
        {
            return EnsureAttributeIff(codeClass as CodeType, attributeFullTypeName, condition);
        }

        /// <param name="codeType">Code type we want to add an attribute if a condition stands</param>
        /// <param name="attributeFullTypeName">Attribute to ensure</param>
        /// <param name="condition">Condition</param>
        /// <returns>The attribute added.</returns>
        public static CodeAttribute EnsureAttributeIff(CodeType codeType, string attributeFullTypeName, bool condition)
        {
            if (codeType == null)
                return null;
            if (codeType.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;
            if (condition)
            {
                CodeModel codeModel = codeType.ProjectItem.ContainingProject.CodeModel;
                if (codeModel == null)
                    return null;
                CodeType attribute = codeModel.CodeTypeFromFullName(attributeFullTypeName);
                if (attribute == null)
                    return null;
                IncrementalGenerator.EnsureUsings(codeType.ProjectItem.FileCodeModel as FileCodeModel2, attribute.Namespace.FullName);
                string attributeName = attribute.Name;
                if (attributeName.EndsWith("Attribute"))
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                return IncrementalGenerator.EnsureAttribute(codeType, attributeName);
            }
            else
            {
                CodeAttribute a = IncrementalGenerator.FindFirstAttribute(codeType as CodeType, attributeFullTypeName);
                if (a != null)
                    a.Delete();
                return null;
            }
        }


        /// <summary>
        /// Ensure the interface has the attribute if and only if the condition stands.
        /// </summary>
        /// <param name="field">Field on which we must ensure the an attribute</param>
        /// <param name="attributeFullTypeName">Fully qualified name of the attribute type</param>
        /// <param name="condition">Condition that must stand for the attribute to exist. If the condition equals
        /// <c>false</c>, the attribute is removed</param>
        /// <returns>The attribute if it could be ensured. And null if the condition is false, or the attribute could
        /// not be created</returns>
        public static CodeAttribute EnsureAttributeIff(CodeVariable field, string attributeFullTypeName, bool condition)
        {
            if (field == null)
                return null;
            if (field.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;
            if (condition)
            {
                CodeType parentType = field.Collection.Parent as CodeType;
                CodeModel codeModel = parentType.ProjectItem.ContainingProject.CodeModel;
                if (codeModel == null)
                    return null;
                CodeType attribute = codeModel.CodeTypeFromFullName(attributeFullTypeName);
                if (attribute == null)
                    return null;
                IncrementalGenerator.EnsureUsings(parentType.ProjectItem.FileCodeModel as FileCodeModel2, attribute.Namespace.FullName);

                string attributeName = attribute.Name;
                if (attributeName.EndsWith("Attribute"))
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                return IncrementalGenerator.EnsureAttribute(field, attributeName);
            }
            else
            {
                CodeAttribute a = IncrementalGenerator.FindFirstAttribute(field, attributeFullTypeName);
                if (a != null)
                    a.Delete();
                return null;
            }
        }

        /// <summary>
        /// Ensure the interface has the attribute if and only if the condition stands.
        /// </summary>
        /// <param name="property">Field on which we must ensure the an attribute</param>
        /// <param name="attributeFullTypeName">Fully qualified name of the attribute type</param>
        /// <param name="condition">Condition that must stand for the attribute to exist. If the condition equals
        /// <c>false</c>, the attribute is removed</param>
        /// <returns>The attribute if it could be ensured. And null if the condition is false, or the attribute could
        /// not be created</returns>
        public static CodeAttribute EnsureAttributeIff(CodeProperty property, string attributeFullTypeName, bool condition)
        {
            if (property == null)
                return null;
            if (property.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;
            if (condition)
            {
                CodeType parentType = property.Collection.Parent as CodeType;
                CodeModel codeModel = parentType.ProjectItem.ContainingProject.CodeModel;
                if (codeModel == null)
                    return null;
                CodeType attribute = codeModel.CodeTypeFromFullName(attributeFullTypeName);
                if (attribute == null)
                    return null;
                IncrementalGenerator.EnsureUsings(parentType.ProjectItem.FileCodeModel as FileCodeModel2, attribute.Namespace.FullName);

                string attributeName = attribute.Name;
                if (attributeName.EndsWith("Attribute"))
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                return IncrementalGenerator.EnsureAttribute(property, attributeName);
            }
            else
            {
                CodeAttribute a = IncrementalGenerator.FindFirstAttribute(property, attributeFullTypeName);
                if (a != null)
                    a.Delete();
                return null;
            }
        }



        /// <summary>
        /// Ensure the interface has the attribute if and only if the condition stands.
        /// </summary>
        /// <param name="codeMethod">Method on which we must ensure the an attribute</param>
        /// <param name="attributeFullTypeName">Fully qualified name of the attribute type</param>
        /// <param name="condition">Condition that must stand for the attribute to exist. If the condition equals
        /// <c>false</c>, the attribute is removed</param>
        /// <returns>The attribute if it could be ensured. And null if the condition is false, or the attribute could
        /// not be created</returns>
        public static CodeAttribute EnsureAttributeIff(CodeFunction2 codeMethod, string attributeFullTypeName, bool condition)
        {
            if (codeMethod == null)
                return null;
            if (codeMethod.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return null;
            if (condition)
            {
                CodeType parentType = codeMethod.Parent as CodeType;
                CodeModel codeModel = parentType.ProjectItem.ContainingProject.CodeModel;
                if (codeModel == null)
                    return null;
                CodeType attribute = codeModel.CodeTypeFromFullName(attributeFullTypeName);
                if (attribute == null)
                    return null;
                IncrementalGenerator.EnsureUsings(parentType.ProjectItem.FileCodeModel as FileCodeModel2, attribute.Namespace.FullName);

                string attributeName = attribute.Name;
                if (attributeName.EndsWith("Attribute"))
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                return IncrementalGenerator.EnsureAttribute(codeMethod, attributeName);
            }
            else
            {
                CodeAttribute a = IncrementalGenerator.FindFirstAttribute(codeMethod, attributeFullTypeName);
                if (a != null)
                    a.Delete();
                return null;
            }
        }


        /// <summary>
        /// Ensure the attribute
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeFullTypeName"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static CodeAttribute EnsureAttributeIff(CodeElement element, string attributeFullTypeName, bool condition)
        {
            if (element is CodeType)
                return EnsureAttributeIff(element as CodeType, attributeFullTypeName, condition);
            else if (element is CodeVariable)
                return EnsureAttributeIff(element as CodeVariable, attributeFullTypeName, condition);
            else if (element is CodeProperty)
                return EnsureAttributeIff(element as CodeProperty, attributeFullTypeName, condition);
            else if (element is CodeFunction)
                return EnsureAttributeIff(element as CodeFunction2, attributeFullTypeName, condition);
            else
                throw new NotImplementedException("EnsureAttributeIff(CodeElement element, string attributeFullTypeName, bool condition) is only implemented for CodeInterface, CodeClass, CodeVariable, CodeProperty, CodeFunction");
        }

        /// <summary>
        /// Ensures the attribute argument exists if and only if a condition stands
        /// </summary>
        /// <param name="attribute">Attribute that should contrain a property conditionnaly</param>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="propertyValue">Value of the property</param>
        /// <param name="condition">Condition that should stand for the property to be present.</param>
        /// <returns></returns>
        public static CodeAttributeArgument EnsureAttributeArgumentIff(CodeAttribute attribute, string propertyName, string propertyValue, bool condition)
        {
            CodeAttribute2 attribute2 = attribute as CodeAttribute2;
            if (attribute2 == null)
                return null;

            // Find the argument
            CodeAttributeArgument theArgument = null;
            foreach (CodeAttributeArgument anArgument in attribute2.Arguments)
                if (anArgument.Name == propertyName)
                {
                    theArgument = anArgument;
                    break;
                }

            if (condition)
            {
                if (theArgument == null)
                    theArgument = attribute2.AddArgument(propertyValue, propertyName, null);
                else if (propertyValue != theArgument.Value)
                    theArgument.Value = propertyValue;
            }
            else
            {
                if (theArgument != null)
                    theArgument.Delete();
                theArgument = null;
            }

            return theArgument;
        }


        /*
        public static void TryToShow(CodeType codeType)
        {
         if (codeType.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
          return;

         if (!codeType.ProjectItem.get_IsOpen(Constants.vsViewKindCode))
          codeType.ProjectItem.Open(Constants.vsViewKindCode);

         TextPoint textPoint = null;
         try
         {
          textPoint = codeType.GetStartPoint(vsCMPart.vsCMPartHeader);
         }
         catch (Exception)
         {

         }
         if (textPoint == null)
          return;
         textPoint.TryToShow(vsPaneShowHow.vsPaneShowTop, 0);
        }
        */

        /// <summary>
        /// Tries to present the source code corresponding to the code element (if it is
        /// in the project)
        /// </summary>
        /// <param name="codeElement">Code element to present</param>
        public static void TryToShow(CodeElement codeElement)
        {
            if (codeElement == null)
                return;
            if (codeElement.InfoLocation != vsCMInfoLocation.vsCMInfoLocationProject)
                return;

            if (!codeElement.ProjectItem.get_IsOpen(Constants.vsViewKindCode))
                codeElement.ProjectItem.Open(Constants.vsViewKindCode);

            TextPoint textPoint = null;
            try
            {
                textPoint = codeElement.GetStartPoint(vsCMPart.vsCMPartHeader);
            }
            catch (Exception)
            {

            }
            if (textPoint == null)
            {
                try
                {
                    textPoint = codeElement.StartPoint;
                }
                catch (Exception)
                {
                }
            }
            if (textPoint == null)
                return;
            textPoint.TryToShow(vsPaneShowHow.vsPaneShowAsIs, 0);
            textPoint.Parent.Selection.MoveTo(textPoint.Line, 0, false);
        }


        /// <summary>
        /// Removes the leading and trailing double-quotes
        /// </summary>
        /// <param name="aStringWithDoubleQuotes">A string with double quotes</param>
        /// <returns>The same string without a leading and trailing double quotes</returns>
        public static string RemoveDoubleQuotes(string aStringWithDoubleQuotes)
        {
            if ((aStringWithDoubleQuotes.Length >= 2) && (aStringWithDoubleQuotes[0] == '"') && (aStringWithDoubleQuotes[aStringWithDoubleQuotes.Length - 1] == '"'))
                return aStringWithDoubleQuotes.Substring(1, aStringWithDoubleQuotes.Length - 2);
            else
                return aStringWithDoubleQuotes;
        }



        /// <summary>
        /// Updates the domain properties of a DomainModel element from attribute arguments values.
        /// </summary>
        /// <typeparam name="T">Model element type</typeparam>
        /// <param name="modelElement">model element to update</param>
        /// <param name="attribute">Attribute from which the information is retreived</param>
        /// <param name="attributeArgumentNameToDomainPropertyNameMapping">Mapping between the argument name and the property name</param>
        /// <remarks>Only the attribute values presented in the mapping are considered.
        /// If an attribute argument value is not present in the attribute, then the corresponding domain property will be set to its
        /// default value (from model)</remarks>
        /// <example>
        /// <code>
        ///  List&lt;string&gt; attributeArgumentNameToDomainPropertyNameMapping = new List&lt;string&gt;(new string[]
        ///  {
        ///   "IsWrapped->IsWrapped", "ProtectionLevel->ProtectionLevel", "WrapperName->WrapperName", "WrapperNamespace->WrapperNamespace"
        ///  });
        ///
        /// // Get information from the attribute
        /// CodeAttribute2 attribute = IncrementalGenerator.FindFirstAttribute(codeType, messageContractAttributeFullName);
        /// 
        /// // And update model from code !
        /// IncrementalGenerator.UpdateDomainPropertiesFromArguments(messageContract, attribute, attributeArgumentNameToDomainPropertyNameMapping);
        /// </code>
        /// 
        /// </example>
        public static void UpdateDomainPropertiesFromAttributeArguments<T>(T modelElement, CodeAttribute2 attribute, List<string> attributeArgumentNameToDomainPropertyNameMapping) where T : ModelElement
        {
            // Build a dictionnary containing mapping from attribute argument to domain property
            Dictionary<string, string> domainPropertyNameFromAttributeArgumentName = new Dictionary<string, string>();
            foreach (string attributeArgumentNameToDomainPropertyName in attributeArgumentNameToDomainPropertyNameMapping)
                if (attributeArgumentNameToDomainPropertyName.Contains("->"))
                {
                    int separatorIndex = attributeArgumentNameToDomainPropertyName.IndexOf("->");
                    string attributeArgumentName = attributeArgumentNameToDomainPropertyName.Substring(0, separatorIndex).Trim();
                    string domainPropertyName = attributeArgumentNameToDomainPropertyName.Substring(separatorIndex + 2).Trim();
                    domainPropertyNameFromAttributeArgumentName.Add(attributeArgumentName, domainPropertyName);
                }
                else
                    domainPropertyNameFromAttributeArgumentName.Add(attributeArgumentNameToDomainPropertyName, attributeArgumentNameToDomainPropertyName);

            UpdateDomainPropertiesFromAttributeArguments<T>(modelElement, attribute, domainPropertyNameFromAttributeArgumentName);
        }

        /// <summary>
        /// Updates the domain properties of a DomainModel element from attribute arguments values.
        /// </summary>
        /// <typeparam name="T">Model element type</typeparam>
        /// <param name="modelElement">model element to update</param>
        /// <param name="attribute">Attribute from which the information is retreived</param>
        /// <param name="domainPropertyNameFromAttributeArgumentName">Mapping between the argument name and the property name</param>
        public static void UpdateDomainPropertiesFromAttributeArguments<T>(T modelElement, CodeAttribute2 attribute, Dictionary<string, string> domainPropertyNameFromAttributeArgumentName) where T : ModelElement
        {
            // Get the domain property info
            DomainClassInfo domainClassInfo = modelElement.Store.DomainDataDirectory.FindDomainClass(typeof(T).FullName);

            // Create the list of properties that can be retreived. If they are are not, they will be put their default value.
            List<string> notFound = new List<string>();
            foreach (string attributeArgumentName in domainPropertyNameFromAttributeArgumentName.Keys)
                notFound.Add(attributeArgumentName);

            using (Transaction transaction = modelElement.Store.TransactionManager.BeginTransaction("Updating properties from attribute arguments"))
            {
                // Get information from the attribute
                if (attribute != null)
                    foreach (CodeAttributeArgument argument in attribute.Arguments)
                    {
                        notFound.Remove(argument.Name);

                        // Find domain property info
                        string domainPropertyName = string.Empty;
                        if (!domainPropertyNameFromAttributeArgumentName.TryGetValue(argument.Name, out domainPropertyName))
                            continue;

                        DomainPropertyInfo domainPropertyInfo = domainClassInfo.FindDomainProperty(domainPropertyName, true);
                        if (domainPropertyInfo == null)
                            throw new ArgumentException(string.Format("Domain property info '{0}' not found in domain class info '{1}'", argument.Name, domainClassInfo.Name));

                        // Cannot set a readonly property !
                        if (domainPropertyInfo.Kind == DomainPropertyKind.Calculated)
                            continue;

                        // string
                        if (domainPropertyInfo.PropertyType == typeof(string))
                            domainPropertyInfo.SetValue(modelElement, IncrementalGenerator.RemoveDoubleQuotes(argument.Value));
                        else if (domainPropertyInfo.PropertyType.IsEnum)
                        {
                            // When typing code there might be times when the enum is incoherent.
                            try
                            {
                                string stringRepresentation = argument.Value;
                                stringRepresentation = stringRepresentation.Replace(domainPropertyInfo.PropertyType.Name + '.', "");
                                object parsed = Enum.Parse(domainPropertyInfo.PropertyType, stringRepresentation);
                                domainPropertyInfo.SetValue(modelElement, parsed);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        else
                        {
                            // When typing code there might be times when value cannot be parsed yet.
                            try
                            {
                                object parsed = Convert.ChangeType(argument.Value, domainPropertyInfo.PropertyType);
                                domainPropertyInfo.SetValue(modelElement, parsed);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                // Default value for arguments were not found
                foreach (string argumentName in notFound)
                {
                    string domainPropertyName = string.Empty;
                    if (!domainPropertyNameFromAttributeArgumentName.TryGetValue(argumentName, out domainPropertyName))
                        continue;
                    DomainPropertyInfo domainPropertyInfo = domainClassInfo.FindDomainProperty(domainPropertyName, true);
                    if (domainPropertyInfo.Kind != DomainPropertyKind.Calculated)
                        domainPropertyInfo.SetValue(modelElement, ((domainPropertyInfo.PropertyType == typeof(string) && (domainPropertyInfo.DefaultValue == null)) ? "" : domainPropertyInfo.DefaultValue));
                }

                if (transaction.HasPendingChanges)
                    transaction.Commit();
            }
        }


        /// <summary>
        /// Ensure the implementation of domain properties as attribute arguments, present only if
        /// the domain property is not equal to its default value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="modelElement"></param>
        /// <param name="attribute"></param>
        /// <param name="domainPropertyToAttributeArgumentNameMapping"></param>
        public static void EnsureAttributeArgumentsFromDomainProperties<T>(T modelElement, CodeAttribute2 attribute, List<string> domainPropertyToAttributeArgumentNameMapping) where T : ModelElement
        {
            // Build a dictionnary containing mapping from attribute argument to domain property
            Dictionary<string, string> attributeArgumentNameFromDomainPropertyName = new Dictionary<string, string>();
            foreach (string domainPropertyNameToAttributeArgumentName in domainPropertyToAttributeArgumentNameMapping)
                if (domainPropertyNameToAttributeArgumentName.Contains("->"))
                {
                    int separatorIndex = domainPropertyNameToAttributeArgumentName.IndexOf("->");
                    string domainPropertyName = domainPropertyNameToAttributeArgumentName.Substring(0, separatorIndex).Trim();
                    string attributeArgumentName = domainPropertyNameToAttributeArgumentName.Substring(separatorIndex + 2).Trim();
                    attributeArgumentNameFromDomainPropertyName.Add(domainPropertyName, attributeArgumentName);
                }
                else
                    attributeArgumentNameFromDomainPropertyName.Add(domainPropertyNameToAttributeArgumentName, domainPropertyNameToAttributeArgumentName);

            EnsureAttributeArgumentsFromDomainProperties<T>(modelElement, attribute, attributeArgumentNameFromDomainPropertyName);
        }

        /// <summary>
        /// Updates the domain properties of a DomainModel element from attribute arguments values.
        /// </summary>
        /// <typeparam name="T">Model element type</typeparam>
        /// <param name="modelElement">model element to update</param>
        /// <param name="attribute">Attribute from which the information is retreived</param>
        /// <param name="attributeArgumentNameFromDomainPropertyName">Mapping between the  property name and argument name</param>
        public static void EnsureAttributeArgumentsFromDomainProperties<T>(T modelElement, CodeAttribute2 attribute, Dictionary<string, string> attributeArgumentNameFromDomainPropertyName) where T : ModelElement
        {
            if (attribute == null)
                return;

            // Get the domain property info
            DomainClassInfo domainClassInfo = modelElement.Store.DomainDataDirectory.FindDomainClass(typeof(T).FullName);

            // generate the properties of the attributeArgumentNameFromDomainPropertyName into attribute arguments
            foreach (string propertyName in attributeArgumentNameFromDomainPropertyName.Keys)
            {
                DomainPropertyInfo domainPropertyInfo = domainClassInfo.FindDomainProperty(propertyName, true);
                if (domainPropertyInfo == null)
                    throw new ArgumentException(string.Format("Domain property info '{0}' not found in domain class info '{1}'", propertyName, domainClassInfo.Name));

                string attributeArgumentName;
                if (!attributeArgumentNameFromDomainPropertyName.TryGetValue(propertyName, out attributeArgumentName))
                    continue;

                // Get the value, default value, and condition
                object value = domainPropertyInfo.GetValue(modelElement);
                object defaultValue = domainPropertyInfo.DefaultValue;
                bool valueIsDefault = ((value == null) && (defaultValue == null)) || (value.Equals(defaultValue));

                // Add using directive if necessary
                if ((!valueIsDefault) && (!domainPropertyInfo.PropertyType.IsPrimitive))
                    EnsureUsings(attribute.ProjectItem, domainPropertyInfo.PropertyType.Namespace);

                // Prepare value of the attribute argument, depending on property type.
                string valueAsString = null;

                // - String
                if (domainPropertyInfo.PropertyType == typeof(string))
                {
                    // String are special as Domain properties : null and Empty are considered the same for the Domain model.
                    if ((!valueIsDefault) && (string.IsNullOrEmpty(value as string)) && (string.IsNullOrEmpty(defaultValue as string)))
                        valueIsDefault = true;

                    // String formatting
                    if (string.IsNullOrEmpty(value as string))
                        valueAsString = "\"\"";
                    else
                        valueAsString = '"' + (value as string) + '"';
                }

                // - Enum
                else if (domainPropertyInfo.PropertyType.IsEnum)
                    valueAsString = domainPropertyInfo.PropertyType.Name + "." + Convert.ToString(value);

                // - bool
                else if (domainPropertyInfo.PropertyType == typeof(bool))
                    valueAsString = ((bool)value) ? "true" : "false";

                // - Other types
                else
                {
                    try { valueAsString = Convert.ToString(value); }
                    catch (Exception) { }
                }

                // Argument
                if (valueAsString != null)
                    EnsureAttributeArgumentIff(attribute, attributeArgumentName, valueAsString, !valueIsDefault);
            }
        }

        /// <summary>
        /// Are two attribute name equal ?
        /// </summary>
        /// <param name="attributeName">Attribute name ending or not with Attribute, fully qualified or not</param>
        /// <param name="referenceAttributeFullyQualifiedName">Fully qualified name of an attribute type (ending
        /// with attribute)</param>
        /// <returns><c>true</c> is both attribute equal and false otherwise</returns>
        public static bool AttributesEqual(string attributeName, string referenceAttributeFullyQualifiedName)
        {
            // Full name with or without attribute
            if (attributeName == referenceAttributeFullyQualifiedName)
                return true;
            else if (attributeName == referenceAttributeFullyQualifiedName.Replace("Attribute", ""))
                return true;

            // Short name
            string shortName;
            int indexDot = referenceAttributeFullyQualifiedName.LastIndexOf('.');
            if (indexDot != -1)
            {
                shortName = referenceAttributeFullyQualifiedName.Substring(0, indexDot);
                if (attributeName == shortName)
                    return true;
                else if (attributeName == shortName.Replace("Attribute", ""))
                    return true;
            }
            return false;
        }

        public static T RecursiveFind<T>(CodeElements elems, Func<T, bool> fct)
        {
            foreach (CodeElement e in elems)
            {
                if (e is T && fct((T)e))
                    return (T)e;

                CodeElements elements = null;
                if (e is CodeClass)
                {
                    var clazz = e as CodeClass;
                    elements = clazz.Children;
                }
                if( e is CodeInterface)
                {
                    elements = (e as CodeInterface).Children;
                }
                if (e is CodeEnum)
                {
                    elements = (e as CodeEnum).Children;
                }
                if (e is CodeNamespace)
                {
                    var ns = e as CodeNamespace;
                    elements = ns.Children;
                }

                if( elements != null)
                {
                    var rst = RecursiveFind<T>(elements, fct);
                    if (rst != null)
                        return rst;
                }
            }
            return default(T);
        }

    }

}


