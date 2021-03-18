﻿using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MusicBeePlugin
{

    public partial class Plugin
    {

#if DEBUG
        string mode = " Debug";
#else
        string mode = "";
# endif
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private static CookieContainer myCookieContainer = new CookieContainer();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Douban Music Artwork" + mode;
            about.Description = "Get album cover from douban music.  " +
                "\n从豆瓣音乐获取专辑封面，使用了豆瓣音乐Api V2";
            about.Author = "Tumuyan";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.ArtworkRetrieval;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 2;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            loadCookie("os=pc; osver=Microsoft-Windows-10-Professional-build-10586-64bit; appver=2.0.3.131777; channel=netease; __remember_me=true;NMTID=00Om_v;", "http://music.163.com");
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();

            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            saveCookie("http://music.163.com");
            Console.WriteLine("close "+reason);
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:
                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    // ...
                    break;

            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return new string[]
                         {
                            "Douban"+mode
                         };
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            // 预处理输入参数
            string[] artists = albumArtist.Split(';', ',', '\\', '/', '&');

            string artist = "";
            string Artist = "";

            foreach (string s1 in artists)
            {// Various Artists
                string s = s1.ToLower().Trim().Replace(" ", "");
                if (s.Equals("variousartist") || s.Equals("variousartists") || s.Equals("群星") || s.Length < 1)
                    continue;
                artist = artist + " " + s1;
                if (Artist.Length < 1)
                    Artist = s1;
            }

            return getDoubanCover(Artist, album);

        }

        // 专辑名需要预处理，降低由于标点符号差异造成的影响
        private string prepareString(string s)
        {
            Regex rgx = new Regex("[\\s\\]\\[\\(\\)`~!@#$%^&\\*()+=|{}':;',\\.<>/\\?~～〜（）「」［］！@#￥%……&*——+|{}【】‘；：”“’。，、？]");
            return rgx.Replace(s.ToLower(), "");
        }

        private string get163Cover(String Artist, String Album)
        {
            Console.WriteLine("search 163: key= " + Artist + " - " + Album);
            if (Album.Replace(" ", "").Length < 1)
                return null;

            JArray SongList = new JArray(), SongList2 = new JArray();//搜索结果曲目列表

            // 从API取回数据

            string[] SearchUrls;

            string base_url = "http://music.163.com/api/search/pc?offset=0&limit=30&type=10&s=" + HttpUtility.UrlEncode(Album);


            if (String.IsNullOrEmpty(Artist))
            {
                SearchUrls = new string[] { base_url };
            }

            else
            {
                SearchUrls = new string[] { base_url + HttpUtility.UrlEncode(" " + Artist), base_url };
            }

            List<int> list_match_album = new List<int>();
            List<int> list_match_artist = new List<int>();
            List<int> list_match_title = new List<int>();
            List<string> list_image = new List<string>();

            string album = prepareString(Album);

            foreach (string SearchUrl in SearchUrls)
            {

                JObject SearchResult = requestJObject(SearchUrl); //搜索结果曲目列表

                if (null != SearchResult["result"])
                {
                    SongList = (JArray)SearchResult["result"]["albums"];//搜索结果曲目列表
                }

                if (SongList != null)
                {
                    for (int i = 0; i < SongList.Count; i++)
                    {

                        String s = SongList[i]["picUrl"].ToString();
                        if (s.Replace(" ", "").Length < 10)
                            continue;

                        list_image.Add(s);

                        // result.albums.[1].name
                        String title = prepareString((SongList[i]["name"] ?? "").ToString());
                        if (title.Contains(album) || album.Contains(title))
                            list_match_album.Add(i);

                        //result.albums.[2].songs (虽然有这个节点，但是大量专辑没有内容）
                        if ((SongList[i]["songs"] ?? "").ToString().ToLower().Contains(album))
                            list_match_title.Add(i);

                        //musics.[1].attrs.singer
                        if (Artist.Length > 0)
                        {
                            // 如果有多个艺术家（虽然这不规范），只匹配第一个。而检索结果对应了专辑艺术家、参与艺术家、发行方
                            if ((SongList[i]["artists"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                                || (SongList[i]["artist"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                                || (SongList[i]["company"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                                )
                                list_match_artist.Add(i);
                        }
                    }
                }

                Console.WriteLine("search 163: load");
            }


            Console.WriteLine("search 163: selsct");

            return selectCover(list_match_album, list_match_artist, list_match_title, list_image);
        }


        private string getDoubanCover(String Artist, String Album)
        {
            if (Album.Replace(" ", "").Length < 1)
                return null;
            // 从API取回数据
            
            string SearchUrl = "https://api.douban.com/v2/music/search?q="
                + HttpUtility.UrlEncode(Album + " " + Artist, System.Text.UnicodeEncoding.GetEncoding("UTF-8")) 
                + "&apiKey=054022eaeae0b00e0fc068c0c0a2102a";

            JObject SearchResult =  requestJObject(SearchUrl);//解析搜索结果
            JArray SongList = (JArray)SearchResult["musics"];//搜索结果曲目列表

            if (SongList.Count < 1)
            {
                SearchUrl = "https://api.douban.com/v2/music/search?q="
                + HttpUtility.UrlEncode(Album, System.Text.UnicodeEncoding.GetEncoding("UTF-8"))
                + "&apiKey=054022eaeae0b00e0fc068c0c0a2102a";

                SearchResult = requestJObject(SearchUrl);//解析搜索结果
                SongList = (JArray)SearchResult["musics"];//搜索结果曲目列表
            }

            if (SongList.Count < 1)
                return null;

            List<int> list_match_album = new List<int>();
            List<int> list_match_artist = new List<int>();
            List<int> list_match_title = new List<int>();
            List<string> list_image = new List<string>();

            string album = prepareString(Album);

            for (int i = 0; i < SongList.Count; i++)
            {
                //musics.[1].image
                //https://img1.doubanio.com/view/subject/s/public/s6498438.jpg

                String s = SongList[i]["image"].ToString().ToLower().Replace("/s/public", "/public");
                if (s.Replace(" ", "").Length < 10)
                    continue;

                list_image.Add(s);

                // 豆瓣音乐API返回的数据中，专辑名称命名为title
                String title = prepareString((SongList[i]["title"] ?? "").ToString());
                if (title.Contains(album) || album.Contains(title))
                    list_match_album.Add(i);

                //musics.[1].attrs.tracks
                if ((SongList[i]["attrs"]["tracks"] ?? "").ToString().ToLower().Contains(album))
                    list_match_title.Add(i);

                //musics.[1].attrs.singer
                if (Artist.Length > 0)
                {
                    // 如果有多个艺术家（虽然这不规范），只匹配第一个
                    if ((SongList[i]["attrs"]["singer"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                        || (SongList[i]["attrs"]["publisher"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                        || (SongList[i]["author"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                        )
                        list_match_artist.Add(i);
                }
            }

            return selectCover(list_match_album, list_match_artist, list_match_title, list_image);

        }

        private static List<VGMdbAlbum> list_vgmdb = new List<VGMdbAlbum>();

        private static void loadVGMdbAlbum(object url)
        {
            VGMdbAlbum vGMdbAlbum = new VGMdbAlbum(url.ToString());
            lock (list_vgmdb)
            {
                list_vgmdb.Add(vGMdbAlbum);
                if (list_vgmdb.Count == count_load_album)
                    event_load_album.Set();
                else
                    Console.WriteLine("load " + list_vgmdb.Count + "/" + count_load_album);
            }
        }

        static int count_load_album = 0;
        static ManualResetEvent event_load_album = new ManualResetEvent(false);

        private string getVGMdbCover(String Artist, String Album)
        {
            if (Album.Replace(" ", "").Length < 1)
                return null;
            // 从API取回搜索结构

            List<string> album_urls;

            if (!String.IsNullOrEmpty(Artist))
            {
                album_urls =  vgmdb_advancedsearch(Artist, Album);
            }
            else
            {
                album_urls = new List<string>();
            }
        }


        private void loadCookie(string default_cookie, string domain)
        {
            Uri domain_uri = new Uri(domain);
            string configPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), about.Name);
            string cookie = default_cookie;
            if (File.Exists(configPath))
            {

                try
                {
                    cookie = cookie + (File.ReadAllText(configPath, System.Text.Encoding.UTF8));

                }
                catch (Exception ex)
                {
                    mbApiInterface.MB_Trace(about.Name + " Failed to load config" + ex);
                }

                string[] tempCookies = cookie.Split(';');
                string tempCookie = null;
                int Equallength = 0;//  =的位置
                string cookieKey = null;
                string cookieValue = null;

                for (int i = 0; i < tempCookies.Length; i++)
                {
                    if (!string.IsNullOrEmpty(tempCookies[i]))
                    {
                        tempCookie = tempCookies[i];

                        Equallength = tempCookie.IndexOf("=");

                        if (Equallength != -1)       //有可能cookie 无=，就直接一个cookiename；比如:a=3;ck;abc=;
                        {

                            cookieKey = tempCookie.Substring(0, Equallength).Trim();
                            //cookie=

                            if (Equallength == tempCookie.Length - 1)    //这种是等号后面无值，如：abc=;
                            {
                                cookieValue = "";
                            }
                            else
                            {
                                cookieValue = tempCookie.Substring(Equallength + 1, tempCookie.Length - Equallength - 1).Trim();
                            }
                        }

                        else
                        {
                            cookieKey = tempCookie.Trim();
                            cookieValue = "";
                        }

                        myCookieContainer.Add(domain_uri,new Cookie(cookieKey, cookieValue));

                    }

                }

            }
        }


        private void saveCookie(string site)
        {

            string configPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), about.Name);
          //  if (File.Exists(configPath))
            {
                try
                {
                    var cookies = myCookieContainer.GetCookies(new Uri(site));
                    string tmp = "";
                    foreach(Cookie cookie in cookies)
                    {
                        tmp = tmp + cookie.ToString()+";";
                    }
                    File.WriteAllText(configPath, tmp);
                    Console.WriteLine("saveCookie path="+configPath+"; site="+site);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }


        // 访问url并解析为JsonObject
        static private JObject requestJObject(string url)
        {
            try
            {
                if (!String.IsNullOrEmpty(url))
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);

                    request.CookieContainer = myCookieContainer;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36 Edg/89.0.774.54";
                    request.Timeout = 8000;
                    request.ReadWriteTimeout = 8000;

                    var response = (HttpWebResponse)request.GetResponse();
                    var SearchString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    myCookieContainer.Add(response.Cookies);

                    if (!String.IsNullOrEmpty(SearchString))
                    {
                        Console.WriteLine("request result str: " + SearchString.Substring(0, Math.Min(100, SearchString.Length - 1)));
                        return JObject.Parse(SearchString);
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("requestJObject:" + url);
                Console.WriteLine(e);
            }
            return JObject.Parse("{}");
        }



    }

    private static List<VGMdbAlbum> list_vgmdb = new List<VGMdbAlbum>();

    private static void loadVGMdbAlbum(object url)
    {
        VGMdbAlbum vGMdbAlbum = new VGMdbAlbum(url.ToString());
        lock (list_vgmdb)
        {
            list_vgmdb.Add(vGMdbAlbum);
            if (list_vgmdb.Count == count_load_album)
                event_load_album.Set();
            else
                Console.WriteLine("load " + list_vgmdb.Count + "/" + count_load_album);
        }
    }

    static int count_load_album = 0;
    static ManualResetEvent event_load_album = new ManualResetEvent(false);

    private string getVGMdbCover(String Artist, String Album)
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

        if (album_urls.Count < 1)
        {
            string SearchUrl = String.Format("http://vgmdb.info/search/albums?q={0}", Album).Replace("&", "%26") + "&format=json";

            JObject SearchResult = requestJObject(SearchUrl);//解析搜索结果
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
            ThreadPool.QueueUserWorkItem(new WaitCallback(loadVGMdbAlbum), album_urls[i]);
        }

        event_load_album.WaitOne(Timeout.Infinite, true);
        int j = 0;
        string _Album = prepareString(Album);

        for (int i = 0; i < count_load_album; i++)
        {
            //取出album cover
            String s = list_vgmdb[i].cover;
            if (s.Length < 10)
                continue;

            list_image.Add(s);

            // 专辑名称命名为title
            String album = prepareString(list_vgmdb[i].getName());
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

    // 使用vgmdb.info的API，获取专辑页面内的信息
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
            SearchResult = requestJObject(url);//解析专辑

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



    /// <summary>
    /// 从取回的封面列表中匹配最合适的封面
    /// </summary>
    /// <param name="list_match_album">匹配到的专辑名列表</param>
    /// <param name="list_match_artist">匹配到的艺术家列表</param>
    /// <param name="list_match_title">匹配到的Title列表</param>
    /// <param name="list_image">封面列表</param>
    /// <returns></returns>
    private string selectCover(List<int> list_match_album, List<int> list_match_artist, List<int> list_match_title, List<string> list_image)
        {
            string url = "";

            if (list_image.Count < 1)
                return null;
            if (list_image.Count == 1)
                return list_image[0];

            if (list_match_title.Count < 1 && list_match_album.Count < 1 && list_match_artist.Count < 1)
                return list_image[0];

            {
                // 搜索到多个结果
                if (list_match_album.Count == 1)
                {
                    // 如果专辑匹配且唯一
                    url = list_image[list_match_album[0]];
                }
                else if (list_match_artist.Count > 0)
                {
                    foreach (int i in list_match_album)
                    {
                        if (list_match_artist.Contains(i))
                        {
                            url = list_image[i];
                            break;
                        }
                    }
                    if (url.Length < 1)
                    {
                        foreach (int i in list_match_title)
                        {
                            if (list_match_artist.Contains(i))
                            {
                                url = list_image[i];
                                break;
                            }
                        }
                    }

                }
                if (url.Length < 1)
                {// 由于艺术家和专辑或title没有同时命中，取消对艺术家的判定
                    if (list_match_album.Count > 0)
                        url = list_image[list_match_album[0]];
                    else if (list_match_title.Count > 0)
                        url = list_image[list_match_title[0]];
                }
            }

            if (url.Length < 1)
                url = list_image[0];

            Console.WriteLine("select cover: " + url);
            return url;
        }
    }



}