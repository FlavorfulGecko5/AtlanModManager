using System.Collections.Generic;

namespace DarkAgesModManager.DeclParser
{
    /// <summary>
    /// Decl property object value
    /// </summary>
    public class DeclPropertyObjectValue : DeclPropertyValue
    {
        /// <summary>
        /// List of decl properties this object contains
        /// </summary>
        public List<DeclProperty> Value;
    }
}
