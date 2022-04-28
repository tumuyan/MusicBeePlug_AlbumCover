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
        string base_url = "https://c.y.qq.com/soso/fcgi-bin/client_search_cp?ct=24&qqmusic_ver=1298&remoteplace=txt.yqq.album&aggr=0&lossless=0&sem=10&t=8&p=1&n=20&format=json&inCharset=utf8&outCharset=utf-8&platform=yqq.json&w=";

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

                JObject SearchResult = requestJObject(SearchUrl, "https://y.qq.com/");

                if (null != SearchResult["data"]["album"]["list"])
                {
                    SongList = (JArray)SearchResult["data"]["album"]["list"];//搜索结果曲目列表

                    for (int i = 0; i < SongList.Count; i++)
                    {
                        //data.album.list[4].albumPic
                        //http://y.gtimg.cn/music/photo_new/T002R180x180M000001ddOOX26S67A_1.jpg

                        String s = SongList[i]["albumPic"].ToString();
                        if (s.Replace(" ", "").Length < 10)
                            continue;

                        list_image.Add(s.Replace("180x180", "800x800"));

                        // data.album.list[1].albumName
                        String title = prepareString((SongList[i]["albumName"] ?? "").ToString(),false);
                        if (title.Contains(album) || album.Contains(title))
                            list_match_album.Add(i);

                        //data.album.list[1].catch_song (虽然有这个节点，但是大量专辑没有内容）
                        if ((SongList[i]["catch_song"] ?? "").ToString().ToLower().Contains(album))
                            list_match_title.Add(i);

                        //data.album.list[4].singer_list[1]  data.album.list[1].singerName
                        if (Artist.Length > 0)
                        {
                            // 如果有多个艺术家（虽然这不规范），只匹配第一个。而检索结果对应了专辑艺术家、参与艺术家，QQ音乐没有提供发行方信息
                            if ((SongList[i]["singer_list"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                                || (SongList[i]["singer_list"] ?? "").ToString().ToLower().Contains(Artist.ToLower())
                                )
                                list_match_artist.Add(i);
                        }
                    }

                }
            }

            return selectCover(list_match_album, list_match_artist, list_match_title, list_image);
        }

    }
}
