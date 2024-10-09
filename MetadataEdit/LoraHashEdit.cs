using System;
using System.Windows.Media;

namespace Metadataviewer
{
    public class LoraHashEdit : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);   
        }

        private string _editlorahash;
        public string EditLoraHash
        {
            get => _editlorahash;
            set => SetProperty(ref _editlorahash, value);
        }

        private string _editlora_new_hash;   
        public string EditLora_New_Hash
        {
            get => _editlora_new_hash;   
            set => SetProperty(ref _editlora_new_hash, value);
        }

        private SolidColorBrush backgroundcolor;
        public SolidColorBrush _BackgroundColor
        {
            get => backgroundcolor;
            set => SetProperty(ref backgroundcolor, value);
        }

        private SolidColorBrush foregroundcolor;
        public SolidColorBrush _ForegroundColor
        {
            get => foregroundcolor;
            set => SetProperty(ref foregroundcolor, value);
        }

        private SolidColorBrush decorationscolor;
        public SolidColorBrush _DecorationsColor
        {
            get => decorationscolor;
            set => SetProperty(ref decorationscolor, value);
        }

    }
}
