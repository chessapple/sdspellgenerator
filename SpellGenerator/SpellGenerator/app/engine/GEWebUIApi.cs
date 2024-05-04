using Newtonsoft.Json;
using SpellGenerator.app.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using SpellGenerator.app.api;
using System.Windows.Markup;
using SpellGenerator.app.file;
using SpellGenerator.app.controller;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Windows.Media.Media3D;
using SpellGenerator.app.tag;
using System.Dynamic;

namespace SpellGenerator.app.engine
{
    public class GEWebUIApi : GenerateEngine
    {
        private HttpClient httpClient = new HttpClient();

        private bool baseDataLoaded = false;

        private WebUIConfig? config;
        private WebUICApiConfig? apiConfig;

        private List<SamplingMethod> samplingMethods = new List<SamplingMethod>();
        private List<string> upscalers = new List<string>();
        private List<string> vaes = new List<string>(new string[] { "默认" });
        private List<string> models = new List<string>();
        public string? selectedModel;

        public static List<string> BASE_UPSCALERS = new List<string>(new string[]
{
            "None",
            "Lanczos",
            "Nearest",
            "ESRGAN_4x",
            "LDSR",
            "ScuNET GAN",
            "ScuNET PSNR",
            "SwinIR_4x"
});


        public GEWebUIApi()
        {
            httpClient.Timeout = TimeSpan.FromMinutes(180);

            samplingMethods.AddRange(SamplingMethod.samplingMethods);
            upscalers.AddRange(BASE_UPSCALERS);
        }

        void MessageApi()
        {
            MessageBox.Show("Api模式不支持该项操作。");
        }

        public override string GetEngineName()
        {
            return "Web UI Api";
        }

        public override void LoadApi(string version)
        {
            base.LoadApi(version);
        }

        public override List<SamplingMethod> GetSamplingMethods()
        {
            return samplingMethods;
        }

        public override void Txt2Img(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            _ = Txt2ImgWebPost(genConfig, batchSize, batchCount, positivePrompt, negativePrompt);

        }

        public override void Img2Img(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            _ = Img2ImgWebPost(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);
        }

        public override void DeepDanbooru(GenImageInfo refImageInfo)
        {
            _ = DeepDanbooruWebPost(refImageInfo);
        }

        public override void FetchModels()
        {
            _ = FetchModelsWebPost();
        }

        public override void FetchExtraModels()
        {
            _ = FetchExtraModelsWebPost();
        }

        public override void ChooseModel(string modelName)
        {
            _ = ChooseModelWebPost(modelName);
        }

        public override void Txt2ImgInterrupt()
        {
            _ = Txt2ImgInterruptWebPost();
        }

        public override void Img2ImgInterrupt()
        {
            _ = Img2ImgInterruptWebPost();
        }

        string GetApiBase()
        {
            return "http://" + host + ":" + port + "/sdapi/v1/";
        }

        void MessageConnectError()
        {
            MessageBox.Show("连接webui后台错误，请确认webui是否已启动。如已启动，请联系开发者。");
        }

        void MessageNoInterface()
        {
            MessageBox.Show("找不到接口，可能与当前webui版本不兼容，请联系开发者。");
        }

        void MessageDeepDanbooru()
        {
            MessageBox.Show("DeepDanbooru未启用，请用--deepdanbooru参数启动webui。");
        }

        void MessageSamplingMethod()
        {
            MessageBox.Show("所选采样算法未被当前webui支持，请重新选择。");
        }

        async Task<dynamic> CallApiPost(string api, object data)
        {
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(GetApiBase()+ api, httpContent);
            var content = await response.Content.ReadAsStringAsync();
            //System.Diagnostics.Debug.WriteLine(content);
            var resultData = JsonConvert.DeserializeObject<dynamic>(content);
            httpContent.Dispose();
            return resultData;
        }

        async Task<dynamic> CallApiGet(string api)
        {
            var response = await httpClient.GetAsync(GetApiBase()+api);
            var content = await response.Content.ReadAsStringAsync();
            //System.Diagnostics.Debug.WriteLine(content);
            var resultData = JsonConvert.DeserializeObject<dynamic>(content);
            return resultData;
        }

        async Task<dynamic> CallApiGetSafeAndCustom(string api)
        {
            try
            {
                var response = await httpClient.GetAsync("http://" + host + ":" + port + "/sp/" + api);
                var content = await response.Content.ReadAsStringAsync();
                //System.Diagnostics.Debug.WriteLine(content);
                var resultData = JsonConvert.DeserializeObject<dynamic>(content);
                return resultData;
            }catch
            {
                return null;
            }
        }

