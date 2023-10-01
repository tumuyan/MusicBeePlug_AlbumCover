//#define CONFIG_API_SERVER
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
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using MusicBeePlugin.Api;

namespace MusicBeePlugin
{

    public partial class Plugin
    {

#if DEBUG
        string mode = " Debug";
#else
        string mode = "";
# endif
        private String name = "163";
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private static CookieContainer myCookieContainer = new CookieContainer();
        private string api_server = "";


        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = name + " Music Artwork" + mode;
            about.Description = "Get album cover from " + name + " music.  " +
                "\n从网易云音乐获取专辑封面";
            about.Author = "Tumuyan";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.ArtworkRetrieval;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 1;
            about.Revision = 0;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
#if CONFIG_API_SERVER
            about.ConfigurationPanelHeight = 40;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            loadConfig();
#else
            about.ConfigurationPanelHeight = 0;
             //           loadCookie("os=pc; osver=Microsoft-Windows-10-Professional-build-10586-64bit; appver=2.0.3.131777; channel=netease; __remember_me=true;NMTID=00Om_v;", "http://music.163.com");      
#endif
            return about;
        }

        private string loadConfig()
        {
           // string api_server;
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath() + "VGMdb_album_Config.conf";
            if (File.Exists(dataPath))
                api_server = File.ReadAllText(dataPath);
            else
                api_server = "http://vgmdb.info";
            Console.WriteLine("Load api_server=" + api_server);
            return api_server;
        }
       

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            //string dataPath = mbApiInterface.Setting_GetPersistentStoragePath() + "VGMdb_album_Config.conf";
            //if (File.Exists(dataPath))
            //    api_server = File.ReadAllText(dataPath);
            //else
            //    api_server = "http://vgmdb.info";

            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
#if CONFIG_API_SERVER
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 6);
                prompt.Text = "API Server:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(prompt.Width+30, 0, 300, textBox.Height);
                textBox.Text =  api_server;
                textBox.TextChanged += new EventHandler(textBox_TextChanged); // 绑定 TextChanged 事件
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
#endif
            }
            return false;
        }


        
    private void textBox_TextChanged(object sender, EventArgs e)
    {
        TextBox textBox = (TextBox)sender;
        string oldValue = textBox.Tag?.ToString() ?? "";
        api_server = textBox.Text;
       // textBox.Tag = api_server;
        Console.WriteLine($"api_server changed \"{oldValue}\" -> \"{api_server}\"");
    }


    // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
    // its up to you to figure out whether anything has changed and needs updating
    public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
#if CONFIG_API_SERVER
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath() + "VGMdb_album_Config.conf";
            File.WriteAllText(dataPath, api_server);
#endif
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            //           saveCookie("http://music.163.com");
            //           Console.WriteLine("close "+reason);
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
            return new string[] { name + mode };
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            // 预处理输入参数，只保留一个艺术家
            string[] artists = albumArtist.Split(';', ',', '\\', '/', '&');
            string Artist = "";

            // 似乎没用上
            string artist = "";

            foreach (string s1 in artists)
            {// Remove Various Artists
                string s = s1.ToLower().Trim().Replace(" ", "");
                if (s.Equals("variousartist") || s.Equals("variousartists") || s.Equals("群星") || s.Length < 1)
                    continue;
                artist = artist + " " + s1;
                if (Artist.Length < 1)
                    Artist = s1;
            }
            // 专辑名称同样需要预处理
            Console.WriteLine("RetrieveArtwork Provider = " + provider + ", Artist = " + Artist + ", album = " + album);
            return new _163().getCover(Artist, album);
            //  return new vgmdb().getCover(Artist, album, api_server);
            //    return new qq().getCover(Artist, album);
            //  return new DoubanApi().getCover(Artist, album);
        }
    }

}