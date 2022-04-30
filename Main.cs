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
        private String name = "QQ";
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private static CookieContainer myCookieContainer = new CookieContainer();


        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = name + " Music Artwork" + mode;
            about.Description = "Get album cover from " + name + " music.  " +
                "\n从QQ音乐获取专辑封面";
            about.Author = "Tumuyan";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.ArtworkRetrieval;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 1;
            about.Revision = 0;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            //           loadCookie("os=pc; osver=Microsoft-Windows-10-Professional-build-10586-64bit; appver=2.0.3.131777; channel=netease; __remember_me=true;NMTID=00Om_v;", "http://music.163.com");
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
            //  return new _163().getCover(Artist, album);
            //  return new vgmdb().getCover(Artist, album);
            return new qq().getCover(Artist, album);
            //  return new DoubanApi().getCover(Artist, album);
        }
    }

}