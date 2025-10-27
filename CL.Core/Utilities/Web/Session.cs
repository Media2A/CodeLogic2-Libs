using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        // Session Strings
        public static string GetSession(string SessionKey)
        {
            var SessionContent = CLU_Web.HC().Session.GetString(SessionKey);
            return SessionContent;
        }

        public static void SetSession(string SessionKey, string SessionValue)
        {
            CLU_Web.HC().Session.SetString(SessionKey, SessionValue);
        }

        // Session Objects
        public static void SetSessionObject<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            CLU_Web.HC().Session.SetString(key, json);
        }

        public static T GetSessionObject<T>(string key)
        {
            var value = CLU_Web.HC().Session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public static void RemoveSession(string SessionKey)
        {
            CLU_Web.HC().Session.Remove(SessionKey);
        }
        public static void ClearSession()
        {
            CLU_Web.HC().Session.Clear();
        }
        public static string GetSessionID()
        {
            return CLU_Web.HC().Session.Id.ToString();
        }
    }
}
