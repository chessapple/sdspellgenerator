using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpellGenerator.app
{
    public class GenImageInfo
    {
        public long seed;
        public byte[]? imageData;
        public BitmapImage? image { get; set; }
        public string imageType;
        public string defaultFileName;
    }
}
