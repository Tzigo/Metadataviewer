using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ImageMetadataParser;
using Microsoft.Win32;
using Path = System.IO.Path;
//using Brush = System.Windows.Media.Brush;

namespace Metadataviewer
{
    public class MainViewModel : ViewModelBase
    {
        MainWindow mainWindow;
        FileExplorerUC FileExplorer;
        public MetaData metadata = new MetaData();

        private ColorTheme colorTheme;
        public MainViewModel()
        {
            CreateImages();
            SetColorTheme(1);
            mainWindow = new MainWindow();
            mainWindow.DataContext = this;

            SubscribeMainWindowEvents();

            FileExplorer = new FileExplorerUC(true) { Width = 390};
            mainWindow.LeftGrid.Children.Add(FileExplorer);
            FileExplorer.SetColorTheme(colorTheme); 
            FileExplorer.Width = mainWindow.LeftGrid.Width;
            FileExplorer.Height = mainWindow.LeftGrid.Height;
            FileExplorer.SetPathPropertyChanged += FileExplorer_SetPathPropertyChanged;
            mainWindow.Show();
            SetSaveTypList();

            LoraHashEdits = new ObservableCollection<LoraHashEdit>();


            ImageCollection = new ObservableCollection<ThumbNail>();
            ImageCollection.CollectionChanged += ImageCollection_CollectionChanged;
            SuccessTimer.Elapsed += SuccessTimer_Elapsed;
            SelectedImageSize = 100;
        }

        private void SubscribeMainWindowEvents()
        {
            mainWindow.MainTab.DragOver += MainTab_DragOver;
            mainWindow.MainTab.Drop += MainTab_Drop;
            mainWindow.ThumbSizeChange += MainWindow_ThumbSizeChange;
            mainWindow.ThumbNailClick += MainWindow_ThumbNailClick;
            mainWindow.EditBtnClick += MainWindow_EditBtnClick;
            mainWindow.SaveBtnClick += MainWindow_SaveBtnClick;
            mainWindow.Name_HashSwitch += MainWindow_Name_HashSwitch;
            mainWindow.MainEditGrid.DragOver += EditGrid_DragOver;
            mainWindow.MainEditGrid.Drop += EditGrid_Drop;
        }

        private void CreateImages()
        {
            SwitchBtnBlack = Img("switch-horizontal-01.png");
            SwitchBtnWhite = Img("switch-horizontal-white.png");
            ResizeBtnImage = Img("image_picture.png");
            DragDropBlack = Img("drag-and-drop-black.png");
            DragDropWhite = Img("drag-and-drop-white.png");
            EditBtnBlack = Img("editing-black.png");
            EditBtnWhite = Img("editing-white.png");
            SaveBtnWhite = Img("save-file_white.png");
            SaveBtnBlack = Img("save-file_black.png");
        }

        private BitmapImage Img(string name)
        {
            Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream("Metadataviewer.Resources." + name);
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            result.StreamSource = res;
            result.EndInit();
            return result;
        }

        #region Drag&Drop

