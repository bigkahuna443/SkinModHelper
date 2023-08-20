using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.SkinModHelper {
    [CustomEntity("SkinModHelper/SkinSwapTrigger")]
    internal class SkinSwapTrigger : Trigger {
        private readonly string skinId;
        private readonly bool revertOnLeave;
        private string oldSkinId;

        public SkinSwapTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
            skinId = data.Attr("skinId", SkinModHelperModule.DEFAULT);
            revertOnLeave = data.Bool("revertOnLeave", false);

            oldSkinId = SkinModHelperModule.Session.SkinOverride;
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (skinId != SkinModHelperModule.DEFAULT && !SkinModHelperModule.skinConfigs.ContainsKey(skinId)) {
                Logger.Log(LogLevel.Warn, "SkinModHelper/SkinModSwapTrigger", $"Tried to swap to unknown skin ID {skinId}.");
                return;
            }

            oldSkinId = SkinModHelperModule.Settings.SelectedSkinMod;
            SkinModHelperModule.SetSessionSkin(skinId);
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (revertOnLeave) {
                SkinModHelperModule.SetSessionSkin(oldSkinId);
            }
        }
    }
}