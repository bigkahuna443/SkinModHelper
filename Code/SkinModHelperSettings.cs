using Celeste.Mod;

namespace SkinModHelper.Module
{
    [SettingName("SKIN_MOD_HELPER_SETTINGS_TITLE")]
    public class SkinModHelperSettings : EverestModuleSettings
    {
        [SettingIgnore]
        public string SelectedSkinMod { get; set; }
    }
}
