
using System.IO;
using System.Net;
using System.Printing;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Rabbit_YouToBeeDownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            init();
        }

        private void init()
        {
            //禁用下载按钮
            this.btn_download.IsEnabled = false;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
         
        }

        private void btn_search_Click(object sender, RoutedEventArgs e)
        {

            //获取界面参数
            string youtubeUrl = this.txt_youtubeurl.Text;
            string proxyUrl = this.txt_proxyurl.Text;

            //解析视频信息 https://ssvid.net/zh-cn
            string apiUrl = "https://www.y2mate.com/mates/en948/analyzeV2/ajax";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("k_query", youtubeUrl);
            param.Add("k_page", "home");
            param.Add("hl", "en");
            param.Add("q_auto", "1");

            //代理
            WebProxy webProxy = new WebProxy(proxyUrl);
            string referer = "https://www.y2mate.com/en948";

            string rspBody = HttpUtil.PostFormData(apiUrl, param, webProxy, referer);

            //解析返回视频信息，最多到480p，再低算了吧，都糊了，人间不值得
            //JsonSerializer.Serialize(object);
            VoideInfo bodyInfo = JsonSerializer.Deserialize<VoideInfo>(rspBody);
            Dictionary<string, object> links = bodyInfo.links.mp4;
            string p = "1080p";
            string k = parseVoideK(links, p);
            if (String.IsNullOrEmpty(k))
            {
                p = "720p";
                k = parseVoideK(links, p);
            }
            if (String.IsNullOrEmpty(k))
            {
                p = "480p";
                k = parseVoideK(links, p);
            }

            //填充界面信息
            this.txt_k.Text = k;
            this.txt_p.Text = p;
            this.txt_vid.Text = bodyInfo.vid;
            this.txt_title.Text = bodyInfo.title;
            string imgUrl = "https://i.ytimg.com/vi/" + bodyInfo.vid + "/0.jpg";

            Console.WriteLine(imgUrl);
            this.img_v.Source = new BitmapImage(new Uri(imgUrl, UriKind.Absolute));

            this.btn_download.IsEnabled = true;
        }

        private string parseVoideK(Dictionary<string, object> links, string q)
        {
            foreach (var item in links)
            {
                string tempStr = item.Value.ToString();
                Dictionary<string, string> tempMap = JsonSerializer.Deserialize<Dictionary<string, string>>(tempStr);
                string qStr = "";
                string k = "";
                tempMap.TryGetValue("q", out qStr);
                if (q.Equals(qStr))
                {
                    tempMap.TryGetValue("k", out k);
                    return k;
                }
            }
            return "";
        }

        private void btn_download_Click(object sender, RoutedEventArgs e)
        {

            Task.Run(() =>
                downloadVoideAsync()
            );


            this.btn_download.IsEnabled = false;
        }

        private string parseTitle(string oTitle)
        {
            if (String.IsNullOrWhiteSpace(oTitle))
            {
                return DateTime.Now.Date.ToString().Replace("-", "_").Replace(":", "_").Replace(" ", "_");
            }
            string newTitle = oTitle;
            string tags = "\\/:*?\"<>|";

            foreach (var item in tags)
            {
                newTitle = newTitle.Replace(item.ToString(), "");
            }

            return newTitle;
        }

        private void downloadVoideAsync()
        {
            string proxyUrl = "";
            string filePath = "";
            string vid = "";
            string k = "";
            this.Dispatcher.Invoke(new Action(() =>
            {
                proxyUrl = txt_proxyurl.Text;
                filePath = txt_savefilepath.Text + "\\" + parseTitle(txt_title.Text);
                vid = txt_vid.Text;
                k = txt_k.Text;
            }));

            //获取视频下载链接
            string apiUrl = "https://www.y2mate.com/mates/convertV2/index";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("vid", vid);
            param.Add("k", k);

            //代理
            WebProxy webProxy = new WebProxy(proxyUrl);

            string referer = "https://www.y2mate.com/youtube/"+ vid;

            string rspBody = HttpUtil.PostFormData(apiUrl, param, webProxy, referer);

            Dictionary<string, string> bodyMap = JsonSerializer.Deserialize<Dictionary<string, string>>(rspBody);
            string downloadLink;
            string fix = "";
            //下载链接
            bodyMap.TryGetValue("dlink", out downloadLink);
            //文件名后缀
            bodyMap.TryGetValue("ftype", out fix);
            filePath = filePath + "." + fix;


            //下载视频
            HttpWebRequest downloadRequest = (HttpWebRequest)WebRequest.Create(downloadLink);
            downloadRequest.Proxy = webProxy;
            long lengthMax = 0;
            using (HttpWebResponse response = (HttpWebResponse)downloadRequest.GetResponse())
            {
                lengthMax = response.ContentLength;
                using (Stream streamResp = response.GetResponseStream())
                {
                    using (FileStream fileStream = File.Create(filePath))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalDownload = 0;
                        long timeTemp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                        while ((bytesRead = streamResp.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            totalDownload += bytesRead;

                            //限制更新频率
                            if (timeTemp < new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds())
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    double barValue = (totalDownload * 1.0 / lengthMax * 100);
                                    string outStr = (totalDownload / 1024.0 / 1024).ToString("F2") + " MB / " + (lengthMax / 1024.0 / 1024).ToString("F2") + " MB (" + barValue.ToString("F2") + "%)";
                                    this.lab_download.Header = outStr;
                                    this.bar_download.Value = barValue;

                                }));
                                timeTemp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                            }
                        }
                    }
                }
            }

            this.bar_download.Value = 100.0;
            this.lab_download.Header = "下载完成";

            this.Dispatcher.Invoke(new Action(() =>
            {
                btn_download.IsEnabled = true;
            }));
        }

        private void btn_selectpath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    this.txt_savefilepath.Text = dialog.SelectedPath;
                }
            }
        }
    }
}