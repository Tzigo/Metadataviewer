using Metadataviewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMetadataParser
{
    public class MetaData
    {
        public string OriginalPath;
        public string Prompt;
        public string NegativePrompt;
        public string CheckpointName;
        public string CheckpointHash;
        public string Steps;
        public string CFG;
        public string Sampler;
        public string Seed;
        public string Size;
        public string LoraNames;
        public string LoraHashes;
        public string Misc;
        public string[] _LoraHashes;
        public Dictionary<string, string> Loras = new Dictionary<string, string>();


        //Woraround für zusätzliches " am Ende des Lora strings
        private string LoraQuotationMarksWorkaround(string lorahash)
        {
            string s = '"'.ToString();

            if (lorahash.EndsWith(s)) { return lorahash.TrimEnd('"'); }
            else if (lorahash.EndsWith(",")) { return lorahash.TrimEnd(','); }
            else { return lorahash; }
        }

        //

        public string GetMetaData(string imagepath)
        {
            return _Getmetadata(imagepath);
        }

        private string _Getmetadata(string imagepath)
        {
            try
            {
                ImageData data = ImageData.FromPath(imagepath);

                ResetFields();

                OriginalPath = imagepath;
                Prompt = data.prompt;
                NegativePrompt = data.negativePrompt;
                CheckpointName = data.model.name;
                CheckpointHash = data.model.hash;
                Steps = data.steps;
                CFG = data.cfg;
                Sampler = data.sampler;
                Seed = data.seed;
                Size = data.size[0] + "  X  " + data.size[1];
                foreach (Model m in data.loras)
                {
                    LoraNames += m.name + " | ";
                    //Woraround für zusätzliches " am Ende des Lora strings
                    string s = LoraQuotationMarksWorkaround(m.hash);

                    Loras.Add(m.name, s);
                    _LoraHashes = _LoraHashes.Append<string>(s).ToArray();
                    LoraHashes += (s + " | ");
                    // 

                    //_LoraHashes = _LoraHashes.Append<string>(m.hash).ToArray();
                    //Loras.Add(m.name, m.hash);
                    //LoraHashes += m.hash + " | ";
                }
                char[] charsToTrim = { ' ', '|', };
                LoraNames = LoraNames.TrimEnd(charsToTrim);
                LoraHashes = LoraHashes.TrimEnd(charsToTrim);
                foreach (string key in data.misc.Keys)
                {
                    Misc += key + " : " + data.misc[key] + Environment.NewLine;
                }

                return null;

            }
            catch (Exception e) { return e.GetType().ToString(); }

        }

        private void ResetFields()
        {
            OriginalPath = "";
            Prompt = "";
            NegativePrompt = "";
            CheckpointName = "";
            CheckpointHash = "";
            Steps = "";
            CFG = "";
            Sampler = "";
            Seed = "";
            Size = "";
            LoraNames = "";
            LoraHashes = "";
            Misc = "";
            _LoraHashes = new string[] { };
            Loras.Clear();

        }

        public string SetEditData(string originalfilepath, string newfilepath, string[] oldhashes, string[] newhashes)
        {
            return MetadataEditor.ReplaceHash(originalfilepath, newfilepath, oldhashes, newhashes);
        }

    }
}
