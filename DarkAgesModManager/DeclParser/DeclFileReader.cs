using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DarkAgesModManager.DeclParser
{
    /// <summary>
    /// Decl file parser class
    /// </summary>
    public class DeclFileReader
    {
        /// <summary>
        /// List of warnings thrown while parsing
        /// </summary>
        internal List<string> Warnings;

        /// <summary>
        /// List of errors found while parsing
        /// </summary>
        internal List<string> Errors;

        /// <summary>
        /// Parses the given decl file as a string into a DeclFile object
        /// </summary>
        /// <param name="declFileContents">decl file contents as a string</param>
        /// <returns>a DeclFile object with the parsed data from the decl file</returns>
        public DeclFileParseResult Parse(string declFileContents)
        {
            Warnings = new List<string>();
            Errors = new List<string>();

            DeclFile declFile = new DeclFile();

            var lines = declFileContents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            // Quick and dirty hack to allow opening braces on a different line to the property name
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("{") && i - 1 >= 0)
                {
                    lines[i - 1] += $" {{";
                    lines[i] = "";
                }
            }

            var properties = new List<DeclProperty>();

            for (var i = 0; i < lines.Length; i++)
            {
                // Remove comments from lines
                lines[i] = RemoveComments(lines[i], "//");
                lines[i] = lines[i].Trim();

                if (string.IsNullOrEmpty(lines[i]))
                {
                    continue;
                }

                // Everything inside the decl file must be a property of some kind
                DeclProperty declProperty = null;

                // Get whole properties and pass them to the property parser
                string[] propertyText = lines[i].Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (propertyText.Length != 2)
                {
                    propertyText = lines[i].Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                }

                if (propertyText.Length != 2)
                {
                    if (lines[i].Equals("{") || lines[i].Equals("}"))
                    {
                        continue;
                    }
                    else
                    {
                        Errors.Add(string.Format("Line {0}: unexpected '{1}'", i, lines[i]));
                        continue;
                    }
                }

                propertyText[0] = propertyText[0].Trim();
                propertyText[1] = propertyText[1].Trim();

                // First we need to determine which kind of property this to correclty parse it
                // This is probably a "single-line" property, so we can just pass the whole line to the property parser
                if (propertyText[1][propertyText[1].Length - 1] != '{')
                {
                    try
                    {
                        declProperty = ParseProperty(lines[i], ref i);
                    }
                    catch (FormatException)
                    {
                        throw;
                    }
                }
                else
                {
                    // Check if the "Important" flag is present
                    if (propertyText[1].Length > 1 && propertyText[1][0] == '!')
                    {
                        declProperty.Important = true;
                    }

                    // This is either an object or an array, so we'll just get the whole block
                    // to pass it to the property parser
                    StringBuilder currentPropertyText = new StringBuilder();
                    int openingBracketCount = 1;
                    int closingBracketCount = 0;

                    currentPropertyText.Append(lines[i]).Append("\n");

                    // Get everything until the block is closed
                    for (int j = i + 1; j < lines.Length; j++, i++, i = j)
                    {
                        // Remove comments from lines
                        lines[j] = RemoveComments(lines[j], "//");
                        lines[j] = lines[j].Trim();

                        if (string.IsNullOrEmpty(lines[j]))
                        {
                            continue;
                        }

                        if (lines[j][lines[j].Length - 1] == '{')
                        {
                            openingBracketCount++;
                            currentPropertyText.Append(lines[j]).Append("\n");
                            continue;
                        }

                        if (lines[j][lines[j].Length - 1] == '}')
                        {
                            closingBracketCount++;
                        }

                        if (openingBracketCount == closingBracketCount)
                        {
                            currentPropertyText.Append(lines[j]);
                            break;
                        }

                        currentPropertyText.Append(lines[j]).Append("\n");
                    }

                    declProperty = ParseProperty(currentPropertyText.ToString(), ref i);
                    currentPropertyText.Clear();
                }

                if (declProperty != null)
                {
                    properties.Add(declProperty);
                }
            }

            declFile.Properties = properties;

            return new DeclFileParseResult()
            {
                Errors = Errors.ToArray(),
                Warnings = Warnings.ToArray(),
                DeclFile = declFile
            };
        }

        /// <summary>
        /// Parses any kind of property into a DeclProperty object
        /// </summary>
        /// <param name="propertyText">text containing the property</param>
        /// <param name="currentLineNumber">current line number reference in the file</param>
        /// <returns>a DeclProperty object containing the parsed data from the property</returns>
        internal DeclProperty ParseProperty(string propertyText, ref int currentLineNumber)
        {
            // Split the property definition into lines (if it has any)
            DeclProperty declProperty = new DeclProperty();
            string[] propertyLines = propertyText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            currentLineNumber -= propertyLines.Length - 1;

            for (int i = 0; i < propertyLines.Length; i++)
            {
                propertyLines[i] = propertyLines[i].Trim();

                if (i > 0)
                {
                    currentLineNumber++;
                }

                // Skip closing brackets
                if (propertyLines[i].Equals("}"))
                {
                    continue;
                }

                // Again, first we need to determine which kind of property this is to properly parse it
                string[] propertyParts = propertyLines[i].Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (propertyParts.Length != 2)
                {
                    propertyParts = propertyLines[i].Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                }

                if (propertyParts.Length != 2 || string.IsNullOrEmpty(propertyParts[1].Trim()))
                {
                    Errors.Add(string.Format("Line {0}: bad property declaration '{1}'", currentLineNumber, propertyLines[i].Trim()));
                    continue;
                }

                propertyParts[0] = propertyParts[0].Trim();
                propertyParts[1] = propertyParts[1].Trim();

                // This is a "single-line" property
                if (propertyParts[1][propertyParts[1].Length - 1] != '{')
                {
                    // Check if the property name is quoted
                    if (propertyParts[0].StartsWith("\"") && propertyParts[0].EndsWith("\""))
                    {
                        declProperty.IsQuoted = true;
                        propertyParts[0] = propertyParts[0].Remove(0, 1);
                        propertyParts[0] = propertyParts[0].Remove(propertyParts[0].Length - 1, 1);
                    }

                    if(!declProperty.IsQuoted && !IsValidPropertyName(propertyParts[0]))
                    {
                        Errors.Add(string.Format("Line {0}: invalid property name '{1}'", currentLineNumber, propertyParts[0]));
                        continue;
                    }

                    declProperty.Name = propertyParts[0];

                    try
                    {
                        // Check for and remove semicolon at the end for the value parser
                        if (propertyParts[1][propertyParts[1].Length - 1] != ';')
                        {
                            Errors.Add(string.Format("Line {0}: missing ';'", currentLineNumber));
                        }
                        else
                        {
                            propertyParts[1] = propertyParts[1].Remove(propertyParts[1].Length - 1, 1);
                        }

                        declProperty.Value = ParsePropertyValue(propertyParts[1].Trim(), ref currentLineNumber);

                        if (declProperty.Value == null)
                        {
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else
                {
                    // Check if the "Important" flag is present
                    if (propertyParts[1].Length > 1 && propertyParts[1][0] == '!')
                    {
                        declProperty.Important = true;
                    }

                    // We will loop through all the properties in this object/array
                    // and do recursion to get the values
                    List<DeclProperty> properties = new List<DeclProperty>();
                    DeclProperty arrayNumProperty = null;
                    bool isArray = false;
                    int arrayNumPropertyLineNumber = 0;
                    int openingBracketCount = 1;
                    int closingBracketCount = 0;

                    currentLineNumber++;

                    // Get everything until the block is closed
                    for (int j = i + 1; j < propertyLines.Length - 1; j++, currentLineNumber++, i = j)
                    {
                        if (string.IsNullOrEmpty(propertyLines[j]))
                        {
                            continue;
                        }

                        if (propertyLines[j][propertyLines[j].Length - 1] == '{')
                        {
                            openingBracketCount++;
                        }

                        if (propertyLines[j][propertyLines[j].Length - 1] == '}')
                        {
                            closingBracketCount++;
                        }

                        if (openingBracketCount == closingBracketCount)
                        {
                            currentLineNumber++;
                            break;
                        }

                        // Check if this is a single property or an object
                        // Everything inside must be a property of some kind
                        // We will get all the text of the property and pass it to the property parser
                        DeclProperty nestedDeclProperty = new DeclProperty();
                        string[] currentPropertyText = propertyLines[j].Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (currentPropertyText.Length != 2)
                        {
                            currentPropertyText = propertyLines[j].Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        }

                        if (currentPropertyText.Length != 2)
                        {
                            if (propertyLines[j].Equals("{") || propertyLines[j].Equals("}"))
                            {
                                continue;
                            }
                            else
                            {
                                Errors.Add(string.Format("Line {0}: unexpected '{1}'", currentLineNumber, propertyLines[j]));
                                continue;
                            }
                        }

                        currentPropertyText[0] = currentPropertyText[0].Trim();
                        currentPropertyText[1] = currentPropertyText[1].Trim();

                        if (string.IsNullOrEmpty(currentPropertyText[1]))
                        {
                            Errors.Add(string.Format("Line {0}: missing property value", currentLineNumber));
                            continue;
                        }

                        // Determine what kind of property this is to get all it's text
                        // This is probably a "single-line" property, so we can just pass the whole line to the property parser
                        if (currentPropertyText[1][currentPropertyText[1].Length - 1] != '{')
                        {
                            // Check if this is an array or an object
                            string trimmedPropertyName = currentPropertyText[0].TrimStart();

                            // Get the number of items in the array
                            // We will use it to check that the number matches with the actual
                            // amount of items
                            if (!isArray & trimmedPropertyName.Equals("num"))
                            {
                                arrayNumPropertyLineNumber = currentLineNumber;
                                arrayNumProperty = ParseProperty(propertyLines[j], ref currentLineNumber);

                                if (arrayNumProperty != null && arrayNumProperty.Value != null &&
                                    arrayNumProperty.Value.GetType() != typeof(DeclPropertyLongValue))
                                {
                                    Errors.Add(string.Format("Line {0}: 'num' property value must be a number", currentLineNumber));
                                }

                                isArray = true;
                            }

                            try
                            {
                                nestedDeclProperty = ParseProperty(propertyLines[j], ref currentLineNumber);
                            }
                            catch (FormatException)
                            {
                                throw;
                            }
                        }
                        else
                        {
                            // Check if the "Important" flag is present
                            if (currentPropertyText[1].Length > 1 && currentPropertyText[1][0] == '!')
                            {
                                nestedDeclProperty.Important = true;
                            }

                            // This is either an object or an array, so we'll just get the whole block
                            // to pass it to the property parser
                            StringBuilder currentNestedPropertyText = new StringBuilder();
                            int nestedOpeningBracketCount = 1;
                            int nestedClosingBracketCount = 0;

                            currentNestedPropertyText.Append(propertyLines[j]).Append("\n");

                            // Get everything until the block is closed
                            for (int k = j + 1; j < propertyLines.Length; k++, currentLineNumber++, j = k)
                            {
                                if (string.IsNullOrEmpty(propertyLines[k]))
                                {
                                    continue;
                                }

                                if (propertyLines[k][propertyLines[k].Length - 1] == '{')
                                {
                                    nestedOpeningBracketCount++;
                                    currentNestedPropertyText.Append(propertyLines[k]).Append("\n");
                                    continue;
                                }

                                if (propertyLines[k][propertyLines[k].Length - 1] == '}')
                                {
                                    nestedClosingBracketCount++;
                                }

                                if (nestedOpeningBracketCount == nestedClosingBracketCount)
                                {
                                    currentNestedPropertyText.Append(propertyLines[k]);
                                    currentLineNumber++;
                                    break;
                                }

                                currentNestedPropertyText.Append(propertyLines[k]).Append("\n");
                            }

                            nestedDeclProperty = ParseProperty(currentNestedPropertyText.ToString(), ref currentLineNumber);
                            currentNestedPropertyText.Clear();
                        }

                        if (nestedDeclProperty != null)
                        {
                            properties.Add(nestedDeclProperty);
                        }
                    }

                    if (!isArray)
                    {
                        DeclPropertyObjectValue objectValue = new DeclPropertyObjectValue();
                        objectValue.Value = properties;

                        declProperty.Value = objectValue;
                    }
                    else
                    {
                        // If this is an array we only need the values
                        uint elementCount = 0;
                        DeclPropertyArrayValue arrayValue = new DeclPropertyArrayValue();
                        arrayValue.Values = new List<DeclPropertyValue>();

                        foreach (var property in properties)
                        {
                            if (property.Value == null || property.Name == null || property.Name.Equals("num"))
                            {
                                continue;
                            }

                            arrayValue.Values.Add(property.Value);
                            elementCount++;
                        }

                        // Print a warning if the 'num' value doesn't match the array's actual element count
                        if (arrayNumProperty != null && arrayNumProperty.Value != null &&
                            arrayNumProperty.Value.GetType() == typeof(DeclPropertyLongValue) &&
                            ((DeclPropertyLongValue)arrayNumProperty.Value).Value != elementCount)
                        {
                            Warnings.Add(string.Format("Line {0}: 'num' property value '{1}' doesn't match the array's actual element count '{2}'",
                                arrayNumPropertyLineNumber, ((DeclPropertyLongValue)arrayNumProperty.Value).Value, elementCount));
                        }

                        declProperty.Value = arrayValue;
                    }

                    // Check if the property name is quoted
                    if (propertyParts[0].StartsWith("\"") && propertyParts[0].EndsWith("\""))
                    {
                        declProperty.IsQuoted = true;
                        propertyParts[0] = propertyParts[0].Remove(0, 1);
                        propertyParts[0] = propertyParts[0].Remove(propertyParts[0].Length - 1, 1);
                    }

                    if (!declProperty.IsQuoted && !IsValidPropertyName(propertyParts[0].Trim()))
                    {
                        Errors.Add(string.Format("Line {0}: invalid property name '{1}'", currentLineNumber, propertyParts[0].Trim()));
                        return null;
                    }

                    declProperty.Name = propertyParts[0].Trim();
                }
            }

            return declProperty;
        }

        /// <summary>
        /// Parses the value of a property
        /// </summary>
        /// <param name="valueText">text containing the value of the property</param>
        /// <param name="currentLineNumber">current line number reference in the file</param>
        /// <returns>a DeclPropertyValue containing the data from the parsed value</returns>
        internal DeclPropertyValue ParsePropertyValue(string valueText, ref int currentLineNumber)
        {
            if (string.IsNullOrEmpty(valueText.Trim()))
            {
                Errors.Add(string.Format("Line {0}: missing property value", currentLineNumber));
                return null;
            }

            // Check for NULL values
            if (valueText.Equals("NULL"))
            {
                return new DeclPropertyNullValue();
            }

            // Check if it's a string
            if (valueText[0] == '"')
            {
                if (valueText[valueText.Length - 1] != '"')
                {
                    Errors.Add(string.Format("Line {0}: missing '\"'", currentLineNumber));
                    return null;
                }

                // Skip closing quotes
                string stringValue = valueText.Substring(1, valueText.Length - 2);

                return new DeclPropertyStringValue()
                {
                    Value = stringValue
                };
            }

            // Check for boolean values
            if (valueText.Equals("true") || valueText.Equals("false"))
            {
                bool boolValue = Boolean.Parse(valueText);

                return new DeclPropertyBooleanValue()
                {
                    Value = boolValue
                };
            }

            // Check for long values
            long intValue = 0;

            if (!long.TryParse(valueText, out intValue))
            {
                // If we can't parse it as a long, try as a double
                if (valueText.Contains(".") || valueText.Contains("e") || valueText.Contains("E"))
                {
                    double doubleValue = 0;

                    if (!Double.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        Errors.Add(string.Format("Line {0}: invalid property value: {1}", currentLineNumber, valueText));
                        return null;
                    }

                    return new DeclPropertyDoubleValue()
                    {
                        Value = doubleValue
                    };
                }
            }

            return new DeclPropertyLongValue()
            {
                Value = intValue
            };
        }

        /// <summary>
        /// Checks if the given string is a valid name for properties
        /// </summary>
        /// <param name="text">string to check</param>
        /// <returns>true if the string is valid, false otherwise</returns>
        internal static bool IsValidPropertyName(string text)
        {
            return Regex.IsMatch(text, @"^[a-zA-Z0-9_/]+$") || Regex.IsMatch(text, @"^[a-zA-Z0-9_\/]+\[[0-9]+\]+$");
        }

        /// <summary>
        /// Removes comments from the specified text
        /// </summary>
        /// <param name="text">text to remove comments from</param>
        /// <returns>the text with comments removed</returns>
        internal static string RemoveComments(string text, string commentsDelimiter)
        {
            int commentStartPos = text.IndexOf(commentsDelimiter);

            if (commentStartPos == -1)
            {
                return text;
            }

            return text.Remove(commentStartPos);
        }
    }
}
