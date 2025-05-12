using System;
using System.Xml;

/*
    Copyright (c) 2017 Sloan Kelly

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
    ----------------------------------------
    Modified 2025 by Alexander Ohlsson
*/

/// <summary>
/// Base Open Street Map (OSM) data node.
/// </summary>
/// 

namespace MapMaker
{
    class BaseOsm
    {
        /// <summary>
        /// Get an attribute's value from the collection using the given 'attrName'. 
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="attrName">Name of the attribute</param>
        /// <param name="attributes">Node's attribute collection</param>
        /// <returns>The value of the attribute converted to the required type</returns>
        protected T GetAttribute<T>(string attrName, XmlAttributeCollection attributes)
        {
            // Check if the attribute exists in the collection
            XmlAttribute attribute = attributes[attrName];
            if (attribute != null)
            {
                string strValue = attribute.Value;
                if (!string.IsNullOrEmpty(strValue))
                {
                    try
                    {
                        // Check if T is a numeric type
                        if (typeof(T) == typeof(int))
                        {
                            if (int.TryParse(strValue, out int result))
                            {
                                return (T)(object)result;
                            }
                        }
                        else if (typeof(T) == typeof(float))
                        {
                            if (float.TryParse(strValue, out float result))
                            {
                                return (T)(object)result;
                            }
                        }
                        else if (typeof(T) == typeof(double))
                        {
                            if (double.TryParse(strValue, out double result))
                            {
                                return (T)(object)result;
                            }
                        }
                        else
                        {
                            // For other types, use default conversion
                            return (T)Convert.ChangeType(strValue, typeof(T));
                        }
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Failed to convert attribute '{attrName}' with value '{strValue}' to type {typeof(T)}.");
                        // Optionally, return a default value or handle it as needed
                    }
                }
            }
            
            // Return default value of T if the attribute doesn't exist or conversion fails
            return default(T);
        }


    }
}