        #region ReadTab
        private void MainTab_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(File))) { e.Effects = DragDropEffects.Copy; }
            else { e.Effects = DragDropEffects.None; }
        }
        private void MainTab_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                dropfiles = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (dropfiles != null && dropfiles.Length > 0)
                {
                    IsDrop = true;
                    LoadThumbs(false, null, null);
                }
                else { dropfiles = null; }
            }
        }

        private bool IsDrop = false;

        private string[] dropfiles;

        #endregion

        #region EditTab
        private void EditGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(File))) { e.Effects = DragDropEffects.Copy; }
            else { e.Effects = DragDropEffects.None; }
        }

        private void EditGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                dropfiles = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (dropfiles != null && dropfiles.Length > 0)
                {
                    if (IsImage(dropfiles[0]))
                    {
                        IsDropItem = true;
                        GetMetaData(dropfiles[0]);
                        SetEditData();
                    }
                    else { dropfiles = null; }   
                }
                else { dropfiles = null; }  
            }
        }

        #endregion

        #endregion

        #region ArgsLoading

        readonly string _app = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        string[] args = Environment.GetCommandLineArgs();

        private void LoadArgs()
        {
            if (args.Length > 1)
            {
                string[] argsfiles = new string[0] {};
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] != _app && IsImage(args[i]))
                    {
                        //argsfiles.Append(args[i]);
                        LoadThumbs(true, args[i], null);
                    }
                }
                //if (argsfiles.Length > 1)
                //{
                //    LoadThumbs(false, null, argsfiles);                             ///Vorbereitung um mehrere argsfiles zu unterstützen            
                //}
                //else { LoadThumbs(true, argsfiles[0]); }
            }

        }
        #endregion

        #region Methods ColorTheme

        public void SetColorTheme(int userselect)
        {
            colorTheme = StaticMethods.GetColorTheme(userselect);

            if (colorTheme.BackGround.Color == Colors.White)
            {
                DragnDropImage = DragDropBlack;
                SwitchBtnImage = SwitchBtnBlack;
                EditBtnImage = EditBtnBlack;    
                SaveBtnImage = SaveBtnBlack;
            }
            else if (colorTheme.BackGround.Color == Colors.Black)
            {
                DragnDropImage = DragDropWhite;
                SwitchBtnImage = SwitchBtnWhite;
                EditBtnImage = EditBtnWhite;
                SaveBtnImage = SaveBtnWhite;
            }
   
            BackgroundColor = colorTheme.BackGround;
            ForegroundColor = colorTheme.ForeGround;
            DecorationsColor = colorTheme.Decorations;

            if (FileExplorer != null) { FileExplorer.SetColorTheme(colorTheme); }
            if (ImageCollection != null && ImageCollection.Count > 0)
            {
                foreach (ThumbNail thumb in ImageCollection)
                {
                    thumb._Brush = BackgroundColor;
                    thumb._Foreground = ForegroundColor;
                    thumb._DecorationsColor = DecorationsColor;
                }
            }
            if (LoraHashEdits != null &&  LoraHashEdits.Count > 0)
            {
                foreach (LoraHashEdit edit in LoraHashEdits)
                {
                    edit._BackgroundColor = BackgroundColor;
                    edit._ForegroundColor = ForegroundColor;
                    edit._DecorationsColor = DecorationsColor;
                }
            }
        }

        #endregion

        #region Properties ColorTheme

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

        #endregion

        #region MetaDataMethods

        private void MainWindow_Name_HashSwitch(object sender, EventArgs e)
        {
            string _switch = mainWindow._Name_HashSwitch;

            if (_switch != null && _switch != string.Empty)
            {
                if (_switch == "ModelTextButton")
                {
                    if (Checkpoint == CheckpointName) { Checkpoint = CheckpointHash; }
                    else if (Checkpoint == CheckpointHash) { Checkpoint = CheckpointName; }
                }
                else if (_switch == "LoraTextButton")
                {
                    if (Lora == LoraNames) { Lora = LoraHashes; }
                    else if (Lora == LoraHashes) { Lora = LoraNames; }
                }
                mainWindow._Name_HashSwitch = string.Empty;
            }
        }

        private void SetMetaDataView()
        {
            Prompt = metadata.Prompt;
            NegativePrompt = metadata.NegativePrompt;
            Checkpoint = metadata.CheckpointName;
            CheckpointName = metadata.CheckpointName;   
            CheckpointHash = metadata.CheckpointHash;
            Steps = metadata.Steps;
            CFG = metadata.CFG;
            Sampler = metadata.Sampler;
            Seed = metadata.Seed;
            Size = metadata.Size;
            Lora = metadata.LoraNames;
            LoraNames = metadata.LoraNames;
            LoraHashes = metadata.LoraHashes;
            Miscellaneous = metadata.Misc;
        }

        private void ResetMetaDataView()
        {
            Prompt = "";
            NegativePrompt = "";
            Checkpoint = "";
            CheckpointName = "";
            CheckpointHash = "";
            Steps = "";
            CFG = "";
            Sampler = "";
            Seed = "";
            Size = "";
            Lora =  "";
            LoraNames = "";
            LoraHashes = "";
            Miscellaneous = "";
        }

        #endregion

        #region ImageViewerUpdate

        private void ResetImageView()
        {
            ImageCollection.Clear();
        }

        private void FileExplorer_SetPathPropertyChanged(object sender, EventArgs e)
        {
            FolderPath = FileExplorer.OutGoingPath;
            if (IsImage(FolderPath))
            {
                ResetImageView();
                LoadThumbs(true, null, null);
                metadata.GetMetaData(FolderPath);
                if (metadata.GetMetaData(FolderPath) != null)
                {
                    /// show error meldung
                }
                else { SetMetaDataView(); }
            }
            else
            {
                ResetImageView();
                LoadThumbs(false, null, null);
            }
        }

        private void ImageCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ImageCollection != null && ImageCollection.Count == 0) { Vis = Visibility.Visible; }
            else { Vis = Visibility.Collapsed; }
        }

        private void MainWindow_ThumbSizeChange(object sender, EventArgs e)
        {
            string btn_name = mainWindow.ThumbSizeBtnName;
            if (btn_name != string.Empty) { SetImageSize(btn_name); }
        }

        private void SetImageSize(string btn_name)
        {
            if (btn_name == "LargeBtn")
            {
                CheckSmall = UnChecked;
                CheckMedium = UnChecked;
                CheckLarge = Checked;
                SelectedImageSize = LargeImage;

                foreach (ThumbNail thumb in ImageCollection)
                {
                    thumb.SelectedSize = SelectedImageSize;
                }
            }
            if (btn_name == "MediumBtn")
            {
                CheckSmall = UnChecked;
                CheckLarge = UnChecked;
                CheckMedium = Checked;
                SelectedImageSize = MediumImage;

                foreach (ThumbNail thumb in ImageCollection)
                {
                    thumb.SelectedSize = SelectedImageSize;
                }
            }
            if (btn_name == "SmallBtn")
            {
                CheckMedium = UnChecked;
                CheckLarge = UnChecked;
                CheckSmall = Checked;
                SelectedImageSize = SmallImage;

                foreach (ThumbNail thumb in ImageCollection)
                {
                    thumb.SelectedSize = SelectedImageSize;
                }
            }
        }

        private void LoadThumbs(bool singleimage, string argsfile, string[] argsfiles)
        {
            if (IsDrop && dropfiles != null && dropfiles.Length > 0)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ResetImageView();
                foreach (string img in dropfiles)
                {
                    if (IsImage(img))
                    {
                        ThumbNail thumb = Thumb(img);
                        ImageCollection.Add(thumb);
                    }
                }

                Mouse.OverrideCursor = Cursors.Arrow;

                if (dropfiles.Length == 1) { ShowSelectedThumbnailData(dropfiles[0]); }

                IsDrop = false;
                dropfiles = null;
                FolderPath = null;
            }
            else if (singleimage)
            {
                if (argsfile != null)
                {
                    ThumbNail thumb = Thumb(argsfile);
                    ImageCollection.Add(thumb);
                }
                else
                {
                    ThumbNail thumb = Thumb(FolderPath);
                    ImageCollection.Add(thumb);
                }
            }
            else
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string[] images = Directory.GetFiles(FolderPath);

                foreach (string img in images)
                {
                    if (IsImage(img))
                    {
                        ThumbNail thumb = Thumb(img);
                        ImageCollection.Add(thumb);
                    }
                }

                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private ThumbNail Thumb(string imgpath)
        {
            try
            {
                return new ThumbNail()
                {
                    Name = Path.GetFileName(imgpath),
                    Path = imgpath,
                    Image = new BitmapImage(new Uri(imgpath, UriKind.Absolute)),
                    SelectedSize = SelectedImageSize,
                    _Brush = BackgroundColor,
                    _Foreground = ForegroundColor,
                    _DecorationsColor = DecorationsColor                   
                };
            }
            catch { return null; }

        }

        private bool IsImage(string filepath)
        {
            if (File.Exists(filepath))
            {
                string file_extension = Path.GetExtension(filepath).ToLower();
                if (file_extension == ".png" || file_extension == ".jpg" || file_extension == ".jpeg" || file_extension == ".webp") { return true; }
            }
            return false;
        }

        private void ShowSelectedThumbnailData(string path)
        {
            foreach (ThumbNail thumb in ImageCollection)
            {
                if (thumb.Path == path)
                {
                    FolderPath = path;
                    if (thumb.IsSelected == true) { System.Diagnostics.Process.Start(path); }

                    thumb._Brush = System.Windows.Media.Brushes.LightSkyBlue;
                    thumb._Foreground = System.Windows.Media.Brushes.Black;
                    thumb.IsSelected = true;
                }
                else
                {
                    thumb._Brush = BackgroundColor;
                    thumb._Foreground = ForegroundColor;
                    thumb.IsSelected = false;
                }
            }
            if (metadata.GetMetaData(FolderPath) != null)
            {
                ///show error message

            }
            else { SetMetaDataView(); }
        }

        private void MainWindow_ThumbNailClick(object sender, EventArgs e)
        {
            ShowSelectedThumbnailData(mainWindow.ThumbNailPath);
        }

        #endregion

        #region Variables ImageViewer

        private readonly SolidColorBrush Checked = new SolidColorBrush(Colors.LightBlue);
        private readonly SolidColorBrush UnChecked = new SolidColorBrush(Colors.Transparent);
        private readonly System.Windows.Media.Brush Unselected = System.Windows.Media.Brushes.White;
        private readonly System.Windows.Media.Brush Selected = System.Windows.Media.Brushes.LightSkyBlue;

        private readonly int SmallImage = 100;
        private readonly int MediumImage = 256;
        private readonly int LargeImage = 512;

        private BitmapImage DragDropBlack;
        private BitmapImage DragDropWhite;

        #endregion

        #region Properties ImageViewer

        private ObservableCollection<ThumbNail> imagecollection;
        public ObservableCollection<ThumbNail> ImageCollection
        {
            get => imagecollection;
            set => SetProperty(ref imagecollection, value);
        }

        private string folderpath;
        public string FolderPath
        {
            get => folderpath;
            set => SetProperty(ref folderpath, value);
        }

        private SolidColorBrush checksmall = new SolidColorBrush(Colors.LightBlue);
        public SolidColorBrush CheckSmall
        {
            get => checksmall;
            set => SetProperty(ref checksmall, value);
        }

        private SolidColorBrush checkmedium = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush CheckMedium
        {
            get => checkmedium;
            set => SetProperty(ref checkmedium, value);
        }

        private SolidColorBrush checklarge = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush CheckLarge
        {
            get => checklarge;
            set => SetProperty(ref checklarge, value);
        }

        private BitmapImage resizebtnimg;
        public BitmapImage ResizeBtnImage
        {
            get => resizebtnimg;
            set => SetProperty(ref resizebtnimg, value);
        }

        private double imageviewerheight;
        public double ImageViewerHeight
        {
            get => imageviewerheight;
            set => SetProperty(ref imageviewerheight, value);
        }

        private int selectedimagesize;
        public int SelectedImageSize
        {
            get => selectedimagesize;
            set => SetProperty(ref selectedimagesize, value);
        }

        private BitmapImage dragndropimage;
        public BitmapImage DragnDropImage
        {
            get => dragndropimage;
            set => SetProperty(ref dragndropimage, value);
        }

        private Visibility vis = Visibility.Visible;
        public Visibility Vis
        {
            get => vis;
            set => SetProperty(ref vis, value);
        }

        #endregion

        #region Variables Metadataview

        private BitmapImage SwitchBtnWhite;
        private BitmapImage SwitchBtnBlack;
        private BitmapImage EditBtnWhite;
        private BitmapImage EditBtnBlack;
        private string CheckpointName = "";
        private string CheckpointHash = "";
        private string LoraNames = "";
        private string LoraHashes = "";

        #endregion

        #region Properties Metadataview

        private BitmapImage switchbtnimg;
        public BitmapImage SwitchBtnImage
        {
            get => switchbtnimg;
            set => SetProperty(ref switchbtnimg, value);
        }

        private BitmapImage editbtnimg;
        public BitmapImage EditBtnImage
        {
            get => editbtnimg;
            set => SetProperty(ref editbtnimg, value);
        }

        private string prompt = "";
        public string Prompt
        {
            get => prompt;
            set => SetProperty(ref prompt, value);
        }

        private string neagtiveprompt = "";
        public string NegativePrompt
        {
            get => neagtiveprompt;
            set => SetProperty(ref neagtiveprompt, value);
        }

        private string checkpoint = "";
        public string Checkpoint
        {
            get => checkpoint;
            set => SetProperty(ref checkpoint, value);
        }

        private string steps = "";
        public string Steps
        {
            get => steps;
            set => SetProperty(ref steps, value);
        }

        private string cfg = "";
        public string CFG
        {
            get => cfg;
            set => SetProperty(ref cfg, value);
        }

        private string sampler = "";
        public string Sampler
        {
            get => sampler;
            set => SetProperty(ref sampler, value);
        }

        private string seed = "";
        public string Seed
        {
            get => seed;
            set => SetProperty(ref seed, value);
        }

        private string size = "";
        public string Size
        {
            get => size;
            set => SetProperty(ref size, value);
        }

        private string lora = "";
        public string Lora
        {
            get => lora;
            set => SetProperty(ref lora, value);
        }

        private string miscellaneous = "";
        public string Miscellaneous
        {
            get => miscellaneous;
            set => SetProperty(ref miscellaneous, value);
        }

        #endregion

        #region MetadatEditMethods
        private void SetSaveTypList()
        {
            SaveTypList = new ObservableCollection<SaveTyps>()
            {
                new SaveTyps(){ SaveType = "Save as .edit.png", SaveTypeValue = "Edit" },
                new SaveTyps(){ SaveType =  "Save to another path", SaveTypeValue = "Path" },
                new SaveTyps(){ SaveType = "Overwrite original", SaveTypeValue = "Override" }

            };
            SaveTyp = SaveTypList[0];
        }
   
        private void MainWindow_EditBtnClick(object sender, EventArgs e)
        {
            if (Path.GetExtension(metadata.OriginalPath).ToLower() != ".png") 
            {
                MessageBox.Show("Currently only PNG files are supported for editing.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsDropItem = false;

            SetEditData();
        }

        private LoraHashEdit LoraEdit(string name, string hash)
        {
            return new LoraHashEdit()
            {
               Name = name,
               EditLoraHash = hash,
                _BackgroundColor = BackgroundColor,
                _ForegroundColor = ForegroundColor,
                _DecorationsColor = DecorationsColor
            };
        }

        private void ResetEditFields()
        {
            EditCheckpointName = "";
            EditCheckpointHash = "";
            EditCheckpoint_New_Hash = "";
            EditImage = null;
            if (LoraHashEdits != null) { LoraHashEdits = null; }
        }

        private void MainWindow_SaveBtnClick(object sender, EventArgs e)
        {

            List<string[]> hashlist = new List<string[]>();
            string error = null;

            if (IsDropItem)
            {
                error = metadata.GetMetaData(DropItemPath);
                if (error != null)
                {
                    ///show error message
                    return;
                }
                else
                {
                    if (Path.GetExtension(metadata.OriginalPath).ToLower() != ".png")
                    {
                        MessageBox.Show("Currently only PNG files are supported for editing.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
            }

            hashlist = CheckEditEntrys();
            string originalpath = metadata.OriginalPath;
            string newpath = "";

            if (hashlist != null && hashlist.Count > 0)
            {
                if (SaveTyp.SaveTypeValue == "Edit")
                {
                    string extension = Path.GetExtension(originalpath);
                    string newfilename = Path.GetFileNameWithoutExtension(originalpath) + ".edit" + Path.GetExtension(originalpath);
                    newpath = Path.Combine(Path.GetDirectoryName(originalpath), newfilename);
                }
                else if (SaveTyp.SaveTypeValue == "Override") { newpath = originalpath; }
                else
                {
                    SaveFileDialog saveFile = new SaveFileDialog()
                    {
                        Filter = "PNG files(*.png)|*.png",
                        InitialDirectory = Path.GetDirectoryName(originalpath),
                    };
                    if (saveFile.ShowDialog() == true)
                    {
                        if (Path.GetExtension(saveFile.FileName).ToLower() != ".png") { newpath = saveFile.FileName + ".png"; }
                        else { newpath = saveFile.FileName; }
                    }
                }

                string[] oldhashes = new string[hashlist.Count];
                string[] newhashes = new string[hashlist.Count];

                for (int i = 0; i < hashlist.Count; i++)
                {
                    oldhashes[i] = hashlist[i][0];
                    newhashes[i] = hashlist[i][1];
                }

                error = metadata.ReplaceHashes(originalpath, newpath, oldhashes, newhashes);
                if (error != null)
                {
                    MessageBox.Show(error);
                    return;
                }
            }
            else 
            {
                MessageBox.Show("An unknown error has occurred!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            error = GetMetaData(newpath);
            if (error != null)
            {
                MessageBox.Show(error);
                return;
            }
            else
            {
                SetEditData();
                if (IsDropItem)
                {
                    EditSaveSuccess = Visibility.Visible;
                    SuccessTimer.Start();
                    return;
                }
                else
                { 
                    UpdateImageViewer_and_MetadataView(newpath, originalpath);
                    EditSaveSuccess = Visibility.Visible;
                    SuccessTimer.Start();
                }
            }
        }

        private List<string[]> CheckEditEntrys()
        {
            int fails = 0;
            int changes = 0;
            List<string[]> hashes = new List<string[]>();

            if (EditCheckpointFalse == Visibility.Visible) { fails++; }
            else if (EditCheckpointTrue == Visibility.Visible)
            { 
                changes++;
                hashes.Add(new string[] { EditCheckpointHash, EditCheckpoint_New_Hash });
            }

            if (LoraHashEdits != null && LoraHashEdits.Count > 0)
            {
                foreach (LoraHashEdit edit in LoraHashEdits)
                {
                    if (edit._EditLoraFalse == Visibility.Visible) { fails++; }
                    else if (edit._EditLoraTrue == Visibility.Visible) 
                    { 
                        changes++;
                        hashes.Add(new string[] { edit.EditLoraHash, edit.EditLora_New_Hash});
                    }
                }
            }

            if (fails > 0)
            {
                MessageBox.Show("An AUTOV2 hash must consist of 10 characters." + Environment.NewLine + "Please check your input.", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            if (fails == 0)
            {
                if (changes == 0)
                {
                    MessageBox.Show("There are no changes!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }
                else { return hashes; }
            }
            return null;
        }

        private string GetMetaData(string path)
        {
            string error = metadata.GetMetaData(path);
            if (error != null) { return error; }
            else { return null; }
        }

        private void SetEditData()
        {
            ResetEditFields();

            EditCheckpointName = metadata.CheckpointName;
            EditCheckpointHash = metadata.CheckpointHash;
            Editfilepath = metadata.OriginalPath;

            EditImage = new BitmapImage(new Uri(metadata.OriginalPath, UriKind.Absolute));

            if (metadata.Loras != null && metadata.Loras.Count > 0)
            {
                LoraHashEdits = new ObservableCollection<LoraHashEdit>();
                foreach (KeyValuePair<string, string> lora in metadata.Loras)
                {
                    LoraHashEdits.Add(LoraEdit(lora.Key, lora.Value));
                }
            }
        }

        private void UpdateImageViewer_and_MetadataView(string newpath, string originalpath)
        {
            if (SaveTyp.SaveTypeValue == "Override")
            {
                if (ImageCollection.Count == 1) { FolderPath = newpath; }
                else { FolderPath = Path.GetDirectoryName(newpath); }
                ResetImageView();
                LoadThumbs(false, null, null);
                ShowSelectedThumbnailData(newpath);
            }
            if (SaveTyp.SaveTypeValue == "Edit")
            {
                FileExplorer.SetPath(Path.GetDirectoryName(newpath));
                ResetImageView();
                FolderPath = Path.GetDirectoryName(newpath);
                LoadThumbs(false, null, null);
                ShowSelectedThumbnailData(newpath);
            }
            if (SaveTyp.SaveTypeValue == "Path")
            {
                if (newpath == originalpath) { return; }
                if (Path.GetDirectoryName(newpath) != Path.GetDirectoryName(originalpath)) { return; }
                else
                {
                    FileExplorer.SetPath(Path.GetDirectoryName(newpath));
                    FolderPath = Path.GetDirectoryName(newpath);
                    ResetImageView();
                    LoadThumbs(false, null, null);
                    ShowSelectedThumbnailData(newpath);
                }
            }
        }

        private void SuccessTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            EditSaveSuccess = Visibility.Hidden;
        }

        private void SetEditVisibility()
        {
            if (EditCheckpoint_New_Hash.Length == 0)
            {
                EditCheckpointFalse = Visibility.Hidden;
                EditCheckpointTrue = Visibility.Hidden;
            }
            if (EditCheckpoint_New_Hash.Length > 0)
            {
                if (EditCheckpoint_New_Hash.Length == 10)
                {
                    EditCheckpointFalse = Visibility.Hidden;
                    EditCheckpointTrue = Visibility.Visible;
                }
                else
                {
                    EditCheckpointFalse = Visibility.Visible;
                    EditCheckpointTrue = Visibility.Hidden;
                }
            }
        }

        #endregion

        #region Variables MetadataEdit

        private BitmapImage SaveBtnBlack;
        private BitmapImage SaveBtnWhite;

        private bool IsDropItem = false;
        private string DropItemPath = null;

        private Timer SuccessTimer = new Timer() { Interval = 3000 };

        #endregion

        #region Properties MetadataEdit

        private ObservableCollection<LoraHashEdit> lorahashedits;
        public ObservableCollection<LoraHashEdit> LoraHashEdits
        {
            get => lorahashedits;
            set => SetProperty(ref lorahashedits, value);
        }

        private ObservableCollection<SaveTyps> savetyplist;
        public ObservableCollection<SaveTyps> SaveTypList
        {
            get => savetyplist;
            set=> SetProperty(ref savetyplist, value);  
        }

        private SaveTyps savetyp;
        public SaveTyps SaveTyp
        {
            get => savetyp;
            set => SetProperty(ref savetyp, value);
        }

        private BitmapImage editimg;
        public BitmapImage EditImage
        {
            get => editimg;
            set => SetProperty(ref editimg, value);
        }

        private BitmapImage savebtnimg;
        public BitmapImage SaveBtnImage
        {
            get => savebtnimg;
            set => SetProperty(ref savebtnimg, value);
        }

        private string editfilepath = "";
        public string Editfilepath
        {
            get=> editfilepath; 
            set => SetProperty(ref editfilepath, value);
        }

        private string editcheckpointname = "";
        public string EditCheckpointName
        {
            get => editcheckpointname;
            set => SetProperty(ref editcheckpointname, value);
        }

        private string editcheckpointhash = "";
        public string EditCheckpointHash
        {
            get => editcheckpointhash;
            set => SetProperty(ref editcheckpointhash, value);
        }

        private string editcheckpoint_new_hash = "";
        public string EditCheckpoint_New_Hash
        {
            get => editcheckpoint_new_hash;
            set
            {
                SetProperty(ref editcheckpoint_new_hash, value);
                SetEditVisibility();
            }
    }

        private Visibility editcheckpointfalse = Visibility.Hidden;
        public Visibility EditCheckpointFalse
        {
            get=> editcheckpointfalse;
            set => SetProperty(ref editcheckpointfalse, value);    
        }

        private Visibility editcheckpointtrue = Visibility.Hidden;
        public Visibility EditCheckpointTrue
        {
            get => editcheckpointtrue;
            set=> SetProperty(ref editcheckpointtrue, value);
        }

        private Visibility editsavesuccess = Visibility.Hidden;
        public Visibility EditSaveSuccess
        {
            get => editsavesuccess;
            set=>SetProperty(ref editsavesuccess, value);
        }

        #endregion
    }
}
