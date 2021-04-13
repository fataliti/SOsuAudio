using System;
using System.Collections.Generic;
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
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Newtonsoft.Json;


namespace SOsuAudio
{

    public partial class MainWindow : Window
    {
      
        private System.Windows.Controls.ProgressBar progressBar;
        private System.Windows.Controls.TextBlock progressText;
        private System.Windows.Controls.Label folderFrom;
        private System.Windows.Controls.Label folderhTo;
        
        
        private Thread parserThread;
        private Settings settings = new Settings();
        

        public MainWindow()
        {
            InitializeComponent();
            this.progressBar = (System.Windows.Controls.ProgressBar)FindName("bar");
            this.progressText = (System.Windows.Controls.TextBlock)FindName("barCount");
            this.folderFrom = (System.Windows.Controls.Label)FindName("osuPath");
            this.folderhTo = (System.Windows.Controls.Label)FindName("transPath");

            settings.Load();
            if (settings.pathFrom != null) {folderFrom.Content = settings.pathFrom;}
            if (settings.pathTo != null) {folderhTo.Content = settings.pathTo;}
            if (settings.checks != null) {
                System.Windows.Controls.CheckBox[] cheks = {
                    (System.Windows.Controls.CheckBox)FindName("std"),
                    (System.Windows.Controls.CheckBox)FindName("taiko"),
                    (System.Windows.Controls.CheckBox)FindName("catch"),
                    (System.Windows.Controls.CheckBox)FindName("mania")
                };
                for (var i = 0; i < cheks.Length; i++)
                {
                    if (settings.checks[i])
                    {
                        cheks[i].IsChecked = true;
                    }
                }
            }
            if (settings.dateBefor != null) {
                var dateElem = (System.Windows.Controls.DatePicker)FindName("dateBefore");
                dateElem.SelectedDate = settings.dateBefor;
            }
            if (settings.dateAfter != null) {
                var dateElem = (System.Windows.Controls.DatePicker)FindName("dateAfter");
                dateElem.SelectedDate = settings.dateAfter;
            }
        }

        private void SelectOsuSongsFolder(object sender, RoutedEventArgs e)
        {
            var select = selectPath();
            if (select != null)
            {
                folderFrom.Content = select;
                settings.pathFrom = select;
            }
        }

        private void SelectTransferSongsFolder(object sender, RoutedEventArgs e)
        {
            var select = selectPath();
            if (select != null)
            {
                folderhTo.Content = select;
                settings.pathTo = select;
            }
        }

        private void resedDateAfter(object sender, RoutedEventArgs e) { resetDate("dateAfter"); }
        private void resedDateBefore(object sender, RoutedEventArgs e) { resetDate("dateBefore"); }

        private void resetDate(string datePickerName) { 
            var dateElem = FindName(datePickerName);
            if (dateElem == null) return;
            ((System.Windows.Controls.DatePicker)dateElem).SelectedDate = null;
        }


        private void TransferSongs(object sender, RoutedEventArgs e)
        {
            if (parserThread != null)
            {
                return;
            }


            var pathFrom = folderFrom.Content.ToString();
            if (!Directory.Exists(pathFrom))
            {
                System.Windows.MessageBox.Show("Songs path not exist");
                return;
            }
            var pathTo = folderhTo.Content.ToString();
            if (!Directory.Exists(pathTo))
            {
                System.Windows.MessageBox.Show("Transfer path not seted");
                return;
            }

            var std = (System.Windows.Controls.CheckBox)FindName("std");
            var taiko = (System.Windows.Controls.CheckBox)FindName("taiko");
            var ctb = (System.Windows.Controls.CheckBox)FindName("catch");
            var mania = (System.Windows.Controls.CheckBox)FindName("mania");
            bool[] checks = { std.IsChecked == true, taiko.IsChecked == true, ctb.IsChecked == true, mania.IsChecked == true };

            var transferer = new Transferer();
            transferer.pathFrom = folderFrom.Content.ToString();
            transferer.pathTo = folderhTo.Content.ToString();
            transferer.checks = checks;
            var dateElem = (System.Windows.Controls.DatePicker)FindName("dateAfter");
            if (dateElem.SelectedDate != null)
                transferer.dateAfter = (DateTime)dateElem.SelectedDate;
            dateElem = (System.Windows.Controls.DatePicker)FindName("dateBefore");
            if (dateElem.SelectedDate != null)
                transferer.dateBefor = (DateTime)dateElem.SelectedDate;


            settings.checks = checks;
            settings.dateAfter = transferer.dateAfter;
            settings.dateBefor = transferer.dateBefor;
            settings.pathFrom = transferer.pathFrom;
            settings.pathTo = transferer.pathTo;

            parserThread = new Thread(() => Transfer(transferer));
            parserThread.Start();
        }

