using System;
using System.Net;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {

        public static bool SetHttpHeader(string key, string value)
        {
            HC().Response.Headers[key] = value;

            if (HC().Request.Headers[key] == value)
            {
                return true;
            }

            return false;
        }
        public static string GetHttpHeader(string key, string value)
        {
            var header = HC().Request.Headers[key];

            return header;
        }
    }
}
