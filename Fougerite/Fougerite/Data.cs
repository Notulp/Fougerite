using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fougerite.Concurrent;

namespace Fougerite
{
    /// <summary>
    /// Provides various utility methods and properties for working with strings, numbers, and arrays.
    /// Includes deprecated methods for handling configurations and shared data tables, which have been replaced
    /// by other mechanisms such as `DataStore`.
    /// </summary>
    public class Data
    {
        /// <summary>
        /// A collection of chat messages recorded in the system.
        /// </summary>
        /// <remarks>
        /// The <c>chat_history</c> variable stores the text of chat messages
        /// in a list. It is mainly used for storing and managing the chat history
        /// of users, such as when displaying past messages or processing chat commands.
        /// </remarks>
        [Obsolete("Use Util.ChatHistory combined with PlayerCache", false)]
        public List<string> chat_history = new List<string>();

        /// <summary>
        /// Represents a collection of usernames corresponding to chat messages in the chat history.
        /// Each entry in this list matches the respective chat message stored in the `chat_history` list,
        /// creating a parallel structure to track which user sent each message.
        /// </summary>
        [Obsolete("Use Util.ChatHistory combined with PlayerCache", false)]
        public List<string> chat_history_username = new List<string>();

        /// <summary>
        /// Represents a singleton instance of the <see cref="Data"/> class.
        /// Provides a globally accessible, lazily initialized instance of the Data class.
        /// </summary>
        private static readonly Lazy<Data> Instance = new Lazy<Data>(() => new Data());

        /// <summary>
        /// A shared, thread-unsafe data container using a Hashtable.
        /// This field is marked as obsolete and it's recommended to use DataStore for storing and managing shared data.
        /// </summary>
        /// <remarks>
        /// This member is retained for backward compatibility but its usage is discouraged
        /// as it may lead to issues in environments that require thread safety or scalability.
        /// </remarks>
        /// <seealso cref="DataStore"/>
        [Obsolete("Use DataStore", false)]
        public readonly Hashtable Fougerite_shared_data = new Hashtable();

        /// <summary>
        /// A static Hashtable marked as obsolete, previously used for managing plugin configuration files.
        /// Users are advised to use modules hosting plugins to manage their configuration files.
        /// </summary>
        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public static Hashtable inifiles = new Hashtable();

        /// <summary>
        /// Adds a key-value pair to a specified table.
        /// </summary>
        /// <param name="tablename">The name of the table to which the key-value pair will be added.</param>
        /// <param name="key">The key object to identify the value in the table.</param>
        /// <param name="val">The value object associated with the specified key in the table.</param>
        [Obsolete("Replaced with DataStore.Add", false)]
        public void AddTableValue(string tablename, object key, object val)
        {
            DataStore.GetInstance().Add(tablename, key, val);
        }

        /// Retrieves the value associated with the specified key within a configuration file section.
        /// <param name="config">The name of the configuration file.</param>
        /// <param name="section">The section within the configuration file.</param>
        /// <param name="key">The key whose value needs to be retrieved.</param>
        /// <return>Returns the value associated with the specified key. Returns null if the key does not exist or is inaccessible.</return>
        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public string GetConfigValue(string config, string section, string key)
        {
            return null;
        }

        /// <summary>
        /// Gets the singleton instance of the Data class.
        /// </summary>
        /// <returns>The singleton instance of the Data class.</returns>
        public static Data GetData()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Retrieves a value from the specified table based on the provided key.
        /// </summary>
        /// <param name="tablename">The name of the table from which the value will be retrieved.</param>
        /// <param name="key">The key associated with the value to retrieve.</param>
        /// <returns>The value associated with the specified key in the table, or null if the key does not exist.</returns>
        [Obsolete("Replaced with DataStore.Get", false)]
        public object GetTableValue(string tablename, object key)
        {
            return DataStore.GetInstance().Get(tablename, key);
        }

        /// Loads configuration data or other necessary resources.
        /// This method is marked as obsolete as modules hosting plugins
        /// are now responsible for managing plugin configuration files.
        /// It currently performs no actions.
        /// Obsolete: Modules hosting plugins will manage plugin config files.
        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public void Load()
        {
            return;
        }

        /// <summary>
        /// Overrides a configuration value in the specified configuration file.
        /// </summary>
        /// <param name="config">The name of the configuration file to modify.</param>
        /// <param name="section">The section within the configuration file where the value is located.</param>
        /// <param name="key">The key of the configuration value to override.</param>
        /// <param name="value">The new value to assign to the specified key.</param>
        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public void OverrideConfig(string config, string section, string key, string value)
        {
            return;
        }

