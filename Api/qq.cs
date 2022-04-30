using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace MusicBeePlugin.Api
{
    class qq:WebApi
    {
        string base_url = "https://c.y.qq.com/soso/fcgi-bin/search_for_qq_cp?g_tk=5381&uin=0&format=jsonp&inCharset=utf-8&outCharset=utf-8&notice=0&platform=h5&needNewCode=1&t=8&ie=utf-8&sem=1&aggr=0&perpage=20&n=20&p=1&w=";
        public string getCover(String Artist, String Album)
        {
            if (Album.Replace(" ", "").Length < 1)
                return null;


            string[] SearchUrls;
            if (String.IsNullOrEmpty(Artist))
            {
                SearchUrls = new string[] { base_url + HttpUtility.UrlEncode(Album) };
            }

            else
            {
                SearchUrls = new string[] {
                    base_url + HttpUtility.UrlEncode(Album) + HttpUtility.UrlEncode(" " + Artist) ,
                    base_url + HttpUtility.UrlEncode(Album) };
            }


            JArray SongList = new JArray();//搜索结果曲目列表

            List<int> list_match_album = new List<int>();
            List<int> list_match_artist = new List<int>();
            List<int> list_match_title = new List<int>();
            List<string> list_image = new List<string>();

            string album = prepareString(Album,false);

            foreach (string SearchUrl in SearchUrls)
            {
                Regex regex = new Regex("^callback\\((.*)\\)$");
                String SearchResultStr = regex.Replace(requestString(SearchUrl, "https://c.y.qq.com/"), "$1");                    ;

                JObject SearchResult = JObject.Parse(SearchResultStr);

                if (null != SearchResult["data"]["album"]["list"])
                {
                    SongList = (JArray)SearchResult["data"]["album"]["list"];//搜索结果曲目列表

                    for (int i = 0; i < SongList.Count; i++)
                    {
                        // https://y.qq.com/music/photo_new/T002R800x800M000001cQG9d29dpgv_1.jpg?max_age=2592000
                        // https://y.qq.com/music/photo_new/T002R800x800M000001cQG9d29dpgv.jpg

                        String s = SongList[i]["albumMID"].ToString();
                        if (s.Replace(" ", "").Length < 10)
                            continue;

                        list_image.Add("https://y.qq.com/music/photo_new/T002R800x800M000"+s+".jpg");

                        String title = prepareString((SongList[i]["albumName"] ?? "").ToString(),false);
                        if (title.Contains(album) || album.Contains(title))
                            list_match_album.Add(i);

                        /* 已经无了
                        //data.album.list[1].catch_song (虽然有这个节点，但是大量专辑没有内容）
                        if ((SongList[i]["catch_song"] ?? "").ToString().ToLower().Contains(album))
                            list_match_title.Add(i);*/


                        if (Artist.Length > 0)
                        {
                            // 如果有多个艺术家（虽然这不规范），只匹配第一个。
                            if ((SongList[i]["singerName"] ?? "").ToString().ToLower().Contains(Artist.ToLower()))
                                list_match_artist.Add(i);
                        }
                    }

                }
            }

            return selectCover(list_match_album, list_match_artist, list_match_title, list_image);
        }

    }
}
