using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.batch
{
    public class BAlgorithmData
    {
        public SamplingMethod method;
        public string name { get => method.webUIName; }
        public bool check { get; set; }
    }
}
