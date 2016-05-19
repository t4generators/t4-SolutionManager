//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using EnvDTE80;
//using VsxFactory.Modeling.VisualStudio.Synchronization;
//using EnvDTE;
//using VsxFactory.Modeling.Strategies;

//namespace VsxFactory.Modeling.VisualStudio.Synchronization
//{
//    public struct Argument
//    {
//        public object Type;
//        public string Name;

//        public Argument(object type, string name)
//        {
//            Type = type;
//            Name = name;
//        }
//    }

//    public class CodeDomClassHelper
//    {
//        private INamingStrategy _namingStrategy;
//        private CodeClass2 _clazz;

//        public CodeDomClassHelper(CodeClass2 clazz, INamingStrategy namingStrategy)
//        {
//            _namingStrategy = namingStrategy;
//            _clazz = clazz;
//        }

//        public CodeFunction EnsureFunction(string name, vsCMAccess access, object type, params Argument[] parameters)
//        {
//            var fct = FindFunction(name, parameters);
//            string code = null;
//            if (fct != null)
//            {
//                code = GetFunctionBody(fct);
//                _clazz.RemoveMember(fct);
//            }

//            fct = _clazz.AddFunction(name, name==_clazz.Name ? vsCMFunction.vsCMFunctionConstructor : vsCMFunction.vsCMFunctionFunction, type, -1, access);
//            if( parameters != null)
//            {
//                for(int i = 0;i<parameters.Length;i++)
//                    fct.AddParameter(parameters[i].Name, parameters[i].Type);
//            }
//            if( code != null)
//                SetFunctionBody(fct, code);
//            return fct;
//        }

//        public CodeFunction FindFunction(string name, params Argument[] parameters)
//        {
//            foreach (CodeElement member in _clazz.Children)
//            {
//                if (member is CodeFunction && member.Name == name)
//                {
//                    CodeFunction fct = member as CodeFunction;
//                    if (parameters != null)
//                    {
//                        if (fct.Parameters.Count == parameters.Length)
//                        {
//                            int i=0;
//                            bool founded = true;
//                            foreach (CodeParameter param in fct.Parameters)
//                            {
//                                if (param.Name != parameters[i].Name || param.Type.AsString != parameters[i].Type.ToString())
//                                {
//                                    founded = false;
//                                    break;
//                                }
//                                i++;
//                            }
//                            if (founded)
//                                return fct;
//                        }
//                    }
//                }
//            }
//            return null;
//        }

//        public void RemoveProperty(string name)
//        {
//            var fieldName = _namingStrategy.CreatePrivateVariableName(name);
//            RemoveField(fieldName);
//            var member = IncrementalGenerator.RecursiveFind<CodeProperty>(_clazz.Members, e => e.Name == name);
//            if (member != null)
//               _clazz.RemoveMember(member);
//        }

//        public CodeProperty EnsureProperty(string name, object type, vsCMAccess getterAccess = vsCMAccess.vsCMAccessPublic, vsCMAccess setterAcces=vsCMAccess.vsCMAccessPublic, string oldName=null)
//        {
//            string keyName = oldName ?? name;
//            var property = IncrementalGenerator.RecursiveFind<CodeProperty>(_clazz.Members, e => e.Name == keyName);
//            if (property != null)
//                _clazz.RemoveMember(property); // Pas trouvé mieux pour modifier le type
//            property = _clazz.AddProperty(name, name, type, -1, getterAccess);
//            string setterCode = (getterAccess != setterAcces ? GetAccessAsString(setterAcces) : "") + "set;";
//            SetFunctionBody(property.Setter, setterCode, vsCMPart.vsCMPartWholeWithAttributes);
//            SetFunctionBody(property.Getter, "get;", vsCMPart.vsCMPartWholeWithAttributes);
//            return property;
//        }

//        private string GetAccessAsString(vsCMAccess access)
//        {
//            switch (access)
//            {
//                case vsCMAccess.vsCMAccessProject:
//                case vsCMAccess.vsCMAccessAssemblyOrFamily:
//                    return "internal ";
//                case vsCMAccess.vsCMAccessPrivate:
//                    return "private ";
//                case vsCMAccess.vsCMAccessProjectOrProtected:
//                case vsCMAccess.vsCMAccessProtected:
//                    return "protected ";
//                case vsCMAccess.vsCMAccessPublic:
//                    return "public ";
//            }
//            return "private ";
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="clazz"></param>
//        /// <param name="name"></param>
//        /// <param name="type"></param>
//        /// <param name="getter"></param>
//        /// <param name="setter"></param>
//        /// <returns></returns>
//        public CodeProperty EnsureProperty(string name, object type, string getterCode, string setterCode, string oldName = null)
//        {
//            var fieldName = _namingStrategy.CreatePrivateVariableName(name);
//            var oldFieldName = oldName != null ? _namingStrategy.CreatePrivateVariableName(oldName) : null;
//            EnsureField(fieldName, type, oldFieldName);

//            string keyName = oldName ?? name;
//            var property = IncrementalGenerator.RecursiveFind<CodeProperty>(_clazz.Members, e => e.Name == keyName);
//            if (property != null)
//            {
//                // TODO mettre un regex
//                getterCode = GetFunctionBody(property.Getter).Replace(fieldName, "{0}").Replace(name, "{1}");
//                setterCode = GetFunctionBody(property.Setter).Replace(fieldName, "{0}").Replace(name, "{1}");
//                _clazz.RemoveMember(property); // Pas trouvé mieux pour modifier le type
//            }
//            getterCode = String.Format(getterCode, fieldName, name);
//            setterCode = String.Format(setterCode, fieldName, name);

//            property = _clazz.AddProperty(name, name, type, -1, vsCMAccess.vsCMAccessPublic);
//            SetFunctionBody(property.Getter, getterCode);
//            SetFunctionBody(property.Setter, setterCode);
//            return property;
//        }

//        private void RemoveField(string name)
//        {
//            var member = IncrementalGenerator.RecursiveFind<CodeVariable>(_clazz.Members, e => e.Name == name);
//            if (member != null)
//                _clazz.RemoveMember(member);
//        }

//        public CodeVariable EnsureField(string fieldName, object type, string oldName=null)
//        {
//            string keyName = oldName ?? fieldName;
//            var field = IncrementalGenerator.RecursiveFind<CodeVariable>(_clazz.Members, e => e.Name == keyName);
//            if (field != null)
//            {
//                if (field.Name == fieldName && field.Type.AsString == type)
//                    return field;
//                _clazz.RemoveMember(field);
//            }
//            return _clazz.AddVariable(fieldName, type, -1, vsCMAccess.vsCMAccessPrivate, null);
//        }

//        public static void SetFunctionBody(CodeFunction fct, string code, vsCMPart scope= vsCMPart.vsCMPartBody)
//        {
//            var editPoint = fct.GetStartPoint(scope).CreateEditPoint();
//            TextPoint finish = fct.GetEndPoint(scope);
//            editPoint.ReplaceText(finish, code, 0);
//            try
//            {
//                editPoint = fct.GetStartPoint(scope).CreateEditPoint();             
//                editPoint.SmartFormat(finish); 
//            } catch { }
//        }

//        public static string GetFunctionBody(CodeFunction fct)
//        {
//            TextPoint start = fct.GetStartPoint(vsCMPart.vsCMPartBody);
//            TextPoint finish = fct.GetEndPoint(vsCMPart.vsCMPartBody);
//            return start.CreateEditPoint().GetText(finish);
//        }
//    }
//}
