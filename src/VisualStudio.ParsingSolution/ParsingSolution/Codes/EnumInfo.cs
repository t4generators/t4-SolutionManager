using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{FullName}")]
    public class EnumInfo : BaseInfo
    {

        private CodeEnum _enum;
        private List<CodeFieldInfo> _fields;
        private TypeInfo type = null;
        private List<AttributeInfo> _attributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumInfo"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="item">The item.</param>
        public EnumInfo(NodeItem parent, CodeEnum item)
            : base(null, item as CodeElement2)
        {

            this._enum = item as CodeEnum;
            this.IsEnum = true;
            this.Namespace = item.Namespace.FullName;
            this.DocComment = this._enum.DocComment;

            IsPublic = this.IsPublic_Impl(this._enum.Access);
            IsPrivate = this.IsPrivate_Impl(this._enum.Access);
            IsProtected = this.IsProtected_Impl(this._enum.Access);
            IsFamilyOrProtected = this.IsFamilyOrProtected_Impl(this._enum.Access);

            this.IsStruct = true;
            this.IsStatic = true;

            GetFields();

        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodeFieldInfo> GetFields()
        {

            if (_fields == null)
            {

                _fields = new List<CodeFieldInfo>();

                var _members = _enum.Members.OfType<CodeElement2>()
                    .Select(c => ObjectFactory.Instance.CreateEnumValue(this, c, type))
                    .Where(d => d != null)
                    .ToList();

                _fields.AddRange(_members);


                InitializeFields(_fields);

            }

            return _fields;

        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public override IEnumerable<AttributeInfo> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    try
                    {
                        _attributes = ObjectFactory.GetAttributes(_enum.Attributes);
                    }
                    catch (Exception)
                    {
                        _attributes = new List<AttributeInfo>();
                    }

                    InitializeAttributes(_attributes);

                }
                return _attributes;
            }
        }

    }

}
