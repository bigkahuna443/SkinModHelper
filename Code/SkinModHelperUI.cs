using Monocle;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.SkinModHelper
{
    public class SkinModHelperUI
    {
        public void CreateMenu(TextMenu menu, bool inGame)
        {
            BuildPlayerSkinSelectMenu(menu, inGame);
            BuildSilhouetteSkinSelectMenu(menu, inGame);

            menu.Add(BuildExSkinSubMenu(menu, inGame));
        }


        private void BuildPlayerSkinSelectMenu(TextMenu menu, bool inGame)
        {
            TextMenu.Option<string> skinSelectMenu = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkin_Selected"));

            skinSelectMenu.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), SkinModHelperModule.DEFAULT, true);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                bool selected = config.Options == SkinModHelperModule.Settings.SelectedPlayerSkin;
                string name = Dialog.Clean("SkinModHelper_Player_" + config.Options);
                if (config.Player_List)
                {
                    skinSelectMenu.Add(name, config.Options, selected);
                }
            }

            // Set our update action on our complete menu
            skinSelectMenu.Change(skinId => SkinModHelperModule.UpdateSkin(skinId));

            if (inGame)
            {
                skinSelectMenu.AddDescription(menu, Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD_DESCRIPTION"));
                Player player = Engine.Scene?.Tracker.GetEntity<Player>();
                if (player != null && player.StateMachine.State == Player.StIntroWakeUp)
                {
                    skinSelectMenu.Disabled = true;
                }
            }


            menu.Add(skinSelectMenu);
        }

        private void BuildSilhouetteSkinSelectMenu(TextMenu menu, bool inGame)
        {
            TextMenu.Option<string> skinSelectMenu = new(Dialog.Clean("SkinModHelper_Settings_SilhouetteSkin_Selected"));

            skinSelectMenu.Add(Dialog.Clean("SkinModHelper_Settings_DefaultSilhouette"), SkinModHelperModule.DEFAULT, true);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                bool selected = config.Options == SkinModHelperModule.Settings.SelectedSilhouetteSkin;
                string name = Dialog.Clean("SkinModHelper_Silhouette_" + config.Options);
                if (config.Silhouette_List)
                {
                    skinSelectMenu.Add(name, config.Options, selected);
                }
            }

            skinSelectMenu.Change(skinId => SkinModHelperModule.UpdateSilhouetteSkin(skinId));

            menu.Add(skinSelectMenu);
        }

        public EaseInSubMenu BuildExSkinSubMenu(TextMenu menu, bool inGame)
        {
            return new EaseInSubMenu(Dialog.Clean("SkinModHelper_Settings_Otherskin"), false).Apply(subMenu =>
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
                {
                    string Options_name = ("SkinModHelper_ExSprite_" + config.Options);
                    bool Options_OnOff = false;

                    if (!SkinModHelperModule.Settings.ExtraXmlList.ContainsKey(config.Options))
                    {
                        SkinModHelperModule.Settings.ExtraXmlList.Add(config.Options, false);
                    }
                    else
                    {
                        Options_OnOff = SkinModHelperModule.Settings.ExtraXmlList[config.Options];
                    }

                    TextMenu.OnOff Options = new TextMenu.OnOff(Dialog.Clean(Options_name), Options_OnOff);
                    Options.Change(OnOff => SkinModHelperModule.UpdateExtraXml(config.Options, OnOff));

                    subMenu.Add(Options);
                }
            });
        }
    }





    public static class CommonExtensions
    {
        public static EaseInSubMenu Apply<EaseInSubMenu>(this EaseInSubMenu obj, Action<EaseInSubMenu> action)
        {
            action(obj);
            return obj;
        }
    }

    public class EaseInSubMenu : TextMenuExt.SubMenu
    {
        private readonly MTexture icon;
        private float alpha;
        private float ease;
        private float unEasedAlpha;
        public EaseInSubMenu(string label, bool enterOnSelect) : base(label, enterOnSelect)
        {
            alpha = unEasedAlpha = true ? 1f : 0f;
            FadeVisible = Visible = true;
            icon = GFX.Gui["downarrow"];
        }

        public bool FadeVisible { get; set; }

        public override float Height() => MathHelper.Lerp(-Container.ItemSpacing, base.Height(), alpha);

        public override void Update()
        {
            ease = Calc.Approach(ease, Focused ? 1f : 0f, Engine.RawDeltaTime * 4f);
            base.Update();

            float targetAlpha = FadeVisible ? 1 : 0;
            if (Math.Abs(unEasedAlpha - targetAlpha) > 0.001f)
            {
                unEasedAlpha = Calc.Approach(unEasedAlpha, targetAlpha, Engine.RawDeltaTime * 3f);
                alpha = FadeVisible ? Ease.SineOut(unEasedAlpha) : Ease.SineIn(unEasedAlpha);
            }

            Visible = alpha != 0;
        }

        public override void Render(Vector2 position, bool highlighted)
        {
            Vector2 top = new(position.X, position.Y - (Height() / 2));

            float currentAlpha = Container.Alpha * alpha;
            Color color = Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : Color.White) * currentAlpha);
            Color strokeColor = Color.Black * (currentAlpha * currentAlpha * currentAlpha);

            bool unCentered = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;

            Vector2 titlePosition = top + (Vector2.UnitY * TitleHeight / 2) + (unCentered ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
            Vector2 justify = unCentered ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            Vector2 iconJustify = unCentered
                ? new Vector2(ActiveFont.Measure(Label).X + icon.Width, 5f)
                : new Vector2(ActiveFont.Measure(Label).X / 2 + icon.Width, 5f);
            DrawIcon(titlePosition, iconJustify, true, Items.Count < 1 ? Color.DarkSlateGray : color, alpha);
            ActiveFont.DrawOutline(Label, titlePosition, justify, Vector2.One, color, 2f, strokeColor);

            if (Focused && ease > 0.9f)
            {
                Vector2 menuPosition = new(top.X + ItemIndent, top.Y + TitleHeight + ItemSpacing);
                RecalculateSize();
                foreach (TextMenu.Item item in Items)
                {
                    if (item.Visible)
                    {
                        float height = item.Height();
                        Vector2 itemPosition = menuPosition + new Vector2(0f, height * 0.5f + item.SelectWiggler.Value * 8f);
                        if (itemPosition.Y + height * 0.5f > 0f && itemPosition.Y - height * 0.5f < Engine.Height)
                        {
                            item.Render(itemPosition, Focused && Current == item);
                        }

                        menuPosition.Y += height + ItemSpacing;
                    }
                }
            }
        }

        private void DrawIcon(Vector2 position, Vector2 justify, bool outline, Color color, float scale)
        {
            if (outline)
            {
                icon.DrawOutlineCentered(position + justify, color, scale);
            }
            else
            {
                icon.DrawCentered(position + justify, color, scale);
            }
        }
    }
}