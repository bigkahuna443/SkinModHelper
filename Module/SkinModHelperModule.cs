using Celeste;
using Celeste.Mod;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace SkinModHelper.Module
{
    public class SkinModHelperModule : EverestModule
    {
        public static SkinModHelperModule Instance;

        public override Type SettingsType => typeof(SkinModHelperSettings);
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;

        public static Dictionary<string, SkinModHelperConfig> skinConfigs;
        public static TextMenu.Option<string> skinSelectMenu;

        private static ILHook TextboxRunRoutineHook;
        private static List<string> spritesWithHair = new List<string>() 
        { 
            "player", "player_no_backpack", "badeline", "player_badeline", "player_playback" 
        };

        public SkinModHelperModule()
        {
            Instance = this;

            skinConfigs = new Dictionary<string, SkinModHelperConfig>();
            skinSelectMenu = new TextMenu.Option<string>("");
        }

        public override void Load()
        {
            Logger.SetLogLevel("SkinModHelper/SkinModHelperModule", LogLevel.Warn);
            Logger.Log("SkinModHelper/SkinModHelperModule", "Initializing SkinModHelper");

            On.Celeste.Dialog.Load += DialogLoadHook;
            On.Monocle.SpriteBank.Create += SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOnHook;
            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;

            IL.Celeste.PlayerHair.ctor += PlayerHairHook;
            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            TextboxRunRoutineHook = new ILHook(
                typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                SwapTextboxHook);
        }

        public override void Unload()
        {
            Logger.Log("SkinModHelper/SkinModHelperModule", "Unloading SkinModHelper");

            On.Celeste.Dialog.Load -= DialogLoadHook;
            On.Monocle.SpriteBank.Create -= SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOnHook;
            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;

            IL.Celeste.PlayerHair.ctor -= PlayerHairHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            TextboxRunRoutineHook.Dispose();
        }

        // Wait for Dialog module to load so we can set up menu
        private void DialogLoadHook(On.Celeste.Dialog.orig_Load orig)
        {
            orig();
            InitializeSettings();
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
                CombineSpriteBanks(GFX.SpriteBank, skinId, config.Value.SpritesXmlPath);
                CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, config.Value.PortraitsXmlPath);
            }
            orig(self);
        }

        private void SwapTextboxHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>()))
            {
                break;
            }
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>()))
            {
                Logger.Log("SkinModHelper/SkinModHelperModule", $"Changing portrait path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((FancyText.Portrait portrait) => ReplacePortraitPath(portrait));
            }
        }

        private void PlayerHairHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/bangs"))) {
                Logger.Log("SkinModHelper/SkinModHelperModule", $"Changing bangs path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>((string bangsPath) => ReplaceBangsPath(bangsPath));
            }
        }

        string ReplaceBangsPath(string bangsPath)
        {
            string skinModPlayerSpriteId = "madeline_" + Settings.SelectedSkinMod;
            if (GFX.SpriteBank.Has(skinModPlayerSpriteId))
            {
                XmlElement xml = GFX.SpriteBank.SpriteData[skinModPlayerSpriteId].Sources[0].XML;
                return xml.Attr("path", "characters/player/") + "bangs";
            }
            return bangsPath;
        }

        // This one requires double hook - for some reason they implemented a tiny version of the Textbox class that behaves differently
        private void CampfireQuestionHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>()))
            {
                break;
            }
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>()))
            {
                Logger.Log("SkinModHelper/SkinModHelperModule", $"Changing portrait path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((FancyText.Portrait portrait) => ReplacePortraitPath(portrait));
                break;
            }
            // This one was a bit cursed, had to go two instructions back to get to it
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("_ask")))
            {
                Logger.Log("SkinModHelper/SkinModHelperModule", $"Changing textbox path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.GotoNext().GotoNext().
                    EmitDelegate<Func<string, string>>((string textboxPath) => ReplaceTextboxPath(textboxPath));
            }
        }

        FancyText.Portrait ReplacePortraitPath(FancyText.Portrait portrait)
        {
            string skinModPortraitSpriteId = portrait.SpriteId + "_" + Settings.SelectedSkinMod;
            if (GFX.PortraitsSpriteBank.Has(skinModPortraitSpriteId))
            {
                portrait.Sprite = skinModPortraitSpriteId.Replace("portrait_", "");
            }
            return portrait;
        }

        string ReplaceTextboxPath(string textboxPath)
        {
            int len = textboxPath.Length;
            string portraitId = "portrait_" + textboxPath.Substring(8, len - 12); // "textbox/[id]_ask"
            if (GFX.PortraitsSpriteBank.Has(portraitId))
            {
                XmlElement xml = GFX.PortraitsSpriteBank.SpriteData[portraitId].Sources[0].XML;
                return "textbox/" + xml.Attr("textbox", "default") + "_ask";
            }
            return textboxPath;
        }

        private void InitializeSettings()
        {
            skinSelectMenu.Add(Dialog.Clean("SKIN_MOD_HELPER_SETTINGS_SELECTED_SKIN_MOD_DEFAULT"), "default", true);

            foreach (ModContent mod in Everest.Content.Mods)
            {
                SkinModHelperConfig config = null;
                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml))
                {
                    config = LoadConfigFile(configAsset);
                    if (string.IsNullOrEmpty(config.SkinId) || skinConfigs.ContainsKey(config.SkinId))
                    {
                        Logger.Log("SkinModHelper/SkinModHelperModule", $"Duplicate or invalid skin mod ID {config.SkinId}, will not register.");
                        continue;
                    }
                    if (!Dialog.Has(config.SkinDialogKey))
                    {
                        Logger.Log("SkinModHelper/SkinModHelperModule", $"Missing or invalid dialog key {config.SkinDialogKey}, will not register.");
                        continue;
                    }
                    skinConfigs.Add(config.SkinId, config);

                    // Change selection to this skin if it matches our last setting
                    bool selected = (config.SkinId == Settings.SelectedSkinMod);
                    skinSelectMenu.Add(Dialog.Clean(config.SkinDialogKey), config.SkinId, selected);

                    Logger.Log("SkinModHelper/SkinModHelperModule", $"Registered new skin mod: {config.SkinId}");
                }
            }

            // Set our update action on our complete menu
            skinSelectMenu.Change(skinId => UpdateSprite(skinId));
        }

        private static SkinModHelperConfig LoadConfigFile(ModAsset skinConfigYaml)
        {
            return YamlHelper.Deserializer.Deserialize<SkinModHelperConfig>(new StreamReader(skinConfigYaml.Stream));
        }

        // Trigger when we change the setting, store the new one. If in-level, redraw player sprite.
        public void UpdateSprite(string skinId)
        {
            Settings.SelectedSkinMod = skinId;
            Player player = (Engine.Scene)?.Tracker.GetEntity<Player>();
            if (player != null)
            {
                player.ResetSprite(player.Sprite.Mode);
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
                Logger.Log("SkinModHelper/SkinModHelperModule", $"Built sprite bank for {xmlPath}.");
                return newBank;
            }
            catch (Exception e)
            {
                Logger.Log("SkinModHelper/SkinModHelperModule", $"Could not build sprite bank for {xmlPath}: {e.Message}.");
                return null;
            }
        }

        // Add any missing vanilla animations to an overridden sprite
        private void PatchSprite(Sprite origSprite, Sprite newSprite)
        {
            Dictionary<string, Sprite.Animation> newAnims = newSprite.GetAnimations();
            foreach (KeyValuePair<string, Sprite.Animation> animEntry in origSprite.GetAnimations())
            {
                string origAnimId = animEntry.Key;
                Sprite.Animation origAnim = animEntry.Value;
                if (!newAnims.ContainsKey(origAnimId))
                {
                    newAnims[origAnimId] = origAnim;
                }
            }
        }
    }
}
