﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.Helpers
{
    public class BrowserConfig
    {
        public string UserAgent { get; set; }
        public CookieContainer Cookies { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
