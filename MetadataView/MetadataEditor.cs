using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMetadataParser
{
    public class PngUtil
    {
        public static Dictionary<byte[], Func<byte[], Tuple<string, string>>> pattern_dict =
            new Dictionary<byte[], Func<byte[], Tuple<string, string>>>()
            {
                { new byte[4] { 0x74, 0x45, 0x58, 0x74 }, F_tEXt },
                { new byte[4] { 0x69, 0x54, 0x58, 0x74 }, F_iTXt }
            };

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

    }

    public class DArray<T>
    {
        private T[] data;
        private int endIndex;
        private int baseEmptySpace = 128;
        public int Count;
        public int Capacity;

        private string dequeEmptyMessage = "The Deque is empty";

        public DArray()
        {
            Capacity = baseEmptySpace;
            Count = 0;
            endIndex = 0;
            data = new T[Capacity];
        }

        public T this[int index]
        {
            get
            {
                if (index < 0) index += Count;
                if (index >= endIndex) throw new IndexOutOfRangeException();
                if (index < 0) throw new IndexOutOfRangeException();
                return data[index];
            }
        }

        public void Push(T element)
        {
            data[endIndex] = element;
            endIndex++;
            Count++;

            Resize();
        }

        public T Pop()
        {
            if (Count == 0) throw new InvalidOperationException(dequeEmptyMessage);
            T element = data[endIndex - 1];
            data[endIndex - 1] = default;
            endIndex--;
            Count--;

            Resize();

            return element;
        }

        private void Resize()
        {
            if (endIndex < Capacity && Capacity - Count < baseEmptySpace * 3) return;

            Capacity = Count + baseEmptySpace;

            T[] newData = new T[Capacity];

            Array.Copy(data, 0, newData, 0, Count);

            data = newData;
        }

        public T[] ToArray()
        {
            T[] output = new T[Count];
            Array.Copy(data, 0, output, 0, Count);
            return output;
        }

        public DArrayEnumerator GetEnumerator()
        {
            return new DArrayEnumerator(this);
        }

        public class DArrayEnumerator
        {
            private int index;
            private DArray<T> data;

            public DArrayEnumerator(DArray<T> deque)
            {
                index = -1;
                data = deque;
            }

            public bool MoveNext()
            {
                index++;
                return (index < data.Count);
            }

            public T Current => data[index];
        }
    }

    internal class MetadataEditor
    {
        private class DestructuredPNG
        {
            public byte[] PreData;
            public Dictionary<string, string> metadata;
            public byte[] PostData;

            private static Dictionary<byte[], Func<byte[], Tuple<string, string>>> pattern_dict = PngUtil.pattern_dict;

            public DestructuredPNG(string path)
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buffer = new byte[1] { 0x00 };
                    int position = 0;
                    Dictionary<int, Func<byte[], Tuple<string, string>>> positions = new Dictionary<int, Func<byte[], Tuple<string, string>>>();
                    List<byte> finalData = new List<byte>();

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
                }

            }
        }

        private class MetadataBlock
        {
            public byte[] header = new byte[4];
            public List<byte> key = new List<byte>();
            public DArray<byte> value = new DArray<byte>();

            public MetadataBlock(FileStream fs, byte[] length, byte[] header)
            {
                Array.Reverse(length);
                int len = BitConverter.ToInt32(length, 0);
                this.header = header;

                bool readValue = false;

                byte[] readBuff = new byte[1];

                switch (Encoding.UTF8.GetString(header))
                {
                    case "tEXt":
                        for (int i = 0; i < len; i++)
                        {
                            fs.Read(readBuff, 0, readBuff.Length);

                            if (!readValue && readBuff[0] == 0)
                            {
                                readValue = true;
                                continue;
                            }

                            if (!readValue)
                            {
                                key.Add(readBuff[0]);
                            }
                            else
                            {
                                value.Push(readBuff[0]);
                            }
                        }
                        break;

                    case "iTXt":
                        int separator = 0;
                        bool exit = false;
                        for (int i = 0; i < len; i++)
                        {
                            if (exit) break;

                            fs.Read(readBuff, 0, readBuff.Length);

                            if (readBuff[0] == 0)
                            {
                                separator++;
                                i++;
                                key.Add(0x00);
                                fs.Read(readBuff, 0, readBuff.Length);
                            }
                            switch (separator)
                            {
                                case 0:
                                    key.Add(readBuff[0]);
                                    break;

                                case 1:
                                    if (readBuff[0] != 0) break;
                                    i++;
                                    key.Add(0x00);
                                    fs.Read(readBuff, 0, readBuff.Length);
                                    while (readBuff[0] != 0)
                                    {
                                        key.Add(readBuff[0]);
                                        fs.Read(readBuff, 0, readBuff.Length);
                                        i++;
                                    }
                                    fs.Seek(-1, SeekOrigin.Current);
                                    key.RemoveAt(key.Count - 1);
                                    i--;
                                    break;

                                case 3:
                                    value.Push(readBuff[0]);
                                    break;

                                default:
                                    key.Add(readBuff[0]);
                                    break;
                            }
                        }
                        break;
                }
            }

            public byte[] ToArray()
            {
                byte[] value = this.value.ToArray();
                byte[] data = new byte[8 + key.Count + 1 + value.Length];
                byte[] length = BitConverter.GetBytes(key.Count + 1 + value.Length);
                Array.Reverse(length);
                length.CopyTo(data, 0);
                header.CopyTo(data, 4);
                key.CopyTo(data, 8);
                data[8 + key.Count] = 0x00;
                value.CopyTo(data, 8 + key.Count + 1);

                return data;
            }
        }

        public static string ReplaceHash(string oldPath, string newPath, string[] oldHashes, string[] newHashes)
        {
            DArray<byte> outStream = new DArray<byte>();

            try
            {
                using (FileStream fs = new FileStream(oldPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Dictionary<byte[], Func<byte[], Tuple<string, string>>> pattern_dict = PngUtil.pattern_dict;
                    byte[] buffer = new byte[1];
                    byte[][] patterns = pattern_dict.Keys.ToArray();
                    int[] matches = new int[patterns.Length];

                    while (fs.Read(buffer, 0, buffer.Length) > 0)
                    {
                        outStream.Push(buffer[0]);

                        for (int i = 0; i < pattern_dict.Count; i++)
                        {
                            byte[] pattern = patterns[i];
                            if (pattern[matches[i]] == buffer[0]) matches[i]++;
                            else matches[i] = 0;

                            if (matches[i] == pattern.Length)
                            {
                                byte[] things = new byte[8];
                                for (int j = 7; j >= 0; j--)
                                {
                                    things[j] = outStream.Pop();
                                }

                                byte[] len = things.Take(4).ToArray();
                                byte[] header = things.Reverse().Take(4).Reverse().ToArray();

                                MetadataBlock block = new MetadataBlock(fs, len, header);
                                for (int j = 0; j < oldHashes.Length; j++)
                                {
                                    if (oldHashes[j] == newHashes[j]) continue;

                                    byte[] oldHash = Encoding.UTF8.GetBytes(oldHashes[j]);

                                    byte[] oldbuff = new byte[oldHash.Length];
                                    int oldMatch = 0;

                                    DArray<byte> newValue = new DArray<byte>();

                                    foreach (byte b in block.value)
                                    {
                                        if (b == oldHash[oldMatch])
                                        {
                                            oldbuff[oldMatch] = b;
                                            oldMatch++;

                                            if (oldMatch == oldHash.Length)
                                            {
                                                foreach (byte b2 in Encoding.UTF8.GetBytes(newHashes[j]))
                                                {
                                                    newValue.Push(b2);
                                                }
                                                oldMatch = 0;
                                            }
                                        }
                                        else if (oldMatch > 0)
                                        {
                                            for (int k = 0; k < oldMatch; k++)
                                            {
                                                newValue.Push(oldbuff[k]);
                                                oldbuff[k] = default;
                                            }
                                            oldMatch = 0;
                                            newValue.Push(b);
                                        }
                                        else
                                        {
                                            newValue.Push(b);
                                        }
                                    }

                                    block.value = newValue;
                                }
                                byte[] finBlock = block.ToArray();
                                foreach (byte b in finBlock)
                                {
                                    outStream.Push(b);
                                }
                                matches[i] = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }

            try
            {
                using (FileStream fs = new FileStream(newPath, FileMode.Create))
                {
                    fs.Write(outStream.ToArray(), 0, outStream.Count);
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }

            return null;
        }

        private static long FindPosition(FileStream fs, byte[] target)
        {
            byte[] buffer = new byte[1];

            int match = 0;

            long originalPosition = fs.Position;

            fs.Seek(0, SeekOrigin.Begin);

            while (fs.Read(buffer, 0, buffer.Length) > 0)
            {
                if (buffer[0] == target[match]) match++;
                else match = 0;

                if (match == target.Length)
                {
                    fs.Seek(originalPosition, SeekOrigin.Begin);
                    return fs.Position - target.Length;
                }
            }

            fs.Seek(originalPosition, SeekOrigin.Begin);

            return -1;
        }

        private static List<long> FindPositions(FileStream fs, byte[] target)
        {
            byte[] buffer = new byte[1];

            int match = 0;

            List<long> positions = new List<long>();

            long originalPosition = fs.Position;

            fs.Seek(0, SeekOrigin.Begin);

            while (fs.Read(buffer, 0, buffer.Length) > 0)
            {
                if (buffer[0] == target[match]) match++;
                else match = 0;

                if (match == target.Length)
                {
                    positions.Add(fs.Position - target.Length);
                }
            }

            fs.Seek(originalPosition, SeekOrigin.Begin);

            return positions;
        }
    }
}
