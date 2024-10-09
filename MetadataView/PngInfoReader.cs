using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace ImageMetadataParser
{
    public class PngInfoReader
    {
        private static Dictionary<byte[], Func<byte[], Tuple<string, string>>> pattern_dict =
            new Dictionary<byte[], Func<byte[], Tuple<string, string>>>()
            {
                { new byte[4] { 0x74, 0x45, 0x58, 0x74 }, F_tEXt },
                { new byte[4] { 0x69, 0x54, 0x58, 0x74 }, F_iTXt }
            };

        public static Dictionary<string, string> GetPngMetadataNew(string filePath)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[1] { 0x00 };
                int position = 0;
                Dictionary<int, Func<byte[], Tuple<string, string>>> positions = new Dictionary<int, Func<byte[], Tuple<string, string>>>();

                byte[][] patterns = pattern_dict.Keys.ToArray();
                int[] matches = new int[patterns.Length];

                for (int i = 0; i < matches.Length; i++)
                {
                    matches[i] = 0;
                }

                while (fs.Read(buffer, 0, buffer.Length) > 0)
                {
                    for (int i = 0; i < pattern_dict.Count; i++)
                    {
                        byte[] pattern = patterns[i];
                        if (pattern[matches[i]] == buffer[0]) matches[i]++;
                        else matches[i] = 0;

                        if (matches[i] == pattern.Length)
                        {
                            positions.Add(position + 1 - pattern.Length, pattern_dict[pattern]);
                            matches[i] = 0;
                        }
                    }

                    position++;
                }

                foreach (int pos in positions.Keys)
                {
                    byte[] lenBuff = new byte[4];
                    fs.Seek(pos - lenBuff.Length, SeekOrigin.Begin);
                    fs.Read(lenBuff, 0, lenBuff.Length);
                    Array.Reverse(lenBuff);
                    int len = BitConverter.ToInt32(lenBuff, 0);
                    fs.Seek(4, SeekOrigin.Current);

                    byte[] charBuff = new byte[len];
                    fs.Read(charBuff, 0, charBuff.Length);

                    Tuple<string, string> result = positions[pos](charBuff);

                    if (result == null) continue;

                    metadata.Add(result.Item1, result.Item2);
                }
            }

            return metadata;
        }


        public static Dictionary<string, string> GetPngMetadata(string filePath)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] pattern = new byte[4] { 0x74, 0x45, 0x58, 0x74 };
                int position = 0;
                byte[] buffer = new byte[1] { 0x00 };
                int match = 0;
                List<int> positions = new List<int>();

                while (fs.Read(buffer, 0, buffer.Length) > 0)
                {
                    if (buffer[0] == pattern[match]) match++;
                    else match = 0;

                    if (match == pattern.Length)
                    {
                        positions.Add(position + 1 - pattern.Length);
                        match = 0;
                    }

                    position++;
                }


                foreach (int pos in positions)
                {
                    byte[] lenBuff = new byte[4];
                    fs.Seek(pos - lenBuff.Length, SeekOrigin.Begin);
                    fs.Read(lenBuff, 0, lenBuff.Length);
                    Array.Reverse(lenBuff);
                    int len = BitConverter.ToInt32(lenBuff, 0);
                    fs.Seek(4, SeekOrigin.Current);

                    string key = "";
                    string value = "";
                    bool writeKey = true;
                    byte[] charBuff = new byte[1];
                    for (int i = 0; i < len; i++)
                    {
                        fs.Read(charBuff, 0, charBuff.Length);
                        if (charBuff[0] == 0x00)
                        {
                            writeKey = false;
                            continue;
                        }
                        if (writeKey)
                        {
                            key += Encoding.UTF8.GetString(charBuff);
                        }
                        else
                        {
                            value += Encoding.UTF8.GetString(charBuff);
                        }
                    }
                    metadata.Add(key, value);
                }
            }

            return metadata;
        }

        private static Tuple<string, string> F_tEXt(byte[] bytes)
        {
            int i = Array.FindIndex(bytes, b => b == 0);
            if (i == -1) return null;
            byte[] key = bytes.TakeWhile(b => b != 0).ToArray();
            byte[] value = bytes.Skip(i + 1).ToArray();

            return new Tuple<string, string>(Encoding.UTF8.GetString(key), Encoding.UTF8.GetString(value));
        }

        private static Tuple<string, string> F_iTXt(byte[] bytes)
        {
            int separator = 0;
            List<byte> key = new List<byte>();
            List<byte> value = new List<byte>();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    separator++;
                    i++;
                }
                switch (separator)
                {
                    case 0:
                        key.Add(bytes[i]);
                        break;

                    case 1:
                        if (bytes[i] != 0) return null;
                        i++;
                        while (bytes[i] != 0) i++;
                        i--;
                        break;

                    case 3:
                        value.Add(bytes[i]);
                        break;

                    default:
                        break;
                }
            }
            return new Tuple<string, string>(Encoding.UTF8.GetString(key.ToArray()), Encoding.UTF8.GetString(value.ToArray()));
        }

        public string PNG(string file)
        {
            if (!File.Exists(file))
            {
                return "File not found";
            }

            else
            {
                Dictionary<string, string> metadata = GetPngMetadata(file);

                string s = "";
                foreach (var entry in metadata)
                {
                    s += "\n\n" + entry.Key + " : " + entry.Value;
                }
                return s;
            }

        }
    }

}
