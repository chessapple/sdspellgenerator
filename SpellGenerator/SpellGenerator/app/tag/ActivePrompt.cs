using SpellGenerator.app.file;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.tag
{
    public class ActivePrompt
    {
        public string defaultSpell = "";
        public List<TagInfo> initSpells = new List<TagInfo>();
        public string spell = "";

        public string name;

        public List<ActiveTagData> activeTags = new List<ActiveTagData>();

        public HashSet<string> usedTags = new HashSet<string>();

        public void UpdateSpell()
        {
            string spell = defaultSpell;
            foreach (ActiveTagData tagData in activeTags)
            {
                if (spell == "")
                {
                    spell = tagData.GetSpell();
                }
                else if (tagData.tagType == (int)TagType.TagHypernetwork || tagData.tagType == (int)TagType.TagLora)
                {
                    spell += " " + tagData.GetSpell();
                }
                else
                {
                    spell += ", " + tagData.GetSpell();
                }
            }
            this.spell = spell;
        }

        public void Add(ActiveTagData activeTagData)
        {
            activeTags.Add(activeTagData);
            usedTags.Add(activeTagData.value);
        }

        public void Remove(ActiveTagData activeTagData)
        {
            activeTags.Remove(activeTagData);
            usedTags.Remove(activeTagData.value);
        }

        public void Clear()
        {
            activeTags.Clear();
            usedTags.Clear();
        }

        public void Load(List<TagData> tags)
        {
            foreach (TagData td in tags)
            {
                TagInfo? tagInfo = null;
                if (td.tagType < 10)
                {
                    tagInfo = AppCore.Instance.FindTagInfo(td.prompt);
                }

                ActiveTagData tagData = new ActiveTagData();
                tagData.strength = td.strength;
                tagData.prompt = td.prompt;
                tagData.colorprompt = td.colorprompt;
                if (tagInfo != null)
                {
                    tagData.tagInfo = tagInfo;
                    tagData.classify = tagInfo.classify;
                    tagData.tagType = tagInfo.tagType;
                    tagData.colorTag = AppCore.Instance.FindColorTag(td.colorprompt);
                    tagData.value = tagInfo.value;
                }
                else if (td.tagType >= 10)
                {
                    tagData.tagType = td.tagType;
                    if (tagData.tagType == (int)TagType.TagTextInversion)
                    {
                        string name = td.prompt;
                        tagInfo = TagInfo.CreateTextInversionTag(name);
                        tagData.tagInfo = tagInfo;
                        tagData.value = tagInfo.value;
                    }
                    if (tagData.tagType == (int)TagType.TagHypernetwork)
                    {
                        int coIndex = td.prompt.IndexOf(":");
                        string name = td.prompt;
                        if (coIndex >= 0)
                        {
                            name = td.prompt.Substring(coIndex + 1);
                        }
                        tagInfo = TagInfo.CreateHypernetworkTag(name);
                        tagData.tagInfo = tagInfo;
                        tagData.value = tagInfo.value;
                    }
                    if (tagData.tagType == (int)TagType.TagLora)
                    {
                        int coIndex = td.prompt.IndexOf(":");
                        string name = td.prompt;
                        if (coIndex >= 0)
                        {
                            name = td.prompt.Substring(coIndex + 1);
                        }
                        tagInfo = TagInfo.CreateLoraTag("", name);
                        tagData.tagInfo = tagInfo;
                        tagData.value = tagInfo.value;
                        if(td.data != null)
                        {
                            LoraLayer loraLayer = new LoraLayer();
                            loraLayer.FromString(td.data);
                            tagData.loraLayer = loraLayer;
                        }
                    }
                }
                else
                {
                    tagData.classify = AppCore.Instance.FindClassify(td.classify);
                    tagData.tagType = td.tagType;
                    tagData.value = td.prompt;
                }

                Add(tagData);

                TagControl tagControl = new TagControl();
                tagControl.InitTag(tagData);
                tagData.control = tagControl;


            }
        }
    }
}
