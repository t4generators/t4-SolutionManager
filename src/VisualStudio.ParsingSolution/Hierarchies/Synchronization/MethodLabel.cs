using System;
using System.Collections.Generic;
using EnvDTE;
using System.Text;

namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
 /// <summary>
 /// Utilities to parse or compute the label of a method (in the form
 /// of its simplified prototype)
 /// </summary>
 internal class MethodLabel
 {
  /// <summary>
  /// Computes the prototype of a method from its name, return type, and parameters.
  /// </summary>
  /// <param name="name">Name of the method</param>
  /// <param name="returnType">Return type</param>
  /// <param name="parameters">Parameters</param>
  /// <returns></returns>
  public static string ComputePrototype(string name, string returnType, List<KeyValuePair<string, string>> parameters)
  {
   List<string> parameterString = new List<string>();
   foreach (KeyValuePair<string, string> parameter in parameters)
    parameterString.Add(parameter.Value + " " + parameter.Key);

   StringBuilder sb = new StringBuilder();
   if (!string.IsNullOrEmpty(returnType))
    sb.Append(returnType.Trim() + " ");
   sb.Append(name);
   sb.Append('(');
   sb.Append(string.Join(", ", parameterString.ToArray()));
   sb.Append(')');
   return sb.ToString();
  }


  /// <summary>
  /// Computes the signature of the method from its name, return type, and parameters.
  /// This signature is computed to be the same as the result of the 
  /// <c>CodeFunction.get_Prototype((int)(vsCMPrototype.vsCMPrototypeType | vsCMPrototype.vsCMPrototypeParamTypes))</c> method.
  /// </summary>
  /// <param name="name">Name of the method</param>
  /// <param name="returnType">Return type of the method</param>
  /// <param name="parameters">Parameters of the method (Key is the name of the parameter, Value is the type)</param>
  /// <returns>The signature</returns>
  public static string ComputeSignature(string name, string returnType, List<KeyValuePair<string, string>> parameters)
  {
   List<string> parameterString = new List<string>();
   foreach (KeyValuePair<string, string> parameter in parameters)
    parameterString.Add(parameter.Value);

   StringBuilder sb = new StringBuilder();
   if (!string.IsNullOrEmpty(returnType))
    sb.Append(returnType.Trim() + " ");

   sb.Append(name);
   sb.Append(" (");
   sb.Append(string.Join(", ", parameterString.ToArray()));
   sb.Append(')');
   return sb.ToString();
  }

  /// <summary>
  /// Compute the label from the prototype elements
  /// </summary>
  /// <param name="knownCodeTypes"></param>
  /// <param name="name"></param>
  /// <param name="returnType"></param>
  /// <param name="parameters"></param>
  public static string ComputeLabel(KnownCodeTypes knownCodeTypes, string name, string returnType, List<KeyValuePair<string, string>> parameters)
  {
   List<string> parameterString = new List<string>();
   foreach (KeyValuePair<string, string> parameter in parameters)
   {
    string parameterName = parameter.Key;
    string fullTypeName = parameter.Value;
    string shortTypeName = VariableLabel.SimplifyTypeName(knownCodeTypes, fullTypeName);
    parameterString.Add(shortTypeName + " " + parameterName);
   }

   string returnShortType = VariableLabel.SimplifyTypeName(knownCodeTypes, returnType);
/*
   if (knownCodeTypes != null)
   {
    CodeType codeType = knownCodeTypes.GetFullNamedType(returnType);
    if (codeType != null)
     returnShortType = KnownCodeTypes.GetShortTypeName(codeType);
   }
*/
   StringBuilder sb = new StringBuilder();
   if (!string.IsNullOrEmpty(returnShortType))
    sb.Append(returnShortType.Trim() + " ");

   sb.Append(name);
   sb.Append('(');
   sb.Append(string.Join(", ", parameterString.ToArray()));
   sb.Append(')');
   return sb.ToString();
  }



  /// <summary>
  /// Parse the label
  /// </summary>
  /// <param name="knownCodeTypes"></param>
  /// <param name="label"></param>
  /// <param name="name"></param>
  /// <param name="returnType"></param>
  /// <param name="parameters"></param>
  public static void ParseLabel(KnownCodeTypes knownCodeTypes, string label, out string name, out string returnType, out List<KeyValuePair<string, string>> parameters)
  {
   // Verify parenthesis
   int indexBeginningOfParameters = label.IndexOf('(');
   int indexEndOfParametes = label.LastIndexOf(')');
   if ((indexBeginningOfParameters == -1) || (indexEndOfParametes == -1))
    throw new ArgumentException(string.Format("Method signature '{0}' is incorrect : missing parenthesis", label));

   // Find return type, and name
   int indexFirstSpace = label.IndexOf(' ');
   if ((indexFirstSpace == -1) || (indexFirstSpace > indexBeginningOfParameters))
   {
    returnType = "void";
    name = label.Substring(0, indexBeginningOfParameters - 1).Trim();
   }
   else
   {
    returnType = label.Substring(0, indexFirstSpace).Trim();
    if ((knownCodeTypes!=null) && (knownCodeTypes.GetNamedTypes(returnType).Length == 1))
     returnType = knownCodeTypes.GetNamedTypes(returnType)[0].FullName;
    name = label.Substring(indexFirstSpace + 1, indexBeginningOfParameters - 1 - indexFirstSpace).Trim();
   }

   // Parse parameters
   parameters = new List<KeyValuePair<string, string>>();
   if (indexEndOfParametes != -1)
   {
    string parameterString = label.Substring(indexBeginningOfParameters + 1, indexEndOfParametes - 1 - indexBeginningOfParameters);
    string[] parameterArray = parameterString.Split(',');
    if (!string.IsNullOrEmpty(parameterString))
    {
     int parameterIndex = 0;
     foreach (string parameter in parameterArray)
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
       throw new ArgumentException(string.Format("Method signature '{0}' is incorrect (parameter #{1})", label, parameterIndex));

      parameterIndex++;
      if ((knownCodeTypes != null) && (knownCodeTypes.GetNamedTypes(parameterType).Length == 1))
       parameterType = knownCodeTypes.GetNamedTypes(parameterType)[0].FullName;
      parameters.Add(new KeyValuePair<string, string>(parameterName, parameterType));
     }
    }
   }
  }


  /// <summary>
  /// Compute the Label from the prototype
  /// </summary>
  /// <param name="prototype"></param>
  /// <param name="name"></param>
  /// <param name="returnType"></param>
  /// <param name="parameters"></param>
  /// <returns></returns>
  public static void ParsePrototype(string prototype, out string name, out string returnType, out List<KeyValuePair<string, string>> parameters)
  {
   // Verify parenthesis
   int indexBeginningOfParameters = prototype.IndexOf('(');
   int indexEndOfParametes = prototype.LastIndexOf(')');
   if ((indexBeginningOfParameters == -1) || (indexEndOfParametes == -1))
    throw new ArgumentException(string.Format("Method signature '{0}' is incorrect : missing parenthesis", prototype));

   // Find return type, and name
   int indexFirstSpace = prototype.IndexOf(' ');
   if ((indexFirstSpace == -1) || (indexFirstSpace > indexBeginningOfParameters))
   {
    returnType = "void";
    name = prototype.Substring(0, indexBeginningOfParameters - 1).Trim();
   }
   else
   {
    returnType = prototype.Substring(0, indexFirstSpace).Trim();
    name = prototype.Substring(indexFirstSpace + 1, indexBeginningOfParameters - 1 - indexFirstSpace).Trim();
   }

   // Parse parameters
   parameters = new List<KeyValuePair<string, string>>();
   if (indexEndOfParametes != -1)
   {
    string parameterString = prototype.Substring(indexBeginningOfParameters + 1, indexEndOfParametes - 1 - indexBeginningOfParameters);
    string[] parameterArray = parameterString.Split(',');
    if (!string.IsNullOrEmpty(parameterString))
    {
     int parameterIndex = 0;
     foreach (string parameter in parameterArray)
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
       throw new ArgumentException(string.Format("Method signature '{0}' is incorrect (parameter #{1})", prototype, parameterIndex));
      parameterIndex++;
      parameters.Add(new KeyValuePair<string, string>(parameterName, parameterType));
     }
    }
   }
  }
 }
}