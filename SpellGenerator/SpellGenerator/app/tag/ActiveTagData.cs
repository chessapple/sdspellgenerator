using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Xml.Linq;
using SpellGenerator.app.utils;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace SpellGenerator.app.tag
{
    public class ActiveTagData
    {
        public TagInfo? tagInfo;
        public TagInfo? colorTag;
        public int strength = 10;
        public ClassifyInfo? classify;
        public string? prompt;
        public string? colorprompt = "";
        public TagControl? control;
        public int tagType;
        public string value;
        public LoraLayer loraLayer;

        // 不是0引用！有列表用到！！！
        public string pureinfo
        {
            get
            {
                string info = "";
                if (tagType == (int)TagType.TagTextInversion)
                {
                    info = "ti:" + prompt;
                }
                else if (tagType == (int)TagType.TagHypernetwork || tagType == (int)TagType.TagLora)
                {
                    info = prompt;
                }
                else if (tagInfo != null)
                {
                    info = tagInfo.name;
                }
                else
                {
                    info = prompt;
                    info = TruncString(info, 20, "...");
                }
                return info;
            }
        }
        

        public string GetShowTag()
        {
            string showTag = "";
            if(tagType == (int)TagType.TagTextInversion)
            {
                showTag = /*"ti:" +*/ prompt;
            }
            else if(tagType == (int)TagType.TagHypernetwork || tagType == (int)TagType.TagLora)
            {
                showTag = prompt.Substring(prompt.IndexOf(":")+1);
            }
            else if (tagInfo != null)
            {
                showTag = tagInfo.name;
                if (colorTag != null)
                {
                    showTag = colorTag.name + showTag;
                }
                else if (colorprompt != null)
                {
                    showTag = colorprompt + showTag;
                }
                showTag = TruncString(showTag, 50, "...");
            }
            else
            {
                if (colorprompt != null)
                {
                    showTag = colorprompt + " ";
                }
                showTag += prompt;
                showTag = TruncString(showTag, 50, "...");
            }
            if (strength != 10)
            {
                showTag += ":" + strength / 10f;
            }
            if(tagType == (int)TagType.TagLora && loraLayer != null)
            {
                int loraIndex = AppCore.Instance.GetLoraLayerIndex(loraLayer);
                showTag += ":" + AppCore.Instance.loraLayers[loraIndex].name;
            }
            return showTag;
        }

        private string TruncString(string str, int maxLength, string end)
        {
            if (str.Length <= maxLength)
            {
                return str;
            }
            string front = str.Substring(0, maxLength - end.Length);
            return front + end;
        }

        public Color GetColor()
        {
            if (tagInfo != null && tagInfo.extra != null && tagInfo.extra.multiple)
            {
                return Color.FromRgb(127, 127, 127);
            }
            if (classify != null)
            {
                return classify.color;
            }
            return Color.FromRgb(127, 127, 127);
        }

        public string GetSpell()
        {
            if(tagType == (int)TagType.TagHypernetwork || tagType == (int)TagType.TagLora)
            {
                if (tagType == (int)TagType.TagLora && loraLayer != null)
                {
                    return "<" + prompt + ":" + strength / 10f + ":"+loraLayer.ToString()+ ">";
                }
                return "<"+prompt + ":" + strength / 10f + ">";
            }
            string spell = prompt;
            if (colorprompt != null && colorprompt != "")
            {
                spell = colorprompt + " " + prompt;
            }
            if (strength != 10)
            {
                spell = "(" + spell + ":" + strength / 10f + ")";
            }

            return spell;
        }


        public TagTipsData tagTipsData
        {
            get
            {
                if(tagInfo != null)
                {
                    TagTipsData rs = tagInfo.tagTipsData;
                    rs.fromList = false;
                    if (tagType == (int)TagType.TagNormal)
                    {
                        if (colorTag != null && !tagInfo.colorDefault)
                        {
                            rs.canDeleteColor = true;
                        }
                        if (colorTag != null)
                        {
                            rs.colorString = colorTag.name;
                            rs.color = colorTag.colorValue;
                        }
                        rs.canSetColor = true;
                    }
                    if(tagType == (int)TagType.TagLora)
                    {
                        if(loraLayer != null)
                        {
                            int indexLora = AppCore.Instance.GetLoraLayerIndex(loraLayer);
                            if(indexLora < AppCore.Instance.loraLayers.Count-1)
                            {
                                rs.loraLayerString = AppCore.Instance.loraLayers[indexLora].name;
                            }
                            else
                            {
                                rs.loraLayerString = loraLayer.ToString();
                            }
                        }
                    }
                    rs.control = control;
                    return rs;
                }
                TagTipsData tagTipsData = new TagTipsData();
                tagTipsData.value = "";
                tagTipsData.typeName = "";
                if (tagType == (int)TagType.TagNormal)
                {
                    tagTipsData.name = value;
                    tagTipsData.typeName = "未入库";
                    if (classify != null)
                    {
                        tagTipsData.typeColor = classify.color;
                    }
                    else
                    {
                        tagTipsData.typeColor = Color.FromRgb(127, 127, 127);
                    }
                    if(colorTag != null)
                    {
                        tagTipsData.canDeleteColor = true;
                        tagTipsData.colorString = colorTag.name;
                        tagTipsData.color = colorTag.colorValue;
                    }
                    tagTipsData.canSetColor = true;
                }
                if (tagType == (int)TagType.TagLora)
                {
                    if (loraLayer != null)
                    {
                        int indexLora = AppCore.Instance.GetLoraLayerIndex(loraLayer);
                        if (indexLora < AppCore.Instance.loraLayers.Count - 1)
                        {
                            tagTipsData.loraLayerString = AppCore.Instance.loraLayers[indexLora].name;
                        }
                        else
                        {
                            tagTipsData.loraLayerString = loraLayer.ToString();
                        }
                    }
                }
                tagTipsData.comment = "";
                tagTipsData.preview = null;
                tagTipsData.previewPath = "";
                tagTipsData.fromList = false;
                tagTipsData.control = control;
                return tagTipsData;
            }
        }
    }
}
