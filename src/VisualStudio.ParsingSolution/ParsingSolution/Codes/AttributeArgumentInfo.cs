namespace VisualStudio.ParsingSolution.Projects.Codes
{

    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class AttributeArgumentInfo
    {

        private EnvDTE80.CodeAttributeArgument item;

        /// <summary>
        /// 
        /// </summary>
        public AttributeArgumentInfo(EnvDTE80.CodeAttributeArgument item)
        {

            this.item = item;
            this.Name = this.item.Name;
            this.Value = this.item.Value;

        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Value { get; private set; }

    }


}
