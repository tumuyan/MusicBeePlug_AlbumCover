using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MusicBeePlugin.Api
{
    class vgmdb:WebApi
    {

        private static string ApiName = "VGMDB";
        class VGMdbAlbum
        {
            public string cover = "";
            private int type;
            public string artist = "";

            private string name = "";
            public string getName() { return name; }
            private JObject SearchResult;

            public VGMdbAlbum(string url)
            {
                // 耗时操作
                SearchResult = requestJObject(url, null);//解析专辑

                type = 0;
                if (SearchResult != null)
                {
                    cover = SearchResult["picture_full"].ToString();
                    type = 1;
                    if (String.IsNullOrEmpty(cover))
                    {
                        cover = SearchResult["picture_small"].ToString();
                        type = 2;
                        if (String.IsNullOrEmpty(cover))
                        {
                            cover = "";
                            type = 10;
                        }
                    }
                }

                artist = SearchResult["performers"].ToString();
                if (String.IsNullOrEmpty(artist))
                    artist = "";

                name = "" + SearchResult["name"].ToString() + "\n" + SearchResult["names"].ToString();

            }
        }


        // 使用vgmdb.net的高级搜索功能，同时检索专辑和艺术家两种信息，并取回专辑的url列表
        static private List<string> vgmdb_advancedsearch(string artist, string album)
        {
            List<string> album_urls = new List<string>();
            try
            {
                {
                    var request = (HttpWebRequest)WebRequest.Create("https://vgmdb.net/search?do=results");
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    string data = "action=advancedsearch&platformmodifier=contain_and&collectionmodifier=0&tracklistmodifier=is&sortby=albumtitle&orderby=ASC&dosearch=Search Albums Now"
                        + "&albumtitles=" + album + "&artistalias=" + artist;
                    //        request.ContentLength = data.Length;

                    StreamWriter writer = new StreamWriter(request.GetRequestStream());
                    writer.Write(data);
                    writer.Flush();
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string retString = reader.ReadToEnd();

                    string pattern = @"https://vgmdb.net/album/\d+";
                    //    href =\"https://vgmdb.net/album/67360\"

                    foreach (Match match in Regex.Matches(retString, pattern))
                        album_urls.Add(match.Value.Replace("https://vgmdb.net/album/", "http://vgmdb.info/album/") + "?format=json");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return album_urls;
        }

        private int count_load_album;
        private ManualResetEvent event_load_album;
        private List<VGMdbAlbum> list_vgmdb;

        public vgmdb()
        {
            count_load_album = 0;
            event_load_album = new ManualResetEvent(false);
            list_vgmdb = new List<VGMdbAlbum>();

        }

        private void loadAlbum(object url)
        {
            VGMdbAlbum album = new VGMdbAlbum(url.ToString());
            lock (list_vgmdb)
            {
                list_vgmdb.Add(album);
                if (list_vgmdb.Count == count_load_album)
                    event_load_album.Set();
                else
                    Console.WriteLine("load " + list_vgmdb.Count + "/" + count_load_album);
            }
        }





        public String getCover(String Artist, String Album)
        {
            if (Album.Replace(" ", "").Length < 1)
                return null;
            // 从API取回搜索结构

            List<string> album_urls;

            if (!String.IsNullOrEmpty(Artist))
            {
                album_urls = vgmdb_advancedsearch(Artist, Album);
            }
            else
            {
                album_urls = new List<string>();
            }

            // 如果高级搜索失败，使用专辑名称再次搜索
            if (album_urls.Count < 1)
            {
                string SearchUrl = String.Format("http://vgmdb.info/search/albums?q={0}", Album).Replace("&", "%26") + "&format=json";

                JObject SearchResult = requestJObject(SearchUrl, null);//解析搜索结果
                JArray SongList = (JArray)SearchResult["results"]["albums"];//搜索结果专辑列表

                if (SongList == null)
                    return null;

                if (SongList.Count < 1)
                    return null;

                count_load_album = Math.Min(10, SongList.Count);

                for (int i = 0; i < SongList.Count; i++)
                {
                    album_urls.Add("http://vgmdb.info/" + SongList[i]["link"].ToString() + "?format=json");
                }
            }

            List<int> list_match_album = new List<int>();
            List<int> list_match_artist = new List<int>();
            List<int> list_match_title = new List<int>();
            List<string> list_image = new List<string>();

            list_vgmdb = new List<VGMdbAlbum>();
            event_load_album.Reset();

            // 至多检查10个搜索结果
            count_load_album = Math.Min(10, album_urls.Count);

            for (int i = 0; i < count_load_album; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(loadAlbum), album_urls[i]);
            }

            event_load_album.WaitOne(Timeout.Infinite, true);
            int j = 0;
            string _Album = prepareString(Album, false);

            for (int i = 0; i < count_load_album; i++)
            {
                //取出album cover
                String s = list_vgmdb[i].cover;
                if (s.Length < 10)
                    continue;

                list_image.Add(s);

                // 专辑名称命名为title
                String album = prepareString(list_vgmdb[i].getName(), false);
                if (album.Contains(_Album))
                    list_match_album.Add(j);

                if (Artist.Length > 0)
                {
                    // 输入的Artist参数已经预处理过，只包含一个艺术家
                    if (list_vgmdb[i].artist.ToLower().Contains(Artist.ToLower()))
                        list_match_artist.Add(j);
                }

                j++;
            }


            return selectCover(list_match_album, list_match_artist, list_match_title, list_image);

        }

    }
}
