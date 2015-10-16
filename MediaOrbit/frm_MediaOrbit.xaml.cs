using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaOrbit
{
    /// <summary>
    /// Interaction logic for frm_MediaOrbit.xaml
    /// </summary>
    public partial class win_MediaOrbit : Window
    {
        ObservableCollection<DirectoryEntry> entries = new ObservableCollection<DirectoryEntry>();
        ObservableCollection<DirectoryEntry> subEntries = new ObservableCollection<DirectoryEntry>();

        private bool userIsDraggingSlider = false;
        private bool ShowingDrives = true; // true if the folder browser is displaying drives; false if it is displaying files & folders

        string Prompt;

        #region Constructors

        public win_MediaOrbit()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(frm_MediaOrbit_Loaded);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        #endregion Constructors


        #region Event Procedures

        void frm_MediaOrbit_Loaded(object sender, RoutedEventArgs e)
        {
            text_CurrentPath.Text = Properties.Settings.Default["DefaultPath"].ToString();
        }

        private void btn_RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            // DEBUG //
            Prompt = "#201:\nThis feature is under development.";
            MessageBox.Show(Prompt, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btn_RotateRight_Click(object sender, RoutedEventArgs e)
        {
            // DEBUG //
            Prompt = "#301:\nThis feature is under development.";
            MessageBox.Show(Prompt, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Information);

            System.Drawing.Image img_02;
            EncoderParameters oEncoderParameters_01;
            ImageCodecInfo oImageCodecInfo_01;

            oEncoderParameters_01 = new EncoderParameters(1);
            oImageCodecInfo_01 = GetEncoderInfo("image/jpeg");

            img_02 = System.Drawing.Image.FromFile(text_PathedFileName_Displayed.Text);

            // Rotate the image.
            img_02.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);

            // WORKING //
            // Unload the picture:
            //img_01.Load(@"C:\Users\Mike\Pictures\Default.jpeg");
            //img_01.Refresh();

            // WORKING //
            // Save the image to the file:
            //oEncoderParameters_01.Param[0] = new EncoderParameter(Encoder.Quality, 75);
            //img_02.Save(text_PathedFileName_Displayed.Text, oImageCodecInfo_01, oEncoderParameters_01);
        }

        private void btn_Rename_Click(object sender, RoutedEventArgs e)
        {
            Prompt = "This will rename the files to ''001'', ''002'', ''003'', etc.";
            MessageBoxResult Result = MessageBox.Show(Prompt, "Confirmation", MessageBoxButton.OKCancel);
            if (Result == MessageBoxResult.OK) RenameFiles();
        }

        private void btn_GatherSubFolderFiles_Click(object sender, RoutedEventArgs e)
        {
            // ToDo //
        }

        private void btn_Backup_Click(object sender, RoutedEventArgs e)
        {
            // ToDo //
        }

        private void list_FolderBrowser_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (list_FolderBrowser.SelectedIndex >= 0)
            {
                SaveInfoFile();

                DirectoryEntry entry = (DirectoryEntry)list_FolderBrowser.Items[list_FolderBrowser.SelectedIndex];

                text_CurrentPath.Text = entry.Fullpath;

                if (entry.Type == EntryType.Dir)
                {
                    subEntries.Clear();

                    try
                    {
                        foreach (string s in Directory.GetDirectories(entry.Fullpath))
                        {
                            DirectoryInfo dir = new DirectoryInfo(s);
                            DirectoryEntry d = new DirectoryEntry(
                                dir.Name, dir.FullName, "<Folder>", "",
                                Directory.GetLastWriteTime(s),
                                "i/icon16-file.png", EntryType.Dir
                                );
                            subEntries.Add(d);
                        }
                        foreach (string f in Directory.GetFiles(entry.Fullpath))
                        {
                            FileInfo file = new FileInfo(f);
                            DirectoryEntry d = new DirectoryEntry(
                                file.Name, file.FullName, file.Extension, file.Length.ToString(),
                                file.LastWriteTime,
                                "i/icon16-file.png", EntryType.File
                                );
                            subEntries.Add(d);
                        }
                    }
                    catch (Exception ex)
                    {
                        Prompt = ex.Message;
                        System.Windows.MessageBox.Show(Prompt, "Problem", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        list_FolderBrowser.DataContext = subEntries;
                    }
                }
            }
        }

        private void list_FolderBrowser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Put the proper text into the "FileName" text block:
            if (list_FolderBrowser.SelectedIndex < 0)
            {
                text_PathedFileName_Displayed.Text = "";
            }
            else
            {
                DirectoryEntry entry = (DirectoryEntry)list_FolderBrowser.Items[list_FolderBrowser.SelectedIndex];
                if (entry.Type == EntryType.Dir) text_PathedFileName_Displayed.Text = "";
                else text_PathedFileName_Displayed.Text = NormalizePath(text_CurrentPath.Text) + entry.Name;
            }
        }

        private void list_FolderBrowser_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString() == "Delete")
            {
                DirectoryEntry entry = (DirectoryEntry)list_FolderBrowser.Items[list_FolderBrowser.SelectedIndex];

                Prompt = "Delete the file " + entry.Fullpath + "?";
                if (MessageBox.Show(Prompt, "Confirmation", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    // Update the image:
                    BitmapImage oBmpImg_01 = new BitmapImage();
                    oBmpImg_01.BeginInit();
                    oBmpImg_01.UriSource = new Uri(@"C:\Users\michael.adams4\Pictures\placeholder.png", UriKind.Relative);
                    oBmpImg_01.CacheOption = BitmapCacheOption.OnLoad;
                    oBmpImg_01.EndInit();
                    img_01.Source = oBmpImg_01;

                    string PathedFileName_New = NormalizePath(text_CurrentPath.Text) + "ZZZ_" + entry.Name;

                    int NumberOfRetries = 100;
                    for (int i = 1; i <= NumberOfRetries; ++i)
                    {
                        try
                        {
                            System.IO.File.Move(entry.Fullpath, PathedFileName_New);
                            break; // When done we can break loop
                        }
                        catch (Exception e2)
                        {
                            // You may check error code to filter some exceptions, not every error
                            // can be recovered.
                            if (i == NumberOfRetries) // Last one, (re)throw exception and exit
                                throw;

                            System.Threading.Thread.Sleep(100);
                        }
                    }

                    // Refresh the drive content listing.
                    ShowDriveContents();

                    Prompt = "Done " + NumberOfRetries;
                    System.Windows.MessageBox.Show(Prompt, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btn_UpOneLevel_Click(object sender, RoutedEventArgs e)
        {
            SaveInfoFile();

            if (text_CurrentPath.Text.Length == 3 && text_CurrentPath.Text.EndsWith(":\\"))
            {
                text_CurrentPath.Text = "";
            }
            else
            {
                text_CurrentPath.Text = GetParentPath(text_CurrentPath.Text);
            }
        }

        private void text_CurrentPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Close the "Info" file.
            text_PathedFileName_Info.Text = "";
            text_Info.Text = "";

            if (text_CurrentPath.Text.Length == 0)
            {
                ShowDrives();
            }
            else
            {
                ShowDriveContents();
            }

            // Read and display the "info.txt" file if possible.
            DisplayInfo();
        }

        private void text_PathedFileName_Displayed_TextChanged(object sender, TextChangedEventArgs e)
        {
            oMediaElement_01.Stop();

            if (text_PathedFileName_Displayed.Text.ToLower().EndsWith(".jpg")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".jpeg")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".png")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".gif")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".bmp")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".ico")
                )
            {
                DisplayStillPic(text_PathedFileName_Displayed.Text);
            }
            else if (text_PathedFileName_Displayed.Text.ToLower().EndsWith(".avi")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".mpeg")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".mp3")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".mp4")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".mov")
                || text_PathedFileName_Displayed.Text.ToLower().EndsWith(".wmv")
                )
            {
                // Display only the appropriate controls:
                img_01.Visibility = Visibility.Collapsed;
                oMediaElement_01.Visibility = Visibility.Visible;
                slide_Progress.Visibility = Visibility.Visible;
                pnl_MediaCtrls.Visibility = Visibility.Visible;

                // Update the video:
                oMediaElement_01.Source = new Uri(text_PathedFileName_Displayed.Text);
            }
            else
            // If the selected file is neither a picture nor a video ...
            {
                // Display only the appropriate controls:
                img_01.Visibility = Visibility.Collapsed;
                oMediaElement_01.Visibility = Visibility.Collapsed;
                slide_Progress.Visibility = Visibility.Collapsed;
                pnl_MediaCtrls.Visibility = Visibility.Collapsed;
            }
        }

        private void slide_Progress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void slide_Progress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            oMediaElement_01.Position = TimeSpan.FromSeconds(slide_Progress.Value);
        }

        private void btn_Play_Click(object sender, RoutedEventArgs e)
        {
            oMediaElement_01.Play();
        }

        private void btn_Pause_Click(object sender, RoutedEventArgs e)
        {
            oMediaElement_01.Pause();
        }

        private void btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            oMediaElement_01.Stop();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if ((oMediaElement_01.Source != null) && (oMediaElement_01.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                slide_Progress.Minimum = 0;
                slide_Progress.Maximum = oMediaElement_01.NaturalDuration.TimeSpan.TotalSeconds;
                slide_Progress.Value = oMediaElement_01.Position.TotalSeconds;
            }
        }

        private void btn_AddRow_Click(object sender, RoutedEventArgs e)
        {
            int i_Pos = pnl_Info.Children.Count - 2;

            var oDockPnl_New = new DockPanel();
            oDockPnl_New.LastChildFill = true;

            var myLbl = new TextBox();
            myLbl.Text = (i_Pos - 2).ToString();
            DockPanel.SetDock(myLbl, Dock.Left);
            oDockPnl_New.Children.Add(myLbl);

            var myBtn = new Button();
            myBtn.Name = "btn_Go_" + myLbl.Text;
            myBtn.Content = "Go";
            DockPanel.SetDock(myBtn, Dock.Right);
            myBtn.Click += btn_Go_Click;
            oDockPnl_New.Children.Add(myBtn);

            var myTextBox = new TextBox();
            myTextBox.Width = 60;
            myTextBox.Text = "1";
            myTextBox.PreviewTextInput += textbox_Numeric_PreviewTextInput;
            oDockPnl_New.Children.Add(myTextBox);
            DockPanel.SetDock(myTextBox, Dock.Right);

            myTextBox = new TextBox();
            oDockPnl_New.Children.Add(myTextBox);

            DockPanel.SetDock(oDockPnl_New, Dock.Top);

            pnl_Info.Children.Insert(i_Pos - 1, oDockPnl_New);
        }

        private void btn_Go_Click(object sender, RoutedEventArgs e)
        {
            Button btn_Source = e.Source as Button;
            int i_RowNum = Convert.ToInt32(btn_Source.Name.Substring(7));
            DockPanel pnl_Selected = pnl_Info.Children[i_RowNum + 1] as DockPanel;
            TextBox textbox_TextToCopy_Selected = pnl_Selected.Children[3] as TextBox;
            TextBox textbox_RowCount_Selected = pnl_Selected.Children[2] as TextBox;
            string str_Body = textbox_TextToCopy_Selected.Text;
            int i_RowCount = Convert.ToInt32(textbox_RowCount_Selected.Text);

            // DEBUG //
            //Prompt = "Go #" + i_RowNum + "\n" + i_RowCount + "x: ''" + str_Body + "''";
            // MessageBox.Show(Prompt, "WPF Demo", MessageBoxButton.OK, MessageBoxImage.None);

            string str_FileTitle_Photo;

            int i_PhotoNum;
            int i_CopyLineCount;

            string str_Line;
            int i_LineNum;
            string str_NewText = "";
            int i_LastChangedLineNum = 0;

            i_CopyLineCount = Convert.ToInt32(textbox_RowCount_Selected.Text);
            if (i_CopyLineCount < 1) i_CopyLineCount = 1;

            for (i_PhotoNum = 1; i_PhotoNum <= i_CopyLineCount; i_PhotoNum++)
            {
                str_NewText = "";

                str_FileTitle_Photo = RemoveFileExtension(GetFileName(text_PathedFileName_Displayed.Text));

                StringCollection coll_Lines = new StringCollection();
                int lineCount = text_Info.LineCount;

                // Get the collection of text lines:
                for (int line = 0; line < lineCount; line++)
                {
                    coll_Lines.Add(text_Info.GetLineText(line).TrimEnd('\r', '\n'));
                }

                for (i_LineNum = 0; i_LineNum < coll_Lines.Count - 1; i_LineNum++)
                {
                    str_Line = coll_Lines[i_LineNum];

                    if (str_Line.StartsWith(str_FileTitle_Photo + " "))
                    {
                        str_NewText += str_Line + textbox_TextToCopy_Selected.Text + "\n";
                        i_LastChangedLineNum = i_LineNum;
                    }
                    else
                    {
                        str_NewText += str_Line + "\n";
                    }
                } // Next i_LineNum

                text_Info.Text = str_NewText;

                // Move to the next photo if possible:
                if ((list_FolderBrowser.SelectedIndex < list_FolderBrowser.Items.Count - 1))
                {
                    list_FolderBrowser.SelectedIndex = list_FolderBrowser.SelectedIndex + 1;
                }
            } // Next i_PhotoNum

            if ((i_LastChangedLineNum > 0))
            {
                //GoToLine(i_LastChangedLineNum + 2);
            }

            textbox_RowCount_Selected.Text = "1";

            // TEST //
            //list_FolderBrowser.Focus();
            Keyboard.Focus(btn_UpOneLevel);
            Keyboard.Focus(list_FolderBrowser);
            //Keyboard.Focus(text_Info);
            //list_FolderBrowser.SelectedIndex = 0;
            //MessageBox.Show(Keyboard.FocusedElement.ToString());
        }
        // End of btn_Go_Click

        private void textbox_Numeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void btn_CreateInfoFile_Click(object sender, RoutedEventArgs e)
        {
            CreateNewInfoFile();
        }

        private void frm_MediaOrbit_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveInfoFile();
        }

        #endregion Event Procedures


        #region Private Methods

        private void ShowDrives()
        {
            foreach (string s in Directory.GetLogicalDrives())
            {
                DirectoryEntry d = new DirectoryEntry(s, s, "<Driver>", "<DIR>", Directory.GetLastWriteTime(s), "Images/dir.gif", EntryType.Dir);
                entries.Add(d);
            }
            this.list_FolderBrowser.DataContext = entries;
            ShowingDrives = true;
            btn_UpOneLevel.IsEnabled = false;
        }

        private void ShowDriveContents()
        {
            ShowingDrives = false;

            if (list_FolderBrowser != null) // ( This is needed because InitializeComponent calls this procedure. )
            {
                subEntries.Clear();

                try
                {
                    if (Directory.Exists(text_CurrentPath.Text))
                    {
                        foreach (string s in Directory.GetDirectories(text_CurrentPath.Text))
                        {
                            DirectoryInfo dir = new DirectoryInfo(s);
                            DirectoryEntry d = new DirectoryEntry(
                                dir.Name, dir.FullName, "<Folder>", "",
                                Directory.GetLastWriteTime(s),
                                "i/icon16-folder.png", EntryType.Dir
                                );
                            subEntries.Add(d);
                        }
                        foreach (string f in Directory.GetFiles(text_CurrentPath.Text))
                        {
                            FileInfo file = new FileInfo(f);
                            DirectoryEntry d = new DirectoryEntry(
                                file.Name, file.FullName, file.Extension, file.Length.ToString(),
                                file.LastWriteTime,
                                "i/icon16-file.png", EntryType.File
                                );
                            subEntries.Add(d);
                        }

                        list_FolderBrowser.DataContext = subEntries;
                    }
                }
                catch (Exception ex)
                {
                    Prompt = ex.Message;
                    System.Windows.MessageBox.Show(Prompt, "Problem", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                btn_UpOneLevel.IsEnabled = true;
            }
        }

        /// <summary>
        /// Read and display the "info.txt" file if possible.
        /// </summary>
        private void DisplayInfo()
        {
            string str_PathedFileName_Info;

            str_PathedFileName_Info = NormalizePath(text_CurrentPath.Text) + "info.txt";

            if (File.Exists(str_PathedFileName_Info))
            {
                text_Info.IsEnabled = true;
                text_Info.Text = File.ReadAllText(str_PathedFileName_Info);
                text_PathedFileName_Info.Text = str_PathedFileName_Info;

                btn_CreateInfoFile.IsEnabled = false;

                // Enable the "CopyText" controls:
                /*
                lbl_textbox_TextToCopy_01.Enabled = True;
                textbox_TextToCopy_01.Enabled = True;
                lbl_textbox_CopyLineCount_01.Enabled = True;
                textbox_CopyLineCount_01.Enabled = True;
                cmdbtn_Copy_01.Enabled = True;
                lbl_textbox_TextToCopy_02.Enabled = True
                textbox_TextToCopy_02.Enabled = True
                lbl_textbox_CopyLineCount_02.Enabled = True
                textbox_CopyLineCount_02.Enabled = True
                cmdbtn_Copy_02.Enabled = True
                lbl_textbox_TextToCopy_03.Enabled = True
                textbox_TextToCopy_03.Enabled = True
                lbl_textbox_CopyLineCount_03.Enabled = True
                textbox_CopyLineCount_03.Enabled = True
                cmdbtn_Copy_03.Enabled = True
                lbl_textbox_TextToCopy_04.Enabled = True
                textbox_TextToCopy_04.Enabled = True
                lbl_textbox_CopyLineCount_04.Enabled = True
                textbox_CopyLineCount_04.Enabled = True
                cmdbtn_Copy_04.Enabled = True
                lbl_textbox_TextToCopy_05.Enabled = True
                textbox_TextToCopy_05.Enabled = True
                lbl_textbox_CopyLineCount_05.Enabled = True
                textbox_CopyLineCount_05.Enabled = True
                cmdbtn_Copy_05.Enabled = True
                lbl_textbox_TextToCopy_06.Enabled = True
                textbox_TextToCopy_06.Enabled = True
                lbl_textbox_CopyLineCount_06.Enabled = True
                textbox_CopyLineCount_06.Enabled = True
                cmdbtn_Copy_06.Enabled = True*/
            }
            else
            {
                text_Info.IsEnabled = false;
                text_Info.Text = "";
                text_PathedFileName_Info.Text = "";

                btn_CreateInfoFile.IsEnabled = true;

                // Disable the "CopyText" controls:
                /*
                lbl_textbox_TextToCopy_01.Enabled = False
                textbox_TextToCopy_01.Enabled = False
                lbl_textbox_CopyLineCount_01.Enabled = False
                textbox_CopyLineCount_01.Enabled = False
                cmdbtn_Copy_01.Enabled = False
                lbl_textbox_TextToCopy_02.Enabled = False
                textbox_TextToCopy_02.Enabled = False
                lbl_textbox_CopyLineCount_02.Enabled = False
                textbox_CopyLineCount_02.Enabled = False
                cmdbtn_Copy_02.Enabled = False
                lbl_textbox_TextToCopy_03.Enabled = False
                textbox_TextToCopy_03.Enabled = False
                lbl_textbox_CopyLineCount_03.Enabled = False
                textbox_CopyLineCount_03.Enabled = False
                cmdbtn_Copy_03.Enabled = False
                lbl_textbox_TextToCopy_04.Enabled = False
                textbox_TextToCopy_04.Enabled = False
                lbl_textbox_CopyLineCount_04.Enabled = False
                textbox_CopyLineCount_04.Enabled = False
                cmdbtn_Copy_04.Enabled = False
                lbl_textbox_TextToCopy_05.Enabled = False
                textbox_TextToCopy_05.Enabled = False
                lbl_textbox_CopyLineCount_05.Enabled = False
                textbox_CopyLineCount_05.Enabled = False
                cmdbtn_Copy_05.Enabled = False
                lbl_textbox_TextToCopy_06.Enabled = False
                textbox_TextToCopy_06.Enabled = False
                lbl_textbox_CopyLineCount_06.Enabled = False
                textbox_CopyLineCount_06.Enabled = False
                cmdbtn_Copy_06.Enabled = False*/
            }
        }

        /// <summary>
        /// Display the specified file in the picture box.
        /// </summary>
        /// <param name="PathedFileName_ToDisplay"></param>
        private void DisplayStillPic(string PathedFileName_ToDisplay)
        {
            // Update the image:
            BitmapImage bmImage = new BitmapImage();
            bmImage.BeginInit();
            bmImage.CacheOption = BitmapCacheOption.OnLoad;
            bmImage.UriSource = new Uri(PathedFileName_ToDisplay, UriKind.Absolute);
            bmImage.EndInit();
            img_01.Source = bmImage;

            // Make the appropriate controls visible:
            img_01.Visibility = Visibility.Visible;
            oMediaElement_01.Visibility = Visibility.Collapsed;
            slide_Progress.Visibility = Visibility.Collapsed;
            pnl_MediaCtrls.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Create and display the template for the info.txt file.
        /// </summary>
        private void CreateNewInfoFile()
        {
            string str_PathedFileName_Info;
            FileInfo oFileInfo_Folder;
            string str_InfoFileText;
            int i_PhotoNum;

            oFileInfo_Folder = new FileInfo(text_CurrentPath.Text);
            str_PathedFileName_Info = NormalizePath(text_CurrentPath.Text) + "info.txt";

            // Add the first lines to the file text:
            str_InfoFileText = oFileInfo_Folder.Directory.Parent.Name + "-" + oFileInfo_Folder.Directory.Name.Substring(0, 2) + "-" + oFileInfo_Folder.Name + "\n";
            str_InfoFileText = str_InfoFileText + "\n";

            // Add the rest of the lines to the file text:
            for (i_PhotoNum = 1; i_PhotoNum <= list_FolderBrowser.Items.Count; i_PhotoNum++)
            {
                str_InfoFileText = str_InfoFileText + i_PhotoNum.ToString("000") + " " + "\n";
            }

            text_Info.Text = str_InfoFileText;

            // Save the new info file:
            SaveInfoFile();

            // Display the new info file:
            DisplayInfo();
        }

        /// <summary>
        /// Rename the appropriate files to "001", "002", etc., keeping their extensions.
        /// </summary>
        private void RenameFiles()
        {
            string str_FileName_Old;
            string str_FileTitle_New;
            string str_PathedFileName_Old;
            string str_PathedFileName_New;

            int i_Consecution;
            int i_FileNum;

            i_FileNum = 0;

            // Hide the picture:
            //img_01.Source = img_Placeholder.Uid;

            for (i_Consecution = 0; i_Consecution < list_FolderBrowser.Items.Count; i_Consecution++)
            {
                DirectoryEntry entry = (DirectoryEntry)list_FolderBrowser.Items[i_Consecution];
                str_FileName_Old = entry.Name;

                if (!str_FileName_Old.StartsWith("ZZZ", StringComparison.CurrentCultureIgnoreCase))
                // If this file has NOT been deleted ...
                {
                    if (str_FileName_Old.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)
                        || str_FileName_Old.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase)
                        || str_FileName_Old.EndsWith(".avi", StringComparison.CurrentCultureIgnoreCase)
                        || str_FileName_Old.EndsWith(".mov", StringComparison.CurrentCultureIgnoreCase)
                        || str_FileName_Old.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase)
                        || str_FileName_Old.EndsWith(".mp4", StringComparison.CurrentCultureIgnoreCase)
                        )
                    // If this file fits the criteria for renaming ...
                    {
                        // Determine the old and new pathed file names:
                        i_FileNum = i_FileNum + 1;
                        str_PathedFileName_Old = NormalizePath(text_CurrentPath.Text) + str_FileName_Old;
                        str_FileTitle_New = i_FileNum.ToString("D3");
                        if (str_FileName_Old.EndsWith(".avi", StringComparison.CurrentCultureIgnoreCase))
                            str_PathedFileName_New = NormalizePath(text_CurrentPath.Text) + str_FileTitle_New + ".avi";
                        else if (str_FileName_Old.EndsWith(".mov", StringComparison.CurrentCultureIgnoreCase))
                            str_PathedFileName_New = NormalizePath(text_CurrentPath.Text) + str_FileTitle_New + ".mov";
                        else if (str_FileName_Old.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase))
                            str_PathedFileName_New = NormalizePath(text_CurrentPath.Text) + str_FileTitle_New + ".mp3";
                        else if (str_FileName_Old.EndsWith(".mp4", StringComparison.CurrentCultureIgnoreCase))
                            str_PathedFileName_New = NormalizePath(text_CurrentPath.Text) + str_FileTitle_New + ".mp4";
                        else
                            str_PathedFileName_New = NormalizePath(text_CurrentPath.Text) + str_FileTitle_New + ".jpeg";

                        // Rename the file.
                        System.IO.File.Move(str_PathedFileName_Old, str_PathedFileName_New);
                        // ... if this file fits the criteria for renaming.
                    }
                    // ... if this file has not been deleted.
                }
            }

            // Refresh the drive content listing.
            ShowDriveContents();
        }

        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;

            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; j++)
            {
                if (encoders[j].MimeType == mimeType)
                {
                    return encoders[j];
                }
            }
            return null;
        }

        /// <summary>
        /// Ensure that a path string ends with "\".
        /// </summary>
        /// <param name="Path_Orig"></param>
        /// <returns>path ending with "\"</returns>
        private string NormalizePath(string Path_Orig)
        {
            if (Path_Orig.EndsWith("\\"))
            {
                return Path_Orig;
            }
            else
            {
                return Path_Orig + "\\";
            }
        }

        private string GetParentPath(string Path_Orig)
        {
            string NPath_Orig = NormalizePath(Path_Orig);

            if (NPath_Orig.Length < 3)
            {
                return NPath_Orig;
            }
            else
            {
                Int32 i_Pos = NPath_Orig.LastIndexOf("\\", NPath_Orig.Length - 2);
                if (i_Pos < 0) return Path_Orig;
                else return NPath_Orig.Substring(0, i_Pos) + "\\";
            }
        }

        /// <summary>
        /// Save the "info.txt" file.
        /// </summary>
        private void SaveInfoFile()
        {
            if (text_Info.Text.Length > 0)
            {
                string str_PathedFileName_Info = NormalizePath(text_CurrentPath.Text) + "info.txt";
                // Create the info.txt file if doesn't already exist:
                FileStream fs = new FileStream(str_PathedFileName_Info, FileMode.OpenOrCreate);
                fs.Close();
                File.WriteAllText(str_PathedFileName_Info, text_Info.Text);
            }
        }

        private static bool IsTextNumeric(string text)
        {
            // ToDo: Make sure there is only one decimal point and only one minus sign.

            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        /// <summary>
        /// Remove the path from a pathed file name.
        /// </summary>
        /// <param name="str_PathedFileName">name of file including its path</param>
        /// <returns>Unpathed file name</returns>
        /// <credits>Original code by Mike Adams</credits>
        protected string GetFileName(string str_PathedFileName)
        {
            // position of the last "\" in the pathed file name
            Int32 i_Pos = 0;

            //textbox_Output.Text += "str_PathedFileName = ''" + str_PathedFileName + "''\r\n";

            // Find the position of the last "\" in the pathed file name.
            i_Pos = str_PathedFileName.LastIndexOf("\\");

            //textbox_Output.Text += "i_Pos = " + i_Pos.ToString() + "\r\n";

            //textbox_Output.Text += "returning ''" + str_PathedFileName.Substring(i_Pos + 1) + "''\r\n";

            return str_PathedFileName.Substring(i_Pos + 1);
        }

        /// <summary>
        /// Remove the extension from a file name.
        /// </summary>
        /// <param name="str_FileName">name of file with optional path</param>
        /// <returns>file path and title</returns>
        protected string RemoveFileExtension(string str_FileName)
        {
            Int32 i_Pos = 0;

            // Find the position of the last "." in the un-pathed file name.
            i_Pos = str_FileName.LastIndexOf(".");

            if (i_Pos <= 0) return str_FileName;
            else return str_FileName.Substring(0, i_Pos);
        }

        #endregion Private Methods
    }

    public enum EntryType
    {
        Dir,
        File
    }

    public class DirectoryEntry
    {
        private string _name;
        private string _fullpath;
        private string _ext;
        private string _size;
        private DateTime _date;
        private string _imagepath;
        private EntryType _type;

        public DirectoryEntry(string name, string fullname, string ext, string size, DateTime date, string imagepath, EntryType type)
        {
            _name = name;
            _fullpath = fullname;
            _ext = ext;
            _size = size;
            _date = date;
            _imagepath = imagepath;
            _type = type;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Ext
        {
            get { return _ext; }
            set { _ext = value; }
        }

        public string Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
        }

        public string ImagePath
        {
            get { return _imagepath; }
            set { _imagepath = value; }
        }

        public EntryType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Fullpath
        {
            get { return _fullpath; }
            set { _fullpath = value; }
        }
    }
}
