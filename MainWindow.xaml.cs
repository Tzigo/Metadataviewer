using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string ThumbSizeBtnName = string.Empty;
        public string ThumbNailPath = string.Empty; 
        public string _Name_HashSwitch = string.Empty;  

        public MainWindow()
        {
            InitializeComponent();
            Resources.Add(SystemParameters.VerticalScrollBarWidthKey, 10d);
            Resources.Add(SystemParameters.HorizontalScrollBarHeightKey, 10d);
        }


        protected virtual void OnThumbSizeChange()
        {
            ThumbSizeChange?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ThumbSizeChange;

        private void ThumbSizeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            ThumbSizeBtnName = btn.Name;
            OnThumbSizeChange();
        }

        protected virtual void OnThumbNailClick()
        {
            ThumbNailClick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ThumbNailClick;

        private void ThumbNail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ThumbNailPath = element.Tag.ToString();
            OnThumbNailClick();
        }

        #region Resize

        private double resize;
        private double leftsize;
        private double rightsize;
        private double centersize;
        private string hash = "Click to view hash";
        private string name = "Click to view name";
        private bool Resize = false;
        private Point StartResize;
        private string MetaDataResizeTBox = string.Empty;
        private double previewheight;

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Width == 0) { return; }

            if (e.PreviousSize.Width != e.NewSize.Width)
            {
                resize = e.NewSize.Width - e.PreviousSize.Width;

                MainTab.Width = MainTab.ActualWidth + resize;

                bool left = false;
                if (LeftGrid.ActualWidth < LeftGrid.MaxWidth)
                {
                    leftsize = LeftGrid.ActualWidth + resize * (LeftGrid.ActualWidth / e.PreviousSize.Width);
                    left = true;
                }
                else { leftsize = LeftGrid.ActualWidth; }
                bool right = false;
                if (RightGrid.ActualWidth < RightGrid.MaxWidth)
                {
                    rightsize = RightGrid.ActualWidth + resize * (RightGrid.ActualWidth / e.PreviousSize.Width);
                    right = true;
                }
                else {  rightsize = RightGrid.ActualWidth; }

                double newsize;
                if (left == false) { newsize = e.NewSize.Width - LeftGrid.ActualWidth; }
                else { newsize = e.NewSize.Width - leftsize; }

                if (right == false) { newsize = newsize - RightGrid.ActualWidth; }
                else { newsize = newsize - rightsize; }


                centersize = newsize - 14;


                LeftGrid.Width = leftsize;
                ((UserControl)LeftGrid.Children[0]).Width = leftsize;

                CenterGrid.Width = centersize;

                //stack.Width = centersize;
                foreach (Grid grid in CenterGrid.Children)
                {
                    grid.Width = centersize;
                }


                RightGrid.Width = rightsize;
                RightGridScrollViewer.Width = rightsize;
                //pnginfo.Width = rightsize - 10;


                seedlabel.Content = LeftGrid.ActualWidth;
            }
        }
        private void Tbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Tbox.Width = TopGrid.ActualWidth - 90;
        }

        private void leftsplitter_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }

        private void leftsplitter_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void LeftThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double newleftwith = Math.Max(LeftGrid.ActualWidth + e.HorizontalChange, 0);
            LeftGrid.Width = newleftwith;  
            CenterGrid.Width = MainTab.ActualWidth - LeftGrid.ActualWidth - RightGrid.ActualWidth - 14;
            ((UserControl)LeftGrid.Children[0]).Width = newleftwith;
        }
    
        private void RightThumb_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }

        private void RightThumb_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void RightThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (MainTab.ActualWidth + e.HorizontalChange >= LeftGrid.ActualWidth + CenterGrid.ActualWidth + RightGrid.Width + 14 && RightGrid.ActualWidth >= RightGrid.MaxWidth) { return; }   
            if (e.HorizontalChange > 0 && RightGrid.ActualWidth == RightGrid.MinWidth) { return; }
            double newrightwith = Math.Max(RightGrid.ActualWidth - e.HorizontalChange, 0);
            RightGrid.Width = newrightwith;
            CenterGrid.Width = MainTab.ActualWidth - LeftGrid.ActualWidth - RightGrid.ActualWidth - 14;
            RightGridScrollViewer.Width = newrightwith;
            pnginfo.Width = newrightwith - 10;
        }

        #endregion

        #region MetadataTextBoxes Resize
        private void Prompttxbresizerbtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MetaDataResizeTBox = "Prompttxbresizerbtn";
            StartResize = Mouse.GetPosition(this);
            previewheight = PromptTboxGrid.ActualHeight;
            Resize = true;
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void Prompttxbresizerbtn_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
            foreach (Line line in Prompttxbresizerbtn.Children)
            {
                line.Stroke = new SolidColorBrush(Colors.DodgerBlue);
            }
        }

        private void Prompttxbresizerbtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Resize) { return; }
            Mouse.OverrideCursor = Cursors.Arrow;
            foreach (Line line in Prompttxbresizerbtn.Children)
            {
                line.Stroke = new SolidColorBrush(Colors.Gray);
            }
        }

        private void NegativePromptResizeBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MetaDataResizeTBox = "NegativePromptResizeBtn";
            StartResize = Mouse.GetPosition(this);
            previewheight = NegativePromptTboxGrid.ActualHeight;
            Resize = true;
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void NegativePromptResizeBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
            foreach (Line line in NegativePromptResizeBtn.Children)
            {
                line.Stroke = new SolidColorBrush(Colors.DodgerBlue);
            }
        }

        private void NegativePromptResizeBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Resize) { return; }
            Mouse.OverrideCursor = Cursors.Arrow;
            foreach (Line line in NegativePromptResizeBtn.Children)
            {
                line.Stroke = new SolidColorBrush(Colors.Gray);
            }
        }

        private void MiscResizeBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MetaDataResizeTBox = "MiscResizeBtn";
            StartResize = Mouse.GetPosition(this);
            previewheight = MiscellaneousTboxGrid.ActualHeight;
            Resize = true;
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void MiscResizeBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
            foreach (Line line in MiscResizeBtn.Children)
            {
                line.Stroke = new SolidColorBrush(Colors.DodgerBlue);
            }
        }

        private void MiscResizeBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Resize) { return; }
            Mouse.OverrideCursor = Cursors.Arrow;
            foreach (Line line in MiscResizeBtn.Children)
            {
                line.Stroke = new SolidColorBrush(Colors.Gray);
            }
        }

        private void pnginfo_MouseMove(object sender, MouseEventArgs e)
        {
            if (Resize && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point move = Mouse.GetPosition(this);
                double resize = move.Y - StartResize.Y;
                if (MetaDataResizeTBox == "Prompttxbresizerbtn")
                {
                    if ((PromptTboxGrid.ActualHeight + resize) > PromptTboxGrid.MinHeight)
                    {
                        Grid grid = (Grid)Prompttxbresizerbtn.Parent;
                        grid.Height = previewheight + resize;

                    }
                    else { EndResize(); }
                }
                else if (MetaDataResizeTBox == "NegativePromptResizeBtn")
                {
                    if ((NegativePromptTboxGrid.ActualHeight + resize) > NegativePromptTboxGrid.MinHeight)
                    {
                        Grid grid = (Grid)NegativePromptResizeBtn.Parent;
                        grid.Height = previewheight + resize;
                    }
                    else { EndResize(); }

                }
                else if (MetaDataResizeTBox == "MiscResizeBtn")
                {
                    if ((MiscellaneousTboxGrid.ActualHeight + resize) > MiscellaneousTboxGrid.MinHeight)
                    {
                        Grid grid = (Grid)MiscResizeBtn.Parent;
                        grid.Height = previewheight + resize;
                        RightGridScrollViewer.ScrollToBottom();
                    }
                    else { EndResize(); }
                }
            }
            else if (MetaDataResizeTBox != string.Empty) { EndResize(); }
        }

        private void EndResize()
        {
            Resize = false;
            previewheight = 0;
            Mouse.OverrideCursor = Cursors.Arrow;
            if (MetaDataResizeTBox == "Prompttxbresizerbtn")
            {
                foreach (Line line in Prompttxbresizerbtn.Children)
                {
                    line.Stroke = new SolidColorBrush(Colors.Gray);
                }
                MetaDataResizeTBox = string.Empty;
            }
            else if (MetaDataResizeTBox == "NegativePromptResizeBtn")
            {
                foreach (Line line in NegativePromptResizeBtn.Children)
                {
                    line.Stroke = new SolidColorBrush(Colors.Gray);
                }
                MetaDataResizeTBox = string.Empty;
            }
            else if (MetaDataResizeTBox == "MiscResizeBtn")
            {
                foreach (Line line in MiscResizeBtn.Children)
                {
                    line.Stroke = new SolidColorBrush(Colors.Gray);
                }
                MetaDataResizeTBox = string.Empty;
            }

        }

        #endregion

        protected virtual void OnName_HashSwitch()
        {
            Name_HashSwitch?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Name_HashSwitch;
 
        private void Model_LoraBtnClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string btn_name = btn.Name;
            if (btn_name == "ModelTextButton")
            {
                if (ModelLabel.Content.ToString() == hash) { ModelLabel.Content = name; }
                else { ModelLabel.Content = hash; }
            }
            else if (btn_name == "LoraTextButton")
            {
                if (LoraLabel.Content.ToString() == hash) { LoraLabel.Content = name; }
                else { LoraLabel.Content = hash; };
            }

            _Name_HashSwitch = btn_name;
            OnName_HashSwitch();
        }

        protected virtual void OnEditBtnClick()
        {
            EditBtnClick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler EditBtnClick;

        private void SendToEditBtn_Click(object sender, RoutedEventArgs e)
        {
            OnEditBtnClick();
            EditTab.IsSelected = true;  
        }


        protected virtual void OnSaveBtnClick()
        {
            SaveBtnClick?.Invoke(this,EventArgs.Empty);
        }

        public event EventHandler SaveBtnClick;

        private void SaveBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnSaveBtnClick();
        }
    }
}
