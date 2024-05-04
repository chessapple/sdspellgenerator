using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellGenerator.app.tag;

namespace SpellGenerator.app.file
{
    public class SpellFile
    {
        public GenConfig? config;
        public string? positiveDefaultSpell;
        public string? negativeSpell;
        public List<TagData>? tags;
        public List<TagData>? negativeTags;
    }
}
