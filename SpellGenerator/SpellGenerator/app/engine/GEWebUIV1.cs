using Newtonsoft.Json;
using SpellGenerator.app.config;
using SpellGenerator.app.controller;
using SpellGenerator.app.file;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SpellGenerator.app.engine
{
    public class GEWebUIV1 : GenerateEngine
    {
        private HttpClient httpClient = new HttpClient();
        private WebUIConfig? config;

        private List<SamplingMethod> samplingMethods = new List<SamplingMethod>();
        private List<string> models = new List<string>();
        public string? selectedModel;
        public GEWebUIV1()
        {
            httpClient.Timeout = TimeSpan.FromMinutes(60);

            samplingMethods.AddRange(SamplingMethod.samplingMethods);
        }

        public override string GetEngineName()
        {
            return "Web UI";
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

        public override void ChooseModel(string modelName)
        {
            _ = ChooseModelWebPost(modelName);
        }

        string GetApiPredict()
        {
            return "http://" + host + ":" + port + "/api/predict/";
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

        void MessageNoFunc()
        {
            MessageBox.Show("该版本无此功能。");
        }

        public override void FetchExtraModels()
        {
            MessageNoFunc();
        }

        async Task CheckConfig()
        {
            if (config != null)
            {
                return;
            }
            var response = await httpClient.GetAsync("http://" + host+":"+port + "/config");
            var content = await response.Content.ReadAsStringAsync();
            var resultData = JsonConvert.DeserializeObject<dynamic>(content);
            WebUIConfig c = new WebUIConfig();
            Dictionary<int, string> elemMap = new Dictionary<int, string>();
            Dictionary<int, object> defaultValueMap = new Dictionary<int, object>();
            System.Diagnostics.Debug.WriteLine((string)resultData.version);
            int checkpointIndex = 0;
            foreach (var comp in resultData.components)
            {
                if (comp.props != null && comp.props.elem_id != null)
                {
                    elemMap[(int)comp.id] = (string)comp.props.elem_id;
                    if (comp.props.elem_id == "setting_sd_model_checkpoint")
                    {
                        //AppCore.Instance.selectedModel = comp.props.value;
                        models = new List<string>();
                        List<string> choices = comp.props.choices.ToObject<List<string>>();
                        models.AddRange(choices);
                        checkpointIndex = comp.id;
                        (Application.Current.MainWindow as MainWindow).RefreshModels();
                    }
                    if (comp.props.elem_id == "txt2img_sampling")
                    {
                        List<SamplingMethod> methods = new List<SamplingMethod>();
                        List<string> choices = comp.props.choices.ToObject<List<string>>();
                        foreach (string methodName in choices)
                        {
                            SamplingMethod method = SamplingMethod.GetSamplingMethod(methodName);
                            if (method == null)
                            {
                                method = new SamplingMethod(methodName, methodName.Replace(" ", "_").ToLower());
                            }
                            methods.Add(method);
                        }
                        samplingMethods = methods;
                        (Application.Current.MainWindow as MainWindow).RefreshSamplingMethods();
                    }
                }
                defaultValueMap[(int)comp.id] = (object)(comp.props?.value);


            }
            Dictionary<string, int> funcMap = new Dictionary<string, int>();
            Dictionary<string, int> inputCountMap = new Dictionary<string, int>();
            for (int funcId = 0; funcId < resultData.dependencies.Count; funcId++)
            {
                var func = resultData.dependencies[funcId];
                if (func.targets.Count > 0)
                {
                    int target = (int)func.targets[0];
                    if (elemMap.ContainsKey(target))
                    {
                        var elemId = elemMap[(int)func.targets[0]];
                        System.Diagnostics.Debug.WriteLine(funcId + " " + elemId + " " + (int)func.inputs.Count);
                        funcMap[elemId] = funcId;
                        inputCountMap[elemId] = (int)func.inputs.Count;
                        /*
                        for(int i = 0; i < func.inputs.Count; i++)
                        {
                            System.Diagnostics.Debug.WriteLine("   v"+i+" "+(int)func.inputs[i]+(elemMap.ContainsKey((int)func.inputs[i])? elemMap[(int)func.inputs[i]] : "") + (defaultValueMap.ContainsKey((int)func.inputs[i]) ? defaultValueMap[(int)func.inputs[i]] : ""));
                        }
                        */
                    }

                }

                if ((string)func.trigger == "load" && func.inputs.Count == 0 && func.outputs.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("load " + funcId);
                    var j = new { fn_index = funcId, data = new object[] { }, session_hash = "yfn515mvsn2" };
                    HttpContent hc = new StringContent(JsonConvert.SerializeObject(j), Encoding.UTF8, "application/json");
                    var r = await httpClient.PostAsync(GetApiPredict(), hc);
                    var cc = await r.Content.ReadAsStringAsync();
                    //System.Diagnostics.Debug.WriteLine(cc);
                    //System.Diagnostics.Debug.WriteLine((int)func.outputs.Count);
                    var rd = JsonConvert.DeserializeObject<dynamic>(cc);
                    for (int i = 0; i < func.outputs.Count; i++)
                    {
                        defaultValueMap[(int)func.outputs[i]] = (object)rd.data[i];
                    }
                }
            }
            if (defaultValueMap.ContainsKey(checkpointIndex))
            {
                System.Diagnostics.Debug.WriteLine(defaultValueMap[checkpointIndex]);
                selectedModel = defaultValueMap[checkpointIndex].ToString();
                (Application.Current.MainWindow as MainWindow).RefreshModels();
            }
            c.refreshModels = funcMap.GetValueOrDefault("refresh_sd_model_checkpoint", -1);
            c.selectModel = funcMap.GetValueOrDefault("setting_sd_model_checkpoint", -1);
            c.txt2Img = funcMap.GetValueOrDefault("txt2img_generate", -1);
            inputCountMap.TryGetValue("txt2img_generate", out c.txt2ImgParamsCount);
            if (funcMap.ContainsKey("txt2img_generate"))
            {
                c.txt2ImgCall = new GradioCall();
                int funcId = funcMap["txt2img_generate"];
                var func = resultData.dependencies[funcId];
                c.txt2ImgCall.LoadFunc(funcId, "txt2img_generate", func, elemMap, defaultValueMap);
            }
            c.txt2ImgProgressStart = funcMap.GetValueOrDefault("txt2img_check_progress_initial", -1);
            c.txt2ImgProgress = funcMap.GetValueOrDefault("txt2img_check_progress", -1);
            c.img2Img = funcMap.GetValueOrDefault("img2img_generate", -1);
            if (funcMap.ContainsKey("img2img_generate"))
            {
                c.img2ImgCall = new GradioCall();
                int funcId = funcMap["img2img_generate"];
                var func = resultData.dependencies[funcId];
                c.img2ImgCall.LoadFunc(funcId, "img2img_generate", func, elemMap, defaultValueMap);
            }
            inputCountMap.TryGetValue("img2img_generate", out c.img2ImgParamsCount);
            c.img2ImgProgressStart = funcMap.GetValueOrDefault("img2img_check_progress_initial", -1);
            c.img2ImgProgress = funcMap.GetValueOrDefault("img2img_check_progress", -1);
            c.deepbooru = funcMap.GetValueOrDefault("deepbooru", -1);

            config = c;
            AppController.Instance.InvokeOnEngineBaseDataLoadedChange();
        }

        public override async Task<List<GenImageInfo>> Txt2ImgGen(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            List<object> data = new List<object>(new object[]
            {
                    positivePrompt, // prompt
                    negativePrompt, // negative_prompt
                    "None", // prompt_style
                    "None", // prompt_style2
                    genConfig.samplingSteps, // steps
                    genConfig.samplingMethod, // sampler_index
                    false, // restore_faces
                    false, // tiling
                    batchCount, // n_iter
                    batchSize, // batch_size
                    genConfig.cfgScale, // cfg_scale
                    genConfig.seed, // seed
                    -1, // subseed
                    0, // subseed_strength
                    0, // seed_resize_from_h
                    0, // seed_resize_from_w
                    false, // seed_enable_extras
                    genConfig.height, // height
                    genConfig.width, // width
                    false, // enable_hr
                    genConfig.denoisingStrength, // denoising_strength
                    0, // firstphase_width
                    0, // firstphase_height
                    "None" // script
             });
            for (int i = data.Count; i < config.txt2ImgParamsCount; i++)
            {
                data.Add(config.txt2ImgCall.defaultParams[i]);
            }
            var jsonObj = new { fn_index = config.txt2Img, data = data.ToArray(), session_hash = "yfn515mvsn2" };
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(content);
            var resultData = JsonConvert.DeserializeObject<dynamic>(content);
            int imageCount = batchCount * batchSize;
            List<GenImageInfo> images = new List<GenImageInfo>();
            string extrJson = resultData.data[1];
            var extrJsonObj = JsonConvert.DeserializeObject<dynamic>(extrJson);
            for (int i = 0; i < imageCount; i++)
            {
                long seed = extrJsonObj.all_seeds[i];
                int imageIndex = i + 1;
                if (imageCount == 1) imageIndex = 0;
                string path = resultData.data[0][imageIndex].name;
                var bitmap = new BitmapImage();
                byte[] imageData;

                imageData = await FetchImage(path);
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(imageData);
                bitmap.EndInit();
                GenImageInfo imageInfo = new GenImageInfo();
                imageInfo.seed = seed;
                imageInfo.imageData = imageData;
                imageInfo.image = bitmap;
                imageInfo.imageType = "png";
                images.Add(imageInfo);
            }

            httpContent.Dispose();
            return images;
        }



        protected async Task Txt2ImgWebPost(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                await CheckConfig();

                if (config.txt2ImgProgressStart == -1 || config.txt2Img == -1)
                {
                    MessageNoInterface();
                    return;
                }

                if (samplingMethods.Find(method => method.webUIName == genConfig.samplingMethod) == null)
                {
                    MessageSamplingMethod();
                    return;
                }

                await httpClient.PostAsync(GetApiPredict(), new StringContent(JsonConvert.SerializeObject(new { fn_index = config.txt2ImgProgressStart, data = new object[] { }, session_hash = "yfn515mvsn2" }), Encoding.UTF8, "application/json"));

                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Visible;
                (Application.Current.MainWindow as MainWindow).ProgressGen.Value = 0;
                _ = FetchProgressTxt2Img();

                List<GenImageInfo> images = await Txt2ImgGen(genConfig, batchSize, batchCount, positivePrompt, negativePrompt);

                AppCore.Instance.DoneGenerate(images);
            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Hidden;
                MessageConnectError();
            }


        }

        protected virtual List<object> GetImg2ImgParam(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
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
                    null, // init_img_inpaint
                    null, // init_mask_inpaint
                    "Draw mask", // mask_mode
                    genConfig.samplingSteps, // steps
                    genConfig.samplingMethod, // sampler_index
                    4, // mask_blur
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

        public override async Task<List<GenImageInfo>> Img2ImgGen(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            List<object> data = GetImg2ImgParam(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);
            for (int i = data.Count; i < config.img2ImgParamsCount; i++)
            {
                data.Add(config.img2ImgCall.defaultParams[i]);
            }

            var jsonObj = new { fn_index = config.img2Img, data = data.ToArray(), session_hash = "yfn515mvsn2" };
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
            var content = await response.Content.ReadAsStringAsync();
            var resultData = JsonConvert.DeserializeObject<dynamic>(content);
            int imageCount = batchCount * batchSize;
            List<GenImageInfo> images = new List<GenImageInfo>();
            string extrJson = resultData.data[1];
            var extrJsonObj = JsonConvert.DeserializeObject<dynamic>(extrJson);
            for (int i = 0; i < imageCount; i++)
            {
                long seed = extrJsonObj.all_seeds[i];
                int imageIndex = i + 1;
                if (imageCount == 1) imageIndex = 0;
                string path = resultData.data[0][imageIndex].name;
                var bitmap = new BitmapImage();
                byte[] imageData;
                imageData = await FetchImage(path);

                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(imageData);
                bitmap.EndInit();
                GenImageInfo imageInfo = new GenImageInfo();
                imageInfo.seed = seed;
                imageInfo.imageData = imageData;
                imageInfo.image = bitmap;
                imageInfo.imageType = "png";
                images.Add(imageInfo);
            }

            httpContent.Dispose();

            return images;
        }

        public async Task Img2ImgWebPost(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                await CheckConfig();

                if (config.img2ImgProgressStart == -1 || config.img2Img == -1)
                {
                    MessageNoInterface();
                    return;
                }

                if (samplingMethods.Find(method => method.webUIName == genConfig.samplingMethod) == null)
                {
                    MessageSamplingMethod();
                    return;
                }

                await httpClient.PostAsync(GetApiPredict(), new StringContent(JsonConvert.SerializeObject(new { fn_index = config.img2ImgProgressStart, data = new object[] { }, session_hash = "yfn515mvsn2" }), Encoding.UTF8, "application/json"));

                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Visible;
                (Application.Current.MainWindow as MainWindow).ProgressGen.Value = 0;
                _ = FetchProgressImg2Img();

                List<GenImageInfo> images = await Img2ImgGen(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);

                AppCore.Instance.DoneGenerate(images);


            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Hidden;
                MessageConnectError();
            }


        }

        async Task<byte[]> FetchImage(string path)
        {
            var response = await httpClient.GetAsync("http://" + host + ":" + port + "/file=" + path.Replace("\\\\", "\\").Replace("\\", "/"));
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task FetchModelsWebPost()
        {
            try
            {
                await CheckConfig();

                if (config.refreshModels == -1)
                {
                    MessageNoInterface();
                    return;
                }

                object[] data =
                {
                };
                var jsonObj = new { fn_index = config.refreshModels, data, session_hash = "yfn515mvsn2" };
                HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
                var content = await response.Content.ReadAsStringAsync();
                var resultData = JsonConvert.DeserializeObject<dynamic>(content);
                List<string> choices = resultData.data[0].choices.ToObject<List<string>>();


                httpContent.Dispose();

                models = new List<string>();
                models.AddRange(choices);


                (Application.Current.MainWindow as MainWindow).RefreshModels();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                MessageConnectError();
            }


        }

        public async Task ChooseModelWebPost(string model)
        {
            try
            {
                await CheckConfig();

                if (config.selectModel == -1)
                {
                    MessageNoInterface();
                    return;
                }
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

        public async Task DeepDanbooruWebPost(GenImageInfo refImageInfo)
        {
            try
            {
                await CheckConfig();

                if (config.deepbooru == -1)
                {
                    MessageDeepDanbooru();
                    return;
                }

                object[] data =
                {
                    "data:image/"+refImageInfo.imageType+";base64,"+Convert.ToBase64String(refImageInfo.imageData)
                };
                var jsonObj = new { fn_index = config.deepbooru, data, session_hash = "yfn515mvsn2" };
                HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
                var content = await response.Content.ReadAsStringAsync();
                var resultData = JsonConvert.DeserializeObject<dynamic>(content);
                string resultPrompt = resultData.data[0];

                System.Diagnostics.Trace.WriteLine(resultPrompt);

                httpContent.Dispose();

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

        public async Task FetchProgressTxt2Img()
        {
            try
            {
                while (AppCore.Instance.generating)
                {
                    object[] data =
                    {
                    };
                    var jsonObj = new { fn_index = config.txt2ImgProgress, data, session_hash = "yfn515mvsn2" };
                    HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
                    var content = await response.Content.ReadAsStringAsync();
                    //System.Diagnostics.Debug.WriteLine(content);

                    bool hasPercentage = false;
                    float percentage = 0;
                    foreach (Match match in Regex.Matches(content, @"(\d+[.]?\d*)%"))
                    {

                        if (float.TryParse(match.Groups[1].Value, out percentage))
                        {
                            //percentage = percentage / 100;
                            hasPercentage = true;
                            // System.Diagnostics.Debug.WriteLine(percentage);
                        }
                        //Console.WriteLine("Found: " + match.Groups[1].Value);
                        break;
                    }
                    if (hasPercentage)
                    {
                        (Application.Current.MainWindow as MainWindow).ProgressGen.Value = percentage;
                    }
                    httpContent.Dispose();
                    await Task.Delay(100);
                }


            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
            }


        }


        public async Task FetchProgressImg2Img()
        {
            try
            {
                while (AppCore.Instance.generating)
                {
                    object[] data =
                    {
                    };
                    var jsonObj = new { fn_index = config.img2ImgProgress, data, session_hash = "yfn515mvsn2" };
                    HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
                    var content = await response.Content.ReadAsStringAsync();
                    //System.Diagnostics.Debug.WriteLine(content);

                    bool hasPercentage = false;
                    float percentage = 0;
                    foreach (Match match in Regex.Matches(content, @"(\d+[.]?\d*)%"))
                    {

                        if (float.TryParse(match.Groups[1].Value, out percentage))
                        {
                            //percentage = percentage / 100;
                            hasPercentage = true;
                            // System.Diagnostics.Debug.WriteLine(percentage);
                        }
                        //Console.WriteLine("Found: " + match.Groups[1].Value);
                        break;
                    }
                    if (hasPercentage)
                    {
                        (Application.Current.MainWindow as MainWindow).ProgressGen.Value = percentage;
                    }
                    httpContent.Dispose();
                    await Task.Delay(100);
                }


            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
            }


        }

        public override bool IsBaseDataLoaded()
        {
            return config != null;
        }

        public override async Task LoadBaseData()
        {
            try
            {
                await CheckConfig();
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

        public async override Task ChooseModelDo(string model)
        {
            object[] data =
{
                    model
                };
            var jsonObj = new { fn_index = config.selectModel, data, session_hash = "yfn515mvsn2" };
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
            await response.Content.ReadAsStringAsync();


            httpContent.Dispose();

            selectedModel = model;
        }

        public override bool CanHighResFix()
        {
            return false;
        }

        public override List<string> GetUpscalers()
        {
            return new List<string>();
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
            return new List<string>();
        }


        public override void Txt2ImgInterrupt()
        {
            MessageNoInterface();
        }

        public override void Img2ImgInterrupt()
        {
            MessageNoInterface();
        }
    }
}
