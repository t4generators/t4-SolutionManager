using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// type info reference
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ClassName}")]
    public class TypeInfo
    {

        private List<TypeInfo> _list = new List<TypeInfo>();
        private int _rank = 0;
        private CodeTypeRef type;
        private IEnumerable<AttributeInfo> _attributes;
        private BaseInfo _classRef;
        private List<CodePropertyInfo> PropertyInfolst;
        private List<CodeEventInfo> EventInfolst;
        private List<CodeFunctionInfo> MethodInfolst;
        private Project project;

        TypeInfo()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public TypeInfo(CodeTypeRef type)
        {

            Set(type);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Set(CodeTypeRef type)
        {

            this._list = new List<TypeInfo>();
            this._rank = 0;
            this._attributes = null;
            this._classRef = null;
            this.IsVoid = false;
            this.IsEnumerable = false;
            this.IsClass = false;
            this.MethodInfolst = null;
            this.EventInfolst = null;
            this.PropertyInfolst = null;

            this.type = type;

            switch (type.TypeKind)
            {

                case vsCMTypeRef.vsCMTypeRefArray:
                case vsCMTypeRef.vsCMTypeRefCodeType:
                case vsCMTypeRef.vsCMTypeRefBool:
                case vsCMTypeRef.vsCMTypeRefByte:
                case vsCMTypeRef.vsCMTypeRefDecimal:
                case vsCMTypeRef.vsCMTypeRefDouble:
                case vsCMTypeRef.vsCMTypeRefFloat:
                case vsCMTypeRef.vsCMTypeRefInt:
                case vsCMTypeRef.vsCMTypeRefLong:
                case vsCMTypeRef.vsCMTypeRefShort:
                case vsCMTypeRef.vsCMTypeRefVariant:
                case vsCMTypeRef.vsCMTypeRefChar:
                case vsCMTypeRef.vsCMTypeRefString:
                    Name = type.AsString;
                    break;

                case vsCMTypeRef.vsCMTypeRefVoid:
                    Name = "void";
                    break;

                case vsCMTypeRef.vsCMTypeRefOther:
                case vsCMTypeRef.vsCMTypeRefPointer:
                case vsCMTypeRef.vsCMTypeRefObject:
                    Name = type.AsFullName;
                    break;

            }

            this.KindType = type.TypeKind.ToString().Substring(11);

            if (!(this.IsVoid = (type.TypeKind == vsCMTypeRef.vsCMTypeRefVoid)))
            {

                this.IsClass = (type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType) || type.TypeKind == vsCMTypeRef.vsCMTypeRefString;

                if (!this.IsClass && type.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
                    this.IsClass = (type.Rank > 0 && type.ElementType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType);

                else if (!this.IsClass && type.TypeKind != vsCMTypeRef.vsCMTypeRefOther)
                    this.IsEnumerable = type.CodeType.get_IsDerivedFrom(typeof(System.Collections.IEnumerable).FullName);

                if (type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                    if (type.CodeType.InfoLocation != vsCMInfoLocation.vsCMInfoLocationExternal)
                        this.project = this.type.CodeType.ProjectItem.ContainingProject;

            }

            int i = 0;
            Parse(Name, this, ref i);
            DispatchType();
            OnCreated();

            /*
				vsCMTypeRefOther,
				vsCMTypeRefCodeType,
				vsCMTypeRefArray,
				vsCMTypeRefVoid,
				vsCMTypeRefPointer,
				vsCMTypeRefString,
				vsCMTypeRefObject,
				vsCMTypeRefByte,
				vsCMTypeRefChar,
				vsCMTypeRefShort,
				vsCMTypeRefInt,
				vsCMTypeRefLong,
				vsCMTypeRefFloat,
				vsCMTypeRefDouble,
				vsCMTypeRefDecimal,
				vsCMTypeRefBool,
				vsCMTypeRefVariant
			*/
        }

        /// <summary>
        /// 
        /// </summary>
        public CodeTypeRef Source
        {
            get
            {
                return type;
            }
            private set
            {
                Set(value);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<CodeFunctionInfo> Methods()
        {

            if (MethodInfolst == null)
            {
                if (type != null)
                {

                    if (type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                        MethodInfolst = type.CodeType.Members.OfType<CodeFunction2>().Select(c => ObjectFactory.Instance.CreateMethod(null, c)).ToList();
                    else if (type.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
                        MethodInfolst = type.ElementType.CodeType.Members.OfType<CodeFunction2>().Select(c => ObjectFactory.Instance.CreateMethod(null, c)).ToList();
                }
            }

            return MethodInfolst;

        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<CodePropertyInfo> Properties()
        {

            if (PropertyInfolst == null)
            {
                if (type != null)
                {
                    if (type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                        PropertyInfolst = type.CodeType.Members.OfType<CodeProperty2>().Select(c => ObjectFactory.Instance.CreateProperty(null, c)).ToList();
                    else if (type.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
                        PropertyInfolst = type.ElementType.CodeType.Members.OfType<CodeProperty2>().Select(c => ObjectFactory.Instance.CreateProperty(null, c)).ToList();
                }
            }

            return PropertyInfolst;

        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<CodeEventInfo> Events()
        {

            if (EventInfolst == null)
            {
                if (type != null)
                {

                    if (type.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType)
                        EventInfolst = type.CodeType.Members.OfType<EnvDTE80.CodeEvent>().Select(c => ObjectFactory.Instance.CreateEvent(null, c)).ToList();
                    else if (type.TypeKind == vsCMTypeRef.vsCMTypeRefArray)
                        EventInfolst = type.ElementType.CodeType.Members.OfType<EnvDTE80.CodeEvent>().Select(c => ObjectFactory.Instance.CreateEvent(null, c)).ToList();
                }
            }

            return EventInfolst;

        }

        /// <summary>
        /// Gets the class reference.
        /// </summary>
        /// <value>
        /// The class reference.
        /// </value>
        public BaseInfo ClassRef
        {
            get
            {

                if (_classRef == null)
                {
                    _classRef = ProjectHelper.GetContext().ResolveType(this).FirstOrDefault();
                }

                return _classRef;

            }
        }



        /// <summary>
        /// Creates the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static TypeInfo Create(CodeTypeRef type)
        {
            TypeInfo t = ObjectFactory.Instance.CreateType(type);
            return t;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void DispatchType()
        {

            if (this.project == null && ObjectFactory.Instance.DefaultProject != null)
            {
                this.project = ObjectFactory.Instance.DefaultProject.Project;
            }

            if (this.project != null)
            {

                foreach (TypeInfo t in this.ElementItems)
                {
                    if (t.Source == null)
                        t.Source = this.project.CodeModel.CreateCodeTypeRef(t.ToString());

                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnCreated()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsDerivedFrom(string type)
        {

            try
            {
                return this.type.CodeType.get_IsDerivedFrom(type);
            }
            catch
            {

            }

            return false;

        }


        private void Parse(string type, TypeInfo t, ref int p)
        {


            System.Text.StringBuilder s = new System.Text.StringBuilder();
            for (int i = p; i < type.Length; i++)
            {

                deb:
                var c = type[i];
                switch (c)
                {

                    case '<':
                        i++;
                        p = i;
                        t.Name = s.ToString();
                        var t2 = new TypeInfo();
                        t.Add(t2);
                        Parse(type, t2, ref p);
                        i = p;

                        if (i < type.Length)
                        {

                            c = type[p];

                            if (c != ',')
                                goto deb;

                            while ((c = type[p]) == ',')
                            {
                                p++;
                                t2 = new TypeInfo();
                                t.Add(t2);
                                Parse(type, t2, ref p);
                                i = p;
                                if (i >= type.Length)
                                    return;
                            }

                            c = type[p];

                            if (c != ',')
                                goto deb;

                        }

                        return;

                    case '>':
                        p = i;
                        p++;
                        t.Name = s.ToString();
                        return;

                    case '[':
                        t.Name = s.ToString();
                        p = i;
                        t.ParseArray(type, ref p);
                        i = p;
                        break;

                    case ',':
                        t.Name = s.ToString();
                        p = i;
                        return;

                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        break;


                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case '.':
                    case '_':
                        s.Append(c);
                        break;

                    case '?':
                        t.IsNullable = true;
                        s.Append(c);
                        break;
                    default:
                        throw new FormatException(type);

                }

            }

        }

        private void ParseArray(string type, ref int p)
        {
            for (int i = p + 1; i < type.Length; i++)
            {
                p = i;
                _rank++;
                var c = type[i];
                if (c == ']')
                    break;
            }
        }

        private void Add(TypeInfo t2)
        {
            this._list.Add(t2);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string KindType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsVoid { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsClass { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsEnumerable { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsGeneric { get { return this.ElementItems.Any(); } }

        /// <summary>
        /// 
        /// </summary>
        public string AsFullName
        {
            get
            {
                return this.type.AsFullName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Rank { get { return _rank; } }

        /// <summary>
        /// 
        /// </summary>
        public string ClassName
        {
            get
            {
                var i = Name.LastIndexOf('.');
                if (i > 0)
                    return Name.Substring(i + 1);

                return Name;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ContainsType(string type)
        {
            if (Name == type)
                return true;
            foreach (var item in ElementItems)
            {
                var t = item.ContainsType(type);
                if (t)
                    return true;
            }

            return false;

        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<TypeInfo> ElementItems { get { return this._list; } }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<AttributeInfo> Attributes
        {
            get
            {
                if (_attributes == null)
                    _attributes = ObjectFactory.GetAttributes(type.CodeType.Attributes);
                return _attributes;

            }
        }

        protected IEnumerable<AttributeInfo> GetAttributes(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).ToList();
        }

        protected AttributeInfo GetAttribute(string attributeType)
        {
            return ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
        }

        protected void ForAttributes(string attributeType, Action<AttributeInfo> act)
        {
            foreach (AttributeInfo attr in ObjectFactory.GetAttributes(Attributes, attributeType))
                act(attr);
        }

        protected string GetArgumentFromAttribute(string attributeType, string argumentName)
        {
            AttributeInfo attr = ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
            if (attr != null)
            {
                AttributeArgumentInfo arg = attr.Arguments.FirstOrDefault(a => a.Name == argumentName);
                if (arg != null)
                    return arg.Value;
            }
            return string.Empty;
        }

        protected string GetArgumentFromAttribute(string attributeType, int indexArgument)
        {
            AttributeInfo attr = ObjectFactory.GetAttributes(Attributes, attributeType).FirstOrDefault();
            if (attr != null)
            {
                AttributeArgumentInfo arg = (attr.Arguments as List<AttributeArgumentInfo>)[indexArgument];
                if (arg != null)
                    return arg.Value;
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {

            System.Text.StringBuilder s = new System.Text.StringBuilder();

            s.Append(Name);

            if (this.ElementItems.Count() > 0)
            {

                s.Append("<");
                bool a = false;

                foreach (var item in this.ElementItems)
                {
                    if (a)
                        s.Append(", ");
                    s.Append(item.ToString());
                    a = true;
                }

                s.Append(">");

            }

            if (_rank > 0)
            {
                s.Append("[");
                for (int m = 1; m < _rank; m++)
                    s.Append(",");
                s.Append("]");
            }

            return s.ToString();

        }

    }


}
