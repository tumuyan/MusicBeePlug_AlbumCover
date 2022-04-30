using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicBeePlugin
{

    // 保存搜索结果并从中筛选
    class SearchData
    {
        List<int> list_match_album = new List<int>();
        List<int> list_match_artist = new List<int>();
        List<int> list_match_title = new List<int>();
        List<string> list_image = new List<string>();

        string Artist = "";

        // 专辑原名,小写
        string Album = "";
        // 特殊符号替换为空格
        string Album1 = "";
        // 特殊符号去除
        string Album2 = "";



        public SearchData(string artist, string album)
        {
            if (null != artist)
                Artist = artist.ToLower();

            if (null != album)
            {
                Album = album.ToLower();
                Album1 = WebApi.prepareString(Album, true);
                Album2 = Album1.Replace(" ", "");
            }

        }



        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="_image">专辑图片url</param>
        /// <param name="_track">专辑内音乐列表</param>
        /// <param name="_artist">艺术家列表</param>
        /// <param name="_album">专辑名列表</param>
        public void add(string _image, string _track, string _artist, string[] _album)
        {
            if (_image.Replace(" ", "").Length < 10)
                return;

            if (list_image.Contains(_image))
                return;

            int index = list_image.Count;

            list_image.Add(_image);

            string s = WebApi.prepareString(_track, false);
            if (s.Contains(Album2) || Album2.Contains(s))
                list_match_album.Add(index);

            foreach (string album in _album)
            {
                s = WebApi.prepareString(album, false);

                if (s.Contains(Album2) || Album2.Contains(s))
                {
                    list_match_title.Add(index);
                    break;
                }
            }

            if (_artist.Contains(Artist))
                list_match_artist.Add(index);

        }

        public string select()
        {
            return WebApi.selectCover(list_match_album, list_match_artist, list_match_title, list_image);
        }

    }


}
