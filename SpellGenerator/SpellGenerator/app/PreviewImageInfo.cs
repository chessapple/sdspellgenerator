using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;

namespace SpellGenerator.app
{
    public class PreviewImageInfo
    {
        public string path;
        private BitmapImage s;
        public BitmapImage source
        {
            get
            {
                if(s == null)
                {
                    try
                    {
                        using (FileStream fs = File.OpenRead(path))
                        {
                            byte[] imageData = new byte[fs.Length];
                            var bitmap = new BitmapImage();
                            fs.Read(imageData, 0, System.Convert.ToInt32(fs.Length));
                            bitmap.BeginInit();
                            bitmap.StreamSource = new System.IO.MemoryStream(imageData);
                            bitmap.EndInit();
                            s = bitmap;
                        };

                    }
                    catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
                }
                return s;
            }
        }

        public PreviewImageInfo(string path)
        {
            this.path = path;
        }
    }
}
