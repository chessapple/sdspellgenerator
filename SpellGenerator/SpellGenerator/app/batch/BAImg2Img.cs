using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellGenerator.app.file;

namespace SpellGenerator.app.batch
{
    public class BAImg2Img : BatchAction
    {

        public GenConfig genConfig;
        public GenImageInfo genImageInfo;
        public int batchCount;
        public string positivePrompt;
        public string negativePrompt;
        public BAImg2Img(GenConfig genConfig, GenImageInfo genImageInfo, int batchCount, string positivePrompt, string negativePrompt)
        {
            this.genConfig = genConfig;
            this.genImageInfo = genImageInfo;
            this.batchCount = batchCount;
            this.positivePrompt = positivePrompt;
            this.negativePrompt = negativePrompt;
        }

        public override string Description  { get => "生成图片";}

        public override async Task Run()
        {
            try
            {
                controller.imageResults = await AppCore.Instance.GetGenerateEngine().Img2ImgGen(genConfig, genImageInfo, 1, batchCount, positivePrompt, negativePrompt);
                success = true;
            }catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
    }
}
