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
using System.Reflection;

namespace iTunesConsole {
    class Program {
        static Program() {
            //这个绑定事件必须要在引用到TestLibrary1这个程序集的方法之前,注意是方法之前,不是语句之间,就算语句是在方法最后一行,在进入方法的时候就会加载程序集,如果这个时候没有绑定事件,则直接抛出异常,或者程序终止了
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            //获取加载失败的程序集的全名
            var assName = new AssemblyName(args.Name).FullName;
            if (args.Name == "Interop.iTunesLib, Version=1.13.0.0, Culture=neutral, PublicKeyToken=null") {
                //读取资源
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("_Console.Resources." +

      new AssemblyName(args.Name).Name + ".dll")) {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    return Assembly.Load(bytes);//加载资源文件中的dll,代替加载失败的程序集
                }
            }
            throw new DllNotFoundException(assName);
        }
        static void Main(string[] args) {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
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
                catch (System.Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
