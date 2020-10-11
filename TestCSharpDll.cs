using System;
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

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Douban Music Artwork";
            about.Description = "Get album cover from douban music.  " +
                "\n从豆瓣音乐获取专辑封面，使用了豆瓣音乐Api V2";
            about.Author = "Tumuyan";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.ArtworkRetrieval;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
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
                            "Douban"
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


            /*          MessageBox.Show(
                          "albumartist:" + albumArtist
                        + "\nalbum:" + album
                        + "\nprovider:" + provider
                        + "\nsource:" + sourceFileUrl
                        + "\nurl:" + url
                        , "plug debug info", MessageBoxButtons.YesNo, MessageBoxIcon.Question);


                       // 把图像从url取回并转换为base64字符串
                              string url = "https://img1.doubanio.com/view/subject/public/s28790429.jpg";
                              var request2 = (HttpWebRequest)WebRequest.Create(url);
                              byte[] bytes;
                              using (Stream stream = request2.GetResponse().GetResponseStream())
                              {
                                  using (MemoryStream mstream = new MemoryStream())
                                  {
                                      int count = 0;
                                      byte[] buffer = new byte[1024];
                                      int readNum = 0;
                                      while ((readNum = stream.Read(buffer, 0, 1024)) > 0)
                                      {
                                          count = count + readNum;
                                          mstream.Write(buffer, 0, readNum);
                                      }
                                      mstream.Position = 0;
                                      using (BinaryReader br = new BinaryReader(mstream))
                                      {
                                          bytes = br.ReadBytes(count);
                                      }
                                  }
                              }
                              return Convert.ToBase64String(bytes);
            */

        }

        // 专辑名需要预处理，降低由于标点符号差异造成的影响
        private string prepareString(string s) {
            Regex rgx = new Regex("[\\s\\]\\[\\(\\)`~!@#$%^&\\*()+=|{}':;',\\.<>/\\?~！@#￥%……&*——+|{}【】‘；：”“’。，、？]");
            return rgx.Replace(s.ToLower(), "");
        }


        private string getDoubanCover(String Artist, String Album)
        {
            // 从API取回数据

            string SearchUrl = String.Format("https://api.douban.com/v2/music/search?q={0} {1}", Artist, Album).Replace("&", "%26");

            var request = (HttpWebRequest)WebRequest.Create(SearchUrl);
            var response = (HttpWebResponse)request.GetResponse();
            var SearchString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            JObject SearchResult = JObject.Parse(SearchString);//解析搜索结果
            JArray SongList = (JArray)SearchResult["musics"];//搜索结果曲目列表

            if (SongList.Count < 1) {
                 SearchUrl = String.Format("https://api.douban.com/v2/music/search?q={0}", Album).Replace("&", "%26");
                request = (HttpWebRequest)WebRequest.Create(SearchUrl);
                response = (HttpWebResponse)request.GetResponse();
                SearchString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                SearchResult = JObject.Parse(SearchString);//解析搜索结果
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
                String title =prepareString((SongList[i]["title"]??"").ToString());
                if (title.Contains(album) || album.Contains(title))
                    list_match_album.Add(i);

                //musics.[1].attrs.tracks
                if ((SongList[i]["attrs"]["tracks"]??"").ToString().ToLower().Contains(album))
                    list_match_title.Add(i);

                //musics.[1].attrs.singer
                if (Artist.Length > 0)
                {
                    // 如果有多个艺术家（虽然这不规范），只匹配第一个
                    if ((SongList[i]["attrs"]["singer"]??"").ToString().ToLower().Contains(Artist.ToLower())
                        || (SongList[i]["attrs"]["publisher"]??"").ToString().ToLower().Contains(Artist.ToLower())
                        || (SongList[i]["author"]??"").ToString().ToLower().Contains(Artist.ToLower())
                        )
                        list_match_artist.Add(i);
                }
            }

            return selectCover(list_match_album, list_match_artist, list_match_title, list_image);
 
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
            return url;
        }
    }



}