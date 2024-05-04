using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.tag
{
    public class TagGroupInfo
    {
        public string name { get; set; } = "";
        public List<TagInfo> tags = new List<TagInfo>();
        public ClassifyInfo? classify;
        public int order;
    }
}
