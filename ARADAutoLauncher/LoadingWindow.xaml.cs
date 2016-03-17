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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ARADAutoLauncher;
using MessageBox = System.Windows.MessageBox;

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
            OneTimePassWindow otpw = null;
            DispatcherFrame dispacherFrame = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var dataJson = DynamicJson.Parse(File.ReadAllText("data.json", new UTF8Encoding()));
                    var wc = new WebClientEx() { Encoding = new UTF8Encoding() };

                    if (dataJson["proxy"] != "")
                    {
                        wc.Proxy = new WebProxy(string.Format("http://{0}/", dataJson["proxy"]));
                    }

                    wc.Headers.Add("Referer", "http://arad.nexon.co.jp/");
                    var str = wc.DownloadString("https://login.nexon.co.jp/login/?gm=arad");

                    var unique_id = new Regex("id='(.*?)'").Matches(str)[0].Groups[1].Value;
                    var unique_password = Regex.Replace(unique_id, "^i", "p");
                    var entm = new Regex("name=(\"|')entm(\"|') value=(\"|')(.*?)(\"|')").Matches(str)[0].Groups[4].Value;

                    var data = new NameValueCollection();
                    data.Add("entm", entm);
                    data.Add(unique_id, dataJson["id"]);
                    data.Add(unique_password, dataJson["password"]);
                    data.Add("onetimepass", "");
                    data.Add("HiddenUrl", "http://arad.nexon.co.jp/");
                    data.Add("otp", "");

                    wc.Headers.Add("Referer", "https://login.nexon.co.jp/login/?gm=arad");
                    str = Encoding.UTF8.GetString(wc.UploadValues("https://login.nexon.co.jp/login/login_process1.aspx", data));

                    // When onetimepass require
                    if (str.Contains("location.replace(\"https://www.nexon.co.jp/login/otp/index.aspx"))

                    {
                        str = wc.DownloadString(new Regex("location\\.replace\\(\"(https://www.nexon.co.jp/login/otp/index\\.aspx.*?)\"\\)").Matches(str)[0].Groups[1].Value);
                        dispacherFrame = new DispatcherFrame(true);
                        otpw = new OneTimePassWindow();
                        otpw.Closed += (o, args) =>
                        {
                            dispacherFrame.Continue = false;
                        };
                        otpw.ShowDialog();
                        Dispatcher.PushFrame(dispacherFrame);

                        var onetimepass = otpw.otp;
                        data = new NameValueCollection();
                        data.Add("otp", onetimepass);
                        wc.UploadValues(new Regex("action=\"(.*?)\" id=\"otploginform\"").Matches(str)[0].Groups[1].Value, data);
                    }

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
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}