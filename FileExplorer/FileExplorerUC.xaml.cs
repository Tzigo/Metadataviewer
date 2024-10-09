using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Metadataviewer
{
    /// <summary>
    /// Interaktionslogik für FileExplorerUC.xaml
    /// </summary>
    public partial class FileExplorerUC : UserControl
    {
        FileExplorerViewModel viewModel;
        private bool ShowOnlyImages;
        public FileExplorerUC(bool showonlyimages)
        {
            InitializeComponent();
            ShowOnlyImages = showonlyimages;
            viewModel = new FileExplorerViewModel(Tree, showonlyimages);
            this.DataContext = viewModel;
        }

        public void SetColorTheme(ColorTheme colors)
        {
            viewModel.SetColorTheme(colors);
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileSystemItem selectedItem && selectedItem.IsDirectory && selectedItem.Children.Count == 1 && selectedItem.Children[0].Name == "Loading...")
            {
                var viewModel = DataContext as FileExplorerViewModel;
                viewModel.LoadDirectory(selectedItem);
            }
        }

        private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Tree.SelectedItem != null)
            {
                if (Tree.SelectedItem is FileSystemItem selecteditem)
                {
                    if (ShowOnlyImages && viewModel.IsImage(selecteditem.Path) || System.IO.Directory.Exists(selecteditem.Path))
                    {
                        OutGoingPath = selecteditem.Path;
                        OnSetPathPropertyChanged();
                    }
                    else if (!ShowOnlyImages)
                    {
                        OutGoingPath = selecteditem.Path;
                        OnSetPathPropertyChanged();
                    }
                }
            }
        }

        public string OutGoingPath;
        protected virtual void OnSetPathPropertyChanged()
        {
            SetPathPropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler SetPathPropertyChanged;

        public void SetPath(string path)
        {
            if (path != null && path != string.Empty)
            {
                viewModel.IncomingPath(path);
            }
        }

    }
}
