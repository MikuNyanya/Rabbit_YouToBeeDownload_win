using System.IO;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Rabbit_YouToBeeDownload
{
    public class HttpUtil
    {
        private static readonly string DefaultUserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36";

        public static string PostFormData(string url, Dictionary<string, string> param,WebProxy proxy,string referer)
        {
            string Ticks = DateTime.Now.Ticks.ToString();
            HttpWebRequest request = null;
            CookieContainer cookie = new CookieContainer();
            //HTTPSQ请求
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            request = WebRequest.Create(url) as HttpWebRequest;

            if(null != proxy) { 
                request.Proxy = proxy;
            }

            request.CookieContainer = cookie;
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "POST";
            request.ContentType = string.Format("multipart/form-data; boundary=--------------------------{0}", Ticks);
            request.UserAgent = DefaultUserAgent;
            request.KeepAlive = true;
            request.Referer = referer;
            //  request.Timeout = 300000;

            string Paramter = GetFormdata(param, Ticks);
            byte[] byteData = Encoding.UTF8.GetBytes(Paramter);
            int length = byteData.Length;
            request.ContentLength = length;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(byteData, 0, byteData.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            using (StreamReader st = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")))
            {
                return st.ReadToEnd().ToString();

            }
        }
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static string Base64Encode(Encoding encodeType, string source)
        {
            string encode = string.Empty;
            byte[] bytes = encodeType.GetBytes(source);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = source;
            }
            return encode;
        }
        public static string GetFormdata(Dictionary<string, string> dic, string ticks)
        {
            string Info = "";
            string Head = string.Format("----------------------------{0}", ticks);
            string Foot = string.Format("----------------------------{0}--", ticks);
            foreach (var item in dic)
            {
                Info += string.Format("{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n", Head, item.Key, item.Value);
            }
            Info += Foot;
            return Info;
        }

    }

}