        async Task CheckBaseData()
        {
            if (baseDataLoaded)
            {
                return;
            }

            var samplers = await CallApiGet("samplers");
            List<SamplingMethod> methods = new List<SamplingMethod>();
            foreach (var sampler in samplers)
            {
                string methodName = sampler.name;
                SamplingMethod method = SamplingMethod.GetSamplingMethod(methodName);
                if (method == null)
                {
                    method = new SamplingMethod(methodName, methodName.Replace(" ", "_").ToLower());
                }
                methods.Add(method);
            }
            samplingMethods = methods;
            (Application.Current.MainWindow as MainWindow).RefreshSamplingMethods();

            var upscalers = await CallApiGet("upscalers");
            List<string> upscalerNames = new List<string>();
            foreach (var upscaler in upscalers)
            {
                string upscalerName = upscaler.name;
                upscalerNames.Add(upscalerName);
            }
            upscalers = upscalerNames;

            var options = await CallApiGet("options");
            string model = options.sd_model_checkpoint;

            var models = await CallApiGet("sd-models");
            List<string> choices = new List<string>();
            foreach (var m in models)
            {
                string modelName = m.title;
                choices.Add(modelName);
            }
            this.models = choices;

            selectedModel = model;

            (Application.Current.MainWindow as MainWindow).RefreshModels();

            var embeddings = await CallApiGet("embeddings");
            Newtonsoft.Json.Linq.JObject v = embeddings.loaded;
            foreach (var embedding in v.Properties())
            {
                TagInfo tag = TagInfo.CreateTextInversionTag(embedding.Name);
                tiGroup.tags.Add(tag);
            }
            var hypernetworks = await CallApiGet("hypernetworks");
            foreach (var hypernetwork in hypernetworks)
            {
                string hypernetworkName = hypernetwork.name;
                TagInfo tag = TagInfo.CreateHypernetworkTag(hypernetworkName);
                hyperGroup.tags.Add(tag);
            }
            var loras = await CallApiGetSafeAndCustom("loras");
            if(loras != null)
            {
                foreach (var lora in loras)
                {
                    string loraName = lora.name;
                    TagInfo tag = TagInfo.CreateLoraTag("", loraName);
                    loraGroup.tags.Add(tag);
                }
            }
            var vaes = await CallApiGetSafeAndCustom("vaes");
            if (vaes != null)
            {
                this.vaes.Clear();
                foreach (var vae in vaes)
                {                 
                    string vaeName = vae.name;
                    this.vaes.Add(vaeName);
                }
                this.vaes.Insert(0, "默认");
            }
            AppCore.Instance.LoadEngineFunctionGroup();
            (Application.Current.MainWindow as MainWindow).UpdateTagGroups();

            baseDataLoaded = true;
            AppController.Instance.InvokeOnEngineBaseDataLoadedChange();
        }

        public async Task FetchModelsWebPost()
        {
            try
            {
                await CheckBaseData();

                await CallApiPost("refresh_checkpoints", new { });

                var models = await CallApiGet("sd-models");
                List<string> choices = new List<string>();
                foreach (var m in models)
                {
                    string modelName = m.title;
                    choices.Add(modelName);
                }
                models = choices;

                (Application.Current.MainWindow as MainWindow).RefreshModels();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                MessageConnectError();
            }


        }

        public async Task Txt2ImgInterruptWebPost()
        {
            try
            {
                await CheckBaseData();

                await CallApiPost("interrupt", new { });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                MessageConnectError();
            }


        }

        public async Task Img2ImgInterruptWebPost()
        {
            try
            {
                await CheckBaseData();

                await CallApiPost("interrupt", new { });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                MessageConnectError();
            }


        }

        public async Task FetchExtraModelsWebPost()
        {
            try
            {
                await CheckBaseData();

                tiGroup.tags.Clear();
                hyperGroup.tags.Clear();
                loraGroup.tags.Clear();

                var hypernetworks = await CallApiGet("hypernetworks");
                foreach (var hypernetwork in hypernetworks)
                {
                    string hypernetworkName = hypernetwork.name;
                    TagInfo tag = TagInfo.CreateHypernetworkTag(hypernetworkName);
                    hyperGroup.tags.Add(tag);
                }
                AppCore.Instance.LoadEngineFunctionGroup();
                (Application.Current.MainWindow as MainWindow).UpdateTagGroups();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                MessageConnectError();
            }


        }

        List<object> MakeData(List<object> source, Dictionary<string, object> values)
        {
            List<object> data = new List<object>();
            for(int i=0; i< source.Count; i++)
            {
                if (source[i] != null && source[i] is string && values.ContainsKey((string)source[i]))
                {
                    data.Add(values[(string)source[i]]);
                }
                else
                {
                    data.Add(source[i]);
                }
            }
            return data;
        }

