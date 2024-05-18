using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbit_YouToBeeDownload
{


    public class VoideInfo
    {
        public string status { get; set; }
        public string mess { get; set; }
        public string page { get; set; }
        public string vid { get; set; }
        public string extractor { get; set; }
        public string title { get; set; }
        public int t { get; set; }
        public string a { get; set; }
        public Links links { get; set; }
    }

    public class Links
    {
        public Dictionary<string,object> mp4 { get; set; }
    }



}
