using System.Windows.Media;

namespace Metadataviewer
{
    public class ThumbNail : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        private string _path;
        public string Path
        {
            get { return this._path; }
            set { this._path = value; }
        }

        private ImageSource _image;
        public ImageSource Image
        {
            get { return this._image; }
            set { this._image = value; }
        }

        private bool _isselected;
        public bool IsSelected
        {
            get { return this._isselected; }
            set { this._isselected = value; }
        }


        private int _selectedimagesize;
        public int SelectedSize
        {
            get => _selectedimagesize;
            set => SetProperty(ref _selectedimagesize, value);
        }

        private Brush brush;
        public Brush _Brush
        {
            get => brush;
            set => SetProperty(ref brush, value);
        }

        private Brush _foreground;
        public Brush _Foreground
        {
            get => _foreground;
            set => SetProperty(ref _foreground, value);
        }

        private SolidColorBrush _decorationscolor;
        public SolidColorBrush _DecorationsColor
        {
            get => _decorationscolor;
            set => SetProperty(ref _decorationscolor, value);
        }

    }
}
