using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicBeePlugin
{
    class WebApi
    {


        // 专辑名需要预处理，降低由于标点符号差异造成的影响
        static public string prepareString(string s, bool replace_to_space)
        {
            Regex rgx = new Regex("[\\s\\]\\[\\(\\)`~!@#$%^&\\*()+=|{}':;',\\.<>/\\?~～〜（）「」［］！@#￥%……&*——+|{}【】‘；：”“’。，、？]+");
            if (replace_to_space)
                return rgx.Replace(s.ToLower(), " ");
            return rgx.Replace(s.ToLower(), "");
        }


        static public bool matchMultWords(string s)
        {
            Regex rgx = new Regex("[\\]\\[\\(\\)`~!@#$%^&\\*()+=|{}':;',\\.<>/\\?~～〜（）「」［］！@#￥%……&*——+|{}【】‘；：”“’。，、？]");
            string t = rgx.Replace(s.ToLower(), "");

            return s.Length > t.Length;
        }



        
        /// <summary>
        /// 请求url内容并解析为JsonObject
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="refer">refer</param>
        /// <returns></returns>
        static public JObject requestJObject(string url, string refer)
        {
            try
            {
                if (!String.IsNullOrEmpty(url))
                {
                    Console.WriteLine("request: " + url);
                    var request = (HttpWebRequest)WebRequest.Create(url);

                    request.CookieContainer = CookieHelper.get().GetCookieCollection();
                    //                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36 Edg/89.0.774.54";
                    if (!String.IsNullOrEmpty(refer))
                        request.Referer = refer;
                    request.Timeout = 10000;
                    request.ReadWriteTimeout = 10000;

                    var response = (HttpWebResponse)request.GetResponse();
                    var SearchString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    CookieHelper.get().addCookie(response.Cookies);

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





        /// <summary>
        /// 从取回的封面列表中匹配最合适的封面
        /// </summary>
        /// <param name="list_match_album">匹配到的专辑名列表</param>
        /// <param name="list_match_artist">匹配到的艺术家列表</param>
        /// <param name="list_match_title">匹配到的Title列表</param>
        /// <param name="list_image">封面列表</param>
        /// <returns></returns>
        static public string selectCover(List<int> list_match_album, List<int> list_match_artist, List<int> list_match_title, List<string> list_image)
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

            Console.WriteLine("ApiLoader select cover: " + url);
            return url;
        }




    }
}
