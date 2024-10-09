using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ImageMetadataParser
{
    public static class JSONReader
    {
        /// <summary>
        /// Parses a JSON string and returns a JSONDict object representing the JSON data.
        /// </summary>
        /// <param name="json">A string containing JSON data.</param>
        /// <returns>A JSONDict object representing the parsed JSON data.</returns>
        /// <exception cref="JSONParseException">When something goes wrong with parsing</exception>
        /// <exception cref="JSONNumberOverflowException">When a number specified in the JSON document is greater than supported by C#</exception>
        public static JSONDict Read(string json)
        {
            return new JSONDict(json);
        }
    }

    public class JSONException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the JSONException class with a specified error message.
        /// Base JSONReader exception. Never thrown directly.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JSONException(string message) : base(message) { }
    }

    public class JSONParseException : JSONException
    {
        /// <summary>
        /// Initializes a new instance of the JSONParseException class with a specified error message.
        /// Thrown when something goes wrong with general parsing.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JSONParseException(string message) : base(message) { }
    }

    public class JSONNumberOverflowException : JSONException
    {
        /// <summary>
        /// Initializes a new instance of the JSONNumberOverflowException class with a specified error message.
        /// Thrown when a number exceeds the limits of C#.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JSONNumberOverflowException(string message) : base(message) { }
    }

    public class DataNode
    {
        /// <summary>
        /// The data type of the JSON value.
        /// </summary>
        public Type type;

        /// <summary>
        /// The value of the JSON node.
        /// </summary>
        public readonly object data;

        /// <summary>
        /// Initializes a new instance of the DataNode class with the specified type and value.
        /// </summary>
        /// <param name="t">The data type of the JSON value.</param>
        /// <param name="o">The value of the JSON node.</param>
        public DataNode(Type t, object o)
        {
            type = t;
            data = o;
        }

        /// <summary>
        /// Gets the value of the current <see cref="DataNode"/> if it matches the specified type; otherwise, returns the provided default value.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="defaultValue">The default value to return if the type does not match.</param>
        /// <returns>The value of the current <see cref="DataNode"/> if its type matches <typeparamref name="T"/>; otherwise, <paramref name="defaultValue"/>.</returns>
        public T GetOr<T>(T defaultValue)
        {
            if (typeof(T) != type) return defaultValue;
            return (T)data;
        }
    }

    public class JSONDict
    {
        private static class JSONDefaultErrors
        {
            public static JSONParseException UnmatchedQuotationmark(int i)
            {
                return new JSONParseException("Umatched quotationmark (\") at position " + i.ToString() + ".");
            }

            public static JSONParseException UnmatchedCurlybrace(int i)
            {
                return new JSONParseException("Umatched curly brace (\"{\") at position " + i.ToString() + ".");
            }

            public static JSONParseException UnmatchedSquareBacket(int i)
            {
                return new JSONParseException("Umatched square bracket (\"[\") at position " + i.ToString() + ".");
            }

            public static JSONParseException FormatError(Type t, int i)
            {
                return new JSONParseException("JSON parsing failed while parsing type " + t.Name + " at position" + i.ToString());
            }

            public static JSONParseException UnknownCharacter(string s, int i)
            {
                return new JSONParseException("JSON parsing failed due to unexpected character " + s + " at position" + i.ToString());
            }

            public static JSONNumberOverflowException NumberOverflow(string message)
            {
                return new JSONNumberOverflowException("JSON parsing failed due to numbers higher than the C# equivalent capacity!\n" + message);
            }
        }

        private enum Mode
        {
            Default,
            Key,
            KeyEnd,
            Value,
            ReadingValue,
            ReadDirect
        }

        /// <summary>
        /// Raw JSON string
        /// </summary>
        public string rawData;

        private readonly Dictionary<string, DataNode> data;

        /// <summary>
        /// Initializes a new instance of the JSONDict class by parsing the provided JSON string.
        /// </summary>
        /// <param name="input">A string containing JSON data.</param>
        /// <exception cref="JSONParseException">When something goes wrong with parsing</exception>
        /// <exception cref="JSONNumberOverflowException">When a number specified in the JSON document is greater than supported by C#</exception>
        public JSONDict(string input)
        {
            rawData = input;
            data = ReadJsonDict(rawData);
        }

        /// <summary>
        /// Gets the DataNode associated with the specified key.
        /// </summary>
        /// <param name="index">The key of the JSON object.</param>
        /// <returns>The DataNode associated with the specified key, or null if the key does not exist.</returns>
        public DataNode this[string index]
        {
            get
            {
                return data.ContainsKey(index) ? data[index] : null;
            }
        }

        /// <summary>
        /// Determines whether the JSONDict contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the JSONDict.</param>
        /// <returns>true if the key is found; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            return data.ContainsKey(key);
        }

        /// <summary>
        /// Gets the collection of keys in the JSON dictionary.
        /// </summary>
        /// <returns>A <see cref="Dictionary{TKey, TValue}.KeyCollection"/> representing the keys of the JSON dictionary.</returns>
        public Dictionary<string, DataNode>.KeyCollection Keys
        {
            get
            {
                return data.Keys;
            }
        }

        /// <summary>
        /// Searches the JSON dictionary and returns the first <see cref="DataNode"/> that matches the specified condition.
        /// </summary>
        /// <param name="func">A function that defines the condition to match.</param>
        /// <returns>The first <see cref="DataNode"/> that satisfies the condition; otherwise, <c>null</c> if no match is found.</returns>
        public DataNode GetFirst(Func<DataNode, bool> func)
        {
            foreach (string key in Keys)
            {
                DataNode node = data[key];
                if (func(node)) return node;
            }
            return null;
        }

        /// <summary>
        /// Retrieves all <see cref="DataNode"/> objects from the JSON dictionary that match the specified condition.
        /// </summary>
        /// <param name="func">A function that defines the condition each <see cref="DataNode"/> must satisfy.</param>
        /// <returns>A <see cref="List{DataNode}"/> containing all <see cref="DataNode"/> objects that satisfy the condition.</returns>
        public List<DataNode> GetAll(Func<DataNode, bool> func)
        {
            List<DataNode> list = new List<DataNode>();
            foreach (string key in Keys)
            {
                DataNode node = data[key];
                if (func(node)) list.Add(node);
            }
            return list;
        }

        /// <summary>
        /// Parses a JSON string representing a dictionary and returns a Dictionary with string keys and DataNode values.
        /// </summary>
        /// <param name="input">The JSON string to parse.</param>
        /// <returns>A Dictionary with string keys and DataNode values.</returns>
        private Dictionary<string, DataNode> ReadJsonDict(string input)
        {
            Dictionary<string, DataNode> output = new Dictionary<string, DataNode>();
            Dictionary<string, string> raw = ReadOneLayerJson(input);

            foreach (string key in raw.Keys)
            {
                string value = raw[key];

                switch (value[0])
                {
                    case '{':
                        // Recursively parse nested JSON object
                        output.Add(key, new DataNode(typeof(JSONDict), new JSONDict(value)));
                        break;

                    case '[':
                        // Parse array and store it as a list of DataNodes
                        output.Add(key, new DataNode(typeof(List<DataNode>), ReadArray(value)));
                        break;

                    case '"':
                        // Store string value, removing the surrounding quotation marks
                        output.Add(key, new DataNode(typeof(string), value.Substring(1, value.Length - 2)));
                        break;

                    default:
                        // Handle boolean, null, and numeric values
                        switch (value)
                        {
                            case "true":
                                output.Add(key, new DataNode(typeof(bool), true));
                                break;

                            case "false":
                                output.Add(key, new DataNode(typeof(bool), false));
                                break;

                            case "null":
                                output.Add(key, new DataNode(null, null));
                                break;

                            default:
                                try
                                {
                                    // Determine if the value is a floating point or integer
                                    if (value.Contains("."))
                                    {
                                        value = value.Replace('.', ','); // Replace '.' with ',' as C# uses comma for floating point representation
                                        try
                                        {
                                            output.Add(key, new DataNode(typeof(float), float.Parse(value)));
                                        }
                                        catch (OverflowException)
                                        {
                                            try
                                            {
                                                output.Add(key, new DataNode(typeof(double), double.Parse(value)));
                                            }
                                            catch (OverflowException)
                                            {
                                                try
                                                {
                                                    output.Add(key, new DataNode(typeof(decimal), decimal.Parse(value)));
                                                }
                                                catch (OverflowException e)
                                                {
                                                    throw JSONDefaultErrors.NumberOverflow(e.Message);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            output.Add(key, new DataNode(typeof(int), int.Parse(value)));
                                        }
                                        catch (OverflowException)
                                        {
                                            try
                                            {
                                                output.Add(key, new DataNode(typeof(long), long.Parse(value)));
                                            }
                                            catch (OverflowException e)
                                            {
                                                throw JSONDefaultErrors.NumberOverflow(e.Message);
                                            }
                                        }
                                    }
                                }
                                catch (FormatException)
                                {
                                    throw JSONDefaultErrors.FormatError(typeof(int), -1);
                                }
                                break;
                        }
                        break;
                }
            }
            return output;
        }

        private enum ReadType
        {
            String,
            Dict,
            Array,
            Direct
        }

        /// <summary>
        /// Parses a JSON string representing an array and returns a List of DataNode objects.
        /// </summary>
        /// <param name="input">The JSON string representing an array.</param>
        /// <returns>A List of DataNode objects.</returns>
        private List<DataNode> ReadArray(string input)
        {
            List<DataNode> output = new List<DataNode>();
            if (input[0] != '[') throw JSONDefaultErrors.UnknownCharacter(input[0].ToString(), -1);
            Mode mode = Mode.Default;
            ReadType reading = ReadType.String;
            Regex whitespace = new Regex(@"\s+");
            Regex alphanum = new Regex(@"[-.\d\w]+");


            string valueBuff = "";
            int awaitingPosition = -1;

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];
                string currentString = current.ToString();

                switch (mode)
                {
                    case Mode.Default:
                        if (whitespace.IsMatch(currentString)) break;
                        switch (current)
                        {
                            case ',':
                                break;

                            case '"':
                                // Find the matching quotation mark for strings
                                int stringEnd = GetMatchingCounter(input, '"', '"', i);
                                if (stringEnd == -1) throw JSONDefaultErrors.UnmatchedQuotationmark(i);
                                mode = Mode.ReadingValue;
                                reading = ReadType.String;
                                awaitingPosition = stringEnd;
                                break;

                            case '{':
                                // Find the matching curly brace for nested objects
                                int dictEnd = GetMatchingCounter(input, '{', '}', i);
                                if (dictEnd == -1) throw JSONDefaultErrors.UnmatchedCurlybrace(i);
                                mode = Mode.ReadingValue;
                                reading = ReadType.Dict;
                                awaitingPosition = dictEnd;
                                break;

                            case '[':
                                // Find the matching square bracket for nested arrays
                                int arrayEnd = GetMatchingCounter(input, '[', ']', i);
                                if (arrayEnd == -1) throw JSONDefaultErrors.UnmatchedSquareBacket(i);
                                mode = Mode.ReadingValue;
                                reading = ReadType.Array;
                                awaitingPosition = arrayEnd;
                                break;

                            default:
                                // Handle numeric, boolean or null values directly
                                mode = Mode.ReadDirect;
                                break;
                        }
                        break;

                    case Mode.ReadingValue:
                        if (i < awaitingPosition)
                        {
                            valueBuff += currentString;
                            break;
                        };
                        mode = Mode.Default;
                        switch (reading)
                        {
                            case ReadType.String:
                                // Store the string value
                                output.Add(new DataNode(typeof(string), valueBuff.Substring(1, valueBuff.Length - 2)));
                                break;

                            case ReadType.Dict:
                                // Parse and store the nested JSON object
                                output.Add(new DataNode(typeof(JSONDict), new JSONDict(valueBuff)));
                                break;

                            case ReadType.Array:
                                // Recursively parse and store the nested array
                                output.Add(new DataNode(typeof(List<DataNode>), ReadArray(valueBuff)));
                                break;
                        }
                        valueBuff = "";
                        awaitingPosition = -1;
                        break;

                    case Mode.ReadDirect:
                        // Handle non-string, non-object, and non-array values like numbers, booleans and null
                        if (alphanum.IsMatch(currentString))
                        {
                            valueBuff += currentString;
                            break;
                        }
                        mode = Mode.Default;
                        switch (valueBuff)
                        {
                            case "true":
                                output.Add(new DataNode(typeof(bool), true));
                                break;

                            case "false":
                                output.Add(new DataNode(typeof(bool), false));
                                break;

                            case "null":
                                output.Add(new DataNode(null, null));
                                break;

                            default:
                                try
                                {
                                    // Determine if the value is a floating point or integer
                                    if (valueBuff.Contains("."))
                                    {
                                        valueBuff = valueBuff.Replace('.', ','); // Replace '.' with ',' as C# uses comma for floating point representation
                                        try
                                        {
                                            output.Add(new DataNode(typeof(float), float.Parse(valueBuff)));
                                        }
                                        catch (OverflowException)
                                        {
                                            try
                                            {
                                                output.Add(new DataNode(typeof(double), double.Parse(valueBuff)));
                                            }
                                            catch (OverflowException)
                                            {
                                                try
                                                {
                                                    output.Add(new DataNode(typeof(decimal), decimal.Parse(valueBuff)));
                                                }
                                                catch (OverflowException e)
                                                {
                                                    throw JSONDefaultErrors.NumberOverflow(e.Message);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            output.Add(new DataNode(typeof(int), int.Parse(valueBuff)));
                                        }
                                        catch (OverflowException)
                                        {
                                            try
                                            {
                                                output.Add(new DataNode(typeof(long), long.Parse(valueBuff)));
                                            }
                                            catch (OverflowException e)
                                            {
                                                throw JSONDefaultErrors.NumberOverflow(e.Message);
                                            }
                                        }
                                    }
                                }
                                catch (FormatException)
                                {
                                    throw JSONDefaultErrors.FormatError(typeof(int), i - valueBuff.Length);
                                }
                                break;
                        }
                        valueBuff = "";
                        break;
                }
            }
            return output;
        }

        /// <summary>
        /// Parses a JSON string representing a single layer (object) and returns a Dictionary with string keys and values as strings.
        /// </summary>
        /// <param name="input">The JSON string to parse.</param>
        /// <returns>A Dictionary with string keys and string values representing the JSON data.</returns>
        private Dictionary<string, string> ReadOneLayerJson(string input)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            if (input[0] != '{') return output;
            Mode mode = Mode.Default;
            Regex whitespace = new Regex(@"\s+");
            Regex alphanum = new Regex(@"[-.\d\w]+");

            string keyBuff = "";
            string valueBuff = "";
            int awaitingPosition = -1;

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];
                string currentString = current.ToString();

                switch (mode)
                {
                    case Mode.Default:
                        if (whitespace.IsMatch(currentString)) break;
                        if (current == '"')
                        {
                            mode = Mode.Key;
                            break;
                        }
                        break;

                    case Mode.Key:
                        if (current == '"')
                        {
                            mode = Mode.KeyEnd;
                            break;
                        }

                        keyBuff += currentString;
                        break;

                    case Mode.KeyEnd:
                        if (current == ':')
                        {
                            mode = Mode.Value;
                        }
                        break;

                    case Mode.Value:
                        if (whitespace.IsMatch(currentString)) break;
                        string mains = "\"{[";
                        string opposites = "\"}]";
                        int index = mains.IndexOf(current);
                        if (index == -1)
                        {
                            mode = Mode.ReadDirect;
                            i--; //Go back one char to read first char in
                            break;
                        }

                        int end = GetMatchingCounter(input, mains[index], opposites[index], i);
                        if (end == -1)
                        {
                            if (mains[index] == '"') throw JSONDefaultErrors.UnmatchedQuotationmark(i);
                            else if (mains[index] == '{') throw JSONDefaultErrors.UnmatchedCurlybrace(i);
                            else if (mains[index] == '[') throw JSONDefaultErrors.UnmatchedSquareBacket(i);
                        }
                        awaitingPosition = end;

                        mode = Mode.ReadingValue;
                        valueBuff += currentString;
                        break;

                    case Mode.ReadingValue:
                        if (i < awaitingPosition)
                        {
                            valueBuff += currentString;
                            break;
                        };
                        mode = Mode.Default;
                        output.Add(keyBuff, valueBuff);
                        valueBuff = "";
                        keyBuff = "";
                        awaitingPosition = -1;
                        break;

                    case Mode.ReadDirect:
                        if (alphanum.IsMatch(currentString))
                        {
                            valueBuff += currentString;
                            break;
                        }
                        mode = Mode.Default;
                        output.Add(keyBuff, valueBuff);
                        valueBuff = "";
                        keyBuff = "";
                        break;
                }
            }
            return output;
        }

        /// <summary>
        /// Finds the index of the closing character that matches the opening character in a JSON string.
        /// </summary>
        /// <param name="input">The JSON string to search through.</param>
        /// <param name="startChar">The opening character (e.g., '{', '[', or '\"').</param>
        /// <param name="endChar">The closing character (e.g., '}', ']', or '\"').</param>
        /// <param name="startIndex">The index of the opening character in the input string.</param>
        /// <returns>The index of the closing character, or -1 if no match is found.</returns>
        private int GetMatchingCounter(string input, char startChar, char endChar, int startIndex = 0)
        {
            if (input[startIndex] != startChar) return -1;

            int counter = 1;

            startIndex++;

            if (startChar == endChar && startChar == '"')
            {
                while (counter != 0 && startIndex < input.Length)
                {
                    char c = input[startIndex];
                    if (startChar == '"' && c == '\\') counter++;
                    if (input[startIndex] == startChar) counter--;

                    startIndex++;
                }
            }
            else
            {
                while (counter != 0 && startIndex < input.Length)
                {
                    char c = input[startIndex];
                    if (startChar == '"' && c == '\\') counter++;
                    if (input[startIndex] == startChar) counter++;
                    else if (input[startIndex] == endChar) counter--;

                    startIndex++;
                }
            }

            return startIndex;
        }
    }
}
