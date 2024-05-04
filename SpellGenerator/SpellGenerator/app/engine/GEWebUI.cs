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
using SpellGenerator.app.batch;
using System.Security.Policy;
using HtmlAgilityPack;
using System.Windows.Controls;
using SpellGenerator.app.tag;

namespace SpellGenerator.app.engine
{
    public class GEWebUI : GenerateEngine
    {
        public static List<string> BASE_UPSCALERS = new List<string>(new string[]
        {
            "Latent",
            "Latent (antialiased)",
            "Latent (bicubic)",
            "Latent (bicubic antialiased)",
            "Latent (nearest)",
            "Latent (nearest-exact)",
            "None",
            "Lanczos",
            "Nearest",
            "ESRGAN_4x",
            "LDSR",
            "ScuNET GAN",
            "ScuNET PSNR",
            "SwinIR_4x"
        });
        


        private HttpClient httpClient = new HttpClient();
        private WebUIConfig? config;
        private WebUICApiConfig? apiConfig;

        private List<SamplingMethod> samplingMethods = new List<SamplingMethod>();
        private List<string> upscalers = new List<string>();
        private List<string> vaes = new List<string>(new string[] {"默认"});
        private List<string> models = new List<string>();
        public string? selectedModel;

        public GEWebUI()
        {
            httpClient.Timeout = TimeSpan.FromMinutes(180);

            samplingMethods.AddRange(SamplingMethod.samplingMethods);
            upscalers.AddRange(BASE_UPSCALERS);
        }

        public override string GetEngineName()
        {
            return "Web UI";
        }

        public override void LoadApi(string version)
        {
            base.LoadApi(version);
            string webUIPath = Path.Combine(AppCore.Instance.basePath, "api", "WebUI", version+".json");
            try
            {
                using (FileStream fs = File.OpenRead(webUIPath))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string json = sr.ReadToEnd();

                        apiConfig = JsonConvert.DeserializeObject<WebUICApiConfig>(json);
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString());
                MessageBox.Show("读取协议文件失败，请检查文件"+webUIPath);
            }
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

        public override void Txt2ImgInterrupt()
        {
            _ = Txt2ImgInterruptWebPost();
        }

        public override void Img2ImgInterrupt()
        {
            _ = Img2ImgInterruptWebPost();
        }