        object GetSettings(GenConfig genConfig)
        {
            dynamic settings = new ExpandoObject();
            if (genConfig.vae != null && genConfig.vae != "" && genConfig.vae != "默认")
            {
                settings.sd_vae = genConfig.vae;
            }
            return settings;
        }

        List<GenImageInfo> FetchImages(dynamic resultData)
        {
            List<GenImageInfo> images = new List<GenImageInfo>();
            string info = resultData.info;
            var extrJsonObj = JsonConvert.DeserializeObject<dynamic>(info);
            //System.Diagnostics.Trace.WriteLine(info);
            int imageCount = resultData.images.Count;
            string prefix = "img_" + new Random().Next(1000);
            for (int i = 0; i < imageCount; i++)
            {

                long seed = extrJsonObj.all_seeds[i];
                string imageStr = resultData.images[i];
                byte[] imageData = Convert.FromBase64String(imageStr);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(imageData);
                bitmap.EndInit();
                GenImageInfo imageInfo = new GenImageInfo();
                imageInfo.seed = seed;
                imageInfo.imageData = imageData;
                imageInfo.image = bitmap;
                imageInfo.imageType = "png";
                imageInfo.defaultFileName = prefix + "_" + seed + ".png";
                images.Add(imageInfo);
            }
            return images;
        }

        public override async Task<List<GenImageInfo>> Txt2ImgGen(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            var data = new
            {
                enable_hr = genConfig.highResFix,
                denoising_strength = genConfig.denoisingStrength,
                firstphase_width = 0,
                firstphase_height = 0,
                hr_scale = genConfig.upscaleBy,
                hr_upscaler = genConfig.upscaler,
                hr_second_pass_steps = 0,
                hr_resize_x = 0,
                hr_resize_y = 0,
                prompt = positivePrompt,
                styles = new List<string>(),
                seed = genConfig.seed,
                subseed = -1,
                subseed_strength = 0,
                seed_resize_from_h = -1,
                seed_resize_from_w = -1,
                sampler_name = genConfig.samplingMethod,
                batch_size = batchSize,
                n_iter = batchCount,
                steps = genConfig.samplingSteps,
                cfg_scale = genConfig.cfgScale,
                width = genConfig.width,
                height = genConfig.height,
                restore_faces = false,
                tiling = false,
                negative_prompt = negativePrompt,
                eta = 1,
                s_churn = 0,
                s_tmax = 0,
                s_tmin = 0,
                s_noise = 1,
                override_settings = GetSettings(genConfig),
                override_settings_restore_afterwards = true,
                script_args = new List<object>(),
                sampler_index = genConfig.samplingMethod
            };
            _ = FetchProgress();
            var resultData = await CallApiPost("txt2img", data);

            List<GenImageInfo> images = FetchImages(resultData);


            return images;
        }



        protected async Task Txt2ImgWebPost(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                await CheckBaseData();

                if (samplingMethods.Find(method => method.webUIName == genConfig.samplingMethod) == null)
                {
                    MessageSamplingMethod();
                    return;
                }

                //await httpClient.PostAsync(GetApiPredict(), new StringContent(JsonConvert.SerializeObject(new { fn_index = config.txt2ImgProgressStart, data = new object[] { }, session_hash = "yfn515mvsn2" }), Encoding.UTF8, "application/json"));

                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Visible;
                (Application.Current.MainWindow as MainWindow).ProgressGen.Value = 0;
                //_ = FetchProgressTxt2Img();

                List<GenImageInfo> images = await Txt2ImgGen(genConfig, batchSize, batchCount, positivePrompt, negativePrompt);

                AppCore.Instance.DoneGenerate(images);

                (Application.Current.MainWindow as MainWindow).ProgressPreview.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                AppCore.Instance.GenerateEnd();
                System.Diagnostics.Trace.WriteLine(ex);

                MessageConnectError();
            }


        }
      

