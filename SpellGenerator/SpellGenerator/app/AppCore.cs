using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Data.OleDb;
using Newtonsoft.Json;
using SpellGenerator.app.utils;
using SpellGenerator.app.engine;
using SpellGenerator.app.config;
using SpellGenerator.app.tag;
using SpellGenerator.app.file;
using Newtonsoft.Json.Linq;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using System.Diagnostics;

namespace SpellGenerator.app
{
    class DaDi
    {
        [Index(3)]
        public string Name { get; set; }
        [Index(0)]
        public string Value { get; set; }
    }



    public class AppCore
    {
        public static AppCore Instance { get; } = new AppCore();

        public TagGroupInfo? colors;
        public TagInfo? black;
        public List<ClassifyInfo> classifyInfos = new List<ClassifyInfo>();
        public List<TagGroupInfo> tagGroups = new List<TagGroupInfo>();


        public ActivePrompt activePositivePrompt = new ActivePrompt();
        public ActivePrompt activeNegativePrompt = new ActivePrompt();
        public ActivePrompt currentActivePrompt;

        private TagGroupInfo? tiGroup = null; // TI 放第一
        private TagGroupInfo? hyperGroup = null; // Hyper 放第二
        private TagGroupInfo? loraGroup = null; // Lora 放第三
        public TagGroupInfo? tempGroup = new TagGroupInfo(); // 临时列表，在这个情况显示用

        public GenConfig? genConfig;
        public int batchSize;
        public List<GenImageInfo> genImageInfos;

        public List<GenerateEngine> generateEngines = new List<GenerateEngine>();
        public int selectedEngineIndex = 0;
        public bool generating = false;
        public GenImageInfo? refImage;
        public SystemConfig? systemConfig;
        public WindowConfig? windowConfig;

        private bool img2img;

        public string? basePath;
        Dictionary<string, List<TagGroupInfo>> tagGroupMap = new Dictionary<string, List<TagGroupInfo>>();
        Dictionary<string, TagGroupInfo> tagGroupInfoMap = new Dictionary<string, TagGroupInfo>();
        public Dictionary<string,PreviewImageInfo> previewImageInfos = new Dictionary<string, PreviewImageInfo>();

        public List<PackInfo> previewPacks;
        public PackInfo lastSelPreviewPack;
        public bool recordLastPreview = false;

        private List<DaDi> extraDict;

        public List<LoraLayer> loraLayers = new List<LoraLayer>();

        public void Initialize()
        {
            activePositivePrompt.name = "正咒";
            activeNegativePrompt.name = "反咒";

            basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(Path.Combine(basePath, "dict")))
            {
                basePath = Directory.GetParent(basePath).Parent.Parent.Parent?.FullName;
            }

            LoadSystemConfig(basePath);
        

            //generateEngines.AddRange(new List<GenerateEngine>() { new GEWebUI(), new GENovalAI() });

            if(systemConfig.backends == null || systemConfig.backends.Count == 0)
            {
                GenerateEngine ge = new GEWebUI();
                ge.name = ge.GetEngineName();
                ge.host = systemConfig.backendIp;
                ge.port = systemConfig.backendPort;
                ge.LoadApi("3");
                generateEngines.Add(ge);
                ge = new GENovalAI();
                ge.name = ge.GetEngineName();
                ge.host = systemConfig.novelAIBackendIp;
                ge.port = systemConfig.novelAIBackendPort;
                generateEngines.Add(ge);
            }
            else
            {
                foreach(var bconfig in systemConfig.backends)
                {
                    GenerateEngine ge = null;
                    if(bconfig.engine == "Web UI V1")
                    {
                        bconfig.engine = "Web UI";
                        bconfig.version = "1";
                    }
                    else if (bconfig.engine == "Web UI V2")
                    {
                        bconfig.engine = "Web UI";
                        bconfig.version = "2";
                    }
                    if (bconfig.engine == "Web UI")
                    {
                        if(bconfig.version == null || bconfig.version.Length == 0)
                        {
                            bconfig.version = "newest";
                        }
                        if(bconfig.version == "1")
                        {
                            ge = new GEWebUIV1();
                        }
                        else if (bconfig.version == "2")
                        {
                            ge = new GEWebUIV2();
                        }
                        else
                        {
                            ge = new GEWebUI();
                            ge.LoadApi(bconfig.version);
                        }
                            
                    }
                    else if (bconfig.engine == "Web UI Api")
                    {
                        ge = new GEWebUIApi();
                    }
                    else if(bconfig.engine == "Noval AI")
                    {
                        ge = new GENovalAI();
                    }
                    else
                    {
                        continue;
                    }
                    ge.name = bconfig.name;
                    ge.host = bconfig.host;
                    ge.port = bconfig.port;
                    generateEngines.Add(ge);
                }
            }

