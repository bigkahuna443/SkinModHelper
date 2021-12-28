using Celeste;
using Celeste.Mod;
using System;

namespace SkinModHelper.Module
{
    [SettingName("SKIN_MOD_HELPER_SETTINGS_TITLE")]
    public class SkinModHelperSettings : EverestModuleSettings
    {
        public string SelectedSkinMod { get; set; }
    }
}