        public override async Task<List<GenImageInfo>> Img2ImgGen(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            var data = new
            {
                init_images = new List<string>(new string[] { "data:image/" + refImageInfo.imageType + ";base64," + Convert.ToBase64String(refImageInfo.imageData) }),
                resize_mode = 0,
                denoising_strength = genConfig.denoisingStrength,
                mask_blur = 4,
                inpainting_fill = 0,
                inpaint_full_res = true,
                inpaint_full_res_padding = 0,
                inpainting_mask_invert = 0,
                initial_noise_multiplier = 0,
                prompt = positivePrompt,
                styles = new List<string>(),
                seed = genConfig.seed,
                subseed = -1,
                subseed_strength = 0,
                seed_resize_from_h = -1,
                seed_resize_from_w = -1,
                sampler_name = genConfig.samplingMethod,
                batch_size = batchSize,
                n_iter = batchCount,
                steps = genConfig.samplingSteps,
                cfg_scale = genConfig.cfgScale,
                width = genConfig.width,
                height = genConfig.height,
                restore_faces = false,
                tiling = false,
                negative_prompt = negativePrompt,
                eta = 1,
                s_churn = 0,
                s_tmax = 0,
                s_tmin = 0,
                s_noise = 1,
                override_settings = GetSettings(genConfig),
                override_settings_restore_afterwards = true,
                script_args = new List<object>(),
                sampler_index = genConfig.samplingMethod,
                include_init_images = false
            };
            _ = FetchProgress();
            var resultData = await CallApiPost("img2img", data);

            List<GenImageInfo> images = FetchImages(resultData);


            return images;
          
        }

        public async Task Img2ImgWebPost(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                await CheckBaseData();

                if (samplingMethods.Find(method => method.webUIName == genConfig.samplingMethod) == null)
                {
                    MessageSamplingMethod();
                    return;
                }

                //await httpClient.PostAsync(GetApiPredict(), new StringContent(JsonConvert.SerializeObject(new { fn_index = config.img2ImgProgressStart, data = new object[] { }, session_hash = "yfn515mvsn2" }), Encoding.UTF8, "application/json"));

                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Visible;
                (Application.Current.MainWindow as MainWindow).ProgressGen.Value = 0;
                //_ = FetchProgressImg2Img();

                List<GenImageInfo> images = await Img2ImgGen(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);

                AppCore.Instance.DoneGenerate(images);

                (Application.Current.MainWindow as MainWindow).ProgressPreview.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                AppCore.Instance.GenerateEnd();
                System.Diagnostics.Trace.WriteLine(ex);

                MessageConnectError();
            }


        }

   

        public async Task DeepDanbooruWebPost(GenImageInfo refImageInfo)
        {
            try
            {
                await CheckBaseData();

                var data = new
                {
                    image = "data:image/" + refImageInfo.imageType + ";base64," + Convert.ToBase64String(refImageInfo.imageData),
                    model = "deepdanbooru"
                };
                var resultData = await CallApiPost("interrogate", data);
                string resultPrompt = resultData.caption;
                AppCore.Instance.ParseDanbooruPrompts(resultPrompt);


            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
                //(Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Hidden;
                MessageConnectError();
                (Application.Current.MainWindow as MainWindow).ButtonToTags.IsEnabled = true;
            }


        }

        public async Task FetchProgress()
        {
            try
            {
                while (AppCore.Instance.generating)
                {
                    var resultData = await CallApiGet("progress");
                    (Application.Current.MainWindow as MainWindow).ProgressGen.Value = resultData.progress * 100d;
                    if (resultData.current_image != null)
                    {
                        (Application.Current.MainWindow as MainWindow).ProgressPreview.Visibility = Visibility.Visible;
                        string dataString = resultData.current_image;
                        byte[] imageData = Convert.FromBase64String(dataString);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(imageData);
                        bitmap.EndInit();
                        (Application.Current.MainWindow as MainWindow).ImageProgressPreview.Source = bitmap;
                    }

                    await Task.Delay(3000);
                }


            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
                (Application.Current.MainWindow as MainWindow).ProgressPreview.Visibility = Visibility.Collapsed;
            }
            (Application.Current.MainWindow as MainWindow).ProgressPreview.Visibility = Visibility.Collapsed;

        }

        public override bool IsBaseDataLoaded()
        {
            return baseDataLoaded;
        }


        public override async Task LoadBaseData()
        {
            try
            {
                await CheckBaseData();
            }
            catch (Exception ex)
            {
                MessageConnectError();
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        public override bool CanChooseModel()
        {
            return true;
        }

        public async override Task ChooseModelDo(string modelName)
        {
            var data = new
            {
                sd_model_checkpoint = modelName
            };
            var options = await CallApiPost("options", data);
        }

        public async Task ChooseModelWebPost(string model)
        {
            try
            {
                await CheckBaseData();

                await ChooseModelDo(model);

                (Application.Current.MainWindow as MainWindow).EnableOperations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                (Application.Current.MainWindow as MainWindow).EnableOperations();
                MessageConnectError();
            }


        }

        public override bool CanHighResFix()
        {
            return true;
        }

        public override List<string> GetUpscalers()
        {
            return upscalers;
        }

        public override List<string> GetModels()
        {
            return models;
        }
        public override string GetCurrentModel()
        {
            return selectedModel;
        }
        public override List<string> GetVaes()
        {
            return vaes;
        }
    }
}