        public override void FetchExtraModels()
        {
            _ = FetchExtraModelsWebPost();
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
                if (comp.type == "html" && comp.props != null)
                {
                    string value = (string)comp.props.value;
                    AddCard(value);
                }
                if (comp.props != null && comp.props.elem_id != null)
                {
                    elemMap[(int)comp.id] = (string)comp.props.elem_id;
                    if (comp.props.elem_id == "setting_sd_model_checkpoint")
                    {
                        //AppCore.Instance.selectedModel = comp.props.value;
                        checkpointIndex = comp.id;
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
                    if (comp.props.elem_id == "txt2img_hr_upscaler")
                    {
                        upscalers.Clear();
                        List<string> choices = comp.props.choices.ToObject<List<string>>();
                        foreach (string upscalerName in choices)
                        {
                            upscalers.Add(upscalerName);
                            //System.Diagnostics.Debug.WriteLine(upscalerName);
                        }
                        (Application.Current.MainWindow as MainWindow).RefreshUpscalers();
                    }
                    if(comp.props.elem_id == "setting_sd_vae")
                    {
                        vaes.Clear();
                        List<string> choices = comp.props.choices.ToObject<List<string>>();
                        foreach (string vaeName in choices)
                        {
                            if(!vaeName.Contains("vae") && !vaeName.Contains("safetensor"))
                            {
                                continue;
                            }
                            vaes.Add(vaeName);
                            //System.Diagnostics.Debug.WriteLine(upscalerName);
                        }
                        vaes.Insert(0, "默认");
                        (Application.Current.MainWindow as MainWindow).RefreshVaes();
                    }
                }
                defaultValueMap[(int)comp.id] = (object)(comp.props?.value);


            }

            AppCore.Instance.LoadEngineFunctionGroup();
            (Application.Current.MainWindow as MainWindow).UpdateTagGroups();

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
                        //System.Diagnostics.Debug.WriteLine("out " + (int)func.outputs[i]);
                    }
                }
            }
            if (defaultValueMap.ContainsKey(checkpointIndex))
            {
                System.Diagnostics.Debug.WriteLine(defaultValueMap[checkpointIndex]);
                dynamic dy = defaultValueMap[checkpointIndex];
                string model = dy.value;

                models = new List<string>();
                List<string> choices = dy.choices.ToObject<List<string>>();
                models.AddRange(choices);

                selectedModel = model;
                
                System.Diagnostics.Debug.WriteLine(model);
                (Application.Current.MainWindow as MainWindow).RefreshModels();
            }
            c.refreshModels = funcMap.GetValueOrDefault("refresh_sd_model_checkpoint", -1);
            c.selectModel = funcMap.GetValueOrDefault("setting_sd_model_checkpoint", -1);
            if(apiConfig.txt2Img != null)
            {
                string txt2ImgId = apiConfig.txt2Img.id;
                c.txt2Img = funcMap.GetValueOrDefault(txt2ImgId, -1);
                inputCountMap.TryGetValue(txt2ImgId, out c.txt2ImgParamsCount);
                if (funcMap.ContainsKey(txt2ImgId))
                {
                    c.txt2ImgCall = new GradioCall();
                    int funcId = funcMap[txt2ImgId];
                    var func = resultData.dependencies[funcId];
                    c.txt2ImgCall.LoadFunc(funcId, txt2ImgId, func, elemMap, defaultValueMap);
                }
            }

            //c.txt2ImgProgressStart = funcMap.GetValueOrDefault("txt2img_check_progress_initial", -1);
            //c.txt2ImgProgress = funcMap.GetValueOrDefault("txt2img_check_progress", -1);
            if(apiConfig.img2Img!= null)
            {
                string img2ImgId = apiConfig.img2Img.id;
                c.img2Img = funcMap.GetValueOrDefault(img2ImgId, -1);
                if (funcMap.ContainsKey(img2ImgId))
                {
                    c.img2ImgCall = new GradioCall();
                    int funcId = funcMap[img2ImgId];
                    var func = resultData.dependencies[funcId];
                    c.img2ImgCall.LoadFunc(funcId, img2ImgId, func, elemMap, defaultValueMap);
                }
                inputCountMap.TryGetValue(img2ImgId, out c.img2ImgParamsCount);
            }

            c.txt2ImgInterrupt = funcMap.GetValueOrDefault("txt2img_interrupt", -1);
            c.img2ImgInterrupt = funcMap.GetValueOrDefault("img2img_interrupt", -1);
            //c.img2ImgProgressStart = funcMap.GetValueOrDefault("img2img_check_progress_initial", -1);
            //c.img2ImgProgress = funcMap.GetValueOrDefault("img2img_check_progress", -1);
            c.deepbooru = funcMap.GetValueOrDefault(apiConfig.deepbooru.id, -1);
            c.refreshExtraModels = funcMap.GetValueOrDefault("txt2img_extra_refresh", -1);

            config = c;
            AppController.Instance.InvokeOnEngineBaseDataLoadedChange();
        }

        string MakeTaskId()
        {
            string r = "abcdefghijklmnopqrstuvwxyz0123456789";
            Random rn = new Random();
            string rs = "";
            for(int i=0; i<15; i++)
            {
                rs += r.Substring(rn.Next(0, r.Length - 1), 1);
            }
            return "task("+rs+")";
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

        List<string> GetSettings(GenConfig genConfig)
        {
            List<string> settings = new List<string>();
            if(genConfig.vae != null && genConfig.vae != "" && genConfig.vae != "默认")
            {
                settings.Add("SD VAE:"+genConfig.vae);
            }
            return settings;
        }

        public override async Task<List<GenImageInfo>> Txt2ImgGen(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            string taskId = MakeTaskId();
            Dictionary<string,object> values = new Dictionary<string,object>();
            values["<taskId>"] = taskId;
            values["<positivePrompt>"] = positivePrompt;
            values["<negativePrompt>"] = negativePrompt;
            values["<samplingSteps>"] = genConfig.samplingSteps;
            values["<samplingMethod>"] = genConfig.samplingMethod;
            values["<batchCount>"] = batchCount;
            values["<batchSize>"] = batchSize;
            values["<cfgScale>"] = genConfig.cfgScale;
            values["<seed>"] = genConfig.seed;
            values["<height>"] = genConfig.height;
            values["<width>"] = genConfig.width;
            values["<denoisingStrength>"] = genConfig.denoisingStrength;
            if (genConfig.highResFix)
            {
                values["<highResFix>"] = true;
                values["<upscaleBy>"] = genConfig.upscaleBy;
                values["<upscaler>"] = genConfig.upscaler;
            }
            else
            {
                values["<highResFix>"] = false;
                values["<upscaleBy>"] = 1;
                values["<upscaler>"] = "None";
            }
            values["<settings>"] = GetSettings(genConfig);

            List<object> data = MakeData(apiConfig.txt2Img.data, values);
            for (int i = data.Count; i < config.txt2ImgParamsCount; i++)
            {
                data.Add(config.txt2ImgCall.defaultParams[i]);
            }
            _ = FetchProgress(taskId);

            var resultData = await CallPredict(config.txt2Img, data);
            //int imageCount = batchCount * batchSize;
            List<GenImageInfo> images = await FetchImages(resultData);

            return images;
        }

        async Task<List<GenImageInfo>> FetchImages(dynamic resultData)
        {
            List<GenImageInfo> images = new List<GenImageInfo>();
            string extrJson = resultData.data[1];
            var extrJsonObj = JsonConvert.DeserializeObject<dynamic>(extrJson);
            int firstImageIndex = extrJsonObj.index_of_first_image;
            int imageCount = resultData.data[0].Count - firstImageIndex;
            if(imageCount <= 0)
            {
                return images;
            }
            for (int i = 0; i < imageCount; i++)
            {
                long seed = extrJsonObj.all_seeds[i];
                int imageIndex = i + firstImageIndex;
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
                imageInfo.defaultFileName = Path.GetFileName(path);
                images.Add(imageInfo);
            }
            return images;
        }

        protected async Task Txt2ImgWebPost(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                await CheckConfig();

                if (config.txt2Img == -1)
                {
                    MessageNoInterface();
                    return;
                }

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
            string taskId = MakeTaskId();
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["<taskId>"] = taskId;
            values["<positivePrompt>"] = positivePrompt;
            values["<negativePrompt>"] = negativePrompt;
            values["<image>"] = "data:image/" + refImageInfo.imageType + ";base64," + Convert.ToBase64String(refImageInfo.imageData);
            values["<samplingSteps>"] = genConfig.samplingSteps;
            values["<samplingMethod>"] = genConfig.samplingMethod;
            values["<batchCount>"] = batchCount;
            values["<batchSize>"] = batchSize;
            values["<cfgScale>"] = genConfig.cfgScale;
            values["<denoisingStrength>"] = genConfig.denoisingStrength;
            values["<seed>"] = genConfig.seed;
            values["<height>"] = genConfig.height;
            values["<width>"] = genConfig.width;
            values["<settings>"] = GetSettings(genConfig);


            List<object> data = MakeData(apiConfig.img2Img.data, values);
            //List<object> data = GetImg2ImgParam(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);
            for (int i = data.Count; i < config.img2ImgParamsCount; i++)
            {
                data.Add(config.img2ImgCall.defaultParams[i]);
            }

            _ = FetchProgress(taskId);

            var resultData = await CallPredict(config.img2Img, data);

            //int imageCount = batchCount * batchSize;
            List<GenImageInfo> images = await FetchImages(resultData);

            return images;
        }

        public async Task Img2ImgWebPost(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                await CheckConfig();

                if (config.img2Img == -1)
                {
                    MessageNoInterface();
                    return;
                }

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

        async Task<byte[]> FetchImage(string path)
        {
            var response = await httpClient.GetAsync("http://" + host + ":" + port + "/file=" + path.Replace("\\\\", "\\").Replace("\\", "/"));
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task Txt2ImgInterruptWebPost()
        {
            try
            {
                await CheckConfig();

                if (config.txt2ImgInterrupt == -1)
                {
                    MessageNoInterface();
                    return;
                }

                object[] data =
                {
                };

                await CallPredict(config.txt2ImgInterrupt, data);
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
                await CheckConfig();

                if (config.img2ImgInterrupt == -1)
                {
                    MessageNoInterface();
                    return;
                }

                object[] data =
                {
                };

                await CallPredict(config.img2ImgInterrupt, data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                MessageConnectError();
            }


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

                var resultData = await CallPredict(config.refreshModels, data);
                List<string> choices = resultData.data[0].choices.ToObject<List<string>>();

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

        async Task<dynamic> CallPredict(int fnIndex, object data)
        {
            var jsonObj = new { fn_index = fnIndex, data, session_hash = "yfn515mvsn2" };
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(GetApiPredict(), httpContent);
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Trace.WriteLine(content);
            var resultData = JsonConvert.DeserializeObject<dynamic>(content);
            httpContent.Dispose();
            return resultData;
        }

        public async Task FetchExtraModelsWebPost()
        {
            try
            {
                await CheckConfig();

                if (config.refreshExtraModels == -1)
                {
                    MessageNoInterface();
                    return;
                }

                object[] data =
                {
                };
                var resultData = await CallPredict(config.refreshExtraModels, data);

                tiGroup.tags.Clear();
                hyperGroup.tags.Clear();
                loraGroup.tags.Clear();

                foreach (string value in resultData.data)
                {
                    AddCard(value);
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

        void AddCard(string value)
        {
            if (value.Contains("txt2img_textual inversion_cards") || value.Contains("txt2img_textual_inversion_cards"))
            {
                //System.Diagnostics.Debug.WriteLine("<html>" + value + "</html>");
                var doc = new HtmlDocument();
                doc.LoadHtml(value);
                foreach (var divNodeP in doc.DocumentNode.ChildNodes)
                {
                    foreach (var divNode in divNodeP.ChildNodes)
                    {
                        if (divNode.HasClass("card"))
                        {
                            //System.Diagnostics.Debug.WriteLine(divNode.OuterHtml);
                            foreach (var subNode in divNode.ChildNodes)
                            {
                                foreach (var subSubNode in subNode.ChildNodes)
                                {
                                    if (subSubNode.HasClass("name"))
                                    {
                                        // System.Diagnostics.Debug.WriteLine(subSubNode.InnerText);
                                        TagInfo tag = TagInfo.CreateTextInversionTag(subSubNode.InnerText);
                                        tiGroup.tags.Add(tag);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (value.Contains("txt2img_hypernetworks_cards"))
            {
                //System.Diagnostics.Debug.WriteLine("<html>" + value + "</html>");
                var doc = new HtmlDocument();
                doc.LoadHtml(value);
                foreach (var divNodeP in doc.DocumentNode.ChildNodes)
                {
                    foreach (var divNode in divNodeP.ChildNodes)
                    {
                        if (divNode.HasClass("card"))
                        {
                            //System.Diagnostics.Debug.WriteLine(divNode.OuterHtml);
                            foreach (var subNode in divNode.ChildNodes)
                            {
                                foreach (var subSubNode in subNode.ChildNodes)
                                {
                                    if (subSubNode.HasClass("name"))
                                    {
                                        //System.Diagnostics.Debug.WriteLine(subSubNode.InnerText);
                                        TagInfo tag = TagInfo.CreateHypernetworkTag(subSubNode.InnerText);
                                        hyperGroup.tags.Add(tag);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            else if (value.Contains("txt2img_lora_cards"))
            {
                System.Diagnostics.Debug.WriteLine("<html>" + value + "</html>");
                var doc = new HtmlDocument();
                doc.LoadHtml(value);
                foreach (var divNodeP in doc.DocumentNode.ChildNodes)
                {
                    foreach (var divNode in divNodeP.ChildNodes)
                    {
                        if (divNode.HasClass("card"))
                        {
                            //System.Diagnostics.Debug.WriteLine(divNode.OuterHtml);
                            foreach (var subNode in divNode.ChildNodes)
                            {
                                string name = "";
                                string group = "";

                                foreach (var subSubNode in subNode.ChildNodes)
                                {
                                    if (subSubNode.HasClass("name"))
                                    {
                                        // System.Diagnostics.Debug.WriteLine(subSubNode.InnerText);
                                        name = subSubNode.InnerText;
                                    }
                                    if (subSubNode.HasClass("additional"))
                                    {
                                        foreach(var sssn in subSubNode.ChildNodes)
                                        {
                                            if(sssn.HasClass("search_term"))
                                            {
                                                string st = sssn.InnerHtml;
                                                System.Diagnostics.Debug.WriteLine(st);
                                                System.Diagnostics.Debug.WriteLine(st.LastIndexOf("/"));
                                                if (st.LastIndexOf("/") > 0)
                                                {
                                                    group = st.Split('/')[1];
                                                }
                                            }
                                        }
                                    }
                                }
                                if(name != "")
                                {
                                    TagInfo tag = TagInfo.CreateLoraTag(group, name);
                                    loraGroup.tags.Add(tag);
                                }
                            }
                        }
                    }
                }
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

                Dictionary<string, object> values = new Dictionary<string, object>();
                values["<image>"] = "data:image/" + refImageInfo.imageType + ";base64," + Convert.ToBase64String(refImageInfo.imageData);

                List<object> data = MakeData(apiConfig.deepbooru.data, values);

                var resultData = await CallPredict(config.deepbooru, data);
                
                string resultPrompt = resultData.data[0];

                //System.Diagnostics.Trace.WriteLine(resultPrompt);

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

        public async Task FetchProgress(string taskId)
        {
            try
            {
                int previewId = 0;
                while (AppCore.Instance.generating)
                {
                    var jsonObj = new { id_task = taskId, id_live_preview = previewId  };
                    HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("http://" + host + ":" + port + "/internal/progress", httpContent);
                    var content = await response.Content.ReadAsStringAsync();
                    //System.Diagnostics.Debug.WriteLine(content);
                    var resultData = JsonConvert.DeserializeObject<dynamic>(content);
                    if(resultData.id_live_preview != null)
                    {
                        previewId = resultData.id_live_preview;
                    }
                    if (resultData.progress != null)
                    {
                        (Application.Current.MainWindow as MainWindow).ProgressGen.Value = resultData.progress*100d;
                    }
                    if (resultData.live_preview != null)
                    {
                        (Application.Current.MainWindow as MainWindow).ProgressPreview.Visibility = Visibility.Visible;
                        string dataString = resultData.live_preview;
                        byte[] imageData = Convert.FromBase64String(dataString.Substring(dataString.IndexOf("base64,")+7));
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
            return config != null;
        }

        public override async Task LoadBaseData()
        {
            try { 
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
            await CallPredict(config.selectModel, data);
 
            selectedModel = model;
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
