using Celeste;
using Celeste.Mod;

namespace SkinModHelper.Module
{
    public class SkinModHelperUI
    {
        public void CreateMenu(TextMenu menu, bool inGame)
        {
            BuildSkinSelectMenu(menu, inGame);
        }

        void BuildSkinSelectMenu(TextMenu menu, bool inGame)
        {
            var skinSelectMenu = new TextMenu.Option<string>(Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD"));

            skinSelectMenu.Add(Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_DEFAULT"), SkinModHelperModule.DEFAULT, true);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                bool selected = (config.SkinId == SkinModHelperModule.Settings.SelectedSkinMod);
                string name = Dialog.Clean(config.SkinDialogKey);
                name = (name == "") ? config.SkinId : name;
                skinSelectMenu.Add(name, config.SkinId, selected);
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
