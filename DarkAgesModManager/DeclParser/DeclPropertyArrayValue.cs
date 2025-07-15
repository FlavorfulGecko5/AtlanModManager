using System.Collections.Generic;

namespace DarkAgesModManager.DeclParser
{
    /// <summary>
    /// Decl property array value
    /// </summary>
    public class DeclPropertyArrayValue : DeclPropertyValue
    {
        /// <summary>
        /// The values of the array
        /// </summary>
        public List<DeclPropertyValue> Values;
    }
}
