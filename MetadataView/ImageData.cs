using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ImageMetadataParser
{
    class Model
    {
        public readonly string name;
        public readonly string hash;

        public Model(string name, string hash)
        {
            this.name = name;
            this.hash = hash;
        }
    }

    internal class ImageData
    {
        public readonly string prompt;
        public readonly string negativePrompt;
        public readonly Model model;
        public readonly string steps;
        public readonly string cfg;
        public readonly string sampler;
        public readonly string seed;
        public readonly int[] size;
        public readonly List<Model> loras;
        public readonly Dictionary<string, string> misc;

        private ImageData(string prompt, string negativePrompt, Model model, string steps, string cfg, string sampler, string seed, int[] size, List<Model> loras, Dictionary<string, string> misc)
        {
            this.prompt = prompt;
            this.negativePrompt = negativePrompt;
            this.model = model;
            this.steps = steps;
            this.cfg = cfg;
            this.sampler = sampler;
            this.seed = seed;
            this.size = size;
            this.loras = loras;
            this.misc = misc;
        }

        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="FormatException"></exception>
        public static ImageData FromPath(string path)
        {
            Dictionary<string, string> dict = PngInfoReader.GetPngMetadataNew(path);
            string format = path.Split('.').Last();
            return FromDict(dict, 0, 0, format);
        }

        public static ImageData FromDict(Dictionary<string, string> data, int width, int height, string format)
        {
            string _tool;
            MetadataFormats.BaseFormat _parser;
            if (format == "png")
            {
                if (data.ContainsKey("parameters"))
                {
                    //swarm format
                    if (data["parameters"].Contains("\"sui_image_params\""))
                    {
                        _tool = "Not Supported";
                    }
                    //a1111 png compatible format
                    else
                    {
                        if (data["parameters"].Contains("\"prompt\""))
                        {
                            //_tool = "ComfyUI\n(A1111 compatible)";
                        }
                        else
                        {
                            //_tool = "A1111 webUI";
                        }
                        _parser = new MetadataFormats.A1111(data);
                        MetadataFormats.Status status = _parser.Parse();
                        if (status != MetadataFormats.Status.READ_SUCCESS) throw new FormatException();
                        string modelHashPattern = @"Model hash: (.+?),";
                        Regex modelHashRegex = new Regex(modelHashPattern);
                        MatchCollection modelHashMatches = modelHashRegex.Matches(_parser.settings);
                        string model_hash;
                        if (modelHashMatches.Count == 0) model_hash = "";
                        else
                        {
                            model_hash = modelHashMatches[0].Groups[1].Value;
                        }
                        if (_parser.otherParameters.ContainsKey("Model hash")) model_hash = _parser.otherParameters["Model hash"];
                        List<Model> loras = new List<Model>();
                        if (_parser.otherParameters.ContainsKey("Hashes"))
                        {
                            string loraPattern = "\"lora:(.+?)\":\\s*\"(\\S+)\"";
                            Regex loraRegex = new Regex(loraPattern);
                            MatchCollection loraMatches = loraRegex.Matches(_parser.otherParameters["Hashes"]);
                            foreach (Match match in loraMatches)
                            {
                                if (!match.Success) continue;
                                loras.Add(new Model(match.Groups[1].Value, match.Groups[2].Value));
                            }
                            _parser.otherParameters.Remove("Hashes");
                        }
                        else if (_parser.raw.Contains("Lora hashes:"))
                        {
                            string loraHashesPattern = "Lora hashes: \"(.+)\"";
                            Regex loraHashesRegex = new Regex(loraHashesPattern);
                            Match loraHashesMatch = loraHashesRegex.Match(_parser.raw);
                            if (loraHashesMatch.Success)
                            {
                                string loraPattern = @"(\S+):\s*(\S+)";
                                Regex loraRegex = new Regex(loraPattern);
                                MatchCollection loraMatches = loraRegex.Matches(loraHashesMatch.Value.Substring("Lora hashes: ".Length + 1));
                                foreach (Match match in loraMatches)
                                {
                                    if (!match.Success) continue;
                                    loras.Add(new Model(match.Groups[1].Value, match.Groups[2].Value));
                                }
                            }
                            List<string> reconstructed = new List<string>();
                            foreach (string key in _parser.otherParameters.Keys)
                            {
                                reconstructed.Add(key + ": " + _parser.otherParameters[key]);
                            }
                            string fullReconstructed = String.Join(", ", reconstructed);
                            fullReconstructed = fullReconstructed.Remove(fullReconstructed.IndexOf("Lora hashes:"), loraHashesMatch.Value.Length + 2);
                            string depattern = @"\s*([^:,]+):\s*([^,]+)";
                            Regex deregex = new Regex(depattern);
                            MatchCollection dematches = deregex.Matches(fullReconstructed);
                            Dictionary<string, string> newOthers = new Dictionary<string, string>();
                            foreach (Match match in dematches)
                            {
                                if (!match.Success) continue;
                                newOthers.Add(match.Groups[1].Value, match.Groups[2].Value);
                            }
                            _parser.otherParameters = newOthers;
                        }
                        Model model = new Model(_parser.parameter["model"], model_hash);
                        return new ImageData(
                            _parser.positive,
                            _parser.negative,
                            model,
                            _parser.parameter["steps"],
                            _parser.parameter["cfg"],
                            _parser.parameter["sampler"],
                            _parser.parameter["seed"],
                            new int[] { _parser.height, _parser.width },
                            loras,
                            _parser.otherParameters);
                    }
                }
                else if (data.ContainsKey("postprocessing"))
                {
                    //_tool = "A1111 webUI\n(Postprocessing)";
                    //_parser = A1111(info = self._info)
                    _parser = new MetadataFormats.A1111(data);
                    MetadataFormats.Status status = _parser.Parse();
                    if (status != MetadataFormats.Status.READ_SUCCESS) throw new FormatException();
                    string modelHashPattern = @"Model hash: (.+?),";
                    Regex modelHashRegex = new Regex(modelHashPattern);
                    MatchCollection modelHashMatches = modelHashRegex.Matches(_parser.settings);
                    string model_hash;
                    if (modelHashMatches.Count == 0) model_hash = "";
                    else
                    {
                        model_hash = modelHashMatches[0].Groups[1].Value;
                    }
                    if (_parser.otherParameters.ContainsKey("Model hash")) model_hash = _parser.otherParameters["Model hash"];
                    List<Model> loras = new List<Model>();
                    if (_parser.otherParameters.ContainsKey("Hashes"))
                    {
                        string loraPattern = "\"lora:(.+?)\":\\s*\"(\\S+)\"";
                        Regex loraRegex = new Regex(loraPattern);
                        MatchCollection loraMatches = loraRegex.Matches(_parser.otherParameters["Hashes"]);
                        foreach (Match match in loraMatches)
                        {
                            if (!match.Success) continue;
                            loras.Add(new Model(match.Groups[1].Value, match.Groups[2].Value));
                        }
                        _parser.otherParameters.Remove("Hashes");
                    }
                    else if (_parser.raw.Contains("Lora hashes:"))
                    {
                        string loraHashesPattern = "Lora hashes: \"(.+)\"";
                        Regex loraHashesRegex = new Regex(loraHashesPattern);
                        Match loraHashesMatch = loraHashesRegex.Match(_parser.raw);
                        if (loraHashesMatch.Success)
                        {
                            string loraPattern = @"(\S+):\s*(\S+)";
                            Regex loraRegex = new Regex(loraPattern);
                            MatchCollection loraMatches = loraRegex.Matches(loraHashesMatch.Value.Substring("Lora hashes: ".Length + 1));
                            foreach (Match match in loraMatches)
                            {
                                if (!match.Success) continue;
                                loras.Add(new Model(match.Groups[1].Value, match.Groups[2].Value));
                            }
                        }
                        List<string> reconstructed = new List<string>();
                        foreach (string key in _parser.otherParameters.Keys)
                        {
                            reconstructed.Add(key + ": " + _parser.otherParameters[key]);
                        }
                        string fullReconstructed = String.Join(", ", reconstructed);
                        fullReconstructed = fullReconstructed.Remove(fullReconstructed.IndexOf("Lora hashes:"), loraHashesMatch.Value.Length + 2);
                        string depattern = @"\s*([^:,]+):\s*([^,]+)";
                        Regex deregex = new Regex(depattern);
                        MatchCollection dematches = deregex.Matches(fullReconstructed);
                        Dictionary<string, string> newOthers = new Dictionary<string, string>();
                        foreach (Match match in dematches)
                        {
                            if (!match.Success) continue;
                            newOthers.Add(match.Groups[1].Value, match.Groups[2].Value);
                        }
                        _parser.otherParameters = newOthers;
                    }
                    Model model = new Model(_parser.parameter["model"], model_hash);
                    return new ImageData(
                        _parser.positive,
                        _parser.negative,
                        model,
                        _parser.parameter["steps"],
                        _parser.parameter["cfg"],
                        _parser.parameter["sampler"],
                        _parser.parameter["seed"],
                        new int[] { _parser.height, _parser.width },
                        loras,
                        _parser.otherParameters);

                }
                //comfyui format
                else if (data.ContainsKey("prompt"))
                {
                    _tool = "ComfyUI";
                    /*_parser = ComfyUI(
                                info = self._info, width = self._width, height = self._height
                            )*/
                    _parser = new MetadataFormats.ComfyUI(data);
                    MetadataFormats.Status status = _parser.Parse();
                    if (status != MetadataFormats.Status.READ_SUCCESS) throw new FormatException();

                    throw new NotImplementedException();
                }
                else
                {
                    _tool = "Not Supported";
                }
            }
            else if (format == "jpeg" || format == "webp")
            {
                throw new NotImplementedException();
            }
            else
            {
                _tool = "Not Supported";
            }

            if (_tool == "Not Supported") throw new NotSupportedException();
            throw new NotSupportedException();
        }
    }
}
