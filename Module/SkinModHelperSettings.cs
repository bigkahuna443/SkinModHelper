using Celeste;
using Celeste.Mod;

namespace SkinModHelper.Module
{
    [SettingName("SKIN_MOD_HELPER_SETTINGS_TITLE")]
    public class SkinModHelperSettings : EverestModuleSettings
    {
        public string SelectedSkinMod { get; set; }
        public void CreateSelectedSkinModEntry(TextMenu menu, bool inGame)
        {
            menu.Add(SkinModHelperModule.skinSelectMenu);
            SkinModHelperModule.skinSelectMenu.Label = Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD");
            SkinModHelperModule.skinSelectMenu.AddDescription(menu, Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD_DESCRIPTION"));
        }
    }
}
