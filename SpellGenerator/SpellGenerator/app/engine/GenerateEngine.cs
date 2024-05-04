using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpellGenerator.app.file;
using SpellGenerator.app.tag;

namespace SpellGenerator.app.engine
{
    public abstract class GenerateEngine
    {
        public abstract bool IsBaseDataLoaded();
        public abstract Task LoadBaseData();
        public abstract void Txt2Img(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt);
        public abstract void Img2Img(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt);
        public abstract void FetchModels();
        public abstract void FetchExtraModels();
        public abstract void ChooseModel(string modelName);
        public abstract Task ChooseModelDo(string modelName);
        public abstract Task<List<GenImageInfo>> Txt2ImgGen(GenConfig genConfig, int batchSize, int batchCount, string positivePrompt, string negativePrompt);
        public abstract Task<List<GenImageInfo>> Img2ImgGen(GenConfig genConfig, GenImageInfo refImageInfo, int batchSize, int batchCount, string positivePrompt, string negativePrompt);
        public abstract List<SamplingMethod> GetSamplingMethods();

        public string name;
        public string host;
        public int port;
        public string version;

        public TagGroupInfo? tiGroup = null; // TI 放第一
        public TagGroupInfo? hyperGroup = null; // Hyper 放第二
        public TagGroupInfo? loraGroup = null; // Lora 放第三

        public GenerateEngine()
        {
            tiGroup = new TagGroupInfo();
            tiGroup.name = "Text Inversion";
            hyperGroup = new TagGroupInfo();
            hyperGroup.name = "Hypernetworks";
            loraGroup = new TagGroupInfo();
            loraGroup.name = "LoRA";
        }

        public virtual void LoadApi(string version)
        {
            this.version = version;
        }

        public string GetName() => name;

        public abstract string GetEngineName();

        public string Name { get => GetName(); }

        public abstract void DeepDanbooru(GenImageInfo refImageInfo);
        public abstract bool CanChooseModel();
        public abstract bool CanHighResFix();
        public abstract List<string> GetUpscalers();

        public abstract List<string> GetModels();
        public abstract string GetCurrentModel();
        public abstract List<string> GetVaes();

        public abstract void Txt2ImgInterrupt();
        public abstract void Img2ImgInterrupt();
    }
}
