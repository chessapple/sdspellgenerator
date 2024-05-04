using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.tag
{


    public class TagData
    {
        public int strength = 0;
        public string? prompt;
        public string? colorprompt;
        public string? classify;
        public int tagType;
        public string? data;
    }
}