        /// <summary>
        /// Splits the given string into an array of substrings using a delimiter while considering substrings enclosed in quotes.
        /// </summary>
        /// <param name="str">The input string to be split.</param>
        /// <returns>An array of strings split by the delimiter, respecting quoted substrings.</returns>
        public string[] SplitQuoteStrings(string str)
        {
            return Facepunch.Utility.String.SplitQuotesStrings(str);
        }

        /// <summary>
        /// Calculates the length of the provided string.
        /// </summary>
        /// <param name="str">The string whose length is to be determined.</param>
        /// <returns>The length of the string specified by <paramref name="str"/>. Returns 0 if the string is null or empty.</returns>
        public int StrLen(string str)
        {
            return string.IsNullOrEmpty(str) ? 0 : str.Length;
        }

        /// <summary>
        /// Extracts a substring from the specified string using the given start and end indices.
        /// </summary>
        /// <param name="str">The input string from which the substring is to be extracted. If null or empty, an empty string is returned.</param>
        /// <param name="from">The zero-based starting index of the substring.</param>
        /// <param name="to">The length of the substring to extract.</param>
        /// <returns>A substring extracted from the input string. If the input string is null or empty, returns an empty string.</returns>
        public string Substring(string str, int from, int to)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Substring(from, to);
        }

        /// <summary>
        /// Converts a string representation of a number to an integer.
        /// </summary>
        /// <param name="num">The string input that represents a number to convert.</param>
        /// <returns>
        /// The integer representation of the input string.
        /// Returns 0 if the input string is null, empty, or cannot be converted to a number.
        /// </returns>
        public int ToInt(string num)
        {
            if (string.IsNullOrEmpty(num)) return 0;
            int result;
            if (int.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            double dbl;
            if (double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out dbl)) return (int)dbl;
            return 0;
        }

        /// <summary>
        /// Converts the provided numeric string to an integer value. If the input string is null, empty, or cannot be parsed, returns 0.
        /// </summary>
        /// <param name="num">The string representation of a numeric value to convert.</param>
        /// <returns>The integer value of the provided string. Returns 0 if the input is invalid or cannot be parsed.</returns>
        public int ToInt(double num)
        {
            return (int)num;
        }

        /// <summary>
        /// Converts the given value to an integer.
        /// </summary>
        /// <param name="num">The value to convert to an integer. This can be a string, double, or float.</param>
        /// <returns>
        /// An integer representation of the given value. Returns 0 if the input is null, empty, or cannot be converted to an integer.
        /// </returns>
        public int ToInt(float num)
        {
            return (int)num;
        }

        /// Converts a double value to a float.
        /// <param name="num">The double value to be converted to float.</param>
        /// <returns>The converted float value.</returns>
        public float ToFloat(double num)
        {
            return (float)num;
        }

        /// Converts the given double value to a float.
        /// <param name="num">The double value to convert.</param>
        /// <return>Returns the converted float value.</return>
        public float ToFloat(int num)
        {
            return (float)num;
        }

