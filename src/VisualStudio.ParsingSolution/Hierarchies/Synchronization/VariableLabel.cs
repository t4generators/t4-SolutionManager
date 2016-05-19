using System;
using System.Collections.Generic;
using EnvDTE;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
    /// <summary>
    /// Utilities to parse or compute the label of a variable (in the form
    /// name : type)
    /// </summary>
    public class VariableLabel
    {
        /// <summary>
        /// Computes the label from name and full type name
        /// </summary>
        /// <param name="knownCodeTypes">Known code types</param>
        /// <param name="fullTypeName">Full type name for the variable</param>
        /// <param name="name">Name of the variable</param>
        /// <returns>The label</returns>
        public static string ComputeLabel(KnownCodeTypes knownCodeTypes, string fullTypeName, string name)
        {
            string label = name;
            if (!string.IsNullOrEmpty(fullTypeName))
            {
                if (knownCodeTypes == null)
                    label += " : " + KnownCodeTypes.SimplifyForCSharp(fullTypeName);
                else
                {
                    string simpleType = SimplifyTypeName(knownCodeTypes, fullTypeName);
                    label += " : " + simpleType;
                }
            }
            return label;
        }

        /// <summary>
        /// Simplify a Type name
        /// </summary>
        /// <param name="knownCodeTypes"></param>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        public static string SimplifyTypeName(KnownCodeTypes knownCodeTypes, string fullTypeName)
        {
            if (knownCodeTypes == null)
                return fullTypeName;

            string post = "";
            while (fullTypeName.EndsWith("[]"))
            {
                fullTypeName = fullTypeName.Substring(0, fullTypeName.Length - 2);
                post = "[]" + post;
            }
            CodeType theType = knownCodeTypes.GetFullNamedType(fullTypeName);
            string simpleType;
            if (theType != null)
                simpleType = KnownCodeTypes.GetShortTypeName(theType) + post;
            else
                simpleType = fullTypeName + post;
            return simpleType;
        }



        /// <summary>
        /// Parse a label in order to get the name of the variable and its full type name
        /// </summary>
        /// <param name="knownCodeTypes"></param>
        /// <param name="newValue"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public static void ParseLabel(KnownCodeTypes knownCodeTypes, string newValue, out string name, ref string type)
        {
            string[] composants = newValue.Split(':');
            if (composants.Length == 0)
                name = type = "";
            else if (composants.Length == 1)
                name = newValue;
            else
            {
                name = composants[0].Trim();
                string simplifiedType = composants[1].Trim();
                type = GetFullTypeName(knownCodeTypes, simplifiedType);

            }
        }


        /// <summary>
        /// Get the full type name from the simplifier type name
        /// </summary>
        /// <param name="knownCodeTypes">Known code types</param>
        /// <param name="simplifiedType">Simplified type</param>
        /// <returns>Full type name</returns>
        public static string GetFullTypeName(KnownCodeTypes knownCodeTypes, string simplifiedType)
        {
            string simplifiedTypeOriginal = simplifiedType;
            string suffix = "";
            bool modified = true;
            while (modified)
            {
                modified = false;
                if (simplifiedType.EndsWith("[]"))
                {
                    simplifiedType = simplifiedType.Substring(0, simplifiedType.Length - 2);
                    suffix = "[]" + suffix;
                    modified = true;
                }
                else if (simplifiedType.EndsWith("[,]"))
                {
                    simplifiedType = simplifiedType.Substring(0, simplifiedType.Length - 3);
                    suffix = "[,]" + suffix;
                    modified = true;
                }
            }

            List<CodeType> possibleTypes;
            if (knownCodeTypes != null)
                possibleTypes = InferType(knownCodeTypes, simplifiedType);
            else
                possibleTypes = new List<CodeType>();

            string type;
            if (possibleTypes.Count == 0)
                type = simplifiedTypeOriginal;
            else if (possibleTypes.Count == 1)
                type = KnownCodeTypes.SimplifyForCSharp(possibleTypes[0].FullName.Replace("+", ".")) + suffix;
            else
                type = ChooseType(simplifiedType, possibleTypes) + suffix;

            return type;
        }


        /// <summary>
        /// Chose a type among other types
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="possibleTypes"></param>
        /// <returns></returns>
        public static string ChooseType(string Type, List<CodeType> possibleTypes)
        {
            TypeChooser typeChooser = new TypeChooser();
            typeChooser.Choices = possibleTypes;
            if (typeChooser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                if (typeChooser.SelectedChoice != null)
                    return typeChooser.SelectedChoice.FullName.Replace("+", ".");
            return Type;
        }


        /// <summary>
        /// Infers the types from a single type name
        /// </summary>
        /// <param name="knownCodeTypes"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<CodeType> InferType(KnownCodeTypes knownCodeTypes, string type)
        {
            return new List<CodeType>(knownCodeTypes.GetNamedTypes(type));
        }
    }
}