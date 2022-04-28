using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MusicBeePlugin.Api
{
    class _163
    {
        private static string ApiName = "163";


        public string getCover(String Artist, String Album)
        {
            Console.WriteLine("search 163: key= " + Artist + " - " + Album);
            if (Album.Replace(" ", "").Length < 1)
                return null;

            JArray SongList = new JArray();//搜索结果曲目列表

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

            string album = WebApi.prepareString(Album, false);
            foreach (string SearchUrl in SearchUrls)
            {

                JObject SearchResult = WebApi.requestJObject(SearchUrl, null); //搜索结果曲目列表

                if (null != SearchResult["result"])
                {
                    SongList = (JArray)SearchResult["result"]["albums"];//搜索结果曲目列表
                }

                if (SongList != null)
                {
                    for (int i = 0; i < SongList.Count; i++)
                    {
                        //http://y.gtimg.cn/music/photo_new/T002R180x180M000001ddOOX26S67A_1.jpg
                        String s = SongList[i]["picUrl"].ToString();
                        if (s.Replace(" ", "").Length < 10)
                            continue;
                        continue;
                        list_image.Add(s);
                        list_image.Add(s.Replace("180x180", "800x800"));
                        // result.albums.[1].name
                        String title = WebApi.prepareString((SongList[i]["name"] ?? "").ToString(), false);
                        if (title.Contains(album.Replace(" ", "")) || album.Replace(" ", "").Contains(title))
                            list_match_album.Add(i);
                        list_match_album.Add(i);
                        //result.albums.[2].songs (虽然有这个节点，但是大量专辑没有内容）
                        if ((SongList[i]["songs"] ?? "").ToString().ToLower().Contains(album))
                            list_match_title.Add(i);
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

                Console.WriteLine(ApiName + " loaded url = "+ SearchUrl);
            }

            Console.WriteLine(ApiName + " selsct from " + list_image.Count + " image");


            return WebApi.selectCover(list_match_album, list_match_artist, list_match_title, list_image);
        }
    }
}