            InitConfig();
            batchSize = 1;
            //System.Diagnostics.Trace.WriteLine(basePath);
            string dictPath = Path.Combine(basePath, "dict");
            ReadClassify(Path.Combine(dictPath, "分组.txt"));

            LoadDict();
            activePositivePrompt.defaultSpell = activePositivePrompt.initSpells[0].value;
            activeNegativePrompt.defaultSpell = activeNegativePrompt.initSpells[0].value;
            LoadPreview();

            LoadExtraTags();

            currentActivePrompt = activePositivePrompt;
        }

        List<PackInfo> GetPacks(string basePackPath, bool sort = true)
        {
            List<PackInfo> packs = new List<PackInfo>();
            packs.Add(new PackInfo(0, basePackPath, "主目录"));

            // 扩展
            string extentionPath = Path.Combine(basePath, "extensions");
            if(Directory.Exists(extentionPath))
            {
                DirectoryInfo folder = new DirectoryInfo(extentionPath);
                foreach (DirectoryInfo packDir in folder.GetDirectories())
                {
                    int order = 1;
                    foreach (FileInfo file in packDir.GetFiles("*.order"))
                    {
                        int o = 0;
                        if(int.TryParse(file.Name.Split(".")[0], out o))
                        {
                            order = o;
                            break;
                        }
                    }
                    packs.Add(new PackInfo(order, packDir.FullName, packDir.Name));
                    System.Diagnostics.Debug.WriteLine(packDir.FullName);
                }
            }

            if(sort)packs.Sort((a, b) =>a.order - b.order);

            return packs;
        }

        public void LoadDict()
        {
            List<PackInfo> packs = GetPacks(Path.Combine(basePath, "dict"));

            tagGroups.Clear();
            tagGroupMap.Clear();
            loraLayers.Clear();
            foreach (PackInfo pack in packs)
            {
                ReadDicts(pack.location);
            }

            foreach(TagGroupInfo tagGroupInfo in tagGroupInfoMap.Values)
            {
                if (tagGroupInfo.classify != null)
                {
                    AddToGroupMap(tagGroupMap, tagGroupInfo.classify.id, tagGroupInfo);
                }
                else
                {
                    AddToGroupMap(tagGroupMap, "none", tagGroupInfo);
                }
            }

            
            foreach (ClassifyInfo classifyInfo in classifyInfos)
            {
                if (tagGroupMap.ContainsKey(classifyInfo.id))
                {
                    tagGroupMap[classifyInfo.id].Sort((a, b) => a.order - b.order);
                    tagGroups.AddRange(tagGroupMap[classifyInfo.id]);
                }
            }
            if (tagGroupMap.ContainsKey("none"))
            {
                tagGroupMap["none"].Sort((a, b) => a.order - b.order);
                tagGroups.AddRange(tagGroupMap["none"]);
            }

            LoraLayer customLayer = new LoraLayer();
            customLayer.name = "自定义";
            customLayer.isEmpty = true;
            loraLayers.Add(customLayer);
        }

        void LoadPreview()
        {
            List<PackInfo> packs = GetPacks(Path.Combine(basePath, "preview"));
            previewPacks = GetPacks(Path.Combine(basePath, "preview"), false);

            foreach (PackInfo pack in packs)
            {
                DirectoryInfo folder = new DirectoryInfo(pack.location);
                foreach (FileInfo file in folder.GetFiles("*.jpg"))
                {
                    string name = Path.GetFileNameWithoutExtension(file.FullName);
                    if(previewImageInfos.ContainsKey(name))
                    {
                        continue;
                    }
                    previewImageInfos[name] = new PreviewImageInfo(file.FullName);
                }
            }
        }

