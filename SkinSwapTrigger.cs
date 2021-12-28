using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using SkinModHelper.Module;
using Microsoft.Xna.Framework;
using System.Linq;

namespace SkinModHelper
{
    [CustomEntity("SkinModHelper/SkinSwapTrigger")]
    class SkinSwapTrigger : Trigger
    {
        private string skinId;
        private string oldSkinId;
        private bool revertOnLeave;

        public SkinSwapTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            skinId = data.Attr("skinId", SkinModHelperModule.DEFAULT);
            revertOnLeave = data.Bool("revertOnLeave", false);

            oldSkinId = SkinModHelperModule.Settings.SelectedSkinMod;
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            if (skinId != SkinModHelperModule.DEFAULT && !SkinModHelperModule.skinConfigs.ContainsKey(skinId))
            {
                Logger.Log("SkinModHelper/SkinModSwapTrigger", $"Tried to swap to unknown skin ID {skinId}.");
                return;
            }
            oldSkinId = SkinModHelperModule.Settings.SelectedSkinMod;
            SkinModHelperModule.UpdateSkin(skinId);
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            if (revertOnLeave)
            {
                SkinModHelperModule.UpdateSkin(oldSkinId);
            }
        }
    }
}