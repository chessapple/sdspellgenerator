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

namespace SpellGenerator
{
    /// <summary>
    /// DialogChooseClassify.xaml 的交互逻辑
    /// </summary>
    public partial class DialogChooseClassify : Window
    {
        public DialogChooseClassify()
        {
            InitializeComponent();
            InitializeClassifies();
            
        }

        void InitializeClassifies()
        {
            ComboBoxClassify.ItemsSource = AppCore.Instance.classifyInfos.ConvertAll(a => a.name);
            ComboBoxClassify.SelectedIndex = 0;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
