
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MusicBeePlugin
{

    class CookieHelper
    {
        private static CookieContainer myCookieContainer;
        private static CookieHelper self;
        public CookieHelper()
        {
            myCookieContainer = new CookieContainer();
        }
        public static CookieHelper get()
        {
            if (self == null) self = new CookieHelper();
            return self;
        }

        public void addCookie(Cookie cookie)
        {
            myCookieContainer.Add(cookie);
        }
        public void addCookie(CookieCollection cookies)
        {
            myCookieContainer.Add(cookies);
        }
        public void addCookie(Uri domain_uri, Cookie cookie)
        {
            myCookieContainer.Add(cookie);
        }
        public CookieContainer GetCookieCollection()
        {
            return myCookieContainer;
        }



        public void loadCookie(string default_cookie, string domain, string configPath)
        {
            Uri domain_uri = new Uri(domain);
            //       string configPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), about.Name);
            string cookie = default_cookie;
            if (File.Exists(configPath))
            {

                try
                {
                    cookie = cookie + (File.ReadAllText(configPath, System.Text.Encoding.UTF8));

                }
                catch (Exception ex)
                {
                    //      mbApiInterface.MB_Trace(about.Name + " Failed to load config" + ex);
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

                        CookieHelper.get().addCookie(domain_uri, new Cookie(cookieKey, cookieValue));

                    }

                }

            }
        }

        public void saveCookie(string site, string configPath)
        {

            //           string configPath = Path.Combine(mbApiInterface.Setting_GetPersistentStoragePath(), about.Name);
            //  if (File.Exists(configPath))
            {
                try
                {
                    var cookies = myCookieContainer.GetCookies(new Uri(site));
                    string tmp = "";
                    foreach (Cookie cookie in cookies)
                    {
                        tmp = tmp + cookie.ToString() + ";";
                    }
                    File.WriteAllText(configPath, tmp);
                    Console.WriteLine("saveCookie path=" + configPath + "; site=" + site);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

    }
}
