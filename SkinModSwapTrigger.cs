using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using SkinModHelper.Module;
using Microsoft.Xna.Framework;

namespace SkinModHelper
{
    [CustomEntity("SkinModHelper/SkinModSwapTrigger")]
    class SkinModSwapTrigger : Trigger
    {
        private string skinId;

        public SkinModSwapTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            skinId = data.Attr("skinId", "default");
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            if (!SkinModHelperModule.skinConfigs.ContainsKey(skinId))
            {
                Logger.Log("SkinModHelper/SkinModSwapTrigger", $"Tried to swap to unknown skin ID {skinId}.");
                skinId = "default";
            }
            SkinModHelperModule.Instance.UpdateSprite(skinId);
            UpdateMenuOption(SkinModHelperModule.skinSelectMenu, skinId);
        }

        private void UpdateMenuOption(TextMenu.Option<string> option, string newValue)
        {
            option.PreviousIndex = option.Index;
            option.Index = option.Values.FindIndex(entry => entry.Item2 == newValue);
        }
    }
}