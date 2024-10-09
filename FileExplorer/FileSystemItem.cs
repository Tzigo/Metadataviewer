using System.Collections.Generic;
using System.ComponentModel;

namespace Metadataviewer
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public List<FileSystemItem> Children { get; set; }

        // Neu: IsExpanded Property für TreeView
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public FileSystemItem()
        {
            Children = new List<FileSystemItem>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
