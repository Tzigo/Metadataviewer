using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageMetadataParser
{
    internal class MetadataFormats
    {
        public enum Status
        {
            UNREAD,
            READ_SUCCESS,
            FORMAT_ERROR,
            COMFYUI_ERROR
        }

        public class BaseFormat
        {
            protected static string[] parametersKey = { "model", "sampler", "seed", "cfg", "steps", "size" };
            public int height;
            public int width;
            public Dictionary<string, string> info;
            public string positive;
            public string negative;
            public string positive_sdxl;
            public string negative_sdxl;
            public string settings;
            public string raw;
            public Dictionary<string, string> parameter;
            public bool is_sdxl;
            public Status status;
            public Dictionary<string, string> otherParameters;

            public BaseFormat(Dictionary<string, string> info, string raw = "", int height = 0, int width = 0)
            {
                this.height = height;
                this.width = width;
                this.info = info;
                positive = "";
                negative = "";
                positive_sdxl = "";
                negative_sdxl = "";
                settings = "";
                this.raw = raw;
                parameter = new Dictionary<string, string>();
                otherParameters = new Dictionary<string, string>();
                foreach (string key in parametersKey)
                {
                    parameter.Add(key, "");
                }
                is_sdxl = false;
                status = Status.UNREAD;
            }

            public Status Parse()
            {
                try
                {
                    Process();
                    status = Status.READ_SUCCESS;
                }
                catch (Exception)
                {
                    status = Status.FORMAT_ERROR;
                }
                return status;
            }

            virtual protected void Process()
            {
                return;
            }
        }

        public class A1111 : BaseFormat
        {
            private string _extra;

            private static string[] settingsKey = { "Model", "Sampler", "Seed", "CFG scale", "Steps", "Size" };

            public A1111(Dictionary<string, string> info, string raw = "") : base(info, raw)
            {
                _extra = "";
            }

            override protected void Process()
            {
                if (raw == "")
                {
                    if (info.ContainsKey("parameters")) raw = info["parameters"];
                    else raw = "";
                    if (info.ContainsKey("postprocessing")) _extra = info["postprocessing"];
                    else _extra = "";
                }
                SDFormat();
            }

            private void SDFormat()
            {
                if (raw == "" && _extra == "") return;

                int stepIndex = raw.IndexOf("\nSteps:");

                if (stepIndex != -1)
                {
                    positive = raw.Substring(0, stepIndex).Trim();
                    settings = raw.Substring(stepIndex).Trim();
                }

                if (raw.Contains("Negative prompt:"))
                {
                    int promptIndex = raw.IndexOf("\nNegative prompt:");
                    int start = promptIndex + "Negative prompt:".Length + 1;

                    if (stepIndex != -1)
                    {
                        negative = raw.Substring(start, stepIndex - start).Trim();
                    }
                    else
                    {
                        negative = raw.Substring(start).Trim();
                    }
                    positive = raw.Substring(0, promptIndex).Trim();
                }
                else if (stepIndex == -1)
                {
                    positive = raw;
                }

                string pattern = @"\s*([^:,]+):\s*([^,]+)";
                Regex regex = new Regex(pattern);
                MatchCollection matches = regex.Matches(settings);
                Dictionary<string, string> settingsDict = new Dictionary<string, string>();
                foreach (Match match in matches)
                {
                    if (!match.Success) continue;
                    if (settingsDict.ContainsKey(match.Groups[1].Value)) continue;
                    settingsDict.Add(match.Groups[1].Value, match.Groups[2].Value);
                }

                if (settingsDict.ContainsKey("Size"))
                {
                    string[] dimensions = settingsDict["Size"].Split('x');
                    width = int.Parse(dimensions[0]);
                    height = int.Parse(dimensions[1]);
                }
                else
                {
                    width = height = 0;
                }

                for (int i = 0; i < parametersKey.Length; i++)
                {
                    parameter[parametersKey[i]] = settingsDict[settingsKey[i]];
                }

                foreach (string key in settingsDict.Keys)
                {
                    if (settingsKey.Contains(key)) continue;
                    otherParameters[key] = settingsDict[key];
                }

                if (_extra != "")
                {
                    raw += _extra;
                    settings += _extra;
                }
            }
        }

        public class ComfyUI : BaseFormat
        {
            private static List<string> intInfo = new List<string> { "steps", "cfg" };
            private static List<string> nodeInfoSimple = new List<string> { "model", "sampler_name", "seed", "scheduler", "positive", "negative" };
            private static List<string> nodeInfoAdvanced = new List<string> { "model", "positive", "negative" };
            private static List<string> stringInfoAdvanced = new List<string> { "sampler_name", "scheduler" };


            public ComfyUI(Dictionary<string, string> info, string raw = "", int height = 0, int width = 0) : base(info, raw, height, width)
            {
            }

            override protected void Process()
            {
                JSONDict dict = new JSONDict(info["prompt"]);
                DataNode KSamplerNode = FindNodeByClassType(dict, new List<string> { "KSampler", "KSamplerAdvanced" });
                if (KSamplerNode == null) throw new Exception();
                JSONDict KSampler = (JSONDict)KSamplerNode.data;

                JSONDict SamplerInputs = KSampler["inputs"].GetOr<JSONDict>(null);
                if (SamplerInputs == null) throw new Exception();

                if ((string)KSampler["class_type"].data == "KSampler")
                {
                    parameter["cfg"] = SamplerInputs["cfg"]?.GetOr(0).ToString() ?? "0";
                    parameter["steps"] = SamplerInputs["steps"]?.GetOr(0).ToString() ?? "0";

                    parameter["model"] = FindNodeByClassType(dict, "CheckpointLoaderSimple")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["ckpt_name"]
                        .GetOr("") ?? "";

                    parameter["sampler"] = FindNodeByClassType(dict, "Sampler Selector")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["sampler_name"]
                        .GetOr("") ?? "";

                    parameter["seed"] = FindNodeByClassType(dict, "Seed", false)?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["seed"]
                        .GetOr(0).ToString() ?? "0";

                    JSONDict img = FindNodeByClassType(dict, "EmptyLatentImage")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null);
                    if (img != null)
                    {
                        int width = img["width"]?.GetOr(0) ?? 0;
                        int height = img["height"]?.GetOr(0) ?? 0;
                        parameter["size"] = $"{width}x{height}";
                    }
                    else parameter["size"] = "";

                    positive = FindNodeByClassType(dict, "LoraTagLoader")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["text"]
                        .GetOr("") ?? "";

                    negative = FindNodeByClassType(dict, "CLIPTextEncode")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["text"]
                        .GetOr("") ?? "";

                }
                else
                {
                    parameter["cfg"] = SamplerInputs["cfg"]?.GetOr(0).ToString() ?? "0";
                    parameter["steps"] = SamplerInputs["steps"]?.GetOr(0).ToString() ?? "0";
                    parameter["sampler"] = SamplerInputs["steps"]?.GetOr("") ?? "";
                    otherParameters.Add("scheduler", SamplerInputs["scheduler"]?.GetOr(""));

                    parameter["model"] = FindNodeByClassType(dict, "CheckpointLoaderSimple")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["ckpt_name"]
                        .GetOr("") ?? "";

                    //No seed given

                    JSONDict img = FindNodeByClassType(dict, "EmptyLatentImage")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null);
                    if (img != null)
                    {
                        int width = img["width"]?.GetOr(0) ?? 0;
                        int height = img["height"]?.GetOr(0) ?? 0;
                        parameter["size"] = $"{width}x{height}";
                    }
                    else parameter["size"] = "";

                    JSONDict loraNode = FindNodeByClassType(dict, "Lora Loader Stack", false)?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null);



                    //Find through SamplerNode
                    positive = FindNodeByClassType(dict, "CLIP Text Encode")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["text"]
                        .GetOr("") ?? "";

                    negative = FindNodeByClassType(dict, "CLIPTextEncode")?
                        .GetOr<JSONDict>(null)?["inputs"]
                        .GetOr<JSONDict>(null)?["text"]
                        .GetOr("") ?? "";
                }
            }

            private DataNode FindNodeByClassType(JSONDict dict, string name, bool Exact = true)
            {
                return dict.GetFirst((DataNode node) =>
                {
                    if (node.type != typeof(JSONDict)) return false;

                    JSONDict dataNode = (JSONDict)node.data;
                    if (!dataNode.ContainsKey("class_type")) return false;

                    string nodeName = (string)dataNode["class_type"].data;
                    if (Exact)
                    {
                        if (nodeName != name) return false;
                    }
                    else
                    {
                        if (!nodeName.StartsWith(name)) return false;
                    }
                    return true;
                });
            }

            private DataNode FindNodeByClassType(JSONDict dict, List<string> names, bool Exact = true)
            {
                return dict.GetFirst((DataNode node) =>
                {
                    if (node.type != typeof(JSONDict)) return false;

                    JSONDict dataNode = (JSONDict)node.data;
                    if (!dataNode.ContainsKey("class_type")) return false;

                    string nodeName = (string)dataNode["class_type"].data;
                    if (Exact)
                    {
                        if (!names.Contains(nodeName)) return false;
                        return true;
                    }
                    else
                    {
                        bool found = false;
                        foreach (string name in names)
                        {
                            if (name.StartsWith(nodeName)) found = true;
                        }
                        return found;
                    }
                });
            }
        }
    }
}
