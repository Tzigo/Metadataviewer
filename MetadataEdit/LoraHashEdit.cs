using System;
using System.Windows;
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
            set
            {
                SetProperty(ref _editlora_new_hash, value);
                SetEditVisibility();
            }


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

        private Visibility editlorafalse = Visibility.Hidden;
        public Visibility _EditLoraFalse
        {
            get => editlorafalse;
            set => SetProperty(ref editlorafalse, value);
        }

        private Visibility editloratrue = Visibility.Hidden;
        public Visibility _EditLoraTrue
        {
            get => editloratrue;
            set => SetProperty(ref editloratrue, value);
        }
        private void SetEditVisibility()
        {
            if (EditLora_New_Hash.Length == 0)
            {
                _EditLoraFalse = Visibility.Hidden;
                _EditLoraTrue = Visibility.Hidden;
            }
            if (EditLora_New_Hash.Length >= 1)
            {
                if (EditLora_New_Hash.Length == 10)
                {
                    _EditLoraFalse = Visibility.Hidden;
                    _EditLoraTrue = Visibility.Visible;
                }
                else
                {
                    _EditLoraFalse = Visibility.Visible;
                    _EditLoraTrue = Visibility.Hidden;
                }
            }
        }
    }
}
