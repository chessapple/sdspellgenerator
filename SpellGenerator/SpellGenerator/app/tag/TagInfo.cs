using SpellGenerator.app.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
namespace SpellGenerator.app.tag
{

    public enum TagType
    {
        TagNormal = 0,
        TagDecorate = 1,
        TagTextInversion = 10,
        TagHypernetwork = 11,
        TagLora = 12
    }

    public class TagInfo
    {
        public string name { get; set; } = "";
        public string nameInList { get; set; } = "";
        public string tipText { get; set; } = "";
        public string subGroup { get; set; } = "";
        public bool hasPreview
        {
            get
            {
                return AppCore.Instance.previewImageInfos.ContainsKey(previewValue);
            }
        }
        public Visibility previewVisibility
        {
            get
            {
                return hasPreview ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public BitmapImage preview
        {
            get
            {
                return hasPreview ? AppCore.Instance.previewImageInfos[previewValue].source : null;
            }
        }

        public bool onlyOnce
        {
            get
            {
                return (tagType < 10 && (extra == null || !extra.multiple)) || tagType == (int)TagType.TagLora || tagType == (int)TagType.TagHypernetwork;
            }
        }

        public string previewPath
        {
            get
            {
                return hasPreview ? AppCore.Instance.previewImageInfos[previewValue].path : "";
            }
        }
        public string previewValue
        {
            get
            {
                if(tagType == (int)TagType.TagNormal)
                {
                    return FileNameUtil.Encode(value);
                }
                else if(tagType == (int)TagType.TagTextInversion)
                {
                    return FileNameUtil.Encode("ti:" + value);
                }
                return FileNameUtil.Encode(value);
            }
        }

        
        public TagTipsData tagTipsData
        {
            get
            {
                TagTipsData tagTipsData= new TagTipsData();
                tagTipsData.value = "";
                tagTipsData.typeName = "";
                if (tagType == (int)TagType.TagNormal)
                {
                    tagTipsData.name = name;
                    if(name != value)
                    {
                        tagTipsData.value = value;
                    }
                    tagTipsData.typeName = group;
                    if (classify != null && !(extra != null && extra.freeMovement))
                    {
                        tagTipsData.typeColor = classify.color;
                    }
                    else
                    {
                        tagTipsData.typeColor = Color.FromRgb(127, 127, 127);
                    }
                }
                else if(tagType == (int)TagType.TagTextInversion)
                {
                    tagTipsData.name = name;
                    tagTipsData.typeName = "ti";
                    tagTipsData.typeColor = Color.FromRgb(127, 127, 127);
                }
                else if (tagType == (int)TagType.TagLora)
                {
                    tagTipsData.name = name;
                    tagTipsData.typeName = "lora";
                    tagTipsData.typeColor = Color.FromRgb(127, 127, 127);
                }
                else if (tagType == (int)TagType.TagHypernetwork)
                {
                    tagTipsData.name = name;
                    tagTipsData.typeName = "hyper";
                    tagTipsData.typeColor = Color.FromRgb(127, 127, 127);
                }
                tagTipsData.comment = comment;
                tagTipsData.preview = preview;
                tagTipsData.previewPath = previewPath;
                tagTipsData.fromList = true;
                tagTipsData.canSetColor = false;
                tagTipsData.canDeleteColor = false;
                return tagTipsData;
            }
        }

        public string comment = "";
        //public bool color = false;
        public bool colorDefault = false;
        public string colorValue = "";
        public string value = "";
        public TagInfo? defaultColorTag = null;
        //public bool used = false;
        public ClassifyInfo? classify;
        public TagExtraInfo? extra;
        public int tagType;
        public string group;

        public void CopyFrom(TagInfo other)
        {
            name = other.name;
            nameInList = other.nameInList;
            tipText = other.tipText;
            //color = other.color;
            colorDefault = other.colorDefault;
            colorValue = other.colorValue;
            value = other.value;
            defaultColorTag = other.defaultColorTag;
            //used = other.used;
            classify = other.classify;
            extra = other.extra;
            tagType = other.tagType;
        }

        public static TagInfo CreateTextInversionTag(string name)
        {
            TagInfo tag = new TagInfo();
            tag.name = name;
            tag.nameInList = name;
            tag.tipText = name + "\nHText Inversion";
            tag.value = name;
            //tag.used = false;
            tag.tagType = (int)TagType.TagTextInversion;
            return tag;
        }

        public static TagInfo CreateHypernetworkTag(string name)
        {
            TagInfo tag = new TagInfo();
            tag.name = name;
            tag.nameInList = name;
            tag.tipText = name+"\nHypernetwork";
            tag.value = "hypernet:"+name;
            //tag.used = false;
            tag.tagType = (int)TagType.TagHypernetwork;
            return tag;
        }

        public static TagInfo CreateLoraTag(string subGroup, string name)
        {
            TagInfo tag = new TagInfo();
            tag.name = name;
            tag.nameInList = name;
            tag.tipText = name + "\nLoRA";
            tag.value = "lora:" + name;
           //tag.used = false;
            tag.tagType = (int)TagType.TagLora;
            tag.subGroup = subGroup;
            return tag;
        }
    }
}
