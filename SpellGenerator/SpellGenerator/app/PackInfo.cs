using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app
{
    public class PackInfo
    {
        public int order { get; set; }
        public string? location { get; set; }
        public string? name { get; set; }

        public PackInfo(int order, string? location, string? name)
        {
            this.order = order;
            this.location = location;
            this.name = name;
        }
    }
}
