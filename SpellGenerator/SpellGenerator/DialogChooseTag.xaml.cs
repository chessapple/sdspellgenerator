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
    public partial class DialogChooseTag : Window
    {
        public DialogChooseTag()
        {
            InitializeComponent();
            InitializeClassifies();

            ComboBoxPack.ItemsSource = AppCore.Instance.previewPacks;
            CheckRecordPreview.IsChecked = AppCore.Instance.recordLastPreview;
            if(AppCore.Instance.recordLastPreview)
            {
                int index = AppCore.Instance.previewPacks.IndexOf(AppCore.Instance.lastSelPreviewPack);
                if(index != -1)
                {
                    ComboBoxPack.SelectedIndex = index;
                }
                else
                {
                    ComboBoxPack.SelectedIndex = 0;
                }
            }
            else
            {
                ComboBoxPack.SelectedIndex = 0;
            }
        }

        void InitializeClassifies()
        {
            ComboBoxTag.ItemsSource = AppCore.Instance.currentActivePrompt.activeTags;
            ComboBoxTag.SelectedIndex = 0;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
