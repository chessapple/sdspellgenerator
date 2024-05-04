using SpellGenerator.app.engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.config
{
    public class WebUIConfig
    {
        public int txt2Img = -1;
        public GradioCall? txt2ImgCall = null;
        public int txt2ImgProgressStart = -1;
        public int txt2ImgProgress = -1;
        public int txt2ImgInterrupt = -1;
        public int img2Img = -1;
        public GradioCall? img2ImgCall = null;
        public int img2ImgProgressStart = -1;
        public int img2ImgProgress = -1;
        public int img2ImgInterrupt = -1;
        public int refreshModels = -1;
        public int selectModel = -1;
        public int refreshExtraModels = -1;
        public int deepbooru = -1;
        public int txt2ImgParamsCount = -1;
        public int img2ImgParamsCount = -1;
    }
}
