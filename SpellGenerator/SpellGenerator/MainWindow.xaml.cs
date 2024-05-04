using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using SpellGenerator.app;
using SpellGenerator.app.utils;
using REghZyFramework.Themes;

using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using SpellGenerator.app.tag;
using SpellGenerator.app.file;
using SpellGenerator.app.controller;
using System.Windows.Media.Effects;
using System.Xml.Linq;
using System.Reflection;
using System.Globalization;
using System.Xml.Schema;


namespace SpellGenerator
{
    class DragAdorner : Adorner
    {
        public DragAdorner(UIElement owner) : base(owner) { }

        public DragAdorner(UIElement owner, UIElement adornElement, bool useVisualBrush, double opacity)
            : base(owner)
        {
            _owner = owner;
            VisualBrush _brush = new VisualBrush
            {
                Opacity = opacity,
                Visual = adornElement
            };

            DropShadowEffect dropShadowEffect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 15,
                Opacity = opacity
            };

            Rectangle r = new Rectangle
            {
                RadiusX = 3,
                RadiusY = 3,
                Fill = _brush,
                Effect = dropShadowEffect,
                Width = adornElement.DesiredSize.Width * scale,
                Height = adornElement.DesiredSize.Height * scale
            };


            XCenter = adornElement.DesiredSize.Width / 2 * scale;
            YCenter = adornElement.DesiredSize.Height / 2 * scale;

            _child = r;
        }

        private void UpdatePosition()
        {
            AdornerLayer adorner = (AdornerLayer)Parent;
            if (adorner != null)
            {
                adorner.Update(AdornedElement);
            }
        }

        #region Overrides