        private void Transfer(Transferer transferer)
        {
            var folders = Directory.GetDirectories(transferer.pathFrom);
            var songList = new List<OsuSong>();

            Console.WriteLine(folders.Length);
            foreach (var folder in folders)
            {

                if (transferer.dateAfter != null)
                {
                    if (Directory.GetLastWriteTime(folder) < transferer.dateAfter)
                    {
                        continue;
                    }
                }

                if (transferer.dateBefor != null)
                {
                    if (Directory.GetLastWriteTime(folder) > transferer.dateBefor)
                    {
                        continue;
                    }
                }


                var files = Directory.GetFiles(folder);
                foreach (var file in files)
                {
                    if (System.IO.Path.GetExtension(file) == ".osu")
                    {
                        var osuMap = File.OpenText(file);
                        var osuInstance = new OsuSong();

                        while (!osuMap.EndOfStream)
                        {
                            var line = osuMap.ReadLine();
                            if (line.Contains("[General]"))
                            {
                                while (line != "")
                                {
                                    line = osuMap.ReadLine();
                                    if (line.StartsWith("Mode"))
                                    {
                                        var mode = line.Split(':')[1].Trim();
                                        osuInstance.mode = mode;
                                    }
                                    else if (line.StartsWith("AudioFilename"))
                                    {
                                        var audioFile = line.Split(':')[1].Trim();
                                        osuInstance.songPath = folder + "/" + audioFile;
                                    }
                                }
                            }

                            if (line.Contains("[Metadata]"))
                            {
                                while (line != "")
                                {
                                    line = osuMap.ReadLine();

                                    if (line.StartsWith("Title"))
                                    {
                                        var title = line.Split(':')[1].Trim();
                                        osuInstance.title = title;
                                    }
                                    else if (line.StartsWith("Artist"))
                                    {
                                        var artist = line.Split(':')[1].Trim();
                                        osuInstance.artist = artist;
                                    }
                                }
                            }
                        }
                        songList.Add(osuInstance);
                        osuMap.Close();
                    }
                }
            }


            var alreadyCopied = new List<String>();
            transferer.songsTotal = songList.Count;
            transferer.songsGeted = 0;

            this.Dispatcher.Invoke(() => {
                progressBar.Maximum = songList.Count;
            });

            int copied = 0;

            foreach (var song in songList)
            {
                this.Dispatcher.Invoke(() => {
                    progressBar.Value = transferer.songsGeted;
                });

                transferer.songsGeted++;
                Console.WriteLine($"{song.artist} - {song.title} [{song.mode}, {song.songPath}]");
                if (alreadyCopied.Contains(song.songPath) || !transferer.checks[getMode(song.mode)])
                    continue;
                alreadyCopied.Add(song.songPath);
                var np = transferer.pathTo + "\\" + removeSpecChars($"{song.artist} - {song.title}.mp3");
                if (!File.Exists(np))
                {
                    File.Copy(song.songPath, np);
                    copied++;
                }
                this.Dispatcher.Invoke(() => {
                    progressText.Text = copied.ToString();
                });
                
            }

            var t = parserThread;
            parserThread = null;
            t.Interrupt();
        }

        
        private string selectPath()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    return fbd.SelectedPath;
                }
            }
            return null;
        }


        private static string removeSpecChars(string str)
        {
            var retstr = "";

            foreach(var ch in str)
            {
                switch(ch)
                {
                    case '>':
                    case '<':
                    case '|':
                    case '/':
                    case '\\':
                    case '"':
                    case '?':
                    case '*':
                    case ':':
                        retstr += " ";
                        break;
                    default:
                        retstr += ch;
                        break;

                }
            }

            return retstr;
        }

        int getMode(string mode)
        {

            if (mode == "" || mode == null)
                return 0;
            else
                return int.Parse(mode);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            settings.Save();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/5kZhhWD");
        }
    }


    public class OsuSong
    {
        public string mode;
        public string songPath;
        public string artist;
        public string title;
    }


    public class Transferer
    {
        public string pathFrom;
        public string pathTo;
        public bool[] checks;
        public DateTime? dateBefor = null;
        public DateTime? dateAfter = null;

        public int songsTotal = 1;
        public int songsGeted = 0;
    }

    public class Settings
    {
        public string pathFrom;
        public string pathTo;
        public bool[] checks;
        public DateTime? dateBefor = null;
        public DateTime? dateAfter = null;

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText("data.json", json);
        }

        public void Load()
        {
            if (File.Exists("data.json"))
            {
                var jsonString = File.ReadAllText("data.json");
                var deser = JsonConvert.DeserializeObject<Settings>(jsonString);
                pathFrom = deser.pathFrom;
                pathTo = deser.pathTo;
                checks = deser.checks;
                dateAfter = deser.dateAfter;
                dateBefor = deser.dateBefor;
            }
        }
    }


}
