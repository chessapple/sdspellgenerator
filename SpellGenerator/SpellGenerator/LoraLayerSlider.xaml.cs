using Newtonsoft.Json.Linq;
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

namespace SpellGenerator
{
    /// <summary>
    /// LoraLayerSlider.xaml 的交互逻辑
    /// </summary>
    public partial class LoraLayerSlider : UserControl
    {
        public LoraLayerSlider()
        {
            InitializeComponent();
        }

        public event EventHandler ValueChanged;

        public string Label { set
            {
                TextLabel.Text = value;
            } }

        public double Value { set
            {
                TextValue.Content = value;
                SliderValue.Value = value;
            }
            get { return SliderValue.Value; }
        }

        private void SliderValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TextValue.Content = SliderValue.Value;
            if(ValueChanged != null)
            {
                ValueChanged(this, e);
            }
            
        }
    }
}
