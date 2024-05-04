using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using SpellGenerator.app;
using SpellGenerator.app.batch;
using System.Text.RegularExpressions;
using Ookii.Dialogs.Wpf;
using System.IO;

namespace SpellGenerator
{
    /// <summary>
    /// DialogBatch.xaml 的交互逻辑
    /// </summary>
    public partial class DialogBatch : Window
    {
        public int roundCount = 1;
        public List<BModelData> models;
        public List<BAlgorithmData> algorithms;
        public bool useRefImage;
        
        private VistaFolderBrowserDialog fileDialog = new VistaFolderBrowserDialog();


        public DialogBatch()
        {
            InitializeComponent();
            InitData();
            UpdateImageNum();
        }

        void InitData()
        {
            TextEngine.Content = "使用后台："+AppCore.Instance.GetGenerateEngine().GetName();
            if(AppCore.Instance.GetGenerateEngine().CanChooseModel())
            {
                SliderRoundSwitch.IsEnabled = true;
                ListModel.IsEnabled = true;

                models = new List<BModelData>();
                foreach(string model in AppCore.Instance.GetGenerateEngine().GetModels())
                {
                    BModelData modelData = new BModelData();
                    modelData.modelName = model;
                    modelData.check = (AppCore.Instance.GetGenerateEngine().GetCurrentModel() == model);
                    models.Add(modelData);
                }
                ListModel.ItemsSource = models;
            }
            else
            {
                SliderRoundSwitch.IsEnabled = false;
                ListModel.IsEnabled = false;
            }
            algorithms = new List<BAlgorithmData>();
            foreach(var samplingMethod in AppCore.Instance.GetGenerateEngine().GetSamplingMethods())
            {
                BAlgorithmData algorithmData = new BAlgorithmData();
                algorithmData.method = samplingMethod;
                algorithmData.check = (AppCore.Instance.genConfig.samplingMethod == samplingMethod.webUIName);
                algorithms.Add(algorithmData);
            }
            ListSamplingMethod.ItemsSource = algorithms;
        }

        private void SliderRoundSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(TextRoundSwitch == null)
            {
                return;
            }
            TextRoundSwitch.Content = SliderRoundSwitch.Value.ToString();
        }

        private void ListSamplingMethodChanged(object sender, RoutedEventArgs e)
        {
            UpdateImageNum();
        }

        private void ListModelChanged(object sender, RoutedEventArgs e)
        {
            UpdateImageNum();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            BatchGenController batchGenController = new BatchGenController();
            if(!Directory.Exists(TextPath.Text))
            {
                MessageBox.Show("请输入有效的图片存放路径。");
                return;
            }
            if (GetModelCount()==0)
            {
                MessageBox.Show("请至少选择一个模型。");
                return;
            }
            if (GetSampleMethodCount() == 0)
            {
                MessageBox.Show("请至少选择一个采样算法。");
                return;
            }
            batchGenController.outputPath = TextPath.Text;
            batchGenController.roundCount = roundCount;
            batchGenController.roundSwitch = (int)SliderRoundSwitch.Value;
            batchGenController.imageNumPerRound = (int)SliderNumPerRound.Value;
            batchGenController.restTime = (int)SliderRestTime.Value;
            batchGenController.algorithms = new List<SamplingMethod>();
            batchGenController.algorithms.AddRange(algorithms.FindAll(x=>x.check).ConvertAll(x=>x.method));
            batchGenController.models = new List<string>();
            if(AppCore.Instance.GetGenerateEngine().CanChooseModel())
            {
                batchGenController.models.AddRange(models.FindAll(x => x.check).ConvertAll(x => x.modelName));
            }
            else
            {
                batchGenController.models.Add("default");
            }
            batchGenController.useRefImage = useRefImage && AppCore.Instance.refImage != null;
            batchGenController.refImage = AppCore.Instance.refImage;
            Close();
            batchGenController.Run();
        }

        private void SliderRestTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextRestTime == null)
            {
                return;
            }
            TextRestTime.Content = SliderRestTime.Value.ToString()+"秒";
        }

        private void TextRound_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*$");
            //System.Diagnostics.Debug.WriteLine(e.Text);
            if (!regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void TextRound_TextChanged(object sender, TextChangedEventArgs e)
        {
            int roundCount = 1;
            if (TextRound.Text == "")
            {
                roundCount = 1;
            }
            else if (!int.TryParse(TextRound.Text, out roundCount))
            {
                roundCount = 1;
                TextRound.Text = "1";
            }
            if (roundCount < 1)
            {
                roundCount = 1;
                TextRound.Text = "1";
            }
            if (roundCount > 100000)
            {
                roundCount = 100000;
                TextRound.Text = "100000";
            }
            this.roundCount = roundCount;
            UpdateImageNum();
        }

        private void UpdateImageNum()
        {
            if(SliderNumPerRound == null || TextImageCount == null || algorithms == null)
            {
                return;
            }
            int imageNum = roundCount * (int)SliderNumPerRound.Value * GetModelCount() * GetSampleMethodCount();
            TextImageCount.Content = string.Format("预计生成{0}张图", imageNum);
        }

        private int GetModelCount()
        {
            if(!AppCore.Instance.GetGenerateEngine().CanChooseModel())
            {
                return 1;
            }
            else
            {
                return models.Count(model => model.check);
            }
        }

        private int GetSampleMethodCount()
        {
            return algorithms.Count(model => model.check);
        }

        private void SliderNumPerRound_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextNumPerRound == null)
            {
                return;
            }
            TextNumPerRound.Content = SliderNumPerRound.Value.ToString();
            UpdateImageNum();
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            if(fileDialog.ShowDialog() == true)
            {
                TextPath.Text = fileDialog.SelectedPath;
            }
        }
    }
}
