using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Xml;
using iTunesLib;

namespace iTunesConsole {
    class Program {
        static void Main(string[] args) {
            //调用iTunes
            iTunesLib.IiTunes iTunes;
            iTunes= new iTunesLib.iTunesAppClass();
            //获取音乐信息
            string title, artist, album;
            IITFileOrCDTrack song;

            song=(IITFileOrCDTrack)iTunes.CurrentTrack;
            title = song.Name;
            artist = song.Artist;
            album = song.Album;
            Console.WriteLine("Song Info:");
            Console.WriteLine("Title: "+title);
            Console.WriteLine("Artist: "+artist);
            Console.WriteLine("Album: "+album);
            //构造搜索URL
            string searchString = title + " " + artist + " " + album;
            string searchURL="http://www.xiami.com/search?key=" + searchString + " &pos=1";
            WebClient wClient = new WebClient();
            byte[] buffer;
            //请求页面
            buffer = wClient.DownloadData(searchURL);
            //转化成字符串，从中获取匹配度最高的歌曲id
            System.Text.UTF8Encoding converter = new System.Text.UTF8Encoding();
            string response = converter.GetString(buffer);
            string[] separators = new string[] { "<h5>歌曲</h5>" };
            string[] sp = response.Split(separators,StringSplitOptions.RemoveEmptyEntries);
            response = sp[1];
            separators=new string[]{"http://www.xiami.com/song/"};
            sp = response.Split(separators,StringSplitOptions.RemoveEmptyEntries);
            response = sp[1];
            //得到歌曲ID
            string song_id = response.Substring(0, response.IndexOf("\""));
            Console.WriteLine("Get Song ID: "+song_id);
            //根据ID请求详细信息
            string xmlURL = "http://www.xiami.com/song/playlist/id/" + song_id + "/object_name/default/object_id/0/cat/xml";
            buffer = wClient.DownloadData(xmlURL);
            if (buffer.Count() < 10)
                throw new System.Exception("该歌曲没有相关信息，可能已被下架");
            string xml = converter.GetString(buffer);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlElement root = null;
            root = doc.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns", "http://xspf.org/ns/0/");
            XmlNodeList listNodes = null;
            listNodes = root.SelectNodes("/ns:playlist/ns:trackList/ns:track/ns:lyric_url",nsmgr);
            foreach (XmlNode node in listNodes) {
                Console.WriteLine(node.InnerText);
                try {
                    wClient.DownloadFile(node.InnerText, song.Location.Substring(0, song.Location.LastIndexOf(".")) + ".lrc");
                }
                catch (System.Net.WebException ex) {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
