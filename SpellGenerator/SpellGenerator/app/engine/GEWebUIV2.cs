using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellGenerator.app.file;

namespace SpellGenerator.app.engine
{
    public class GEWebUIV2 : GEWebUIV1
    {
        protected override List<object> GetImg2ImgParam(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            return new List<object>(new object[]
            {
                    0, // mode
                    positivePrompt, // prompt
                    negativePrompt, // negative_prompt
                    "None", // prompt_style
                    "None", // prompt_style2
                    "data:image/"+refImageInfo.imageType+";base64,"+Convert.ToBase64String(refImageInfo.imageData), // init_img
                    null, // init_img_with_mask
                    null, // init_img_with_mask_orig
                    null, // init_img_inpaint
                    null, // init_mask_inpaint
                    "Draw mask", // mask_mode
                    genConfig.samplingSteps, // steps
                    genConfig.samplingMethod, // sampler_index
                    4, // mask_blur
                    1, // mask_alpha
                    "fill", // inpainting_fill
                    false, // restore_faces
                    false, // tiling
                    batchCount, // n_iter
                    batchSize, // batch_size
                    genConfig.cfgScale, // cfg_scale
                    genConfig.denoisingStrength, // denoising_strength
                    genConfig.seed, // seed
                    -1, // subseed
                    0, // subseed_strength
                    0, // seed_resize_from_h
                    0, // seed_resize_from_w
                    false, // seed_enable_extras
                    genConfig.height, // height
                    genConfig.width, // width
                    "Crop and resize", // resize_mode
                    false, // inpaint_full_res
                    32, // inpaint_full_res_padding
                    "Inpaint masked", // inpainting_mask_invert
                    "", // img2img_batch_input_dir
                    "", // img2img_batch_output_dir
                    "None" // script


            });
        }
    }
}
