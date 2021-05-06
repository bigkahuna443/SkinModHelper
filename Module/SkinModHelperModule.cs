using Celeste;
using Celeste.Mod;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace SkinModHelper.Module
{
    public class SkinModHelperModule : EverestModule
    {
        public static SkinModHelperModule Instance;

        public override Type SettingsType => typeof(SkinModHelperSettings);
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;

        public static Dictionary<string, SkinModHelperConfig> skinConfigs;
        public static Dictionary<int, string> idHashes;

        private static ILHook TextboxRunRoutineHook;
        private static Dictionary<PlayerSpriteMode, string> playerSpriteModes;

        public SkinModHelperModule()
        {
            Instance = this;
            skinConfigs = new Dictionary<string, SkinModHelperConfig>();
            idHashes = new Dictionary<int, string>();
            playerSpriteModes = new Dictionary<PlayerSpriteMode, string>()
            {
                { PlayerSpriteMode.Madeline, "player"},
                { PlayerSpriteMode.MadelineNoBackpack, "player_no_backpack" },
                { PlayerSpriteMode.Badeline, "badeline" },
                { PlayerSpriteMode.MadelineAsBadeline, "player_badeline" },
                { PlayerSpriteMode.Playback, "player_playback" }
            };
        }

        public override void Load()
        {
            Logger.SetLogLevel("SkinModHelper/SkinModHelperModule", LogLevel.Warn);
            Logger.Log("SkinModHelper/SkinModHelperModule", "Initializing SkinModHelper");

            On.Monocle.SpriteBank.Create += SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOnHook;
            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;
            On.Celeste.PlayerSprite.ctor += PlayerSpriteCtorHook;

            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;
            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            TextboxRunRoutineHook = new ILHook(
                typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                SwapTextboxHook);
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);
            InitializeSettings();
        }

        public override void Unload()
        {
            Logger.Log("SkinModHelper/SkinModHelperModule", "Unloading SkinModHelper");

            On.Monocle.SpriteBank.Create -= SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOnHook;
            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;

            On.Celeste.PlayerSprite.ctor -= PlayerSpriteCtorHook;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            TextboxRunRoutineHook.Dispose();
        }

        public static bool IsModeCelestenet(PlayerSpriteMode mode)
        {
            return Convert.ToBoolean((int)mode >> 31);
        }

        public static int GetHashFromMode(PlayerSpriteMode mode)
        {
            return ((int)mode & ~(1 << 31)) >> 3;
        }

        public static PlayerSpriteMode GetBaseModeFromMode(PlayerSpriteMode mode)
        {
            PlayerSpriteMode baseMode = mode & (PlayerSpriteMode)7;
            if (baseMode >= PlayerSpriteMode.Madeline && baseMode <= PlayerSpriteMode.Playback)
            {
                return baseMode;
            }
            else
            {
                return PlayerSpriteMode.Madeline;
            }
        }

        public static PlayerSpriteMode BuildMode(int hash, PlayerSpriteMode baseMode, bool celestenet = false)
        {
            int c = celestenet ? 1 : 0;
            return (PlayerSpriteMode)((c << 31) | (hash << 3)) | baseMode;
        }

        private void InitializeSettings()
        {
            foreach (ModContent mod in Everest.Content.Mods)
            {
                SkinModHelperConfig config = null;
                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml))
                {
                    config = LoadConfigFile(configAsset);
                    Regex r = new Regex(@"^[a-zA-Z0-9]+_[a-zA-Z0-9]+$");
                    if (string.IsNullOrEmpty(config.SkinId) || !r.IsMatch(config.SkinId) || skinConfigs.ContainsKey(config.SkinId))
                    {
                        Logger.Log("SkinModHelper/SkinModHelperModule", $"Duplicate or invalid skin mod ID {config.SkinId}, will not register.");
                        continue;
                    }
                    if (string.IsNullOrEmpty(config.SkinDialogKey))
                    {
                        Logger.Log("SkinModHelper/SkinModHelperModule", $"Missing or invalid dialog key {config.SkinDialogKey}, will not register.");
                        continue;
                    }
                    skinConfigs.Add(config.SkinId, config);
                    idHashes.Add(config.SkinId.GetHashCode() >> 4, config.SkinId);
                    Logger.Log("SkinModHelper/SkinModHelperModule", $"Registered new skin mod: {config.SkinId}");
                }
            }
            if (Settings.SelectedSkinMod == null || !skinConfigs.ContainsKey(Settings.SelectedSkinMod))
            {
                Settings.SelectedSkinMod = SkinModHelperConfig.DEFAULT_SKIN;
            }
        }

        // Trigger when we change the setting, store the new one. If in-level, redraw player sprite.
        public static void UpdateSkin(string skinId)
        {
            Settings.SelectedSkinMod = skinId;
            Player player = (Engine.Scene)?.Tracker.GetEntity<Player>();
            if (player != null)
            {
                int hash = 0;
                if (Settings.SelectedSkinMod != SkinModHelperConfig.DEFAULT_SKIN)
                {
                    hash = idHashes.FirstOrDefault(x => x.Value == Settings.SelectedSkinMod).Key;
                }
                PlayerSpriteMode baseMode = player.Sprite.Mode & (PlayerSpriteMode)7;
                if (player.Active)
                {
                    player.ResetSpriteNextFrame(BuildMode(hash, baseMode));
                }
                else
                {
                    player.ResetSprite(BuildMode(hash, baseMode));
                }
            }
        }

        private void PlayerSpriteCtorHook(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode)
        {
            bool celestenet = IsModeCelestenet(mode);
            int hash = GetHashFromMode(mode);
            PlayerSpriteMode baseMode = GetBaseModeFromMode(mode);

            Console.WriteLine($"BEFORE: celestenet {celestenet}, hash {hash}, baseMode {baseMode}, mode {mode}");

            if (!celestenet && Settings.SelectedSkinMod != SkinModHelperConfig.DEFAULT_SKIN && hash == 0)
            {
                hash = idHashes.FirstOrDefault(x => x.Value == Settings.SelectedSkinMod).Key;
                mode = BuildMode(hash, baseMode);
            }

            orig(self, mode);

            if (!playerSpriteModes.ContainsKey(mode)) 
            {
                string spriteId;
                string baseId = playerSpriteModes[baseMode];

                if (idHashes.ContainsKey(hash))
                {
                    spriteId = baseId + "_" + idHashes[hash];
                }
                else
                {
                    spriteId = baseId;
                }
                DynData<PlayerSprite> playerSpriteData = new DynData<PlayerSprite>(self);
                playerSpriteData["spriteName"] = spriteId;
                GFX.SpriteBank.CreateOn(self, spriteId);
            }
            Console.WriteLine($"AFTER: celestenet {celestenet}, hash {hash}, baseMode {baseMode}, mode {mode}");
        }

        private MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index)
        {
            DynData<PlayerSprite> playerSpriteData = new DynData<PlayerSprite>(self.Sprite);
            PlayerSpriteMode mode = playerSpriteData.Get<PlayerSpriteMode>("Mode");
            int hash = GetHashFromMode(mode);
            PlayerSpriteMode baseMode = GetBaseModeFromMode(mode);
            string spriteName = playerSpriteData.Get<string>("spriteName");

            if (idHashes.ContainsKey(hash) && GFX.SpriteBank.SpriteData.ContainsKey(spriteName))
            {
                string basePath = skinConfigs[idHashes[hash]].GetUniquePath();
                if (index == 0)
                {
                    string newBangsPath = "";
                    if (baseMode != PlayerSpriteMode.Madeline)
                    {
                        newBangsPath = basePath + playerSpriteModes[baseMode] + "/bangs";
                        if (GFX.Game.Has(newBangsPath + "00"))
                        {
                            return GFX.Game.GetAtlasSubtextures(newBangsPath)[self.Sprite.HairFrame];
                        }
                    }
                    newBangsPath = basePath + "characters/player/bangs";
                    if (GFX.Game.Has(newBangsPath + "00"))
                    {
                        return GFX.Game.GetAtlasSubtextures(newBangsPath)[self.Sprite.HairFrame];
                    }
                }
                else
                {
                    string newHairPath = "";
                    if (baseMode != PlayerSpriteMode.Madeline)
                    {
                        newHairPath = basePath + playerSpriteModes[baseMode] + "/hair00";
                        if (GFX.Game.Has(newHairPath))
                        {
                            return GFX.Game[newHairPath];
                        }
                    }
                    newHairPath = basePath + "characters/player/hair00";
                    if (GFX.Game.Has(newHairPath))
                    {
                        return GFX.Game[newHairPath];
                    }
                }
            }
            return orig(self, index);
        }

        // If our current skinmod has an overridden sprite bank, use that sprite data instead
        private Sprite SpriteBankCreateOnHook(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id)
        {
            if (sprite is PlayerSprite)
            {
                if (id == "")
                {
                    return null;
                }
                else if (self.SpriteData.ContainsKey(id))
                {
                    Console.WriteLine($"CreateOn: {id}");
                    return orig(self, sprite, id);
                }
                else
                {
                    Regex r = new Regex(@"^(\w+)_[A-Za-z0-9]+_[A-Za-z0-9]+$");
                    string baseId = r.Match(id).Groups[1].Value;
                    Console.WriteLine($"CreateOn: {baseId}");
                    return orig(self, sprite, baseId);
                }
            }
            string newId = id + "_" + Settings.SelectedSkinMod;
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
                if (skinId == SkinModHelperConfig.DEFAULT_SKIN)
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
            if (Settings.SelectedSkinMod != SkinModHelperConfig.DEFAULT_SKIN)
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
        string ReplaceTextboxPath(string textboxPath)
        {
            if (Settings.SelectedSkinMod != SkinModHelperConfig.DEFAULT_SKIN)
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
            return YamlHelper.Deserializer.Deserialize<SkinModHelperConfig>(new StreamReader(skinConfigYaml.Stream));
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
                    if (playerSpriteModes.Values.Contains(spriteId))
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
