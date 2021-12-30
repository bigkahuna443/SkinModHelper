using Celeste;
using Celeste.Mod;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SkinModHelper.Module
{
    public class SkinModHelperModule : EverestModule
    {
        public static SkinModHelperModule Instance;
        public static readonly string DEFAULT = "Default";
        public static readonly int MAX_DASHES = 5;

        public override Type SettingsType => typeof(SkinModHelperSettings);
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;

        public static SkinModHelperUI UI;

        public static Dictionary<string, SkinModHelperConfig> skinConfigs;
        private static ILHook TextboxRunRoutineHook;
        private static readonly List<string> spritesWithHair = new List<string>() 
        { 
            "player", "player_no_backpack", "badeline", "player_badeline", "player_playback" 
        };

        public SkinModHelperModule()
        {
            Instance = this;
            UI = new SkinModHelperUI();
            skinConfigs = new Dictionary<string, SkinModHelperConfig>();
        }

        public override void Load()
        {
            Logger.SetLogLevel("SkinModHelper", LogLevel.Info);
            Logger.Log("SkinModHelper", "Initializing SkinModHelper");

            Everest.Content.OnUpdate += EverestContentUpdateHook;

            On.Monocle.SpriteBank.Create += SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOnHook;
            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;
            On.Celeste.Player.Render += PlayerRenderHook;
            On.Celeste.Player.UpdateHair += PlayerUpdateHairHook;
            On.Celeste.PlayerDeadBody.Render += PlayerDeadBodyRenderHook;
            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;

            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += DreamBlockHook;
            IL.Celeste.DeathEffect.Draw += DeathEffectDrawHook;
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool += FlyFeatherHook;
            TextboxRunRoutineHook = new ILHook(
                typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                SwapTextboxHook);
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            ReloadSettings();
            UpdateParticles();
        }

        public override void Unload()
        {
            Logger.Log("SkinModHelper", "Unloading SkinModHelper");

            Everest.Content.OnUpdate -= EverestContentUpdateHook;

            On.Monocle.SpriteBank.Create -= SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOnHook;
            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;
            On.Celeste.Player.Render -= PlayerRenderHook;
            On.Celeste.PlayerDeadBody.Render -= PlayerDeadBodyRenderHook;

            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool -= DreamBlockHook;
            IL.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool -= FlyFeatherHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            TextboxRunRoutineHook.Dispose();

            On.Celeste.Player.UpdateHair -= PlayerUpdateHairHook;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
        {
            base.CreateModMenuSection(menu, inGame, snapshot);
            UI.CreateMenu(menu, inGame);
        }

        private void EverestContentUpdateHook(ModAsset oldAsset, ModAsset newAsset)
        {
            if (newAsset != null && newAsset.PathVirtual.StartsWith("SkinModHelperConfig"))
            {
                ReloadSettings();
            }
        }

        private void ReloadSettings()
        {
            skinConfigs.Clear();
            Instance.LoadSettings();

            foreach (ModContent mod in Everest.Content.Mods)
            {
                SkinModHelperConfig config;
                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml))
                {
                    config = LoadConfigFile(configAsset);

                    Regex skinIdRegex = new Regex(@"^[a-zA-Z0-9]+_[a-zA-Z0-9]+$");
                    if (string.IsNullOrEmpty(config.SkinId) || !skinIdRegex.IsMatch(config.SkinId) || skinConfigs.ContainsKey(config.SkinId))
                    {
                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"Duplicate or invalid skin mod ID {config.SkinId}, will not register.");
                        continue;
                    }

                    // Default colors taken from vanilla
                    config.GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1]);
                    config.GeneratedHairColors[0] = Calc.HexToColor("44B7FF");
                    config.GeneratedHairColors[1] = Calc.HexToColor("AC3232");
                    config.GeneratedHairColors[2] = Calc.HexToColor("FF6DEF");

                    List<bool> changed = new List<bool>(new bool[MAX_DASHES + 1]);

                    if (config.HairColors != null)
                    {
                        foreach (SkinModHelperConfig.HairColor hairColor in config.HairColors)
                        {
                            Regex hairColorRegex = new Regex(@"^[a-fA-F0-9]{6}$");
                            if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColorRegex.IsMatch(hairColor.Color))
                            {
                                config.GeneratedHairColors[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                                changed[hairColor.Dashes] = true;
                            }
                            else
                            {
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Invalid hair color or dash count values provided for {config.SkinId}.");
                            }
                        }
                    }

                    // Fill upper dash range with the last customized dash color
                    for (int i = 3; i <= MAX_DASHES; i++)
                    {
                        if (!changed[i])
                        {
                            config.GeneratedHairColors[i] = config.GeneratedHairColors[i - 1];
                        }
                    }

                    skinConfigs.Add(config.SkinId, config);
                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new skin mod: {config.SkinId}");
                }
            }
            if (Settings.SelectedSkinMod == null || !skinConfigs.ContainsKey(Settings.SelectedSkinMod))
            {
                Settings.SelectedSkinMod = DEFAULT;
            }
        }

        private void PlayerRenderHook(On.Celeste.Player.orig_Render orig, Player self)
        {
            if (UniqueSkinSelected())
            {
                int dashCount = Math.Min(self.Dashes, MAX_DASHES);
                string colorGradePath = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "dash";

                // Default to two-dash color grade if we don't have one for higher dash counts
                while (dashCount > 2 && !GFX.ColorGrades.Has(colorGradePath + dashCount))
                {
                    dashCount--;
                }

                if (GFX.ColorGrades.Has(colorGradePath + dashCount))
                {
                    Effect fxColorGrading = GFX.FxColorGrading;
                    fxColorGrading.CurrentTechnique = fxColorGrading.Techniques["ColorGradeSingle"];
                    Engine.Graphics.GraphicsDevice.Textures[1] = GFX.ColorGrades[colorGradePath + dashCount].Texture.Texture_Safe;
                    Draw.SpriteBatch.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, fxColorGrading, (self.Scene as Level).GameplayRenderer.Camera.Matrix);
                    orig(self);
                    Draw.SpriteBatch.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, (self.Scene as Level).GameplayRenderer.Camera.Matrix);
                    return;
                }
            }
            orig(self);
        }

        private void PlayerDeadBodyRenderHook(On.Celeste.PlayerDeadBody.orig_Render orig, PlayerDeadBody self)
        {
            DynData<PlayerDeadBody> deadBody = new DynData<PlayerDeadBody>(self);
            int dashCount = deadBody.Get<Player>("player").Dashes;
            if (Settings.SelectedSkinMod != DEFAULT)
            {
                string colorGradePath = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "dash";

                while (dashCount > 2 && !GFX.ColorGrades.Has(colorGradePath + dashCount))
                {
                    dashCount--;
                }
                if (GFX.ColorGrades.Has(colorGradePath + dashCount))
                {
                    Effect fxColorGrading = GFX.FxColorGrading;
                    fxColorGrading.CurrentTechnique = fxColorGrading.Techniques["ColorGradeSingle"];
                    Engine.Graphics.GraphicsDevice.Textures[1] = GFX.ColorGrades[colorGradePath + dashCount].Texture.Texture_Safe;
                    Draw.SpriteBatch.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, fxColorGrading, (self.Scene as Level).GameplayRenderer.Camera.Matrix);
                    orig(self);
                    Draw.SpriteBatch.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, (self.Scene as Level).GameplayRenderer.Camera.Matrix);
                    return;
                }
            }
            orig(self);
        }

        private MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index)
        {
            if (UniqueSkinSelected())
            {
                if (index == 0)
                {
                    string newBangsPath = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "characters/player/bangs";
                    if (GFX.Game.Has(newBangsPath + "00"))
                    {
                        List<MTexture> bangsTextures = GFX.Game.GetAtlasSubtextures(newBangsPath);
                        if (bangsTextures.Count > self.Sprite.HairFrame)
                        {
                            return bangsTextures[self.Sprite.HairFrame];
                        }
                        else
                        {
                            return bangsTextures[0];
                        }
                    }
                }
                string newHairPath = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "characters/player/hair00";
                if (GFX.Game.Has(newHairPath))
                {
                    return GFX.Game[newHairPath];
                }
            }
            return orig(self, index);
        }

        private void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity)
        {
            orig(self, applyGravity);
            if (UniqueSkinSelected())
            {
                int dashCount = Math.Min(self.Dashes, MAX_DASHES);
                self.Hair.Color = skinConfigs[Settings.SelectedSkinMod].GeneratedHairColors[dashCount];
            }
        }

        // If our current skinmod has an overridden sprite bank, use that sprite data instead
        private Sprite SpriteBankCreateOnHook(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id)
        {
            String newId = id + "_" + Settings.SelectedSkinMod;
            if (self.SpriteData.ContainsKey(newId))
            {
                id = newId;
            }
            return orig(self, sprite, id);
        }

        private Sprite SpriteBankCreateHook(On.Monocle.SpriteBank.orig_Create orig, SpriteBank self, string id)
        {
            String newId = id + "_" + Settings.SelectedSkinMod;
            if (self.SpriteData.ContainsKey(newId))
            {
                id = newId;
            }
            return orig(self, id);
        }

        // Wait until the main sprite bank is created, then combine with our skin mod banks
        private void LevelLoaderLoadingThreadHook(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
        {
            foreach (KeyValuePair<string, SkinModHelperConfig> config in skinConfigs)
            {
                string skinId = config.Key;
                if (skinId == DEFAULT)
                {
                    continue;
                }
                string spritesXmlPath = "Graphics/" + config.Value.GetUniquePath() + "Sprites.xml";
                string portraitsXmlPath = "Graphics/" + config.Value.GetUniquePath() + "Portraits.xml";
                CombineSpriteBanks(GFX.SpriteBank, skinId, spritesXmlPath);
                CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
            }
            orig(self);
        }

        private void DreamBlockHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/dreamblock/particles")))
            {
                Logger.Log("SkinModHelper", $"Changing hair path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>(ReplaceDreamBlockParticle);
            }
        }

        private static string ReplaceDreamBlockParticle(string dreamBlockParticle)
        {
            if (UniqueSkinSelected())
            {
                string newDreamBlockParticle = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "objects/dreamblock/particles";
                if (GFX.Game.Has(newDreamBlockParticle))
                {
                    return newDreamBlockParticle;
                }
            }
            return dreamBlockParticle;
        }

        private void DeathEffectDrawHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/hair00"))) {
                Logger.Log("SkinModHelper", $"Changing hair path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>(ReplaceDeathParticle);
            }
        }

        private static string ReplaceDeathParticle(string deathParticle)
        {
            if (UniqueSkinSelected())
            {
                string newDeathParticle = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "characters/player/death_particle";
                if (GFX.Game.Has(newDeathParticle))
                {
                    return newDeathParticle;
                }
            }
            return deathParticle;
        }

        private void FlyFeatherHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/flyFeather/outline")))
            {
                Logger.Log("SkinModHelper", $"Changing feather outline path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>(ReplaceFeatherOutline);
            }
        }
        private static string ReplaceFeatherOutline(string featherOutline)
        {
            if (UniqueSkinSelected())
            {
                string newFeatherOutline = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "objects/flyFeather/outline";
                if (GFX.Game.Has(newFeatherOutline))
                {
                    return newFeatherOutline;
                }
            }
            return featherOutline;
        }

        private void SwapTextboxHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            // Move to the last occurence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) { }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true)
            {
                Logger.Log("SkinModHelper", $"Changing portrait path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>(ReplacePortraitPath);
            }
        }

        // This one requires double hook - for some reason they implemented a tiny version of the Textbox class that behaves differently
        private void CampfireQuestionHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            // Move to the last occurrence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) { }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true)
            {
                Logger.Log("SkinModHelper", $"Changing portrait path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>(ReplacePortraitPath);
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("_ask"), 
                instr => instr.MatchCall(out MethodReference method) && method.Name == "Concat"))
            {
                Logger.Log("SkinModHelper", $"Changing textbox path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>(ReplaceTextboxPath);
            }
        }

        private static FancyText.Portrait ReplacePortraitPath(FancyText.Portrait portrait)
        {
            if (UniqueSkinSelected())
            {
                string skinModPortraitSpriteId = portrait.SpriteId + "_" + Settings.SelectedSkinMod;
                if (GFX.PortraitsSpriteBank.Has(skinModPortraitSpriteId))
                {
                    portrait.Sprite = skinModPortraitSpriteId.Replace("portrait_", "");
                }
            }
            return portrait;
        }

        // ReplacePortraitPath makes textbox path funky, so correct to our real path or revert to vanilla if it does not exist
        private static string ReplaceTextboxPath(string textboxPath)
        {
            if (UniqueSkinSelected())
            {
                string originalPortraitId = textboxPath.Split('_')[0].Replace("textbox/", ""); // "textbox/[orig portrait id]_[skin id]_ask"
                string newTextboxPath = "textbox/" + skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + originalPortraitId + "_ask";
                if (GFX.Portraits.Has(newTextboxPath))
                {
                    textboxPath = newTextboxPath;
                }
            }
            return textboxPath;
        }

        private static SkinModHelperConfig LoadConfigFile(ModAsset skinConfigYaml)
        {
            return skinConfigYaml.Deserialize<SkinModHelperConfig>();
        }

        private static void UpdateParticles()
        {
            FlyFeather.P_Collect.Source = GFX.Game["particles/feather"];
            FlyFeather.P_Boost.Source = GFX.Game["particles/feather"];

            if (Settings.SelectedSkinMod != DEFAULT)
            {
                string featherParticle = skinConfigs[Settings.SelectedSkinMod].GetUniquePath() + "particles/feather";
                if (GFX.Game.Has(featherParticle))
                {
                    FlyFeather.P_Collect.Source = GFX.Game[featherParticle];
                    FlyFeather.P_Boost.Source = GFX.Game[featherParticle];
                }


            }
        }

        // Combine skin mod XML with a vanilla sprite bank
        private void CombineSpriteBanks(SpriteBank origBank, string skinId, string xmlPath)
        {
            SpriteBank newBank = BuildBank(origBank, xmlPath);
            if (newBank == null)
            {
                return;
            }

            // For each overridden sprite, patch it and add it to the original bank with a unique identifier
            foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData)
            {
                string spriteId = spriteDataEntry.Key;
                SpriteData newSpriteData = spriteDataEntry.Value;

                if (origBank.SpriteData.TryGetValue(spriteId, out SpriteData origSpriteData))
                {
                    PatchSprite(origSpriteData.Sprite, newSpriteData.Sprite);

                    string newSpriteId = spriteId + "_" + skinId;
                    origBank.SpriteData[newSpriteId] = newSpriteData;

                    // Build hair!
                    if (spritesWithHair.Contains(spriteId))
                    {
                        PlayerSprite.CreateFramesMetadata(newSpriteId);
                    }
                }
            }
        }

        private SpriteBank BuildBank (SpriteBank origBank, string xmlPath)
        {
            try
            {
                SpriteBank newBank = new SpriteBank(origBank.Atlas, xmlPath);
                Logger.Log("SkinModHelper", $"Built sprite bank for {xmlPath}.");
                return newBank;
            }
            catch (Exception e)
            {
                Logger.Log("SkinModHelper", $"Could not build sprite bank for {xmlPath}: {e.Message}.");
                return null;
            }
        }

        // Add any missing vanilla animations to an overridden sprite
        private void PatchSprite(Sprite origSprite, Sprite newSprite)
        {
            Dictionary<string, Sprite.Animation> newAnims = newSprite.GetAnimations();
            // Shallow copy... sometimes new animations get added mid-update?
            Dictionary<string, Sprite.Animation> oldAnims = new Dictionary<string, Sprite.Animation>(origSprite.GetAnimations());
            foreach (KeyValuePair<string, Sprite.Animation> animEntry in oldAnims)
            {
                string origAnimId = animEntry.Key;
                Sprite.Animation origAnim = animEntry.Value;
                if (!newAnims.ContainsKey(origAnimId))
                {
                    newAnims[origAnimId] = origAnim;
                }
            }
        }

        // Trigger when we change the setting, store the new one. If in-level, redraw player sprite.
        public static void UpdateSkin(string newSkinId)
        {
            Settings.SelectedSkinMod = newSkinId;
            UpdateParticles();

            Player player = (Engine.Scene)?.Tracker.GetEntity<Player>();
            if (player != null)
            {
                if (player.Active)
                {
                    player.ResetSpriteNextFrame(player.Sprite.Mode);
                }
                else
                {
                    player.ResetSprite(player.Sprite.Mode);
                }
            }
        }

        public static bool UniqueSkinSelected()
        {
            return (Settings.SelectedSkinMod != null && Settings.SelectedSkinMod != DEFAULT);
        }
    }
}
