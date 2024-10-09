using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Metadataviewer
{
    public class FileExplorerViewModel : ViewModelBase
    {
        public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }

        private readonly TreeView Tree;

        private bool ShowOnlyImages;

        public FileExplorerViewModel(TreeView tree, bool showonlyimages)
        {
            ShowOnlyImages = showonlyimages;
            FileSystemItems = new ObservableCollection<FileSystemItem>();
            Tree = tree;
            LoadDrives();
        }

        private void LoadDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                var driveItem = new FileSystemItem
                {
                    Name = drive.Name,
                    Path = drive.RootDirectory.FullName,
                    IsDirectory = true
                };

                driveItem.Children.Add(new FileSystemItem { Name = "Loading...", Path = "", IsDirectory = true });
                driveItem.PropertyChanged += (object s, System.ComponentModel.PropertyChangedEventArgs e) =>
                {
                    if (e.PropertyName != "IsExpanded") return;
                    if (s.GetType() != typeof(FileSystemItem)) return;
                    FileSystemItem i = (FileSystemItem)s;
                    if (drive.DriveType == DriveType.CDRom || drive.DriveType == DriveType.Removable && !drive.IsReady) return;
                    if (i.Children.Count > 1 || i.Children[0].Name != "Loading...") return;
                    LoadDirectory(i);
                };
                FileSystemItems.Add(driveItem);
            }
        }

        public void LoadDirectory(FileSystemItem item)
        {
            item.Children.Clear(); // Entferne das Dummy-Item "Loading..."

            if (Directory.Exists(item.Path))
            {
                try
                {
                    // Lade Verzeichnisse
                    var directories = Directory.GetDirectories(item.Path);
                    foreach (var directory in directories)
                    {
                        var directoryInfo = new DirectoryInfo(directory);
                        var directoryItem = new FileSystemItem
                        {
                            Name = directoryInfo.Name,
                            Path = directoryInfo.FullName,
                            IsDirectory = true
                        };

                        // Füge ein Dummy-Item hinzu, um anzuzeigen, dass es Kinder geben könnte
                        directoryItem.Children.Add(new FileSystemItem { Name = "Loading...", Path = "", IsDirectory = true });
                        directoryItem.PropertyChanged += (object s, System.ComponentModel.PropertyChangedEventArgs e) =>
                        {
                            if (e.PropertyName != "IsExpanded") return;
                            if (s.GetType() != typeof(FileSystemItem)) return;
                            FileSystemItem i = (FileSystemItem)s;
                            if (i.Children.Count > 1 || i.Children.Count == 0 || i.Children[0].Name != "Loading...") return;
                            LoadDirectory(i);
                        };
                        item.Children.Add(directoryItem);
                    }

                    // Lade Dateien
                    var files = Directory.GetFiles(item.Path);
                    foreach (var file in files)
                    {
                        if (ShowOnlyImages && IsImage(file))
                        {
                            var fileInfo = new FileInfo(file);
                            var fileItem = new FileSystemItem
                            {
                                Name = fileInfo.Name,
                                Path = fileInfo.FullName,
                                IsDirectory = false
                            };

                            item.Children.Add(fileItem);
                        }
                        else if (!ShowOnlyImages)
                        {
                            var fileInfo = new FileInfo(file);
                            var fileItem = new FileSystemItem
                            {
                                Name = fileInfo.Name,
                                Path = fileInfo.FullName,
                                IsDirectory = false
                            };

                            item.Children.Add(fileItem);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Behandeln von Zugriffsfehlern (beispielsweise durch fehlende Berechtigungen)
                    MessageBox.Show("Access not possible");
                }
                catch (Exception ex)
                {
                    // Allgemeine Fehlerbehandlung
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private bool IsEmpytDirectory(string path)
        {
            int num = Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length;
            return num == 0;
        }

        internal bool IsImage(string filepath)
        {
            string file_extension = System.IO.Path.GetExtension(filepath).ToLower();
            if (file_extension == ".png" || file_extension == ".jpg" || file_extension == ".jpeg" || file_extension == ".webp") { return true; }

            return false;
        }

        internal void IncomingPath(string path)
        {
            if (File.Exists(path) || Directory.Exists(path)) { OpenPath(path); }
        }

        // Methode zum Öffnen eines bestimmten Pfades
        public void OpenPath(string path)
        {
            // Schritt 1: Schließe alle geöffneten Knoten, bevor ein neuer Pfad geöffnet wird
            CollapseAllItems(FileSystemItems);

            // Schritt 2: Finde das passende Laufwerk und navigiere zum Pfad
            foreach (var driveItem in FileSystemItems)
            {
                if (path.StartsWith(driveItem.Path, StringComparison.OrdinalIgnoreCase))
                {
                    // Sicherstellen, dass das Laufwerk geladen ist
                    LoadDirectory(driveItem);
                    ExpandToPath(driveItem, path);
                    break;
                }
            }
        }

        // Methode zum Schließen aller Knoten
        private void CollapseAllItems(ObservableCollection<FileSystemItem> items)
        {
            foreach (var item in items)
            {
                item.IsExpanded = false;
                foreach (var child in item.Children)
                {
                    CollapseAllItems(new ObservableCollection<FileSystemItem> { child });
                }
            }
        }

        // Rekursive Methode zum Expandieren des Pfades
        private void ExpandToPath(FileSystemItem currentItem, string path)
        {
            // Wenn das aktuelle Item der Zielpfad ist, setze den expandierten Zustand
            if (string.Equals(currentItem.Path.TrimEnd('\\'), path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                currentItem.IsExpanded = true;

                // Lade Kinderknoten nur, wenn sie noch nicht geladen sind
                if (currentItem.Children.Count == 1 && currentItem.Children[0].Name == "Loading...")
                {
                    LoadDirectory(currentItem); // Laden der tatsächlichen Inhalte
                }

                return; // Beende hier, da wir das Ziel erreicht haben
            }

            // Setze den expandierten Zustand und lade Kinderknoten
            currentItem.IsExpanded = true;

            // Lade die Kinderknoten des aktuellen Verzeichnisses
            LoadDirectory(currentItem);

            // Durchlaufe alle Kinderknoten rekursiv und expandiere den passenden Pfad
            foreach (var childItem in currentItem.Children)
            {
                if (path.StartsWith(childItem.Path, StringComparison.OrdinalIgnoreCase))
                {
                    ExpandToPath(childItem, path);
                    break; // Beende die Schleife, sobald der Pfad gefunden wurde
                }
            }
        }

        private void BringItemIntoView(FileSystemItem item)
        {
            var container = FindTreeViewItem(Tree, item);
            if (container != null)
            {
                container.BringIntoView();
                CenterTreeViewItem(container); // Zentrieren
            }
            else
            {
                // Wenn das Item nicht sofort gefunden wird, erneut versuchen
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    BringItemIntoView(item); // Versuche es erneut, bis das Item gefunden wird
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void CenterTreeViewItem(TreeViewItem item)
        {
            ScrollViewer scrollViewer = GetScrollViewer(item);
            if (scrollViewer != null)
            {
                GeneralTransform transform = item.TransformToAncestor(scrollViewer);
                Rect rect = transform.TransformBounds(new Rect(new Point(0, 0), item.RenderSize));

                double offset = rect.Top - (scrollViewer.ViewportHeight / 2) + (rect.Height / 2);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset);
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer) { return o as ScrollViewer; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Methode zum Finden des TreeViewItem
        private TreeViewItem FindTreeViewItem(ItemsControl parent, object item)
        {
            if (parent == null)
                return null;
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container)
                return container;

            foreach (object childItem in parent.Items)
            {
                ItemsControl childControl = parent.ItemContainerGenerator.ContainerFromItem(childItem) as ItemsControl;
                container = FindTreeViewItem(childControl, item);
                if (container != null)
                    return container;
            }
            return null;
        }

        internal void SetColorTheme(ColorTheme colors)
        {
            BackgroundColor = colors.BackGround;
            ForegroundColor = colors.ForeGround;
            DecorationsColor = colors.Decorations;
        }

        private string getpath;
        public string GetPath
        {
            get => getpath;
            set
            {
                SetProperty(ref getpath, value);
                OpenPath(GetPath);
            }
        }

        private SolidColorBrush backgroundcolor;
        public SolidColorBrush BackgroundColor
        {
            get => backgroundcolor;
            set => SetProperty(ref backgroundcolor, value);
        }

        private SolidColorBrush foregroundcolor;
        public SolidColorBrush ForegroundColor
        {
            get => foregroundcolor;
            set => SetProperty(ref foregroundcolor, value);
        }

        private SolidColorBrush decorationscolor;
        public SolidColorBrush DecorationsColor
        {
            get => decorationscolor;
            set => SetProperty(ref decorationscolor, value);
        }
    }
}
