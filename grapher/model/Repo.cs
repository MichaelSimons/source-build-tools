using System;

namespace Grapher.Model
{
    public class Repo {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool NonArcade { get; set; }
        public bool IsExternal { get; set; }
    }
}