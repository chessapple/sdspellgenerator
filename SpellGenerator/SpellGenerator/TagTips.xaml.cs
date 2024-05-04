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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpellGenerator.app.tag;
using System.Web;
using System.Drawing.Imaging;
using System.Xml.Linq;
using System.Diagnostics;
using Microsoft.Xaml.Behaviors.Layout;

namespace SpellGenerator
{
    /// <summary>
    /// TagTips.xaml 的交互逻辑
    /// </summary>
    public partial class TagTips : System.Windows.Controls.UserControl
    {
        public TagTips()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TagTipsValueProperty =
        DependencyProperty.Register("TagTipsValue", typeof(TagTipsData), typeof(TagTips), new PropertyMetadata(OnTagTipsValueChanged));

        private string previewPath = "";
        private TagControl control;
        private string typeName;

        public TagTipsData TagTipsValue
        {
            get { return (TagTipsData)GetValue(TagTipsValueProperty); }
            set { SetValue(TagTipsValueProperty, value);
            }
        }

        private Window window;

        private void TagTips_Loaded(object sender, RoutedEventArgs e)
        {
            window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }

        private void TagTips_Unloaded(object sender, RoutedEventArgs e)
        {
            if(window != null)
            window.KeyDown -= HandleKeyPress;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            // do something here
            //System.Diagnostics.Debug.WriteLine(e.Key);
            if(e.Key == Key.F && previewPath != "")
            {
                try
                {
                    Process.Start("explorer", @"/select,"+previewPath);
                }
                catch
                {

                }
            }
            else if(e.Key == Key.C && control != null && typeName != "lora" && typeName != "hyper")
            {
                control.SetColor();
            }
            else if (e.Key == Key.X && control != null && typeName != "lora" && typeName != "hyper")
            {
                control.CancelColor();
            }
            else if (e.Key == Key.S && control != null && typeName == "lora")
            {
                control.SetLayer();
            }
        }

        private static void AddTextRun(TextBlock textBlock, string text, Color color, double fontSize )
        {
            var r = new Run(text);
            r.Foreground = new SolidColorBrush(color);
            r.FontSize = fontSize;
            textBlock.Inlines.Add(r);
        }

        public void UpdateContent(TagTipsData newValue)
        {
            var tagTips = this;
            tagTips.previewPath = "";
            tagTips.control = newValue.control;
            // 设置或更新提示窗口的内容
            tagTips.textContent.Inlines.Clear();
            tagTips.textContent.Inlines.Add(new Run(newValue.name));
            if (newValue.typeName != "")
            {
                tagTips.textContent.Inlines.Add(new Run("   "));
                AddTextRun(tagTips.textContent, newValue.typeName, newValue.typeColor, 12);
            }

            if (newValue.value != "")
            {
                tagTips.textContent.Inlines.Add(new LineBreak());
                tagTips.textContent.Inlines.Add(new Run(newValue.value));
            }
            if (newValue.comment != "")
            {
                tagTips.textContent.Inlines.Add(new LineBreak());
                AddTextRun(tagTips.textContent, newValue.comment, (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
            }
            if (newValue.colorString != null && newValue.colorString != "")
            {
                tagTips.textContent.Inlines.Add(new LineBreak());
                AddTextRun(tagTips.textContent, newValue.colorString, (Color)ColorConverter.ConvertFromString(newValue.color), 12);
            }
            if (newValue.loraLayerString != null && newValue.loraLayerString != "")
            {
                tagTips.textContent.Inlines.Add(new LineBreak());
                AddTextRun(tagTips.textContent, newValue.loraLayerString, (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
            }
            tagTips.textContent.Inlines.Add(new LineBreak());

            bool needSplit = false;
            bool newLine = false;

            if (newValue.fromList)
            {
                AddTextRun(tagTips.textContent, "双击", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "添加标签", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);

                needSplit = true;
            }
            else
            {
                AddTextRun(tagTips.textContent, "拖动", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "移动位置，", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                AddTextRun(tagTips.textContent, "点击+-", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "修改权重，", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                AddTextRun(tagTips.textContent, "点击×", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "删除标签", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                newLine = true;

            }



            if (newValue.preview != null)
            {
                if (needSplit)
                {
                    AddTextRun(tagTips.textContent, "，", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                }
                if (newLine)
                {
                    tagTips.textContent.Inlines.Add(new LineBreak());
                    newLine = false;
                }
                AddTextRun(tagTips.textContent, "F", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "打开预览图位置", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);

                tagTips.previewPath = newValue.previewPath;

                needSplit = true;
            }
            if (!newValue.fromList && newValue.canSetColor)
            {
                if (needSplit)
                {
                    AddTextRun(tagTips.textContent, "，", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                }
                if (newLine)
                {
                    tagTips.textContent.Inlines.Add(new LineBreak());
                    newLine = false;
                }
                AddTextRun(tagTips.textContent, "C", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "设置颜色", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);

                needSplit = true;
            }
            if (!newValue.fromList && newValue.canDeleteColor)
            {
                if (needSplit)
                {
                    AddTextRun(tagTips.textContent, "，", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                }
                if (newLine)
                {
                    tagTips.textContent.Inlines.Add(new LineBreak());
                    newLine = false;
                }
                AddTextRun(tagTips.textContent, "X", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "取消颜色", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);

                needSplit = true;
            }
            this.typeName = newValue.typeName;
            if (!newValue.fromList && newValue.typeName == "lora")
            {
                if (needSplit)
                {
                    AddTextRun(tagTips.textContent, "，", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                }
                if (newLine)
                {
                    tagTips.textContent.Inlines.Add(new LineBreak());
                    newLine = false;
                }
                AddTextRun(tagTips.textContent, "S", (Color)ColorConverter.ConvertFromString("#cccc00"), 12);
                AddTextRun(tagTips.textContent, "设置分层", (Color)ColorConverter.ConvertFromString("#aaaaaa"), 12);
                needSplit = true;
            }
            if (newValue.preview != null)
            {
                tagTips.imagePreview.Visibility = Visibility.Visible;
                tagTips.imagePreview.Source = newValue.preview;
            }
            else
            {
                tagTips.imagePreview.Visibility = Visibility.Collapsed;
            }
        }

        private static void OnTagTipsValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            // 获取新的值
            var newValue = e.NewValue as TagTipsData;
            // 获取当前控件
            var tagTips = d as TagTips;
            tagTips.UpdateContent(newValue);
        }
    }
}
