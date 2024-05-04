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
using System.Xml;
using SpellGenerator.app.tag;

namespace SpellGenerator
{
    /// <summary>
    /// TagControl.xaml 的交互逻辑
    /// </summary>
    public partial class TagControl : UserControl
    {
        public TagControl()
        {
            InitializeComponent();
            ToolTipService.SetBetweenShowDelay(this, 0);
            ToolTipService.SetInitialShowDelay(this, 0);
        }

        public ActiveTagData? tagData;


        private TagTips tagTips;


        public void InitTag(ActiveTagData tagData)
        {
            this.tagData = tagData;

            SetTagColor(tagData.GetColor());
            TextTag.Content = tagData.GetShowTag();
            if (tagData.colorTag != null)
            {
                ButtonSelectColor.Visibility = Visibility.Visible;
                ButtonSelectColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tagData.colorTag.colorValue));
            }
            else
            {
                ButtonSelectColor.Visibility = Visibility.Collapsed;
            }
            /*
            if(tagData.tagInfo != null)
            {
                var toolTip = new ToolTip();
                toolTip.Content = tagData.tagInfo.tipText;
                this.ToolTip = toolTip;
            }*/
            if(tagTips == null)
            {
                tagTips = new TagTips();
            }
            var tagTipsData = tagData.tagTipsData;
            tagTipsData.control = this;
            tagTips.TagTipsValue = tagTipsData;
            var toolTip = new ToolTip();
            toolTip.Content = tagTips;
            toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            this.ToolTip = toolTip;
        }

        void UpdateToolTips()
        {
            var tagTipsData = tagData.tagTipsData;
            tagTipsData.control = this;
            tagTips.UpdateContent(tagTipsData);
        }

        void SetTagColor(Color color)
        {
            BorderBg.BorderBrush = new SolidColorBrush(color);
            //TextTag.Foreground = new SolidColorBrush(color);
            TextTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEBEB"));
            ButtonSelectColor.BorderBrush = new SolidColorBrush(color);
           // ButtonIncStrength.Foreground = new SolidColorBrush(color);
            //ButtonDecStrength.Foreground = new SolidColorBrush(color);
            //ButtonRemoveTag.Foreground = new SolidColorBrush(color);
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow).RemoveTag(this);
        }

        private void ButtonIncStrength_Click(object sender, RoutedEventArgs e)
        {
            if(tagData.strength >= 50)
            {
                return;
            }
            tagData.strength++;
            UpdateStrength();
        }

        private void ButtonDecStrength_Click(object sender, RoutedEventArgs e)
        {
            if (tagData.strength <= 1)
            {
                return;
            }
            tagData.strength--;
            UpdateStrength();
        }

        void UpdateStrength()
        {
            /*
            if (tagData.strength == 0)
            {
                BorderBg.Background = Brushes.Transparent;
                TextTag.FontWeight = FontWeights.Normal;
            }
            else if(tagData.strength == -1)
            {
                BorderBg.Background = Brushes.Transparent;
                TextTag.FontWeight = FontWeights.Thin;
            }
            else
            {
                TextTag.FontWeight = FontWeights.Normal;
                BorderBg.Background = Brushes.RoyalBlue.Clone();
                BorderBg.Background.Opacity = 0.1*tagData.strength;
            }*/
            TextTag.Content = tagData.GetShowTag();
            (Application.Current.MainWindow as MainWindow).UpdateSpell();
        }

        private void ButtonSelectColor_Click(object sender, RoutedEventArgs e)
        {
            SetColor();
        }

        public void SetColor()
        {
            DialogColorSelector dialogColorSelector = new DialogColorSelector();
            dialogColorSelector.Owner = Application.Current.MainWindow;
            if (dialogColorSelector.ShowDialog() == true)
            {
                tagData.colorTag = dialogColorSelector.selectedColor;
                tagData.colorprompt = tagData.colorTag.value;
                ButtonSelectColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tagData.colorTag.colorValue));
                ButtonSelectColor.BorderBrush = new SolidColorBrush(tagData.GetColor());
                ButtonSelectColor.Visibility = Visibility.Visible;
                TextTag.Content = tagData.GetShowTag();
                (Application.Current.MainWindow as MainWindow).UpdateSpell();
                if(tagTips != null && tagData != null)
                {
                    tagTips.TagTipsValue = tagData.tagTipsData;
                }

                UpdateToolTips();
            }
        }

        public void SetLayer()
        {
            DialogLoraLayer dialogLoraLayer = new DialogLoraLayer();
            dialogLoraLayer.Owner = Application.Current.MainWindow;
            dialogLoraLayer.Init(tagData.loraLayer);
            dialogLoraLayer.ShowDialog();//if (dialogLoraLayer.ShowDialog() == true)
            {
                if(dialogLoraLayer.loraLayer.IsAll())
                {
                    tagData.loraLayer = null;
                }
                else
                {
                    tagData.loraLayer = dialogLoraLayer.loraLayer;
                }
                TextTag.Content = tagData.GetShowTag();
                (Application.Current.MainWindow as MainWindow).UpdateSpell();
                UpdateToolTips();
            }
        }

        private void ButtonSelectColor_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            CancelColor();
        }

        public void CancelColor()
        {
            if (tagData.tagInfo != null && tagData.tagInfo.colorDefault)
            {
                return;
            }
            tagData.colorTag = null;
            tagData.colorprompt = "";
            //ButtonSelectColor.Background = Brushes.Transparent;
            //ButtonSelectColor.BorderBrush = Brushes.Transparent;
            ButtonSelectColor.Visibility = Visibility.Collapsed;
            TextTag.Content = tagData.GetShowTag();
            (Application.Current.MainWindow as MainWindow).UpdateSpell();
            if (tagTips != null && tagData != null)
            {
                tagTips.TagTipsValue = tagData.tagTipsData;
            }
        }

        Point? potentialDragStartPoint = null;

        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (potentialDragStartPoint == null)
            {
                potentialDragStartPoint = e.GetPosition(this);
            }

            
        }

        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            potentialDragStartPoint = null;
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (potentialDragStartPoint == null) { return; }

            var dragPoint = e.GetPosition(this);

            Vector potentialDragLength = dragPoint - potentialDragStartPoint.Value;
            if (potentialDragLength.Length > 5)
            {
                DataObject data = new DataObject(this);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                potentialDragStartPoint = null;
            }
        }
    }
}
