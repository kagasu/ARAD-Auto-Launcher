using Codeplex.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ARADLoginTool
{
    /// <summary>
    /// LoadingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                try
                {
                    var dataJson = DynamicJson.Parse(File.ReadAllText("data.json", new UTF8Encoding()));
                    var wc = new WebClientEx() { Encoding = new UTF8Encoding() };

                    if (dataJson["proxy"] != "")
                    {
                        wc.Proxy = new WebProxy(string.Format("http://{0}/", dataJson["proxy"]));
                    }

                    var str = wc.DownloadString("http://arad.nexon.co.jp/");

                    var unique_id = new Regex("class=\"nexonid\" name=\"(.*?)\"").Matches(str)[0].Groups[1].Value;
                    var unique_password = new Regex("class=\"password\" name=\"(.*?)\"").Matches(str)[0].Groups[1].Value;
                    var login_key = new Regex("name=\"login_key\" value=\"(.*?)\"").Matches(str)[0].Groups[1].Value;

                    var data = new NameValueCollection();
                    data.Add(unique_id, dataJson["id"]);
                    data.Add(unique_password, dataJson["password"]);
                    data.Add("login_key", login_key);
                    wc.UploadValues("https://arad.nexon.co.jp/login/loginprocess.aspx", data);

                    str = wc.DownloadString("http://arad.nexon.co.jp/launcher/game/GameStart.aspx");

                    var parameters = new List<string>();
                    parameters.Add(new Regex("\\[\"ServerIP\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"ServerPort\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"ServerType\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"SiteType\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"UserId\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"PassportString\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"LauncherChecksum\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);
                    parameters.Add(new Regex("\\[\"CharCount\"\\] = \"(.*?)\"").Matches(str)[0].Groups[1].Value);

                    Process.Start("neoplecustomurl://" + string.Join("/", parameters));

                    // Delete Arad.lnk if exists
                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Arad.lnk"))
                    {
                        File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Arad.lnk");
                    }

                    // monitoring Arad.lnk
                    var watcher = new FileSystemWatcher();
                    watcher.Path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    watcher.Filter = "Arad.lnk";
                    watcher.NotifyFilter = NotifyFilters.FileName;

                    // exclude subdirectories
                    watcher.IncludeSubdirectories = false;

                    // start monitoring
                    var changedResult = watcher.WaitForChanged(WatcherChangeTypes.Created);

                    // Delete Arad.lnk when created
                    if (changedResult.ChangeType == WatcherChangeTypes.Created)
                    {
                        // wait for another process file handle dispose
                        Thread.Sleep(500);
                        File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\Arad.lnk");
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Environment.Exit(0);
                }
            }).Start();
        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}