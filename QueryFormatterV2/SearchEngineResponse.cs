using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SupportAgent
{
    public class SearchEngineResponse
    {
        public bool success { get; set; }
        public List<Doc> docs;

    }

    public class Doc
    {
        public string UID { get; set; }
        public string URL { get; set; }
        public string Level1_Display { get; set; }
        public string Level2_Display { get; set; }
        public string Answer { get; set; }
        public List<string> Product { get; set; }
        public List<string> Keyword { get; set; }
        public string Level1 { get; set; }
        public string _version_ { get; set; }
        public float score { get; set; }
        public float titleScore { get; set; }

    }
}