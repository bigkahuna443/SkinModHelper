using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Mod.SkinModHelper {
    [SettingName("SKIN_MOD_HELPER_SETTINGS_TITLE")]
    public class SkinModHelperSettings : EverestModuleSettings {

        public enum BackpackMode { Default, Off, On }
        private BackpackMode backpack = BackpackMode.Default;

        public BackpackMode Backpack {
            get => backpack;
            set {
                backpack = value;
                SkinModHelperModule.RefreshPlayerSpriteMode();
            }
        }


        [SettingIgnore]
        public string SelectedPlayerSkin { get; set; }


        [SettingIgnore]
        public string SelectedSilhouetteSkin { get; set; }


        [SettingIgnore]
        public Dictionary<string, bool> ExtraXmlList { get; set; } = new();










        public void CreateBackpackEntry(TextMenu textMenu, bool inGame) {
            Array enumValues = Enum.GetValues(typeof(BackpackMode));
            Array.Sort((int[])enumValues);
            TextMenu.Item item = new TextMenu.Slider("SkinModHelper_options_Backpack".DialogClean(),
                    i => {
                        string enumName = enumValues.GetValue(i).ToString();
                        return $"SkinModHelper_options_{nameof(BackpackMode)}_{enumName}".DialogClean();
                    }, 0, enumValues.Length - 1, (int)Backpack)
                .Change(value => Backpack = (BackpackMode)value);

            if (SkinModHelperUI.Disabled(inGame)) {
                item.Disabled = true;
            }
            textMenu.Add(item);
        }
    }
}
