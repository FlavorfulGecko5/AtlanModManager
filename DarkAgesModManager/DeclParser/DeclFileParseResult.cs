namespace DarkAgesModManager.DeclParser
{
    /// <summary>
    /// Decl file parse result class
    /// </summary>
    public class DeclFileParseResult
    {
        /// <summary>
        /// Warnings thrown while parsing
        /// </summary>
        public string[] Warnings;

        /// <summary>
        /// Errors found while parsing
        /// </summary>
        public string[] Errors;

        /// <summary>
        /// The parsed decl file
        /// </summary>
        public DeclFile DeclFile;
    }
}