        /// Converts a string representation of a number to its double-precision floating-point equivalent.
        /// If the input string is null, empty, or cannot be converted, it returns 0.0.
        /// <param name="num">The string representation of the number to convert.</param>
        /// <returns>The double-precision floating-point value equivalent to the input string, or 0.0 if conversion fails.</returns>
        public double ToDouble(string num)
        {
            if (string.IsNullOrEmpty(num)) return 0.0;
            double result;
            if (double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0.0;
        }

        /// Converts the specified string to an unsigned long (ulong).
        /// If the string is null, empty, or cannot be parsed to an unsigned long, the method returns 0.
        /// <param name="num">The string to be converted to an unsigned long.</param>
        /// <returns>The converted unsigned long value, or 0 if the conversion fails.</returns>
        public ulong ToUlong(string num)
        {
            if (string.IsNullOrEmpty(num)) return 0UL;
            ulong result;
            if (ulong.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0UL;
        }

        /// Converts a numeric string to a long integer value.
        /// If the input is null, empty, or cannot be parsed, it returns 0.
        /// <param name="num">The string representation of a number to convert.</param>
        /// <returns>The converted long integer value, or 0 if the conversion fails or input is invalid.</returns>
        public long Tolong(string num)
        {
            if (string.IsNullOrEmpty(num)) return 0L;
            long result;
            if (long.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0L;
        }

        /// <summary>
        /// Converts the specified string to lowercase characters.
        /// </summary>
        /// <param name="str">The input string to convert. If null or empty, an empty string is returned.</param>
        /// <returns>Returns the lowercase version of the input string, or an empty string if the input is null or empty.</returns>
        public string ToLower(string str)
        {
            return string.IsNullOrEmpty(str) ? string.Empty : str.ToLower();
        }

        /// <summary>
        /// Converts the specified string to uppercase characters.
        /// </summary>
        /// <param name="str">The string to be converted to uppercase.</param>
        /// <returns>
        /// A new string where all characters in the input string are converted to uppercase.
        /// If the input string is null or empty, an empty string is returned.
        /// </returns>
        public string ToUpper(string str)
        {
            return string.IsNullOrEmpty(str) ? string.Empty : str.ToUpper();
        }

        /// Rounds a double value up to the nearest greater integer value.
        /// <param name="value">The double value to be rounded up.</param>
        /// <return>The smallest integer value that is greater than or equal to the specified double value.</return>
        public double RoundUp(double value)
        {
            return Math.Ceiling(value);
        }

        /// Rounds the given double value down to the nearest whole number.
        /// <param name="value">The double value to be rounded down.</param>
        /// <returns>The largest integer less than or equal to the specified value.</returns>
        public double RoundDown(double value)
        {
            return Math.Floor(value);
        }

        /// Rounds a given double precision floating-point value to the nearest integer, based on the specified rounding methodology.
        /// <param name="value">The double precision floating-point number to be rounded.</param>
        /// <param name="even">
        /// If true, the method uses midpoint rounding to nearest even number (MidpointRounding.ToEven).
        /// If false, the method rounds away from zero (MidpointRounding.AwayFromZero).
        /// </param>
        /// <return>The rounded value as a double precision floating-point number.</return>
        public double Round(double value, bool even)
        {
            return Math.Round(value, even ? MidpointRounding.ToEven : MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Converts a string representation of a number to a single-precision floating-point number.
        /// </summary>
        /// <param name="num">The string representation of the number to convert.</param>
        /// <returns>
        /// The converted single-precision floating-point number.
        /// Returns 0.0f if the input string is null, empty, or cannot be parsed successfully.
        /// </returns>
        public float ToFloat(string num)
        {
            if (string.IsNullOrEmpty(num)) return 0.0f;
            float result;
            if (float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0.0f;
        }

        /// Converts the provided string representation of a boolean value to an actual boolean.
        /// The method treats the following string values as true (case-insensitive):
        /// "true", "1", "yes", "on". All other values are considered false.
        /// <param name="val">The string input to be converted to a boolean.</param>
        /// <returns>
        /// True if the input matches one of the valid true values, otherwise false.
        /// Returns false if the input string is null or empty.
        /// </returns>
        public bool ToBool(string val)
        {
            if (string.IsNullOrEmpty(val)) return false;
            string clean = val.Trim().ToLower();
            return clean == "true" || clean == "1" || clean == "yes" || clean == "on";
        }

        /// Converts the given object to its string representation.
        /// <param name="obj">The object to be converted to a string. Can be null.</param>
        /// <return>The string representation of the object if it is not null; otherwise, an empty string.</return>
        public string ToString(object obj)
        {
            return obj == null ? string.Empty : obj.ToString();
        }

        /// <summary>
        /// Determines whether the provided string input can be parsed as an integer.
        /// </summary>
        /// <param name="input">The string to check for integer validity.</param>
        /// <returns>True if the input can be parsed as an integer; otherwise, false.</returns>
        public bool IsInt(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            int res;
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out res);
        }

        /// <summary>
        /// Determines whether the given input string can be parsed as a floating-point number.
        /// </summary>
        /// <param name="input">The string to check for a valid floating-point format.</param>
        /// <returns>True if the input string represents a floating-point number; otherwise, false.</returns>
        public bool IsFloat(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            float res;
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out res);
        }

        /// <summary>
        /// Determines whether the provided string represents a numeric value.
        /// </summary>
        /// <param name="input">The string to evaluate.</param>
        /// <returns>True if the string is a numeric value; otherwise, false.</returns>
        public bool IsNumeric(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            double res;
            return double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out res);
        }

        /// <summary>
        /// Determines whether the specified input string contains only alphabetic characters.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if the input contains only alphabetic characters; otherwise, false.</returns>
        public bool IsAlpha(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.All(char.IsLetter);
        }

        /// <summary>
        /// Determines whether the specified string contains only alphanumeric characters.
        /// </summary>
        /// <param name="input">The string to check for alphanumeric characters.</param>
        /// <returns>True if the string contains only letters and digits; otherwise, false.</returns>
        public bool IsAlphaNumeric(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.All(char.IsLetterOrDigit);
        }

        /// <summary>
        /// Removes all leading and trailing white-space characters from the specified string.
        /// </summary>
        /// <param name="str">The input string to be trimmed. If null or empty, an empty string is returned.</param>
        /// <returns>A new string with all leading and trailing white-space characters removed. If the input string is null or empty, an empty string is returned.</returns>
        public string Trim(string str)
        {
            return string.IsNullOrEmpty(str) ? string.Empty : str.Trim();
        }

        /// <summary>
        /// Replaces all occurrences of a specified substring within the given string with another specified substring.
        /// </summary>
        /// <param name="str">The string to perform the replacement operation on.</param>
        /// <param name="oldValue">The substring to be replaced.</param>
        /// <param name="newValue">The substring to replace all occurrences of <paramref name="oldValue"/>.</param>
        /// <returns>A new string with all occurrences of <paramref name="oldValue"/> replaced by <paramref name="newValue"/>. If <paramref name="str"/> is null or empty, it returns an empty string.</returns>
        public string Replace(string str, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Replace(oldValue, newValue);
        }

        /// <summary>
        /// Determines whether a specified string contains another specified value.
        /// </summary>
        /// <param name="str">The string to search within.</param>
        /// <param name="value">The string to locate within the specified string.</param>
        /// <returns>
        /// True if the specified value occurs within the given string; otherwise, false.
        /// </returns>
        public bool StringContains(string str, string value)
        {
            if (str == null || value == null) return false;
            return str.Contains(value);
        }

        /// Splits a string into an array of substrings based on the provided separator.
        /// <param name="str">The string to be split. If null or empty, an empty array is returned.</param>
        /// <param name="separator">The separator string used to define where each split occurs.</param>
        /// <return>An array of substrings resulting from the split operation.</return>
        public string[] Split(string str, string separator)
        {
            if (string.IsNullOrEmpty(str)) return new string[0];
            return str.Split(new[] { separator }, StringSplitOptions.None);
        }

        /// Joins the elements of a string array into a single string with a specified separator between each element.
        /// <param name="arr">The array of strings to be joined.</param>
        /// <param name="separator">The string to use as a separator between each element in the array.</param>
        /// <returns>A single string containing the elements of the array joined by the specified separator. If the array is null, an empty string is returned.</returns>
        public string Join(string[] arr, string separator)
        {
            if (arr == null) return string.Empty;
            return string.Join(separator, arr);
        }

        /// <summary>
        /// Removes color tags from the provided string.
        /// Specifically, it targets tags formatted as "[color #XXXXXX]" or similar.
        /// </summary>
        /// <param name="str">The input string that may contain color tags.</param>
        /// <returns>A string with all color tags removed. If the input string is null or empty, an empty string is returned.</returns>
        public string StripColors(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return Regex.Replace(str, @"\[color\s*#?[a-fA-F0-9]{6}\]", "", RegexOptions.IgnoreCase);
        }

        /// Removes empty or null elements from the provided string array, trims each string, and returns the cleaned array.
        /// <param name="arr">The string array to clean.</param>
        /// <return>Returns a new string array containing non-null, non-empty, and trimmed elements from the original array.</return>
        public string[] CleanArray(string[] arr)
        {
            if (arr == null) return new string[0];
            return arr.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToArray();
        }

        /// <summary>
        /// Returns a subset of the given string array based on the specified start index and count.
        /// </summary>
        /// <param name="arr">The array to slice.</param>
        /// <param name="start">The zero-based starting index of the slice.</param>
        /// <param name="count">The number of elements to include in the slice.</param>
        /// <returns>An array containing the specified subset of elements. Returns an empty array if the input array is null or the parameters are out of range.</returns>
        public string[] SliceArray(string[] arr, int start, int count)
        {
            if (arr == null) return new string[0];
            return arr.Skip(start).Take(count).ToArray();
        }

        /// <summary>
        /// Determines whether a specified array contains a specified value,
        /// using a case-insensitive comparison.
        /// </summary>
        /// <param name="arr">The array of strings to search through.</param>
        /// <param name="value">The string value to look for in the array.</param>
        /// <returns>True if the array contains the specified value; otherwise, false.</returns>
        public bool ArrayContains(string[] arr, string value)
        {
            if (arr == null || value == null) return false;
            return arr.Contains(value, StringComparer.OrdinalIgnoreCase);
        }
    }
}