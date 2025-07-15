namespace DarkAgesModManager.DeclParser
{
    /// <summary>
    /// Decl property class
    /// </summary>
    public class DeclProperty
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name;

        /// <summary>
        /// Indicates wether or not the name of the property is quoted
        /// </summary>
        public bool IsQuoted;

        /// <summary>
        /// The value of the property
        /// </summary>
        public DeclPropertyValue Value;

        /// <summary>
        /// If this property is overriding another property in an inherited
        /// file, this will mark it as important to have preference
        /// </summary>
        public bool Important;
    }
}
