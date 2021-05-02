using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using SkinModHelper.Module;
using Microsoft.Xna.Framework;
using System;

namespace SkinModHelper
{
    [CustomEntity("SkinModHelper/SkinModSwapTrigger")]
    class SkinModSwapTrigger : Trigger
    {
        private string skinId;
        private string oldId;
        private bool revertOnLeave;

        public SkinModSwapTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            skinId = data.Attr("skinId", "default_skin");
            revertOnLeave = data.Bool("revertOnLeave", false);
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            if (skinId != SkinModHelperConfig.DEFAULT_SKIN && !SkinModHelperModule.skinConfigs.ContainsKey(skinId))
            {
                Logger.Log("SkinModHelper/SkinModSwapTrigger", $"Tried to swap to unknown skin ID {skinId}.");
                return;
            }
            oldId = SkinModHelperModule.Settings.SelectedSkinMod;
            SkinModHelperModule.UpdateSkin(skinId);
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            if (revertOnLeave)
            {
                SkinModHelperModule.UpdateSkin(oldId);
            }
        }
    }
}