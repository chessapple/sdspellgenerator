using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.file
{
    public class GenConfig
    {
        public string engineType;
        public int width;
        public int height;
        public int samplingSteps;
        public string samplingMethod;
        public long seed;
        public float cfgScale;
        public float denoisingStrength;
        public bool autoGroup;
        public bool highResFix = false;
        public float upscaleBy = 1;
        public string upscaler;
        public string vae;

        public void CopyFrom(GenConfig other)
        {
            engineType = other.engineType;
            width = other.width;
            height = other.height;
            samplingSteps = other.samplingSteps;
            samplingMethod = other.samplingMethod;
            seed = other.seed;
            cfgScale = other.cfgScale;
            denoisingStrength = other.denoisingStrength;
            autoGroup = other.autoGroup;
            highResFix = other.highResFix;
            upscaleBy = other.upscaleBy;
            upscaler = other.upscaler;
            vae = other.vae;
        }
    }
}
