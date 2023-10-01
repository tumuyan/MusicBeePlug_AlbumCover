using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.UI.WebControls;

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

            // use vgmdb.net website
            public VGMdbAlbum(string url)
            {
                // 耗时操作
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
            //    request.ContentType = "application/x-www-form-urlencoded";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string retString = reader.ReadToEnd();

                //  < div id="coverart" style = "background-image: url('https://medium-media.vgm.io/albums/55/120455/120455-a2aed067614d.jpg')" title = "EUMU-015" ></ div >
               
                string album_pattern = @"id=""coverart"" style=""background-image: url\('([^']+)";
                RegexOptions options = RegexOptions.Multiline;
                Regex album_regex = new Regex(album_pattern);
                Match album_match = album_regex.Match(retString);

                //  < h1 >< span class="albumtitle" lang="en" style="display:inline">DIGIMON HISTORY 1999-2006 ALL THE BEST</span><span class="albumtitle" lang="ja" style="display:none"><em> / </em>DIGIMON HISTORY 1999-2006 ALL THE BEST</span><span class="albumtitle" lang="ja-Latn" style="display:none"><em> / </em>DIGIMON HISTORY 1999-2006 ALL THE BEST</span> </h1>
                string name_pattern = @"class=""albumtitle""(.+?)>(.+?)(</span>)"; 
                Regex name_regex = new Regex(album_pattern);

                // <b><span title="Performer" class="artistname" lang="en" style="display:inline">Performer</span><span style="display:none"><em> / </em></span><span title="Performer" class="artistname" lang="ja" style="display:none">Performer</span></b>


                if (album_match.Groups.Count > 0)
                {
                    cover =album_match.Groups[1].Value;
                    type = 1;

 
                    List<string> names = new List<string>();
                    foreach (Match m in Regex.Matches(retString, name_pattern, options))
                    {
                        if (m.Groups.Count > 2)
                        {
                          string v = m.Groups[2].Value.Replace("<em> / </em>","").Trim();
                            if (!names.Contains(v))
                            {   names.Add(v);
                                name = name + " " + v;
                            }
                        }
                    }

                    // 只获取了一个封面，未获取artist
                }
                else
                {
                    Console.WriteLine("[Error]Empty album SearchResult = "+SearchResult+ ", url = " + url );
                }
            }


            // use vgmdb json api
            public VGMdbAlbum(string url, string site)
            {
                url = url.Replace("https://vgmdb.net/album/", site + "/album/") + "?format=json";
                // 耗时操作
                SearchResult = requestJObject(url, null); //解析专辑
                type = 0;
                if (SearchResult != null && SearchResult.ContainsKey("name"))
                {

                    if (SearchResult.ContainsKey("picture_full"))
                    {
                        cover = SearchResult["picture_full"].ToString();
                        type = 1;
                    }
                    else if (SearchResult.ContainsKey("picture_medium"))
                    {
                        cover = SearchResult["picture_medium"].ToString();
                        type = 2;
                    }
                    else if (SearchResult.ContainsKey("picture_small"))
                    {
                        cover = SearchResult["picture_small"].ToString();
                        type = 3;
                    }
                    else if (SearchResult.ContainsKey("covers"))
                    {
                        JArray covers = (JArray)SearchResult["covers"];
                        if (covers != null && covers.Count > 0)
                        {
                            JObject cover_obj = (JObject)covers[0];
                            if (cover_obj.ContainsKey("full"))
                            {
                                cover = cover_obj["full"].ToString();
                                type = 1;
                            }
                            else if (cover_obj.ContainsKey("medium"))
                            {
                                cover = cover_obj["medium"].ToString();
                                type = 2;
                            }
                            else if (cover_obj.ContainsKey("small"))
                            {
                                cover = cover_obj["small"].ToString();
                                type = 3;
                            }
                            else
                            {
                                type = 10;
                            }

                        }
                        else
                        {
                            type = 10;
                        }
                    }

                    if (SearchResult.ContainsKey("performers"))
                        artist = SearchResult["performers"].ToString();
                    else if (SearchResult.ContainsKey("vocals"))
                        artist = SearchResult["vocals"].ToString();

                    name = "" + SearchResult["name"].ToString() + "\n" + SearchResult["names"].ToString();
                }
                else
                {
                    Console.WriteLine("[Error]Empty album SearchResult = " + SearchResult + ", url = " + url);
                }
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
                        + "&albumtitles=" + album;
                    if(!String.IsNullOrEmpty(artist))
                        data+= "&artistalias=" + artist;
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
                        album_urls.Add(match.Value);
                    //   album_urls.Add(match.Value.Replace("https://vgmdb.net/album/", "http://vgmdb.info/album/") + "?format=json");

                    if (album_urls.Count > 0)
                        return album_urls;
                    else if (!String.IsNullOrEmpty(artist))
                        return vgmdb_advancedsearch("", album);
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
            String[] u = (String[])url;
            //   VGMdbAlbum album = new VGMdbAlbum(url.ToString(),"http://vgmdb.info");

            VGMdbAlbum album = null;
            if (u.Length<2 || u[1] == null || u[1].Trim().Length < 1)
                album = new VGMdbAlbum(u[0]);
            else
                album = new VGMdbAlbum(u[0], u[1]);
            lock (list_vgmdb)
            {
                list_vgmdb.Add(album);
                if (list_vgmdb.Count == count_load_album)
                    event_load_album.Set();
                else
                    Console.WriteLine("load " + list_vgmdb.Count + "/" + count_load_album);
            }
        }





        public String getCover(String Artist, String Album, String api_server)
        {
            if (Album.Replace(" ", "").Length < 1)
                return null;
            // 从API取回搜索结构

            List<string> album_urls  = vgmdb_advancedsearch(Artist, Album);
 

            // 如果高级搜索失败，使用专辑名称再次搜索
            if (album_urls.Count < 1)
            {
                
                string SearchUrl=null;
                if (api_server.Trim().Length < 1)
                    SearchUrl = String.Format(api_server + "http://vgmdb.info/search/albums?q={0}", Album).Replace("&", "%26") + "&format=json";
                else
                    SearchUrl = String.Format(api_server + "/search/albums?q={0}", Album).Replace("&", "%26") + "&format=json";
                JObject SearchResult = requestJObject(SearchUrl, null);//解析搜索结果
                if (SearchResult == null)
                {
                    Console.WriteLine("[Error]vgmdb SearchResult == null, SearchUrl = " + SearchUrl);
                    return null;
                }
                if(!SearchResult.ContainsKey("results"))
                {
                    Console.WriteLine("[Error]vgmdb SearchResult not has results, SearchUrl = " + SearchUrl);
                    return null;
                }


                JArray SongList = (JArray)SearchResult["results"]["albums"];//搜索结果专辑列表

                if (SongList == null)
                    return null;

                if (SongList.Count < 1)
                    return null;

                count_load_album = Math.Min(10, SongList.Count);

                for (int i = 0; i < SongList.Count; i++)
                {
                    album_urls.Add("https://vgmdb.net/" + SongList[i]["link"].ToString());
                    //  album_urls.Add("http://vgmdb.info/" + SongList[i]["link"].ToString() + "?format=json");
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
                ThreadPool.QueueUserWorkItem(new WaitCallback(loadAlbum),new String[]{ album_urls[i],api_server});
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
