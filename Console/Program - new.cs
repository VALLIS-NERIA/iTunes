using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using iTunesLib;
using System;
using System.Reflection;

namespace iTunesConsole {
    class Program {
        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");

            if (dllName.EndsWith("_resources")) return null;

            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());

            byte[] bytes = (byte[])rm.GetObject(dllName);

            return System.Reflection.Assembly.Load(bytes);
        }
        public class Song {
            public string title, artist, album, location;
            public string lyric_URL = null;
            public string song_id = null;
            public Song(string ti, string ar, string al, string lo) {
                title = ti;
                artist = ar;
                album = al;
                location = lo;
            }

            public Song(IITFileOrCDTrack song) {
                title = song.Name;
                artist = song.Artist;
                album = song.Album;
                location = song.Location;
            }

            public Song(Song s) {
                
                title = Console.ReadLine();
                artist = Console.ReadLine();
                album = Console.ReadLine();
                location = s.location;
            }

            public void Print() {
                Console.WriteLine("Song Info:");
                Console.Write(song_id == null ? null : "Song ID: " + song_id + Environment.NewLine);
                Console.WriteLine("Title: " + title);
                Console.WriteLine("Artist: " + artist);
                Console.WriteLine("Album: " + album);
            }

            public override string ToString() {
                return (title + " " + artist + " " + " album");
            }
        }
        class XMLHelper{
            XmlNamespaceManager nsmgr;
            XmlElement root;
            public XMLHelper(XmlElement root1, XmlNamespaceManager nsmgr1) {
                root = root1;
                nsmgr = nsmgr1;
            }
            public string Get(string key){
                return root.SelectSingleNode("/ns:playlist/ns:trackList/ns:track/ns:" + key, nsmgr).InnerText;
            }
        }
        static Song GetFromiTunes() {
            //调用iTunes
            iTunesLib.IiTunes iTunes;
            iTunes = new iTunesLib.iTunesAppClass();
            //获取音乐信息
            IITFileOrCDTrack song;            
            song = (IITFileOrCDTrack)iTunes.CurrentTrack;
            Song s = new Song(song);
            s.Print();
            return s;

        }

        static string Search(Song song) {
            string searchString = song.ToString();
            string searchURL = "http://www.xiami.com/search?key=" + searchString + " &pos=1";
            WebClient wClient = new WebClient();
            byte[] buffer;
            //请求页面
            buffer = wClient.DownloadData(searchURL);
            //转化成字符串，从中获取匹配度最高的歌曲id
            System.Text.UTF8Encoding converter = new System.Text.UTF8Encoding();
            string response = converter.GetString(buffer);
            return response;
        }

        static List<string> Analyze(string response) {
            List<string> ids = new List<string>();
            string[] separators = new string[] { "<h5>歌曲</h5>" };
            string[] sp = response.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Count() == 1) throw new System.Exception("未找到相关歌曲，或者歌曲无法试听");
            response = sp[1];
            separators = new string[] { "<input checked=\"checked\" type=\"checkbox\"  value=\"" };
            sp = response.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < sp.Count();i++ ) {
                response = sp[i];
                //得到歌曲ID
                string song_id = response.Substring(0, response.IndexOf("\""));
                ids.Add(song_id);
                Console.WriteLine("Get Song ID: " + song_id);
                //根据ID请求详细信息
            }
            return ids;
        }

        static Song GetInformation(string song_id){
            WebClient wClient = new WebClient();
            string xmlURL = "http://www.xiami.com/song/playlist/id/" + song_id + "/object_name/default/object_id/0/cat/xml";
            byte[] buffer = wClient.DownloadData(xmlURL);
            System.Text.UTF8Encoding converter = new System.Text.UTF8Encoding();
            string xml = converter.GetString(buffer);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlElement root = null;
            root = doc.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns", "http://xspf.org/ns/0/");
            XMLHelper xmlHelper = new XMLHelper(root, nsmgr);
            Song onlineSong = new Song(xmlHelper.Get("title"), xmlHelper.Get("artist"), xmlHelper.Get("album"), songPlaying.location);
                try {
                    wClient.DownloadFile(xmlHelper.Get("lyric_url"), songPlaying.location.Substring(0, songPlaying.location.LastIndexOf(".")) + ".lrc");
                    Console.WriteLine("done");
                    return onlineSong;
                }
                catch (System.Net.WebException ex) {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            }

//TODO
/*
 * GetInformation加参数，不用全局变量。
 * 建立一个Song列表，如果有信息一样的直接采用，否则询问用户
 * 
 */
        static Song songPlaying;
        static void Main() {
            songPlaying = GetFromiTunes();
            
            //构造搜索URL
            string response = Search(songPlaying);
            List<string> ids = Analyze(response);
            foreach (string id in ids) {
                if (GetInformation(id).song_id != null)
                    break;
            }
            
        }

        Program() {
           // AppDomain.CurrentDomain.AssemblyResolve +=new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }
    }
}
