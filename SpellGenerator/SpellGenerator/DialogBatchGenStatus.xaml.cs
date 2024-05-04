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
using SpellGenerator.app.batch;
using SpellGenerator.app;

namespace SpellGenerator
{
    /// <summary>
    /// DialogBatchGenStatus.xaml 的交互逻辑
    /// </summary>
    public partial class DialogBatchGenStatus : Window
    {
        public BatchGenController batchGenController;

        public bool canClose = false;

        public DialogBatchGenStatus()
        {
            InitializeComponent();
     
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(canClose)
            {
                return;
            }
            e.Cancel = true;
            if(MessageBox.Show("批量操作将在完成当前行动后中止，是否继续？", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                batchGenController.stop = true;
            }
            
        }

        public void SetStatus(string status)
        {
            TextStatus.Content = status;
        }

        public void SetProgress(string progressText, double progress)
        {
            TextProgress.Content = progressText;
            Progress.Value = progress;
        }
    }
}
