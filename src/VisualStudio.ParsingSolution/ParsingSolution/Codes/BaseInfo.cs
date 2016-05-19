using EnvDTE80;
using System;
using System.Linq;
using System.Collections.Generic;

namespace VisualStudio.ParsingSolution.Projects.Codes
{


    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class BaseInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public BaseInfo(BaseInfo parent, CodeElement2 item)
        {
            this.Name = item.Name;
            this.FullName = item.FullName;
            this.IsCodeType = item.IsCodeType;
            this.Root = parent;
            this.Source = item;
            this.Project = new NodeProject(item.ProjectItem.ContainingProject);

            try
            {
                this.Location = new LocationInfo(item.StartPoint, item.EndPoint);
            }
            catch (Exception)
            {
                this.Location = new LocationInfo(null, null);
            }

            this.IsArray = false;
            this.ElementType = null;

        }

        public virtual GenericArguments GenericArguments
        {
            get
            {
                return new GenericArguments();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is array.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is array; otherwise, <c>false</c>.
        /// </value>
        public bool IsArray { get; protected set; }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        protected CodeElement2 Source { get; private set; }

        /// <summary>
        /// Namespace
        /// </summary>
        public string Namespace { get; protected set; }

        /// <summary>
        /// Code documentation
        /// </summary>
        public string DocComment { get; protected set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        /// <value>
        /// The summary.
        /// </value>
        public string Summary { get; protected set; }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        public string FullName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is code type.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is code type; otherwise, <c>false</c>.
        /// </value>
        public bool IsCodeType { get; private set; }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        public BaseInfo Root { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enum.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is enum; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnum { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is interface.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is interface; otherwise, <c>false</c>.
        /// </value>
        public bool IsInterface { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is class.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is class; otherwise, <c>false</c>.
        /// </value>
        public bool IsClass { get; protected set; }

        /// <summary>
        /// Gets the location of the code.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public LocationInfo Location { get; private set; }

        /// <summary>
        /// Is abstract
        /// </summary>
        public bool IsAbstract { get; protected set; }

        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <value>
        /// The project.
        /// </value>
        public NodeProject Project { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is public.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is public; otherwise, <c>false</c>.
        /// </value>
        public bool IsPublic { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is private.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is private; otherwise, <c>false</c>.
        /// </value>
        public bool IsPrivate { get; protected set; }


        /// <summary>
        /// Gets a value indicating whether this instance is protected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is protected; otherwise, <c>false</c>.
        /// </value>
        public bool IsProtected { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is family or protected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is family or protected; otherwise, <c>false</c>.
        /// </value>
        public bool IsFamilyOrProtected { get; protected set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is static.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is static; otherwise, <c>false</c>.
        /// </value>
        public bool IsStatic { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is structure.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is structure; otherwise, <c>false</c>.
        /// </value>
        public bool IsStruct { get; protected set; }

        /// <summary>
        /// Determines whether [is public_ implementation] [the specified a].
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsPublic_Impl(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessPublic) == EnvDTE.vsCMAccess.vsCMAccessPublic;
        }

        /// <summary>
        /// Determines whether [is private_ implementation] [the specified a].
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsPrivate_Impl(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessPrivate) == EnvDTE.vsCMAccess.vsCMAccessPrivate;
        }

        /// <summary>
        /// Determines whether [is protected_ implementation] [the specified a].
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsProtected_Impl(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessProtected) == EnvDTE.vsCMAccess.vsCMAccessProtected;
        }

        /// <summary>
        /// Determines whether [is family_ implementation] [the specified a].
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsFamily_Impl(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessProject) == EnvDTE.vsCMAccess.vsCMAccessProject;
        }

        /// <summary>
        /// Determines whether [is family or protected_ implementation] [the specified a].
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        protected bool IsFamilyOrProtected_Impl(EnvDTE.vsCMAccess a)
        {
            return (a & EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected) == EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected;
        }

        public virtual ClassInfo GetBase()
        {
            return null;
        }

        public BaseInfo ElementType { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeAttributes(List<AttributeInfo> attributes)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeProperties(List<CodePropertyInfo> properties)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeConstructors(List<CodeFunctionInfo> contructors)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeMethods(List<CodeFunctionInfo> methods)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeEvents(List<CodeEventInfo> events)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeFields(List<CodeFieldInfo> fields)
        {

        }


        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public virtual IEnumerable<AttributeInfo> Attributes
        {
            get
            {
                return new AttributeInfo[] { };
            }
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <returns></returns>
        public IEnumerable<AttributeInfo> GetAttributes(bool inherit)
        {

            List<AttributeInfo> l = Attributes.ToList();

            if (inherit)
            {
                var _t = this.GetBase();
                while (_t != null)
                {
                    l.AddRange(_t.Attributes);
                    _t = _t.GetBase();
                }
            }

            return l;

        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <returns></returns>
        public IEnumerable<AttributeInfo> GetAttributes(string attributeType, bool inherit)
        {
            return ObjectFactory.GetAttributes(GetAttributes(inherit), attributeType).ToList();
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <returns></returns>
        public IEnumerable<AttributeInfo> GetAttributes(Type attributeType, bool inherit)
        {
            return ObjectFactory.GetAttributes(GetAttributes(inherit), attributeType).ToList();
        }

        public string AssemblyQualifiedName
        {
            get
            {

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                if (!string.IsNullOrEmpty(this.Namespace))
                {
                    sb.Append(this.Namespace);
                    sb.Append(".");
                }

                // TODO: write embeded type

                sb.Append(this.Name);

                var generics = this.GenericArguments;

                if (generics.Any())
                    sb.AppendFormat("`{0}", generics.Count());

                //sb.Append("[");
                //foreach (GenericArgument g in generics)
                //{

                //}
                //sb.Append("]");

                sb.Append(", ");

                sb.Append(this.Project.Name);

                return sb.ToString();

            }
        }

    }




}
