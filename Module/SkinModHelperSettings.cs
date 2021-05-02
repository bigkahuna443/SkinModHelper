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
            var skinSelectMenu = new TextMenu.Option<string>(Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD"));

            skinSelectMenu.Add(Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD_DEFAULT"), "default_skin", true);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                bool selected = (config.SkinId == SkinModHelperModule.Settings.SelectedSkinMod);
                skinSelectMenu.Add(Dialog.Clean(config.SkinDialogKey), config.SkinId, selected);
            }

            // Set our update action on our complete menu
            skinSelectMenu.Change(skinId => SkinModHelperModule.UpdateSkin(skinId));

            if (inGame)
            {
                skinSelectMenu.AddDescription(menu, Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD_DESCRIPTION"));
            }
            menu.Add(skinSelectMenu);
        }
    }
}
