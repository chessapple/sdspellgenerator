using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellGenerator.app.file;

namespace SpellGenerator.app.batch
{
    public class BATxt2Img : BatchAction
    {

        public GenConfig genConfig;
        public int batchCount;
        public string positivePrompt;
        public string negativePrompt;
        public BATxt2Img(GenConfig genConfig, int batchCount, string positivePrompt, string negativePrompt)
        {
            this.genConfig = genConfig;
            this.batchCount = batchCount;
            this.positivePrompt = positivePrompt;
            this.negativePrompt = negativePrompt;
        }

        public override string Description  { get => "生成图片";}

        public override async Task Run()
        {
            try
            {
                controller.imageResults = await AppCore.Instance.GetGenerateEngine().Txt2ImgGen(genConfig, 1, batchCount, positivePrompt, negativePrompt);
                success = true;
            }catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
    }
}
