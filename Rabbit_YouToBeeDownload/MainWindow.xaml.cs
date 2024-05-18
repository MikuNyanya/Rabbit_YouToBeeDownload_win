
using System.IO;
using System.Net;
using System.Printing;
using System.Text.Json;
using System.Windows;

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
            txtOutAppend("==========\r\n使用说明：\r\n-填写代理 如果可以直接访问y2mate则可以不填\r\n-填写油管视频页面链接\r\n-点击视频解析\r\n-如能在下方看到视频信息则表明解析成功，此时下载按钮就可以点击了\r\n-选择视频保存位置后 点击下载即可\r\n下载进度在此处显示");
            txtOutAppend("==========\r\n本程序属于个人使用性质，没有做任何异常处理\r\n但程序主要功能没问题，所以闪退的时候请自行检查视频链接和代理是否正确");
            txtOutAppend("==========\r\n这里是兔子");
        }

        private void btn_search_Click(object sender, RoutedEventArgs e)
        {

            //获取界面参数
            string youtubeUrl = this.txt_youtubeurl.Text;
            string proxyUrl = this.txt_proxyurl.Text;

            //解析视频信息
            string apiUrl = "https://www.y2mate.com/mates/analyzeV2/ajax";
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("k_query", youtubeUrl);
            param.Add("k_page", "home");
            param.Add("hl", "en");
            param.Add("q_auto", "1");

            //代理
            WebProxy webProxy = new WebProxy(proxyUrl);

            string rspBody = HttpUtil.PostFormData(apiUrl, param, webProxy);

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

            txtOutAppend("==========\r\n解析完成");
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

            string rspBody = HttpUtil.PostFormData(apiUrl, param, webProxy);

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
                                    txtOutAppend((totalDownload / 1024.0 / 1024).ToString("F2") + " MB / " + (lengthMax / 1024.0 / 1024).ToString("F2") + " MB (" + (totalDownload * 1.0 / lengthMax * 100).ToString("F2") + "%)");
                                }));
                                timeTemp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                            }
                        }
                    }
                }
            }

            txtOutAppend("==========\r\n视频下载完成");

            this.Dispatcher.Invoke(new Action(() =>
            {
                btn_download.IsEnabled = true;
            }));
        }

        private void txtOutAppend(string appendText)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txt_out.Text = "\r\n" + txt_out.Text;
                txt_out.Text = appendText + txt_out.Text;
            }));
        }
    }
}