        void LoadSystemConfig(string basePath)
        {
            string oldConfigPath = Path.Combine(basePath, "config.json");
            string configPath = Path.Combine(basePath, "config.cfg");
            if (!File.Exists(configPath))
            {
                configPath = oldConfigPath;
            }
            try
            {
                using (FileStream fs = File.OpenRead(configPath))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string json = sr.ReadToEnd();

                        systemConfig = JsonConvert.DeserializeObject<SystemConfig>(json);
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
            if(systemConfig == null)
            {
                systemConfig = new SystemConfig();
                BackendConfig backendConfig = new BackendConfig();
                backendConfig.engine = "Web UI";
                backendConfig.host = "127.0.0.1";
                backendConfig.port = 7860;
                systemConfig.backends = new List<BackendConfig>();
                systemConfig.backends.Add(backendConfig);
            }
        }



        public void SaveSpell(string fileName)
        {
            try
            {
                SpellFile spellFile = new SpellFile();
                spellFile.config = genConfig;
                spellFile.positiveDefaultSpell = activePositivePrompt.defaultSpell;
                spellFile.negativeSpell = activeNegativePrompt.defaultSpell;
                spellFile.tags = new List<TagData>();
                foreach(ActiveTagData activeTagData in activePositivePrompt.activeTags)
                {
                    TagData tagData = new TagData();
                    tagData.prompt = activeTagData.prompt;
                    tagData.colorprompt = activeTagData.colorprompt;
                    tagData.strength = activeTagData.strength;
                    if(activeTagData.classify != null)
                    {
                        tagData.classify = activeTagData.classify.id;
                    }
                    tagData.tagType = activeTagData.tagType;
                    if(activeTagData.loraLayer != null && !activeTagData.loraLayer.IsAll())
                    {
                        tagData.data = activeTagData.loraLayer.ToString();
                    }
                    spellFile.tags.Add(tagData);
                }
                spellFile.negativeTags = new List<TagData>();
                foreach (ActiveTagData activeTagData in activeNegativePrompt.activeTags)
                {
                    TagData tagData = new TagData();
                    tagData.prompt = activeTagData.prompt;
                    tagData.colorprompt = activeTagData.colorprompt;
                    tagData.strength = activeTagData.strength;
                    if (activeTagData.classify != null)
                    {
                        tagData.classify = activeTagData.classify.id;
                    }
                    tagData.tagType = activeTagData.tagType;
                    spellFile.negativeTags.Add(tagData);
                }
                string fileStr = JsonConvert.SerializeObject(spellFile);
                using (FileStream fs = File.Open(fileName, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(fileStr);
                        sw.Flush();
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        void ReadClassify(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }
            IEnumerable<string> lines = File.ReadLines(fileName);
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }
                string[] parts = line.Split(':');
                ClassifyInfo classifyInfo = new ClassifyInfo();
                classifyInfo.id = parts[0];
                classifyInfo.name = parts[1];
                classifyInfo.color = (Color)ColorConverter.ConvertFromString(parts[2]);
                classifyInfos.Add(classifyInfo);
            }
        }

        void InitConfig()
        {
            genConfig = new GenConfig();
            genConfig.engineType = generateEngines[0].GetName();
            genConfig.width = 512;
            genConfig.height = 512;
            genConfig.seed = -1;
            genConfig.samplingMethod = SamplingMethod.k_euler_a.webUIName;
            genConfig.samplingSteps = 30;
            genConfig.cfgScale = 7.5f;
            genConfig.denoisingStrength = 0.75f;
            genConfig.autoGroup = true;
            genConfig.highResFix = false;
            genConfig.upscaleBy = 1.5f;
            genConfig.upscaler = "Latent";
        }

        void ReadDicts(string root)
        {
            DirectoryInfo folder = new DirectoryInfo(root);
            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                ReadDict(Path.GetFileNameWithoutExtension(file.FullName), file.FullName);
            }
        }

        public void UpdateSpell()
        {
            activePositivePrompt.UpdateSpell();
            activeNegativePrompt.UpdateSpell();
        }

        void AddToGroupMap(Dictionary<string, List<TagGroupInfo>> tagGroupMap, string id, TagGroupInfo groupInfo)
        {
            List<TagGroupInfo>? tagGroupsA = null;
            if(tagGroupMap.ContainsKey(id))
            {
                tagGroupsA = tagGroupMap[id];
            }
            else
            {
                tagGroupsA = new List<TagGroupInfo>();
                tagGroupMap[id] = tagGroupsA;
            }
            tagGroupsA.Add(groupInfo);
        }

        public ClassifyInfo? GetClassifyInfo(string id)
        {
            foreach(ClassifyInfo classifyInfo in classifyInfos)
            {
                if(classifyInfo.id == id)
                {
                    return classifyInfo;
                }
            }
            return null;
        }

        void ReadDict(string tagGroupName, string fileName)
        {
            if (tagGroupName == "分组")
            {
                return;
            }
            TagGroupInfo tagGroup = null;
            if (tagGroupName == "颜色")
            {
                tagGroup = colors;
                if(tagGroup == null)
                {
                    tagGroup = new TagGroupInfo();
                    tagGroup.name = tagGroupName;
                    colors = tagGroup;
                }
            }
            else if(tagGroupName != "正咒" && tagGroupName != "反咒" && tagGroupName != "分层预设")
            {
                if (!tagGroupInfoMap.TryGetValue(tagGroupName, out tagGroup))
                {
                    tagGroup = new TagGroupInfo();
                    tagGroup.name = tagGroupName;
                    tagGroupInfoMap[tagGroupName] = tagGroup;
                }

            }
            string subGroup = "";

            
            IEnumerable<string> lines = File.ReadLines(fileName);
            ClassifyInfo? classifyInfo = null;
            foreach (string line in lines)
            {
                if(line.Trim().Length == 0)
                {
                    continue;
                }
                if (line.StartsWith(":") && classifyInfo == null)
                {
                    string[] split = line.Split(':');
                    if (split.Length == 3)
                    {
                        classifyInfo = GetClassifyInfo(split[1]);
                        if(classifyInfo != null)
                        {
                           int.TryParse(split[2], out tagGroup.order);
                           tagGroup.classify = classifyInfo;
                        }
                    }
                    continue;
                }

                string kvPair = line.Trim();

                TagExtraInfo extra = null;

                string comment = "";
                int commentIndex = kvPair.IndexOf("//");
                if (commentIndex != -1)
                {
                    comment = kvPair.Substring(commentIndex + 2).Trim();
                    kvPair = kvPair.Substring(0, commentIndex).Trim();

                    string extraStart = "<extra>";
                    string extraEnd = "</extra>";
                    if(comment.IndexOf(extraStart) != -1 && comment.IndexOf(extraEnd) != -1)
                    {
                        int extStartIndex = comment.IndexOf(extraStart);
                        int extEndIndex = comment.IndexOf(extraEnd) + extraEnd.Length;
                        string extStr = comment.Substring(extStartIndex, extEndIndex-extStartIndex);
                        //System.Diagnostics.Debug.WriteLine(extStr);
                        comment = comment.Replace(extStr, "").Trim();
                        string json = extStr.Substring(extraStart.Length, extStr.Length - extraStart.Length - extraEnd.Length);
                        try
                        {
                            extra = JsonConvert.DeserializeObject<TagExtraInfo>(json);
                        }catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                    }
                }

                int eIndex = kvPair.IndexOf('=');
                if (eIndex == -1)
                {
                    if(kvPair.IndexOf("<") != -1 && kvPair.IndexOf(">") != -1)
                    {
                        int startIndex = kvPair.IndexOf('<');
                        int endIndex = kvPair.IndexOf('>');
                        if(startIndex < endIndex)
                        {
                            subGroup = kvPair.Substring(startIndex+1, endIndex - startIndex-1).Trim();
                        }
                    }


                    continue;
                }

                string key = kvPair.Substring(0, eIndex).Trim();
                string value = kvPair.Substring(eIndex + 1).Trim();
               // System.Diagnostics.Trace.WriteLine(key);
               // System.Diagnostics.Trace.WriteLine(value);
                if(tagGroupName == "正咒")
                {
                    TagInfo t = new TagInfo();
                    t.tagType = (int)TagType.TagNormal;
                    t.name = key;
                    t.value = value;
                    activePositivePrompt.initSpells.Add(t);
                    continue;
                }
                if(tagGroupName == "反咒")
                {
                    TagInfo t = new TagInfo();
                    t.tagType = (int)TagType.TagNormal;
                    t.name = key;
                    t.value = value;
                    activeNegativePrompt.initSpells.Add(t);
                    continue;
                }
                if(tagGroupName == "分层预设")
                {
                    LoraLayer loraLayer = new LoraLayer();
                    loraLayer.name = key;
                    loraLayer.isEmpty = false;
                    loraLayer.FromString(value);
                    loraLayers.Add(loraLayer);
                    continue;
                }
                TagInfo tagInfo = new TagInfo();
                tagInfo.tagType = (int)TagType.TagNormal;
                tagInfo.name = key;
                if (value.StartsWith("<color>"))
                {
                    tagInfo.value = value.Substring(7);
                    tagInfo.colorDefault = true;
                }
                else if(value.StartsWith("<color?>"))
                {
                    tagInfo.value = value.Substring(8);
                    tagInfo.colorDefault = false;
                }
                else
                {
                    tagInfo.value = value;
                    tagInfo.colorDefault = false;
                }

                tagInfo.classify = tagGroup.classify;
                if(tagGroupName == "颜色")
                {
                    if(tagInfo.name.Contains(":"))
                    {
                        string[] vs = tagInfo.name.Split(':');
                        tagInfo.colorValue = vs[0];
                        tagInfo.name = vs[1];
                    }
                    else
                    {
                        tagInfo.colorValue = tagInfo.name;
                        tagInfo.name = "";
                    }
                }
                tagInfo.nameInList = tagInfo.name + "    " + tagInfo.value;
                tagInfo.comment = comment;
                tagInfo.tipText = tagInfo.nameInList+(tagInfo.comment==""?"":("\n"+tagInfo.comment));
                tagInfo.extra = extra;
                tagInfo.subGroup = subGroup;

                if(tagGroupName == "颜色")
                {    
                    bool exists = false;
                    foreach(TagInfo t in tagGroup.tags)
                    {
                        if(t.value == tagInfo.value)
                        {
                            //t.CopyFrom(tagInfo);
                            exists = true;
                            //tagInfo = t;
                        }
                    }
                    if(!exists)
                    {
                        tagGroup.tags.Add(tagInfo);
                    }
                }
                else
                {
                    var t = FindGroupAndTagInfo(tagInfo.value);
                    if (t == null)
                    {
                        tagInfo.group = tagGroup.name;
                        tagGroup.tags.Add(tagInfo);
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine("重复的tag：" + t.Value.Item1.name + " " + t.Value.Item2.nameInList + " vs " + tagGroup.name + " " + tagInfo.nameInList);
                    }
                }



                if (tagGroupName == "颜色" && tagInfo.value == "black")
                {
                    black = tagInfo;
                }
            }
            if(tagGroupName == "颜色")
            {
                if(black == null)
                {
                    black = new TagInfo();
                    black.name = "#000000";
                    black.value = "black";
                    black.colorDefault = false;
                    colors.tags.Add(black);
                }
                return;
            }

        }

        public GenerateEngine GetGenerateEngine()
        {
            return generateEngines[selectedEngineIndex];
        }

        public int GetGEIndex(string name)
        {
            for(int i=0; i<generateEngines.Count; i++)
            {
                if(generateEngines[i].GetName() == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public void GenerateImage(int batchSize, int batchCount, bool useRefImage, GenConfig? customConfig=null)
        {
            if (generating)
            {
                return;
            }
            generating = true;
            GenerateEngine ge = GetGenerateEngine();
            GenConfig cfg = customConfig ?? genConfig;

            (Application.Current.MainWindow as MainWindow).ButtonGenerate.Visibility = Visibility.Collapsed;
            (Application.Current.MainWindow as MainWindow).ButtonInterrupt.Visibility = Visibility.Visible;

            if (useRefImage && refImage != null)
            {
                img2img = true;
                ge.Img2Img(genConfig, refImage, batchSize, batchCount, activePositivePrompt.spell, activeNegativePrompt.spell);
            }
            else
            {
                img2img = false;
                ge.Txt2Img(genConfig, batchSize, batchCount, activePositivePrompt.spell, activeNegativePrompt.spell);
            }
            try
            {

            }
            catch { }
        }

        public void DoneGenerate(List<GenImageInfo> images)
        {
            List<GenImageInfo> oldImages = genImageInfos;
            genImageInfos = images;
            (Application.Current.MainWindow as MainWindow).RefreshImages();
            GenerateEnd();
        }

        public void GenerateEnd()
        {
            generating = false;
            (Application.Current.MainWindow as MainWindow).ProgressGen.Visibility = Visibility.Hidden;


            (Application.Current.MainWindow as MainWindow).ButtonGenerate.Visibility = Visibility.Visible;
            (Application.Current.MainWindow as MainWindow).ButtonInterrupt.Visibility = Visibility.Collapsed;
        }

        public void Interrupt()
        {
            if(img2img)
            {
                GetGenerateEngine().Img2ImgInterrupt();
            }
            else
            {
               GetGenerateEngine().Txt2ImgInterrupt();
            }
        }

        public void FecthModels()
        {
            GenerateEngine ge = GetGenerateEngine();
            ge.FetchModels();
        }

        public void ChooseModel(string model)
        {
            GenerateEngine ge = GetGenerateEngine();
            ge.ChooseModel(model);
        }



        public void DeepDanbooru()
        {
            if(refImage == null)
            {
                MessageBox.Show("请先加载要解析的图片");
                return;
            }
            (Application.Current.MainWindow as MainWindow).ButtonToTags.IsEnabled = false;
            GenerateEngine ge = GetGenerateEngine();
            ge.DeepDanbooru(refImage);
        }

        public (TagGroupInfo, TagInfo)? FindGroupAndTagInfo(string prompt)
        {
            foreach (TagGroupInfo groupInfo in AppCore.Instance.tagGroupInfoMap.Values)
            {
                foreach (TagInfo tagInfo in groupInfo.tags)
                {
                    if (tagInfo.value == prompt)
                    {
                        return (groupInfo, tagInfo);
                    }
                }
            }
            return null;
        }


        public TagInfo? FindTagInfo(string prompt)
        {
            foreach (TagGroupInfo groupInfo in AppCore.Instance.tagGroups)
            {
                foreach (TagInfo tagInfo in groupInfo.tags)
                {
                    if (tagInfo.value == prompt)
                    {
                        return tagInfo;
                    }
                }
            }
            return null;
        }

        public TagInfo? FindColorTag(string prompt)
        {
            foreach (TagInfo tagInfo in AppCore.Instance.colors.tags)
            {
                if (tagInfo.value == prompt)
                {
                    return tagInfo;
                }
            }
            return null;
        }

        public ClassifyInfo? FindClassify(string id)
        {
            foreach (ClassifyInfo classifyInfo in AppCore.Instance.classifyInfos)
            {
                if (classifyInfo.id == id)
                {
                    return classifyInfo;
                }
            }
            return null;
        }

        protected void AddTagText(List<TagInfo> tags, string value)
        {
            TagInfo tagInfo = FindTagInfo(value);
            if (tagInfo != null)
            {
                tags.Add(tagInfo);
            }
            else
            {
                int sIndex = value.IndexOf(" ");
                string prefix = "";
                string body = "";
                if (sIndex != -1)
                {
                    prefix = value.Substring(0, sIndex);
                    body = value.Substring(sIndex + 1);
                    System.Diagnostics.Debug.WriteLine(prefix + ":" + body);
                    tagInfo = FindTagInfo(body);
                }
                TagInfo newTagInfo = new TagInfo();
                newTagInfo.tagType = (int)TagType.TagNormal;
                if (tagInfo != null)
                {
                    TagInfo colorTag = FindColorTag(prefix);

                    if (colorTag != null)
                    {
                        newTagInfo.value = tagInfo.value;
                        newTagInfo.classify = tagInfo.classify;
                        //newTagInfo.color = tagInfo.color;
                        newTagInfo.name = tagInfo.name;
                        newTagInfo.colorDefault = true;
                        newTagInfo.nameInList = colorTag.name + tagInfo.name + "    " + value;
                        newTagInfo.comment = tagInfo.comment;
                        newTagInfo.tipText = newTagInfo.nameInList + (tagInfo.comment==""?"":("\n"+tagInfo.comment));
                        newTagInfo.defaultColorTag = colorTag;
                        newTagInfo.group = tagInfo.group;
                    }
                    else
                    {
                        newTagInfo.value = value;
                        newTagInfo.classify = tagInfo.classify;
                        //newTagInfo.color = false;
                        newTagInfo.name = prefix + tagInfo.name;
                        newTagInfo.colorDefault = false;
                        newTagInfo.nameInList = prefix + tagInfo.name + "    " + value;
                        newTagInfo.comment = "";
                        newTagInfo.tipText = newTagInfo.nameInList;
                        newTagInfo.defaultColorTag = null;
                        newTagInfo.group = "非词库";
                    }
                    
                }
                else
                {
                    newTagInfo.value = value;
                    newTagInfo.classify = null;
                    //newTagInfo.color = false;
                    newTagInfo.name = value;
                    newTagInfo.colorDefault = false;
                    newTagInfo.nameInList = value;
                    newTagInfo.comment = "";
                    newTagInfo.tipText= newTagInfo.nameInList;
                    newTagInfo.defaultColorTag = null;
                    newTagInfo.group = "非词库";
                }
                tags.Add(newTagInfo);
            }
        }

        public void SearchTags(string tagText)
        {
            tempGroup.tags.Clear();
            if (tagText.Trim().Length == 0) {
                (Application.Current.MainWindow as MainWindow).UpdateTags();
                return;
            }
            List<TagInfo> tags = new List<TagInfo>();

            if(tagText == "::nopreview")
            {
                foreach (var group in tagGroups)
                {
                    foreach (var tag in group.tags)
                    {
                        if (!tag.hasPreview && !tag.colorDefault)
                        {
                            if (tag.name == "分隔符")
                            {
                                continue;
                            }
                            if (group.name.Contains("无效词"))
                            {
                                continue;
                            }
                            tags.Add(tag);
                        }
                    }
                }
                tempGroup.tags = tags;
                (Application.Current.MainWindow as MainWindow).UpdateTempTags();
                return;
            }
            

            List<TagInfo> equalValueTags = new List<TagInfo>();
            List<TagInfo> equalNameTags = new List<TagInfo>();
            List<TagInfo> otherTags = new List<TagInfo>();

            /*
            BoyerMoore bm = new BoyerMoore(tagText);


            // Create a Stopwatch instance and start it
            Stopwatch watch = Stopwatch.StartNew();

            // Some code to measure
            int sum = 0;
            foreach (var etag in extraDict)
            {
                sum += bm.Search(etag.Value);
                sum += bm.Search(etag.Name);
            }

            // Stop the stopwatch
            watch.Stop();

            // Get the elapsed milliseconds
            long elapsedMs = watch.ElapsedMilliseconds;

            // Print the result
            Debug.WriteLine("The loop1 took {0} milliseconds to execute.", elapsedMs);

            // Create a Stopwatch instance and start it
            watch = Stopwatch.StartNew();

            // Some code to measure
            sum = 0;
            foreach (var etag in extraDict)
            {
                sum += etag.Value.IndexOf(tagText, StringComparison.Ordinal);
                sum += etag.Name.IndexOf(tagText, StringComparison.Ordinal);
            }

            // Stop the stopwatch
            watch.Stop();

            // Get the elapsed milliseconds
            elapsedMs = watch.ElapsedMilliseconds;

            // Print the result
            Debug.WriteLine("The loop2 took {0} milliseconds to execute.", elapsedMs);

            */
            foreach (var group in tagGroups)
            {
                foreach (var tag in group.tags)
                {
                    int i = tag.value.IndexOf(tagText, StringComparison.Ordinal);//bm.Search(tag.value);
                    if (i != -1)
                    {
                        if(i==0 && tag.value.Length == tagText.Length)
                        {
                            equalValueTags.Add(tag);
                        }
                        else
                        {
                            if (otherTags.Count >= 45)
                            {
                                continue;
                            }
                            otherTags.Add(tag);
                        }
                        continue;
                    }
                    if (i == -1)
                    {
                        i = tag.name.IndexOf(tagText, StringComparison.Ordinal);//bm.Search(tag.name);
                    }
                    if(i != -1)
                    {
                        if (i == 0 && tag.name.Length == tagText.Length)
                        {
                            equalNameTags.Add(tag);
                        }
                        else
                        {
                            if (otherTags.Count >= 45)
                            {
                                continue;
                            }
                            otherTags.Add(tag);
                        }
                    }
                }
            }
            if(equalValueTags.Count == 0)
            {
                TagInfo newTagInfo = new TagInfo();
                newTagInfo.tagType = (int)TagType.TagNormal;

                newTagInfo.value = tagText;
                newTagInfo.classify = null;
                    //newTagInfo.color = false;
                newTagInfo.name = tagText;
                newTagInfo.colorDefault = false;
                newTagInfo.nameInList = "自定义Tag："+tagText;
                newTagInfo.comment = "";
                newTagInfo.tipText = newTagInfo.nameInList;
                newTagInfo.defaultColorTag = null;
                newTagInfo.group = "非词库";
                
                tags.Add(newTagInfo);
            }
            tags.AddRange(equalValueTags);
            tags.AddRange(equalNameTags);
            tags.AddRange(otherTags);
            if (tags.Count < 45)
            {
                foreach (var etag in extraDict)
                {
                    if (etag.Value.IndexOf(tagText, StringComparison.Ordinal) != -1 || etag.Name.IndexOf(tagText, StringComparison.Ordinal) != -1)//if(bm.Search(etag.Value) != -1 || bm.Search(etag.Name) != -1)
                    {
                        TagInfo newTagInfo = new TagInfo();
                        newTagInfo.tagType = (int)TagType.TagNormal;

                        newTagInfo.value = etag.Value;
                        newTagInfo.classify = null;
                        //newTagInfo.color = false;
                        newTagInfo.name = etag.Name;
                        newTagInfo.colorDefault = false;
                        newTagInfo.nameInList = etag.Name + "    " + etag.Value;
                        newTagInfo.comment = "";
                        newTagInfo.tipText = newTagInfo.nameInList;
                        newTagInfo.defaultColorTag = null;
                        newTagInfo.group = "Danbooru";

                        tags.Add(newTagInfo);

                        if (tags.Count >= 45)
                        {
                            break;
                        }
                    }
                }
            }
            tempGroup.tags = tags;
            (Application.Current.MainWindow as MainWindow).UpdateTempTags();
        }



        public void ParseDanbooruPrompts(string prompt)
        {
            tempGroup.tags.Clear();
            List<TagInfo> tags = new List<TagInfo>();
            string[] p = prompt.Split(',');
            foreach(string s in p)
            {
                string value = s.Trim();
                if(value.Length == 0)
                {
                    continue;
                }
                int cIndex = value.IndexOf("_\\(");
                if(cIndex != -1)
                {
                    value = value.Substring(0, cIndex);
                }
                value = value.Replace('_', ' ');
                value = value.Trim();

                AddTagText(tags, value);
                System.Diagnostics.Debug.WriteLine(value);
            }
            tempGroup.tags = tags;
            (Application.Current.MainWindow as MainWindow).ButtonToTags.IsEnabled = true;
            //RefreshFunctionGroups();
            (Application.Current.MainWindow as MainWindow).UpdateTempTags();
        }

        public void ParsePromptString(string prompt)
        {
            tempGroup.tags.Clear();
            List<TagInfo> tags = new List<TagInfo>();
            string[] p = prompt.Split(',');
            foreach (string s in p)
            {
                string value = s.Trim();
                if (value.Length == 0)
                {
                    continue;
                }
                int sInex = value.IndexOf(":");
                if (sInex != -1)
                {
                    value = value.Substring(0, sInex);
                }
                value = value.Replace("(", "").Replace(")", "").Replace(":", "").Replace("{", "").Replace("}", "").Replace(" "," ");
                value = value.ToLower();
                value = value.Trim();

                AddTagText(tags, value);
                System.Diagnostics.Debug.WriteLine(value);
            }
            tempGroup.tags = tags;
           // (Application.Current.MainWindow as MainWindow).ButtonToTags.IsEnabled = true;
            //RefreshFunctionGroups();
            (Application.Current.MainWindow as MainWindow).UpdateTempTags();
        }

        void RemoveGroup(TagGroupInfo group)
        {
            if(group != null)
            {
                tagGroups.Remove(group);
            }
        }

        void CheckAddGroup(int index, TagGroupInfo group)
        {
            if(group != null && group.tags.Count > 0)
            {
                tagGroups.Insert(index, group);
            }
        }

        public void RefreshFunctionGroups()
        {
            RemoveGroup(tiGroup);
            RemoveGroup(hyperGroup);
            RemoveGroup(loraGroup);

            CheckAddGroup(0, loraGroup);
            CheckAddGroup(0, hyperGroup);
            CheckAddGroup(0, tiGroup);
        }

        public void LoadEngineFunctionGroup()
        {
            RemoveGroup(tiGroup);
            RemoveGroup(hyperGroup);
            RemoveGroup(loraGroup);
            tiGroup = GetGenerateEngine().tiGroup;
            hyperGroup = GetGenerateEngine().hyperGroup;
            loraGroup = GetGenerateEngine().loraGroup;
            RefreshFunctionGroups();
        }

        public void LoadExtraTags()
        {
            if(!File.Exists(Path.Combine(basePath, "extradict", "danbooru.csv")))
            {
                return;
            }
            using (var reader = new StreamReader(Path.Combine(basePath, "extradict", "danbooru.csv")))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            }))
            {
                csv.Read();
                var records = csv.GetRecords<DaDi>();
                extraDict = new List<DaDi>();

                HashSet<string> keys = new HashSet<string>();
                foreach (var group in tagGroups)
                {
                    foreach (var tag in group.tags)
                    {
                        keys.Add(tag.value);
                    }
                }
                HashSet<string> colorKeys = new HashSet<string>();
                foreach (var tag in colors.tags)
                {
                    colorKeys.Add(tag.value);
                }

                foreach (var record in records)
                {
                    if(!keys.Contains(record.Value))
                    {
                        int indexOfSpace = record.Value.IndexOf(' ');
                        if(indexOfSpace != -1)
                        {
                            string a = record.Value.Substring(0, indexOfSpace);
                            string b = record.Value.Substring(indexOfSpace + 1);
                            if(colorKeys.Contains(a) && keys.Contains(b))
                            {
                                continue;
                            }
                        }
                        keys.Add(record.Value);
                        extraDict.Add(record);
                    }
                }
            }
        }

        public int GetLoraLayerIndex(LoraLayer loraLayer)
        {
            for(int i=0; i<loraLayers.Count; i++)
            {
                if (loraLayers[i].IsEqual(loraLayer))
                    return i;
            }
            return -1;
        }

    }
}
