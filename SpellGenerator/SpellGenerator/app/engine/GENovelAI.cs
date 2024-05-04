using Newtonsoft.Json;
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
using SpellGenerator.app.file;

namespace SpellGenerator.app.engine
{
    public class GENovalAI : GenerateEngine
    {
        private HttpClient httpClient = new HttpClient();



        private List<SamplingMethod> samplingMethods = new List<SamplingMethod>(new SamplingMethod[]
        {
             SamplingMethod.k_euler_a,
             SamplingMethod.k_euler,
             SamplingMethod.ddim,
             SamplingMethod.plms
        });

        public override List<SamplingMethod> GetSamplingMethods()
        {
            return samplingMethods;
        }

        public override string GetEngineName()
        {
            return "Noval AI";
        }


        public GENovalAI()
        {
            httpClient.Timeout = TimeSpan.FromMinutes(60);
        }

        public override void Txt2Img(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            _ = Txt2ImgWebPost(genConfig, batchSize, batchCount, positivePrompt, negativePrompt);

        }

        public override void Img2Img(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            _ = Img2ImgWebPost(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);
        }

        void MessageConnectError()
        {
            MessageBox.Show("连接NovelAI后台错误，请确认NovelAI是否已启动。如已启动，请联系开发者。");
        }

        void MessageNovelAI()
        {
            MessageBox.Show("NovelAI不支持该项操作。");
        }



        public override async Task<List<GenImageInfo>> Txt2ImgGen(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            long seed = genConfig.seed;
            if (seed == -1)
            {
                seed = new Random().Next();
            }
            string sampler = SamplingMethod.GetSamplingMethod(genConfig.samplingMethod).novelAIName;

            var jsonObj = new
            {
                prompt = positivePrompt,
                uc = negativePrompt,
                ucPreset = 0,
                genConfig.height,
                genConfig.width,
                n_samples = batchCount,
                steps = genConfig.samplingSteps,
                sampler,
                scale = genConfig.cfgScale
            };
            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(jsonObj));
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("http://" + host + ":" + port + "/generate-stream"));
            httpRequestMessage.Content = httpContent;

            //httpRequestMessage.Headers.Add("Accept", "*/*");
            /*
            httpRequestMessage.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
            httpRequestMessage.Headers.Add("Authorization", "Bearer");
            httpRequestMessage.Headers.Add("Connection", "keep-alive");
            httpRequestMessage.Headers.Add("Host", "101.35.83.34:6789");
            httpRequestMessage.Headers.Add("Origin", "http://101.35.83.34:6789");
            httpRequestMessage.Headers.Add("User-Agen", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");
            System.Diagnostics.Debug.WriteLine(httpRequestMessage.ToString());*/
            var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();
            //System.Diagnostics.Debug.WriteLine("http://" + AppCore.Instance.systemConfig.novelAIBackendIp + ":" + AppCore.Instance.systemConfig.novelAIBackendPort + "/generate-stream");

            List<GenImageInfo> images = new List<GenImageInfo>();
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var currentLine = reader.ReadLine();
                    //System.Diagnostics.Debug.WriteLine(currentLine);
                    if (currentLine.StartsWith("data:"))
                    {
                        byte[] imageData = Convert.FromBase64String(currentLine.Substring(5));
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(imageData);
                        bitmap.EndInit();
                        GenImageInfo imageInfo = new GenImageInfo();
                        imageInfo.seed = seed;
                        seed++;
                        imageInfo.imageData = imageData;
                        imageInfo.image = bitmap;
                        imageInfo.imageType = "png";
                        images.Add(imageInfo);


                    }
                }
            }

            httpContent.Dispose();
            return images;
        }

        public async Task Txt2ImgWebPost(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {

                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Visible;
                (Application.Current.MainWindow as MainWindow).ProgressGen.Value = 0;

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

        public override async Task<List<GenImageInfo>> Img2ImgGen(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            long seed = genConfig.seed;
            if (seed == -1)
            {
                seed = new Random().Next();
            }
            string sampler = SamplingMethod.GetSamplingMethod(genConfig.samplingMethod).novelAIName;

            var jsonObj = new
            {
                prompt = positivePrompt,
                uc = negativePrompt,
                ucPreset = 0,
                genConfig.height,
                genConfig.width,
                n_samples = batchCount,
                steps = genConfig.samplingSteps,
                sampler,
                scale = genConfig.cfgScale,
                image = Convert.ToBase64String(refImageInfo.imageData),
                noise = 0.1f,
                strength = genConfig.denoisingStrength
            };
            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(jsonObj));
            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("http://" + host + ":" + port + "/generate-stream"));
            httpRequestMessage.Content = httpContent;
            var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();


            List<GenImageInfo> images = new List<GenImageInfo>();
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var currentLine = reader.ReadLine();

                    if (currentLine.StartsWith("data:"))
                    {
                        byte[] imageData = Convert.FromBase64String(currentLine.Substring(5));
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(imageData);
                        bitmap.EndInit();
                        GenImageInfo imageInfo = new GenImageInfo();
                        imageInfo.seed = seed;
                        seed++;
                        imageInfo.imageData = imageData;
                        imageInfo.image = bitmap;
                        imageInfo.imageType = "png";
                        images.Add(imageInfo);


                    }
                }
            }

            httpContent.Dispose();
            return images;
        }

        public async Task Img2ImgWebPost(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt)
        {
            try
            {
                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Visible;
                (Application.Current.MainWindow as MainWindow).ProgressGen.Value = 0;

                List<GenImageInfo> images = await Img2ImgGen(genConfig, refImageInfo, batchSize, batchCount, positivePrompt, negativePrompt);

                AppCore.Instance.DoneGenerate(images);


            }
            catch (Exception ex)
            {
                AppCore.Instance.generating = false;
                System.Diagnostics.Trace.WriteLine(ex);
                (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Hidden;
            }


        }

        public override void FetchModels()
        {
            MessageNovelAI();
        }

        public override void FetchExtraModels()
        {
            MessageNovelAI();
        }

        public override void Txt2ImgInterrupt()
        {
            MessageNovelAI();
        }

        public override void Img2ImgInterrupt()
        {
            MessageNovelAI();
        }

        public override void ChooseModel(string modelName)
        {
            MessageNovelAI();
            (Application.Current.MainWindow as MainWindow).EnableOperations();
        }

        public override void DeepDanbooru(GenImageInfo refImageInfo)
        {
            MessageNovelAI();
            (Application.Current.MainWindow as MainWindow).ButtonToTags.IsEnabled = true;
        }

        public override bool IsBaseDataLoaded()
        {
            return true;
        }

        public override async Task LoadBaseData()
        {

        }

        public override bool CanChooseModel()
        {
            return false;
        }

        public async override Task ChooseModelDo(string modelName)
        {

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
            return new List<string>();
        }
        public override string GetCurrentModel()
        {
            return null;
        }
        public override List<string> GetVaes()
        {
            return new List<string>();
        }
    }
}
