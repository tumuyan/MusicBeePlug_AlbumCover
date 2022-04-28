using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MusicBeePlugin.Api
{
    // 豆瓣API v2，已经无法使用
    class DoubanApi:WebApi
    {

        public string getCover(String Artist, String Album)
        {
            if (Album.Replace(" ", "").Length < 1)
                return null;
            // 从API取回数据

            string base_url = "https://api.douban.com/v2/music/search?apiKey=054022eaeae0b00e0fc068c0c0a2102a&q=";

            List<string> list_url = new List<string>();

            if (!String.IsNullOrEmpty(Artist))
                list_url.Add(base_url + HttpUtility.UrlEncode(Album + " " + Artist, System.Text.UnicodeEncoding.GetEncoding("UTF-8")));

            list_url.Add(base_url + HttpUtility.UrlEncode(Album, System.Text.UnicodeEncoding.GetEncoding("UTF-8")));

            if (WebApi.matchMultWords(Album))
                list_url.Add(base_url + HttpUtility.UrlEncode(WebApi.prepareString(Album, true), System.Text.UnicodeEncoding.GetEncoding("UTF-8")));

            if (list_url.Count < 1)
                return null;

            SearchData data = new SearchData(Artist, Album);

            foreach (string SearchUrl in list_url)
            {
                Console.WriteLine("SearchUrl = \t" + SearchUrl);
                JObject SearchResult = WebApi.requestJObject(SearchUrl, null);//解析搜索结果
                JArray SongList = (JArray)SearchResult["musics"];//搜索结果曲目列表

                if (null == SongList)
                    continue;

                for (int i = 0; i < SongList.Count; i++)
                {
                    data.add(
                        (SongList[i]["image"] ?? "").ToString().ToLower().Replace("/s/public", "/public"),
                        (SongList[i]["attrs"]["tracks"] ?? "").ToString().ToLower(),
                        ((SongList[i]["attrs"]["singer"] ?? "").ToString() + (SongList[i]["attrs"]["publisher"] ?? "").ToString() + (SongList[i]["author"] ?? "").ToString()).ToLower(),
                        new string[] { (SongList[i]["title"] ?? "").ToString(), }
                    );
                }

            }
            return data.select();
        }
    }
}
