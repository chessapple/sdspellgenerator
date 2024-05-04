using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpellGenerator.app.batch
{
    public class BASaveImages : BatchAction
    {
        public List<GenImageInfo> images;
        public long startSeed;
        public string modelName;
        public string samplingMethodName;
        public string rootPath;
        public BASaveImages(List<GenImageInfo> images, long startSeed, string modelName, string samplingMethodName, string rootPath)
        {
            this.images = images;
            this.startSeed = startSeed;
            this.modelName = modelName;
            this.samplingMethodName = samplingMethodName;
            this.rootPath = rootPath;
        }

        public override string Description  { get => "保存图片";}

        public override async Task Run()
        {
            try
            {
                string vModelName = modelName.Replace("(", "").Replace(")", "").Replace(" ", "_");
                string vSamplingMethodName = samplingMethodName.Replace("(", "").Replace(")", "").Replace(" ", "_").Replace("+", "p");
                for(int i=0; i<images.Count; i++)
                {
                    string fileName = Path.Combine(rootPath, vModelName + "_" + vSamplingMethodName + "_" + (startSeed + i) + ".png");
                    using (FileStream fs = File.Open(fileName, FileMode.Create))
                    {
                        fs.Write(images[i].imageData, 0, images[i].imageData.Length);
                    }
                }
                
                success = true;
            }catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
    }
}
