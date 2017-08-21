using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace NeteaseLib {
    public class NeteaseException : Exception {
        public NeteaseException(string msg) : base(msg) { }
    }

    public class Netease {
        public static string GetLyricByID(string id) {
            // lv:歌词，kv:卡拉OK歌词，tv:翻译歌词
            string lyricURL = $"http://music.163.com/api/song/lyric?id={id}&lv=-1&tv=-1";
            var wClient = new WebClient();
            byte[] buffer = wClient.DownloadData(lyricURL);
            string lyricJsonText = new UTF8Encoding().GetString(buffer);
            dynamic lyricJsonObj = JsonConvert.DeserializeObject<dynamic>(lyricJsonText);
            try {
                if (lyricJsonObj.nolyric != null) return "";
                return lyricJsonObj.lrc.lyric;
            }
            catch {
#if DEBUG
                throw new NeteaseException("Invalid Lyric Json Format");
#endif
            }
        }

        // overload
        public static string GetLyricByID(long id) { return GetLyricByID(id.ToString()); }

        public static NeteaseResponse Search(string searchString) {
            // POST的参数，type=1代表搜索歌曲
            string postString = $"s={searchString}&type=1"; 
            byte[] postData = Encoding.UTF8.GetBytes(postString);  
            string url = "http://music.163.com/api/search/pc"; 
            var wClient = new WebClient();
            // 必须的Header
            wClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded"); 
            byte[] responseData = wClient.UploadData(url, "POST", postData); 
            string responseString = Encoding.UTF8.GetString(responseData); 
            var response = JsonConvert.DeserializeObject<NeteaseResponse>(responseString);
            return response;
        }
    }
}