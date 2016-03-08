using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{


    /// <summary>
    /// code parser object factory.
    ///you can override the methods for customize the object results.
    /// </summary>
    public class ObjectFactory
    {

        private static ObjectFactory _instance;

        /// <summary>
        /// 
        /// </summary>
        public static ObjectFactory Instance
        {

            get
            {
                if (_instance == null)
                    _instance = new ObjectFactory();
                return _instance;
            }
            set
            {
                _instance = value;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool AcceptInterface(CodeInterface2 c)
        {
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        public virtual bool AcceptClass(CodeClass2 c)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool AcceptEnum(CodeEnum e)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public NodeProject DefaultProject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual TypeInfo CreateType(CodeTypeRef type)
        {
            return new TypeInfo(type);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual EnumInfo CreateEnum(NodeItem parent, CodeEnum item)
        {
            return new EnumInfo(parent, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual CodeFieldInfo CreateEnumValue(EnumInfo parent, CodeElement item, TypeInfo type)
        {
            return new CodeFieldInfo(parent, item, type);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual ClassInfo CreateClass(NodeItem parent, CodeClass2 item)
        {
            return new ClassInfo(parent, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual InterfaceInfo CreateInterface(NodeItem parent, CodeInterface2 item)
        {
            return new InterfaceInfo(parent, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual AttributeInfo CreateAttribute(CodeAttribute2 item)
        {
            return new AttributeInfo(item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual CodeFunctionInfo CreateMethod(BaseInfo parent, CodeFunction2 item)
        {
            return new CodeFunctionInfo(parent, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual AttributeArgumentInfo CreateAttributeArgument(EnvDTE80.CodeAttributeArgument item)
        {
            return new AttributeArgumentInfo(item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual MethodParamInfo CreatParameter(CodeFunctionInfo parent, CodeParameter2 item, string parameterComment)
        {
            return new MethodParamInfo(parent, item, parameterComment);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual CodePropertyInfo CreateProperty(BaseInfo parent, CodeProperty2 item)
        {
            return new CodePropertyInfo(parent, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual CodeEventInfo CreateEvent(BaseInfo parent, EnvDTE80.CodeEvent item)
        {
            return new CodeEventInfo(parent, item);
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<AttributeInfo> GetAttributes(IEnumerable<AttributeInfo> attributes, string attributeType)
        {

            var ar = attributeType.Split(',');

            foreach (AttributeInfo attr in attributes)
                if (ar.Contains(attr.FullName, StringComparer.OrdinalIgnoreCase))
                    yield return attr;

            yield break;

        }

        /// <summary>
        /// 
        /// </summary>
        public static List<AttributeInfo> GetAttributes(CodeClass2 type)
        {


            var l = new List<AttributeInfo>();

            while (type != null)
            {
                var attribute = GetAttributes(type.Attributes);
                l.AddRange(attribute);
                if (type.Bases.Count == 0)
                    return null;
                type = (CodeClass2)type.Bases.Item(1);
            }

            return l;

        }

        /// <summary>
        /// 
        /// </summary>
        public static List<AttributeInfo> GetAttributes(CodeElements attributes)
        {
            List<AttributeInfo> _result = new List<AttributeInfo>();
            for (int i = 1; i <= attributes.Count; i++)
            {
                try
                {
                    var attrib = (CodeAttribute2)attributes.Item(i);
                    _result.Add(ObjectFactory.Instance.CreateAttribute(attrib));
                }
                catch
                {
                    continue;
                }
            }
            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        public static CMAccess Convert(EnvDTE.vsCMAccess item)
        {

            switch (item)
            {

                case EnvDTE.vsCMAccess.vsCMAccessAssemblyOrFamily:
                    return CMAccess.AssemblyOrFamily;
                case EnvDTE.vsCMAccess.vsCMAccessDefault:
                    return CMAccess.Default;
                case EnvDTE.vsCMAccess.vsCMAccessPrivate:
                    return CMAccess.Private;
                case EnvDTE.vsCMAccess.vsCMAccessProject:
                    return CMAccess.Project;
                case EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected:
                    return CMAccess.ProjectOrProtected;
                case EnvDTE.vsCMAccess.vsCMAccessProtected:
                    return CMAccess.Protected;
                case EnvDTE.vsCMAccess.vsCMAccessPublic:
                    return CMAccess.Public;
                case EnvDTE.vsCMAccess.vsCMAccessWithEvents:
                    return CMAccess.WithEvents;

            }

            return CMAccess.Default;

        }
    }


}
