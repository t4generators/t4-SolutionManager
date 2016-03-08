using EnvDTE;
using EnvDTE80;
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

        /// <summary>
        /// 
        /// </summary>
        public EnumInfo(NodeItem parent, CodeEnum item)
            : base(null, item as CodeElement2)
        {

            this._enum = item as CodeEnum;
            this.IsEnum = true;
            this.Namespace = item.Namespace.FullName;
            this.DocComment = this._enum.DocComment;

            GetFields();

        }

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        public virtual void InitializeFields(List<CodeFieldInfo> fields)
        {

        }

    }

}
