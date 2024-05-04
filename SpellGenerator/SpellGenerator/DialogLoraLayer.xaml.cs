using SpellGenerator.app;
using SpellGenerator.app.tag;
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

namespace SpellGenerator
{
    /// <summary>
    /// DialogLoraLayer.xaml 的交互逻辑
    /// </summary>
    public partial class DialogLoraLayer : Window
    {
        public DialogLoraLayer()
        {
            InitializeComponent();
        }

        private List<LoraLayerSlider> sliders;
        public LoraLayer loraLayer;
        private bool changing = false;
        private string[] labels = new string[]{"BAS",
            "IN\n01", "IN\n02", "IN\n04", "IN\n05", "IN\n07", "IN\n08",
            "MID",
            "OUT\n03", "OUT\n04", "OUT\n05", "OUT\n06", "OUT\n07", "OUT\n08", "OUT\n09", "OUT\n10", "OUT\n11"
        };

        public void Init(LoraLayer loraLayer)
        {
            this.loraLayer = new LoraLayer();
            if (loraLayer == null)
            {
                loraLayer = new LoraLayer();
                loraLayer.SetAll();
            }
            this.loraLayer.CopyFrom(loraLayer);
            sliders = new List<LoraLayerSlider>();
            foreach(var child in ContainerSliders.Children)
            {
                if(child is LoraLayerSlider)
                {
                    LoraLayerSlider slider = (LoraLayerSlider)child;
                    sliders.Add(slider);
                    slider.Label = labels[sliders.Count - 1];// + "\n" + "O11";
                    slider.ValueChanged += Slider_ValueChanged;
                }
            }
            ComboBoxLoraLayer.ItemsSource = AppCore.Instance.loraLayers;
            UpdateLoraLayer();
        }

        private void Slider_ValueChanged(object? sender, EventArgs e)
        {
            if (changing)
            {
                return;
            }
            for (int i = 0; i < sliders.Count; i++)
            {
                loraLayer.layers[i] = Math.Round(sliders[i].Value * 10)/10; 
            }
            UpdateLoraLayer();
        }

        void UpdateLoraLayer()
        {
            changing = true;
            ComboBoxLoraLayer.SelectedIndex = AppCore.Instance.GetLoraLayerIndex(loraLayer);
            for(int i=0; i<sliders.Count; i++)
            {
                sliders[i].Value = loraLayer.layers[i];
            }
            changing = false;
        }

        private void ComboBoxLoraLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(changing)
            {
                return;
            }
            if(ComboBoxLoraLayer.SelectedIndex == AppCore.Instance.loraLayers.Count -1)
            {
                return;
            }
            loraLayer.CopyFrom(AppCore.Instance.loraLayers[ComboBoxLoraLayer.SelectedIndex]);
            UpdateLoraLayer();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
