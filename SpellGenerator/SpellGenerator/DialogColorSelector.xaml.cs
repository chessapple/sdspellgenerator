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
using SpellGenerator.app.tag;

namespace SpellGenerator
{
    class ColorData
    {
        public Color color;
        public TagInfo? colorTag;
        public double hue;
        public double value;
        public double saturation;
    }
    /// <summary>
    /// DialogColorSelector.xaml 的交互逻辑
    /// </summary>
    public partial class DialogColorSelector : Window
    {
        public DialogColorSelector()
        {
            InitializeComponent();
            InitColors();
        }

        public TagInfo? selectedColor;

        void InitColors()
        {
            List<ColorData> colors = new List<ColorData>();
            foreach(TagInfo colorTag in AppCore.Instance.colors.tags)
            {
                ColorData colorData = new ColorData();
                colorData.colorTag = colorTag;
                colorData.color = (Color)ColorConverter.ConvertFromString(colorTag.colorValue);
                double r = colorData.color.R / 255d;
                double g = colorData.color.G / 255d;
                double b = colorData.color.B / 255d;
                double max = Math.Max(r, Math.Max(g, b));
                double min = Math.Min(r, Math.Min(g, b));
                colorData.value = max;
                if(max == min)
                {
                    colorData.hue = 0;
                    colorData.saturation = 0;
                }
                else
                {
                    if (max == r)
                    {
                        colorData.hue = (g - b) / (max - min);
                    }
                    else if (max == g)
                    {
                        colorData.hue = 2 + (b - r) / (max - min);
                    }
                    else if (max == b)
                    {
                        colorData.hue = 4 + (r - g) / (max - min);
                    }
                    colorData.hue *= 60;
                    colorData.hue %= 360;
                    if (colorData.hue < 0) colorData.hue = colorData.hue + 360;

                    colorData.saturation = (max - min) / max;
                }
                colors.Add(colorData);

            }
            colors.Sort((a, b) => {
                if(a.hue != b.hue)
                {
                    return a.hue.CompareTo(b.hue);
                }
                return a.saturation.CompareTo(b.saturation);
                }
            );
            int size = 1;
            while (size * size < colors.Count)
            {
                size++;
            }
            for(int i=0; i<size; i++)
            {
                List<ColorData> rlist = new List<ColorData>();
                for(int j=0; i*size+j<colors.Count&&j<size; j++)
                {
                    ColorData color = colors[i*size+j];
                    rlist.Add(color);
                }
                rlist.Sort((a,b)=> {
                    if(a.value != b.value)
                    {
                        return a.value.CompareTo(b.value);
                    }
                    if(a.saturation != b.saturation)
                    {
                        return b.saturation.CompareTo(a.saturation);
                    }
                    return a.hue.CompareTo(b.hue);
                    });
                for(int j=0; j<rlist.Count; j++)
                {
                    Button btn = new Button();
                    double w = ColorContainer.Width / size;
                    btn.Width = w - 2;
                    btn.Height = w - 2;
                    btn.HorizontalAlignment = HorizontalAlignment.Left;
                    btn.VerticalAlignment = VerticalAlignment.Top;
                    btn.Margin = new Thickness(j* w + 1, i* w + 1, 0, 0);
                   // System.Diagnostics.Trace.WriteLine(w+" "+ColorContainer.Width+" "+btn.Margin.Left+" "+btn.Margin.Top);

                    btn.Content = "";
                    btn.BorderBrush = Brushes.Transparent;
                    btn.Background = new SolidColorBrush(rlist[j].color);
                    TagInfo colorTag = rlist[j].colorTag;
                    btn.Click += (c, e) =>
                    {
                        selectedColor = colorTag;
                        DialogResult = true;
                    };
                    var toolTip = new ToolTip();
                    toolTip.Content = colorTag.tipText;
                    btn.ToolTip = toolTip;
                    ColorContainer.Children.Add(btn);
                }

            }
        }
    }
}