        protected override Visual GetVisualChild(int index)
        {
            return _child;
        }
        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }
        protected override Size MeasureOverride(Size finalSize)
        {
            _child.Measure(finalSize);
            return _child.DesiredSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {

            _child.Arrange(new Rect(_child.DesiredSize));
            return finalSize;
        }
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup result = new GeneralTransformGroup();

            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
            return result;
        }

        #endregion

        #region Field & Properties

        public double scale = 1;
        protected UIElement _child;
        protected VisualBrush _brush;
        protected UIElement _owner;
        protected double XCenter;
        protected double YCenter;
        private double _leftOffset;
        public double LeftOffset
        {
            get { return _leftOffset; }
            set
            {
                _leftOffset = value - XCenter;
                UpdatePosition();
            }
        }
        private double _topOffset;
        public double TopOffset
        {
            get { return _topOffset; }
            set
            {
                _topOffset = value - YCenter;

                UpdatePosition();
            }
        }

        #endregion
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private SaveFileDialog saveImageFileDialog = new SaveFileDialog();
        private SaveFileDialog saveSpellFileDialog = new SaveFileDialog();
        private OpenFileDialog openRefFileDialog = new OpenFileDialog();
        private OpenFileDialog openSpellFileDialog = new OpenFileDialog();

        private DragAdorner currentAdorner;

        private bool dragInProgress = false;

        private bool changingDefaultPrompt = false;
        private bool changingComboDefaultPrompt = false;

        public MainWindow()
        {
            InitializeComponent();

            ResizeWindow();

            AppCore.Instance.Initialize();
            AppCore.Instance.LoadEngineFunctionGroup();
            UpdateTagGroups();
            TextDefaultPrompt.Text = AppCore.Instance.currentActivePrompt.defaultSpell;
            UpdateSpell();

            ComboBoxEngine.ItemsSource = AppCore.Instance.generateEngines;


            LoadConfig();
            TextBatchCount.Content = 1;
            SliderBatchCount.Value = 1;
            ProgressGen.Visibility = Visibility.Hidden;

            LoadLastSpell();
            UpdateActivePrompt();

            AppController.Instance.onEngineBaseDataLoadedChange += Instance_onEngineBaseDataLoadedChange;
        }

        private void Instance_onEngineBaseDataLoadedChange()
        {
            UpdateAILoaded();

            AppCore.Instance.LoadEngineFunctionGroup();
            UpdateTagGroups();
        }

        void ResizeWindow()
        {
            double kW = SystemParameters.WorkArea.Width;
            double kH = SystemParameters.WorkArea.Height;
            if (this.MinWidth > kW)
            {
                this.MinWidth = kW;
            }
            if (this.MinHeight > kH)
            {
                this.MinHeight = kH;
            }
            if (this.Width > kW)
            {
                this.Width = kW;
            }
            if (this.Height > kH)
            {
                this.Height = kH;
            }
        }

        public void RefreshSamplingMethods()
        {
            ComboBoxSamplingMethod.ItemsSource = AppCore.Instance.GetGenerateEngine().GetSamplingMethods();
            ComboBoxSamplingMethod.SelectedIndex = AppCore.Instance.GetGenerateEngine().GetSamplingMethods().FindIndex(x => x.webUIName == AppCore.Instance.genConfig.samplingMethod);
            if (ComboBoxSamplingMethod.SelectedIndex == -1)
            {
                ComboBoxSamplingMethod.SelectedIndex = 0;
            }
        }

        public void RefreshUpscalers()
        {
            ComboBoxUpscaler.ItemsSource = AppCore.Instance.GetGenerateEngine().GetUpscalers();
            ComboBoxUpscaler.SelectedIndex = AppCore.Instance.GetGenerateEngine().GetUpscalers().FindIndex(x => x == AppCore.Instance.genConfig.upscaler);
            if (ComboBoxUpscaler.SelectedIndex == -1)
            {
                ComboBoxUpscaler.SelectedIndex = 0;
            }
        }

        public void RefreshVaes()
        {
            ComboBoxVae.ItemsSource = AppCore.Instance.GetGenerateEngine().GetVaes();
            ComboBoxVae.SelectedIndex = AppCore.Instance.GetGenerateEngine().GetVaes().FindIndex(x => x == AppCore.Instance.genConfig.vae);
            if (ComboBoxVae.SelectedIndex == -1)
            {
                ComboBoxVae.SelectedIndex = 0;
            }
        }


        void LoadConfig()
        {
            ComboBoxEngine.SelectedIndex = AppCore.Instance.GetGEIndex(AppCore.Instance.genConfig.engineType);
            if (ComboBoxEngine.SelectedIndex == -1)
            {
                ComboBoxEngine.SelectedIndex = 0;
            }
            AppCore.Instance.selectedEngineIndex = ComboBoxEngine.SelectedIndex;
            RefreshSamplingMethods();
            TextImageWidth.Content = AppCore.Instance.genConfig.width;
            SliderImageWidth.Value = AppCore.Instance.genConfig.width;
            TextImageHeight.Content = AppCore.Instance.genConfig.height;
            SliderImageHeight.Value = AppCore.Instance.genConfig.height;
            TextSeed.Text = AppCore.Instance.genConfig.seed.ToString();
            TextSamplingSteps.Content = AppCore.Instance.genConfig.samplingSteps;
            SliderSamplingSteps.Value = AppCore.Instance.genConfig.samplingSteps;
            TextCFGScale.Content = AppCore.Instance.genConfig.cfgScale;
            SliderCFGScale.Value = AppCore.Instance.genConfig.cfgScale;
            TextDenoisingStrength.Content = AppCore.Instance.genConfig.denoisingStrength;
            SliderDenoisingStrength.Value = AppCore.Instance.genConfig.denoisingStrength;
            CheckAutoGroup.IsChecked = AppCore.Instance.genConfig.autoGroup;
            CheckHighResFix.IsChecked = AppCore.Instance.genConfig.highResFix;
            RefreshUpscalers();
            TextScaleBy.Content = AppCore.Instance.genConfig.upscaleBy;
            SliderScaleBy.Value = AppCore.Instance.genConfig.upscaleBy;
            UpdateUpscaleState();
            RefreshVaes();

            UpdateAILoaded();
        }

        void UpdateAILoaded()
        {
            ButtonLoadBaseData.Visibility = AppCore.Instance.GetGenerateEngine().IsBaseDataLoaded() ? Visibility.Collapsed : Visibility.Visible;
        }

        void UpdateUpscaleState()
        {
            if (!AppCore.Instance.GetGenerateEngine().CanHighResFix())
            {
                TextHighResState.Content = "不支持高清修复";
            }
            else if (!AppCore.Instance.genConfig.highResFix)
            {
                TextHighResState.Content = "未开启高清修复";
            }
            else
            {
                TextHighResState.Content = "高清修复后的图片大小：" + Math.Floor(AppCore.Instance.genConfig.upscaleBy * AppCore.Instance.genConfig.width) + "x" + Math.Floor(AppCore.Instance.genConfig.upscaleBy * AppCore.Instance.genConfig.height);
            }
        }

        public void UpdateTagGroups()
        {
            TagGroupInfo groupInfo = ListTagGroups.SelectedItem as TagGroupInfo;
            ListTagGroups.ItemsSource = null;
            ListTagGroups.ItemsSource = AppCore.Instance.tagGroups;
            if (groupInfo != null)
            {
                if (AppCore.Instance.tagGroups.Contains(groupInfo))
                {
                    ListTagGroups.SelectedIndex = AppCore.Instance.tagGroups.IndexOf(groupInfo);
                }
                else
                {
                    UpdateTags();
                }
            }

        }

        private void ListTagGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTags();

        }

        public void UpdateTags()
        {
            TagGroupInfo groupInfo = ListTagGroups.SelectedItem as TagGroupInfo;
            if(groupInfo == null)
            {
                groupInfo = AppCore.Instance.tempGroup;
            }
            if (groupInfo != null)
            {
                List<TagInfo> tags = new List<TagInfo>();
                foreach (TagInfo tag in groupInfo.tags)
                {
                    if(AppCore.Instance.currentActivePrompt == AppCore.Instance.activeNegativePrompt &&(tag.tagType == (int)TagType.TagHypernetwork || tag.tagType == (int)TagType.TagLora))
                    {
                        continue;
                    }
                    if (!(tag.onlyOnce && AppCore.Instance.currentActivePrompt.usedTags.Contains(tag.value)))
                    {
                        tags.Add(tag);
                    }
                }
                if (groupInfo != AppCore.Instance.tempGroup && tags.Find(a => a.subGroup != "") != null)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(tags);
                    view.GroupDescriptions.Add(new PropertyGroupDescription("subGroup"));
                    ListTags.ItemsSource = view;
                }
                else
                {
                    ListTags.ItemsSource = tags;
                }
            }

        }

        private void ButtonCopyNagativePrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(AppCore.Instance.activeNegativePrompt.spell);
            } catch { }
        }

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTag();
        }

        private void ListTags_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddTag();
        }

        void AddTag()
        {
            //TagGroupInfo groupInfo = ListTagGroups.SelectedItem as TagGroupInfo;
            TagInfo tagInfo = ListTags.SelectedItem as TagInfo;
            if (tagInfo == null)
            {
                return;
            }
            ActiveTagData tagData = new ActiveTagData();
            tagData.tagType = tagInfo.tagType;
            tagData.strength = 10;
            tagData.tagInfo = tagInfo;
            tagData.prompt = tagInfo.value;
            tagData.value = tagInfo.value;
            if (tagInfo.classify == null && tagInfo.tagType < 10)
            {
                DialogChooseClassify dialogChooseClassify = new DialogChooseClassify();
                dialogChooseClassify.Title = "选择分组：" + tagInfo.nameInList;
                dialogChooseClassify.Owner = this;
                if (dialogChooseClassify.ShowDialog() != true)
                {
                    return;
                }
                tagInfo.classify = AppCore.Instance.classifyInfos[dialogChooseClassify.ComboBoxClassify.SelectedIndex];
            }
            tagData.classify = tagInfo.classify;
            //if (tagInfo.color)
            {
                if (tagInfo.defaultColorTag != null)
                {
                    tagData.colorTag = tagInfo.defaultColorTag;
                    tagData.colorprompt = tagData.colorTag.value;
                }
                else if (tagInfo.colorDefault)
                {
                    tagData.colorTag = AppCore.Instance.black;
                    tagData.colorprompt = "black";
                }

            }
            AppCore.Instance.currentActivePrompt.Add(tagData);
            //if (tagInfo.tagType < 10 && (tagInfo.extra == null || !tagInfo.extra.multiple)) tagInfo.used = true;
            TagControl tagControl = new TagControl();
            tagControl.InitTag(tagData);
            tagData.control = tagControl;
            TagsContainer.Children.Add(tagControl);
            UpdateTags();
            if (AppCore.Instance.genConfig.autoGroup)
            {
                SortTags();
            }
            UpdateSpell();
        }


        void AddToGroupMap(Dictionary<string, List<ActiveTagData>> tagGroupMap, string id, ActiveTagData tagData)
        {
            List<ActiveTagData>? tags = null;
            if (tagGroupMap.ContainsKey(id))
            {
                tags = tagGroupMap[id];
            }
            else
            {
                tags = new List<ActiveTagData>();
                tagGroupMap[id] = tags;
            }
            tags.Add(tagData);
        }

        void SortTags()
        {
            Dictionary<string, List<ActiveTagData>> tagGroupMap = new Dictionary<string, List<ActiveTagData>>();
            List<int> fixedTagIndex = new List<int>();
            List<ActiveTagData> fixedTags = new List<ActiveTagData>();
            for (int i = 0; i < AppCore.Instance.currentActivePrompt.activeTags.Count; i++)
            {
                ActiveTagData tagData = AppCore.Instance.currentActivePrompt.activeTags[i];
                if (tagData.tagType >= 10 || tagData.tagInfo != null && tagData.tagInfo.extra != null && tagData.tagInfo.extra.freeMovement)
                {
                    fixedTagIndex.Add(i);
                    fixedTags.Add(tagData);
                    continue;
                }
                if (tagData.classify != null)
                {
                    AddToGroupMap(tagGroupMap, tagData.classify.id, tagData);
                }
                else
                {
                    AddToGroupMap(tagGroupMap, "none", tagData);
                }
            }
            AppCore.Instance.currentActivePrompt.activeTags.Clear();
            foreach (ClassifyInfo classifyInfo in AppCore.Instance.classifyInfos)
            {
                if (tagGroupMap.ContainsKey(classifyInfo.id))
                {
                    AppCore.Instance.currentActivePrompt.activeTags.AddRange(tagGroupMap[classifyInfo.id]);
                }
            }
            if (tagGroupMap.ContainsKey("none"))
            {
                AppCore.Instance.currentActivePrompt.activeTags.AddRange(tagGroupMap["none"]);
            }
            for (int i = 0; i < fixedTags.Count; i++)
            {
                if (fixedTagIndex[i] < AppCore.Instance.currentActivePrompt.activeTags.Count)
                {
                    AppCore.Instance.currentActivePrompt.activeTags.Insert(fixedTagIndex[i], fixedTags[i]);
                }
                else
                {
                    AppCore.Instance.currentActivePrompt.activeTags.Add(fixedTags[i]);
                }
            }
            RefreshActiveTags();
        }

        public void RefreshActiveTags()
        {
            TagsContainer.Children.Clear();
            foreach (ActiveTagData tagData in AppCore.Instance.currentActivePrompt.activeTags)
            {
                TagsContainer.Children.Add(tagData.control);
            }
        }

        public void RemoveTag(TagControl tagControl)
        {
            AppCore.Instance.currentActivePrompt.Remove(tagControl.tagData);
            TagsContainer.Children.Remove(tagControl);
            UpdateTags();
            UpdateSpell();
        }

        private void ButtonCopyPositivePrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(AppCore.Instance.activePositivePrompt.spell);
            } catch (Exception ex)
            {

            }
        }

        private void TextCustomSpell_TextChanged(object sender, TextChangedEventArgs e)
        {
            //UpdateSpell();
        }

        public void UpdateSpell()
        {
            AppCore.Instance.UpdateSpell();
            TextPrompt.Text = AppCore.Instance.currentActivePrompt.spell;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sInfo = new System.Diagnostics.ProcessStartInfo(LinkWeb.NavigateUri.AbsoluteUri)
                {
                    UseShellExecute = true,
                };
                System.Diagnostics.Process.Start(sInfo);
            }
            catch (Exception ex)
            {

            }
        }

        private void ButtonGenerate_Click(object sender, RoutedEventArgs e)
        {
            SaveLastSpell();
            //_ = Post();
            AppCore.Instance.GenerateImage(1, (int)SliderBatchCount.Value, CheckRefImage.IsChecked == true);
        }




        public void RefreshImages()
        {
            ImagePreview.Source = null;
            ListImages.ItemsSource = AppCore.Instance.genImageInfos;
            if (AppCore.Instance.genImageInfos.Count > 0)
            {
                ListImages.SelectedIndex = 0;
            }
        }

        private void ListImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreviewImage();

        }

        private void UpdatePreviewImage()
        {
            if (ListImages.SelectedIndex < 0)
            {
                return;
            }
            GenImageInfo imageInfo = AppCore.Instance.genImageInfos[ListImages.SelectedIndex];
            ImagePreview.Source = imageInfo.image;
            TextImageDesc.Content = "Seed: " + imageInfo.seed;
        }

        private void SliderBatchCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBatchCount == null)
            {
                return;
            }
            TextBatchCount.Content = (int)SliderBatchCount.Value;
        }

        private void SliderImageWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextImageWidth == null)
            {
                return;
            }
            TextImageWidth.Content = (int)SliderImageWidth.Value;
            AppCore.Instance.genConfig.width = (int)SliderImageWidth.Value;
            UpdateUpscaleState();
        }

        private void SliderImageHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextImageHeight == null)
            {
                return;
            }
            TextImageHeight.Content = (int)SliderImageHeight.Value;
            AppCore.Instance.genConfig.height = (int)SliderImageHeight.Value;
            UpdateUpscaleState();
        }

        private void TextSeed_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^-?[0-9]*$");
            System.Diagnostics.Debug.WriteLine(e.Text);
            if (!regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
            if (e.Text == "-")
            {
                e.Handled = false;
                return;
            }
            /*
            long v;
            if(!long.TryParse(e.Text, out v))
            {
                e.Handled = true;
                return;
            }
            if(v<-1)
            {
                return;
            }*/
        }

        private void TextSeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }
            long seed = 0;
            if (TextSeed.Text == "-")
            {
                seed = 0;
            }
            else if (TextSeed.Text == "")
            {
                seed = 0;
            }
            else if (!long.TryParse(TextSeed.Text, out seed))
            {
                seed = -1;
                TextSeed.Text = "-1";
            }
            if (seed < -1)
            {
                seed = -1;
                TextSeed.Text = "-1";
            }
            AppCore.Instance.genConfig.seed = seed;
        }

        private void ButtonUseSeed_Click(object sender, RoutedEventArgs e)
        {
            if (ListImages.SelectedIndex < 0)
            {
                return;
            }
            GenImageInfo imageInfo = AppCore.Instance.genImageInfos[ListImages.SelectedIndex];
            TextSeed.Text = imageInfo.seed.ToString();
            AppCore.Instance.genConfig.seed = imageInfo.seed;
        }

        private void ButtonAsRef_Click(object sender, RoutedEventArgs e)
        {
            if (ListImages.SelectedIndex < 0)
            {
                return;
            }
            GenImageInfo imageInfo = AppCore.Instance.genImageInfos[ListImages.SelectedIndex];
            AppCore.Instance.refImage = imageInfo;
            ImageRef.Source = imageInfo.image;
        }

        private void ButtonSaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (ListImages.SelectedIndex < 0)
            {
                return;
            }
            try
            {
                GenImageInfo imageInfo = AppCore.Instance.genImageInfos[ListImages.SelectedIndex];
                saveImageFileDialog.Title = "保存图片";
                saveImageFileDialog.FileName = imageInfo.defaultFileName != null ? imageInfo.defaultFileName : "未命名";
                saveImageFileDialog.Filter = "图片文件(*.png)|*.png";
                if (saveImageFileDialog.ShowDialog() == true)
                {
                    string fileName = saveImageFileDialog.FileName;
                    using (FileStream fs = File.Open(fileName, FileMode.Create))
                    {
                        fs.Write(imageInfo.imageData, 0, imageInfo.imageData.Length);
                    }
                }
            } catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        private void ComboBoxSamplingMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxSamplingMethod.SelectedIndex < 0)
            {
                return;
            }
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }
            AppCore.Instance.genConfig.samplingMethod = AppCore.Instance.GetGenerateEngine().GetSamplingMethods()[ComboBoxSamplingMethod.SelectedIndex].webUIName;
        }

        private void SliderSamplingSteps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextSamplingSteps == null)
            {
                return;
            }
            TextSamplingSteps.Content = (int)SliderSamplingSteps.Value;
            AppCore.Instance.genConfig.samplingSteps = (int)SliderSamplingSteps.Value;
        }

        private void SliderCFGScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextCFGScale == null)
            {
                return;
            }
            TextCFGScale.Content = (float)SliderCFGScale.Value;
            AppCore.Instance.genConfig.cfgScale = (float)SliderCFGScale.Value;
        }

        private void SliderDenoisingStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextDenoisingStrength == null)
            {
                return;
            }
            TextDenoisingStrength.Content = (float)SliderDenoisingStrength.Value;
            AppCore.Instance.genConfig.denoisingStrength = (float)SliderDenoisingStrength.Value;
        }

        private void ComboBoxEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxEngine.SelectedIndex < 0)
            {
                return;
            }
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }
            AppCore.Instance.genConfig.engineType = AppCore.Instance.generateEngines[ComboBoxEngine.SelectedIndex].GetName();
            AppCore.Instance.selectedEngineIndex = ComboBoxEngine.SelectedIndex;
            RefreshSamplingMethods();
            RefreshUpscalers();
            RefreshVaes();
            RefreshModels();
            UpdateUpscaleState();
            UpdateAILoaded();
            AppCore.Instance.LoadEngineFunctionGroup();
            UpdateTagGroups();
        }

        private void CheckAutoGroup_Click(object sender, RoutedEventArgs e)
        {
            AppCore.Instance.genConfig.autoGroup = CheckAutoGroup.IsChecked == true;
            if (AppCore.Instance.genConfig.autoGroup)
            {
                SortTags();
                UpdateSpell();
            }
        }


        private void MenuItemClear_Click(object sender, RoutedEventArgs e)
        {
            ClearSpell();
        }

        void ClearSpell()
        {
            AppCore.Instance.currentActivePrompt.Clear();
            TagsContainer.Children.Clear();
            UpdateTags();
            UpdateSpell();
        }

        void UpdateActivePrompt()
        {
            TextDefaultPromptTip.Content = AppCore.Instance.currentActivePrompt.name + "起手";

            changingDefaultPrompt = true;
            TextDefaultPrompt.Text = AppCore.Instance.currentActivePrompt.defaultSpell;
            changingDefaultPrompt = false;
            List<String> showInitTags = new List<string>();
            int matchIndex = -1;
            for(int i=0; i< AppCore.Instance.currentActivePrompt.initSpells.Count; i++)
            {
                showInitTags.Add(AppCore.Instance.currentActivePrompt.initSpells[i].name);
                if(AppCore.Instance.currentActivePrompt.initSpells[i].value == AppCore.Instance.currentActivePrompt.defaultSpell)
                {
                    matchIndex = i;
                }
            }
            showInitTags.Add("自定义");
            changingComboDefaultPrompt = true;
            ComboBoxDefaultPrompt.ItemsSource = showInitTags;
            if(matchIndex != -1)
            {
                ComboBoxDefaultPrompt.SelectedIndex = matchIndex;
            }
            else
            {
                ComboBoxDefaultPrompt.SelectedIndex = showInitTags.Count-1;
            }
            changingComboDefaultPrompt = false;
            TextPrompt.Text = AppCore.Instance.currentActivePrompt.spell;
            RefreshActiveTags();
            UpdateTags();
        }

        private void MenuItemSaveSpell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveSpellFileDialog.Title = "保存咒文";
                saveSpellFileDialog.FileName = "未命名";
                saveSpellFileDialog.Filter = "咒语文件(*.spl)|*.spl";
                if (saveSpellFileDialog.ShowDialog() == true)
                {
                    string fileName = saveSpellFileDialog.FileName;
                    AppCore.Instance.SaveSpell(fileName);
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        private void OpenRefImage(string fileName)
        {
            try
            {
                using (FileStream fs = File.OpenRead(fileName))
                {
                    byte[] imageData = new byte[fs.Length];
                    var bitmap = new BitmapImage();
                    fs.Read(imageData, 0, System.Convert.ToInt32(fs.Length));
                    bitmap.BeginInit();
                    bitmap.StreamSource = new System.IO.MemoryStream(imageData);
                    bitmap.EndInit();
                    GenImageInfo imageInfo = new GenImageInfo();
                    imageInfo.seed = -1;
                    imageInfo.imageData = imageData;
                    imageInfo.image = bitmap;
                    imageInfo.imageType = System.IO.Path.GetExtension(fileName).Substring(1).ToLower();
                    AppCore.Instance.refImage = imageInfo;
                    ImageRef.Source = imageInfo.image;
                };

            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        private void ButtonBrowseRef_Click(object sender, RoutedEventArgs e)
        {
            openRefFileDialog.Title = "打开参考图";
            openRefFileDialog.Filter = "图片文件(*.png;*.jpg;*.bmp) | *.png;*.jpg;*.bmp";
            if (openRefFileDialog.ShowDialog() == true)
            {
                string fileName = openRefFileDialog.FileName;
                OpenRefImage(fileName);
            }


        }

        void LoadSpell(string fileName)
        {
            try
            {
                using (FileStream fs = File.OpenRead(fileName))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string json = sr.ReadToEnd();
                        System.Diagnostics.Debug.WriteLine(json);

                        SpellFile? spellFile = JsonConvert.DeserializeObject<SpellFile>(json);
                        if (spellFile != null)
                        {
                            AppCore.Instance.genConfig = spellFile.config;
                            AppCore.Instance.activePositivePrompt.defaultSpell = spellFile.positiveDefaultSpell;
                            AppCore.Instance.activeNegativePrompt.defaultSpell = spellFile.negativeSpell;

                            TextDefaultPrompt.Text = AppCore.Instance.currentActivePrompt.defaultSpell;

                            LoadConfig();
                            AppCore.Instance.activePositivePrompt.Clear();
                            AppCore.Instance.activeNegativePrompt.Clear();
                            ClearSpell();

                            AppCore.Instance.activePositivePrompt.Load(spellFile.tags);
                            if(spellFile.negativeTags == null)
                            {
                                spellFile.negativeTags = new List<TagData>();
                            }
                            AppCore.Instance.activeNegativePrompt.Load(spellFile.negativeTags);

                            UpdateTags();
                            UpdateSpell();
                            UpdateActivePrompt();
                        }
                    }

                };

            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        private void MenuItemLoadSpell_Click(object sender, RoutedEventArgs e)
        {
            openSpellFileDialog.Title = "打开咒文";
            openSpellFileDialog.Filter = "咒语文件(*.spl)|*.spl";
            if (openSpellFileDialog.ShowDialog() == true)
            {
                string fileName = openSpellFileDialog.FileName;
                LoadSpell(fileName);
            }
        }

        private void SaveLastSpell()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string lastSpellPath = System.IO.Path.Combine(basePath, "lastSpell.spl");
            try
            {
                AppCore.Instance.SaveSpell(lastSpellPath);
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        private void LoadLastSpell()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string lastSpellPath = System.IO.Path.Combine(basePath, "lastSpell.spl");
            try
            {
                if (File.Exists(lastSpellPath))
                {
                    LoadSpell(lastSpellPath);
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex.ToString()); }
        }

        private void ButtonFetchModels_Click(object sender, RoutedEventArgs e)
        {
            AppCore.Instance.FecthModels();
        }

        public void RefreshModels()
        {
            ComboBoxModel.SelectedIndex = -1;
            ComboBoxModel.ItemsSource = null;
            ComboBoxModel.ItemsSource = AppCore.Instance.GetGenerateEngine().GetModels();

            if (AppCore.Instance.GetGenerateEngine().GetCurrentModel() != null)
            {
                int index = AppCore.Instance.GetGenerateEngine().GetModels().IndexOf(AppCore.Instance.GetGenerateEngine().GetCurrentModel());
                ComboBoxModel.SelectedIndex = index;
            }
            else
            {
                ComboBoxModel.SelectedIndex = -1;
            }
        }

        private void ComboBoxModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxModel.SelectedIndex == -1)
            {
                return;
            }
            if (AppCore.Instance.GetGenerateEngine().GetCurrentModel() == AppCore.Instance.GetGenerateEngine().GetModels()[ComboBoxModel.SelectedIndex])
            {
                return;
            }
            DisableOperations();
            AppCore.Instance.ChooseModel(AppCore.Instance.GetGenerateEngine().GetModels()[ComboBoxModel.SelectedIndex]);
        }

        private void DisableOperations()
        {
            ButtonGenerate.IsEnabled = false;
            ButtonFetchModels.IsEnabled = false;
            ComboBoxModel.IsEnabled = false;
        }

        public void EnableOperations()
        {
            ButtonGenerate.IsEnabled = true;
            ButtonFetchModels.IsEnabled = true;
            ComboBoxModel.IsEnabled = true;
        }

        private void ImageRef_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (filePaths.Length == 0)
                {
                    return;
                }
                string ext = System.IO.Path.GetExtension(filePaths[0]);
                if (ext != ".png" && ext != ".jpg" && ext != ".bmp")
                {
                    return;
                }
                OpenRefImage(filePaths[0]);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void ButtonToTags_Click(object sender, RoutedEventArgs e)
        {
            AppCore.Instance.DeepDanbooru();
        }

        public void UpdateTempTags()
        {
            ListTagGroups.SelectedIndex = -1;
            UpdateTags();
        }


        private void MenuItemParsePrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string prompt = Clipboard.GetText();
                if (prompt != null && prompt.Trim().Length > 0)
                {
                    AppCore.Instance.ParsePromptString(prompt);
                }
            }
            catch { }
        }



        private async Task LoadAndOpenBatchDialog()
        {
            await AppCore.Instance.GetGenerateEngine().LoadBaseData();
            var dialog = new DialogBatch();
            dialog.useRefImage = CheckRefImage.IsChecked == true;
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void ButtonSavePreview_Click(object sender, RoutedEventArgs e)
        {
            if (ListImages.SelectedIndex < 0)
            {
                return;
            }
            GenImageInfo imageInfo = AppCore.Instance.genImageInfos[ListImages.SelectedIndex];
            if (imageInfo.image.DecodePixelWidth != imageInfo.image.DecodePixelHeight)
            {
                if (MessageBox.Show("该图片高度和宽度不同，会影响预览的效果，是否继续？", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            if (AppCore.Instance.currentActivePrompt.activeTags.Count == 0)
            {
                MessageBox.Show("当前没有可以生成预览的词条");
                return;
            }
            var dialog = new DialogChooseTag();
            dialog.Owner = this;
            var result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }
            var prompt = AppCore.Instance.currentActivePrompt.activeTags[dialog.ComboBoxTag.SelectedIndex].prompt;
            if (AppCore.Instance.currentActivePrompt.activeTags[dialog.ComboBoxTag.SelectedIndex].tagType == (int)TagType.TagTextInversion)
            {
                prompt = "ti:" + prompt;
            }
            var filename = FileNameUtil.Encode(prompt) + ".jpg";
            filename = System.IO.Path.Combine(AppCore.Instance.previewPacks[dialog.ComboBoxPack.SelectedIndex].location, filename);
            AppCore.Instance.recordLastPreview = dialog.CheckRecordPreview.IsChecked == true;
            if (AppCore.Instance.recordLastPreview)
            {
                AppCore.Instance.lastSelPreviewPack = AppCore.Instance.previewPacks[dialog.ComboBoxPack.SelectedIndex];
            }
            else
            {
                AppCore.Instance.lastSelPreviewPack = null;
            }
            if (File.Exists(filename))
            {
                if (MessageBox.Show("此词条已有预览图，是否覆盖？", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            try
            {
                BitmapImage temp = new BitmapImage();

                temp.BeginInit();

                if (imageInfo.image.DecodePixelWidth >= imageInfo.image.DecodePixelHeight)
                {
                    temp.DecodePixelWidth = 512;
                }
                else
                {
                    temp.DecodePixelHeight = 512;
                }
                temp.StreamSource = new MemoryStream(imageInfo.imageData);
                temp.CreateOptions = BitmapCreateOptions.None;
                temp.CacheOption = BitmapCacheOption.Default;
                temp.EndInit();
                BitmapEncoder encoder = new JpegBitmapEncoder();
                MemoryStream stream = new MemoryStream();
                encoder.Frames.Add(BitmapFrame.Create(temp as BitmapSource));
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                byte[] data = new byte[stream.Length];
                BinaryReader br = new BinaryReader(stream);
                br.Read(data, 0, (int)stream.Length);
                br.Close();
                stream.Close();
                using (FileStream fs = File.Open(filename, FileMode.Create))
                {
                    fs.Write(data, 0, data.Length);
                }

                AppCore.Instance.previewImageInfos[FileNameUtil.Encode(prompt)] = new PreviewImageInfo(filename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void MenuItemReload_Click(object sender, RoutedEventArgs e)
        {
            AppCore.Instance.activePositivePrompt.initSpells.Clear();
            AppCore.Instance.activeNegativePrompt.initSpells.Clear();
            AppCore.Instance.LoadDict();
            AppCore.Instance.RefreshFunctionGroups();
            ListTagGroups.ItemsSource = null;
            ListTagGroups.ItemsSource = AppCore.Instance.tagGroups;
            ListTags.ItemsSource = null;
        }



        private void ButtonLoadBaseData_Click(object sender, RoutedEventArgs e)
        {
            _ = AppCore.Instance.GetGenerateEngine().LoadBaseData();
        }

        private void ComboBoxUpscaler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }

            if (ComboBoxUpscaler.SelectedIndex == -1)
            {
                return;
            }

            AppCore.Instance.genConfig.upscaler = AppCore.Instance.GetGenerateEngine().GetUpscalers()[ComboBoxUpscaler.SelectedIndex];
        }

        private void CheckHighResFix_Click(object sender, RoutedEventArgs e)
        {
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }
            AppCore.Instance.genConfig.highResFix = CheckHighResFix.IsChecked == true;
            UpdateUpscaleState();
        }

        private void SliderScaleBy_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }
            if (TextScaleBy == null)
            {
                return;
            }
            TextScaleBy.Content = (float)SliderScaleBy.Value;
            AppCore.Instance.genConfig.upscaleBy = (float)SliderScaleBy.Value;
            UpdateUpscaleState();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveLastSpell();
        }

        private void ButtonFetchCards_Click(object sender, RoutedEventArgs e)
        {
            AppCore.Instance.GetGenerateEngine().FetchExtraModels();
        }

        private void TagsContainer_DragOver(object sender, DragEventArgs e)
        {
           
            dragInProgress = true;
            TagControl dragControl = null;
            try
            {
                IDataObject dataObject = e.Data;
                if(dataObject.GetData(typeof(TagControl)) == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }
                dragControl = (TagControl)dataObject.GetData(typeof(TagControl));
            }
            catch
            {
                e.Effects = DragDropEffects.None;
                return;
            }
            WrapPanel panel = sender as WrapPanel;
            if (panel != null)
            {
                // 获取鼠标相对于WrapPanel的位置
                Point position = e.GetPosition(panel);
                //System.Diagnostics.Debug.WriteLine(position);

                e.Effects = DragDropEffects.Move;

                // 定义两个变量，分别表示左边和右边最接近鼠标位置的子控件
                UIElement leftElement = null;
                UIElement rightElement = null;
                int insertIndex = -1;

                if(panel.Children.Count == 0)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // 遍历子控件，找到左边和右边最接近鼠标位置的子控件
               // System.Diagnostics.Debug.WriteLine(panel.Children.Count);
                for (int i=0; i<panel.Children.Count; i++)
                {
                    var element = panel.Children[i];
                    UIElement nextElement = null;
                    if(i+1<panel.Children.Count)
                    {
                        nextElement = panel.Children[i+1];
                    }
                   // System.Diagnostics.Debug.WriteLine(panel.Children.IndexOf(element));
                    //System.Diagnostics.Debug.WriteLine(element);
                    // 获取子控件相对于WrapPanel的坐标和大小
                    Point elementPosition = element.TranslatePoint(new Point(0, 0), panel);
                    //System.Diagnostics.Debug.WriteLine(elementPosition);
                    double elementWidth = element.DesiredSize.Width;
                    double elementHeight = element.DesiredSize.Height;

                    // 判断是否在同一行或同一列
                    var padding = 0;
                    if(i==0 && position.Y < elementPosition.Y-padding)
                    {
                        rightElement = element;
                        leftElement = null;
                        insertIndex = 0;
                        break;
                    }
                    bool sameRow = position.Y >= elementPosition.Y-padding && position.Y <= elementPosition.Y + elementHeight + padding;

                    if (sameRow)
                    {
                        if(position.X >= elementPosition.X + elementWidth / 2)
                        {
                            if(nextElement == null)
                            {
                                leftElement = element;
                                rightElement = null;
                                insertIndex = panel.Children.Count;
                                break;
                            }
                            else
                            {
                                Point elementPositionNext = nextElement.TranslatePoint(new Point(0, 0), panel);
                                double elementWidthNext = nextElement.DesiredSize.Width;
                                double elementHeightNext = nextElement.DesiredSize.Height;
                                bool sameRowNext = position.Y >= elementPositionNext.Y - padding && position.Y <= elementPositionNext.Y + elementHeightNext + padding;
                                if (sameRowNext)
                                {
                                    if(position.X <= elementPositionNext.X + elementWidthNext/2)
                                    {
                                        leftElement = element;
                                        rightElement = nextElement;
                                        insertIndex = i + 1;
                                        break;
                                    }
                                }
                                else
                                {
                                    leftElement = element;
                                    rightElement = null;
                                    insertIndex = i + 1;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            rightElement = element;
                            leftElement = null;
                            insertIndex = i;
                            break;
                        }
                    }
                }

                if(insertIndex == -1)
                {
                    leftElement = panel.Children[panel.Children.Count-1];
                    rightElement = null;
                    insertIndex = panel.Children.Count;
                }

        

                // 创建一个Adorner对象，用于显示一个指示器
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                if (adornerLayer != null)
                {
                    // 移除之前的Adorner对象
                    if (currentAdorner != null)
                    {
                        //adornerLayer.Remove(currentAdorner);
                        //currentAdorner = null;
                    }
                    else
                    {
                        currentAdorner = new DragAdorner(panel, dragControl, true, 0.8);
                        adornerLayer.Add(currentAdorner);
                    }

                    // 创建一个新的Adorner对象，并设置其位置和大小
                    //Border border = new Border();
                    //border.Width = 30;
                    //border.Height = 30;
                    //border.BorderThickness = Thickness.;
                    //border.BorderBrush = new 
                    
                    
                    if (leftElement != null)
                    {
                        // 如果左右都有子控件，那么指示器的位置在两个子控件之间
                        Point leftPosition = leftElement.TranslatePoint(new Point(0, 0), panel);
              
                        double leftWidth = leftElement.DesiredSize.Width;
                        double leftHeight = leftElement.DesiredSize.Height;
                 

                       // Canvas.SetLeft(currentAdorner, leftPosition.X + leftWidth - dragControl.DesiredSize.Width/2);
                       // Canvas.SetTop(currentAdorner, leftPosition.Y);
                       // System.Diagnostics.Debug.WriteLine(leftPosition.X + leftWidth - dragControl.DesiredSize.Width / 2 + " " + leftPosition.Y);
                        currentAdorner.LeftOffset = leftPosition.X + leftWidth;
                        currentAdorner.TopOffset = leftPosition.Y + leftHeight/2;
                    }
                    else if (rightElement != null)
                    {


                        Point rightPosition = rightElement.TranslatePoint(new Point(0, 0), panel);

                        double rightWidth = rightElement.DesiredSize.Width;
                        double rightHeight = rightElement.DesiredSize.Height;


                        currentAdorner.LeftOffset = rightPosition.X;
                        currentAdorner.TopOffset = rightPosition.Y + rightHeight / 2;

                    }
                    else
                    {
                        adornerLayer.Remove(currentAdorner);
                        currentAdorner = null;
                    }
                }
            }
        }

        private void TagsContainer_Drop(object sender, DragEventArgs e)
        {
           
            WrapPanel panel = sender as WrapPanel;
            if(panel == null)
            {
                return;
            }
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null)
            {
                // 移除之前的Adorner对象
                if (currentAdorner != null)
                {
                    adornerLayer.Remove(currentAdorner);
                    currentAdorner = null;
                }
            }
            TagControl dragControl = null;
            try
            {
                IDataObject dataObject = e.Data;
                if (dataObject.GetData(typeof(TagControl)) == null)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }
                dragControl = (TagControl)dataObject.GetData(typeof(TagControl));
            }
            catch
            {
                e.Effects = DragDropEffects.None;
                return;
            }
        
            if (panel != null)
            {
                // 获取鼠标相对于WrapPanel的位置
                Point position = e.GetPosition(panel);
                //System.Diagnostics.Debug.WriteLine(position);

                e.Effects = DragDropEffects.Move;

                // 定义两个变量，分别表示左边和右边最接近鼠标位置的子控件
                UIElement leftElement = null;
                UIElement rightElement = null;
                int insertIndex = -1;

                if (panel.Children.Count == 0)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // 遍历子控件，找到左边和右边最接近鼠标位置的子控件
                // System.Diagnostics.Debug.WriteLine(panel.Children.Count);
                for (int i = 0; i < panel.Children.Count; i++)
                {
                    var element = panel.Children[i];
                    UIElement nextElement = null;
                    if (i + 1 < panel.Children.Count)
                    {
                        nextElement = panel.Children[i + 1];
                    }
                    // System.Diagnostics.Debug.WriteLine(panel.Children.IndexOf(element));
                    //System.Diagnostics.Debug.WriteLine(element);
                    // 获取子控件相对于WrapPanel的坐标和大小
                    Point elementPosition = element.TranslatePoint(new Point(0, 0), panel);
                    //System.Diagnostics.Debug.WriteLine(elementPosition);
                    double elementWidth = element.DesiredSize.Width;
                    double elementHeight = element.DesiredSize.Height;

                    // 判断是否在同一行或同一列
                    var padding = 0;
                    if (i == 0 && position.Y < elementPosition.Y - padding)
                    {
                        rightElement = element;
                        leftElement = null;
                        insertIndex = 0;
                        break;
                    }
                    bool sameRow = position.Y >= elementPosition.Y - padding && position.Y <= elementPosition.Y + elementHeight + padding;

                    if (sameRow)
                    {
                        if (position.X >= elementPosition.X + elementWidth / 2)
                        {
                            if (nextElement == null)
                            {
                                leftElement = element;
                                rightElement = null;
                                insertIndex = panel.Children.Count;
                                break;
                            }
                            else
                            {
                                Point elementPositionNext = nextElement.TranslatePoint(new Point(0, 0), panel);
                                double elementWidthNext = nextElement.DesiredSize.Width;
                                double elementHeightNext = nextElement.DesiredSize.Height;
                                bool sameRowNext = position.Y >= elementPositionNext.Y - padding && position.Y <= elementPositionNext.Y + elementHeightNext + padding;
                                if (sameRowNext)
                                {
                                    if (position.X <= elementPositionNext.X + elementWidthNext / 2)
                                    {
                                        leftElement = element;
                                        rightElement = nextElement;
                                        insertIndex = i + 1;
                                        break;
                                    }
                                }
                                else
                                {
                                    leftElement = element;
                                    rightElement = null;
                                    insertIndex = i + 1;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            rightElement = element;
                            leftElement = null;
                            insertIndex = i;
                            break;
                        }
                    }
                }

                if (insertIndex == -1)
                {
                    leftElement = panel.Children[panel.Children.Count - 1];
                    rightElement = null;
                    insertIndex = panel.Children.Count;
                }

                System.Diagnostics.Debug.WriteLine(insertIndex);
                int currentIndex = panel.Children.IndexOf(dragControl);
                if(currentIndex == -1)
                {
                    return;
                }
                if(insertIndex > currentIndex)
                {
                    insertIndex--;
                }
                if(insertIndex == currentIndex)
                {
                    return;
                }
                

                AppCore.Instance.currentActivePrompt.activeTags.Remove(dragControl.tagData);
                AppCore.Instance.currentActivePrompt.activeTags.Insert(insertIndex, dragControl.tagData);
                if (AppCore.Instance.genConfig.autoGroup)
                {
                    SortTags();
                }
                else
                {
                    TagsContainer.Children.Remove(dragControl);
                    TagsContainer.Children.Insert(insertIndex, dragControl);
                }
                UpdateSpell();
            }
        }

        private void TagsContainer_OnRealTargetDragLeave(object sender, DragEventArgs e)
        {
            
            WrapPanel panel = sender as WrapPanel;
            if (panel == null)
            {
                return;
            }
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(panel);
            if (adornerLayer != null)
            {
                // 移除之前的Adorner对象
                if (currentAdorner != null)
                {
                    adornerLayer.Remove(currentAdorner);
                    currentAdorner = null;
                }
            }
        }

        private void TagsContainer_DragLeave(object sender, DragEventArgs e)
        {
            
            dragInProgress = false;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (dragInProgress == false) TagsContainer_OnRealTargetDragLeave(sender, e);
            }));


        }

        private void TagsContainer_DragEnter(object sender, DragEventArgs e)
        {
            dragInProgress = true;
        }

        private void TextSearchTag_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppCore.Instance.SearchTags(TextSearchTag.Text);
        }

        private void MenuItemBatch_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("长期高负荷运行会让显卡工作不正常甚至影响寿命，请合理设置每批数量和休息时间。", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                _ = LoadAndOpenBatchDialog();
            }
        }

        private void ComboBoxDefaultPrompt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(changingComboDefaultPrompt)
            {
                return;
            }
            if (ComboBoxDefaultPrompt.SelectedIndex == -1)
            {
                return;
            }
            if (ComboBoxDefaultPrompt.SelectedIndex == AppCore.Instance.currentActivePrompt.initSpells.Count)
            {
                return;
            }
            AppCore.Instance.currentActivePrompt.defaultSpell = AppCore.Instance.currentActivePrompt.initSpells[ComboBoxDefaultPrompt.SelectedIndex].value;
            changingDefaultPrompt = true;
            TextDefaultPrompt.Text = AppCore.Instance.currentActivePrompt.defaultSpell;
            changingDefaultPrompt = false;
            UpdateSpell();
        }

        private void TextDefaultPrompt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(changingDefaultPrompt)
            {
                return;
            }
            AppCore.Instance.currentActivePrompt.defaultSpell = TextDefaultPrompt.Text;
            int matchIndex = -1;
            for (int i = 0; i < AppCore.Instance.currentActivePrompt.initSpells.Count; i++)
            {
                if (AppCore.Instance.currentActivePrompt.initSpells[i].value == AppCore.Instance.currentActivePrompt.defaultSpell)
                {
                    matchIndex = i;
                }
            }
            changingComboDefaultPrompt = true;
            if (matchIndex != -1)
            {
                ComboBoxDefaultPrompt.SelectedIndex = matchIndex;
            }
            else
            {
                ComboBoxDefaultPrompt.SelectedIndex = AppCore.Instance.currentActivePrompt.initSpells.Count;
            }
            changingComboDefaultPrompt = false;
            UpdateSpell();
        }

        private void RadioButtonPositive_Checked(object sender, RoutedEventArgs e)
        {
            if (TextDefaultPrompt == null) return;
            AppCore.Instance.currentActivePrompt = AppCore.Instance.activePositivePrompt;
            UpdateActivePrompt();
        }

        private void RadioButtonNegative_Checked(object sender, RoutedEventArgs e)
        {
            if (TextDefaultPrompt == null) return;
            AppCore.Instance.currentActivePrompt = AppCore.Instance.activeNegativePrompt;
            UpdateActivePrompt();
        }

        private void ComboBoxVae_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxVae.SelectedIndex < 0)
            {
                return;
            }
            if (AppCore.Instance.genConfig == null)
            {
                return;
            }
            AppCore.Instance.genConfig.vae = AppCore.Instance.GetGenerateEngine().GetVaes()[ComboBoxVae.SelectedIndex];
        }

        private void ButtonInterrupt_Click(object sender, RoutedEventArgs e)
        {
            AppCore.Instance.Interrupt();
        }

        Point? potentialDragStartPoint = null;
        private void ImagePreview_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (potentialDragStartPoint == null)
            {
                potentialDragStartPoint = e.GetPosition(this);
            }
        }

        private void ImagePreview_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            potentialDragStartPoint = null;
        }

        private void ImagePreview_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (potentialDragStartPoint == null) { return; }

            var dragPoint = e.GetPosition(this);

            Vector potentialDragLength = dragPoint - potentialDragStartPoint.Value;
            if (potentialDragLength.Length > 5)
            {
                if (ListImages.SelectedIndex < 0)
                {
                    return;
                }
                GenImageInfo imageInfo = AppCore.Instance.genImageInfos[ListImages.SelectedIndex];
                DataObject data = new DataObject(DataFormats.Bitmap, imageInfo.image);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                potentialDragStartPoint = null;
            }
        }
    }
}




