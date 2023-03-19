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
using Mono.Cecil.Cil;
using Celeste.Mod.UI;
using Celeste.Mod;

using Celeste.Mod.JungleHelper;
using Celeste.Mod.SaveFilePortraits;

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperModule : EverestModule {
        public static SkinModHelperModule Instance;
        public static readonly string DEFAULT = "Default";
        public static readonly string ORIGINAL = "Original";
        public static readonly int MAX_DASHES = 5;

        private static readonly List<string> spritesWithHair = new() {
            "player", "player_no_backpack", "badeline", "player_badeline", "player_playback"
        };

        public override Type SettingsType => typeof(SkinModHelperSettings);
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;


        public static SkinModHelperUI UI;

        public static Dictionary<string, SkinModHelperConfig> skinConfigs;
        public static Dictionary<string, SkinModHelperConfig> OtherskinConfigs;

        public static string Xmls_record;

        public static Dictionary<string, string> SpriteSkin_record;
        public static Dictionary<string, List<string>> SpriteSkins_records;

        public static Dictionary<string, string> PortraitsSkin_record;
        public static Dictionary<string, List<string>> PortraitsSkins_records;

        public static Dictionary<string, string> OtherSkin_record;
        public static Dictionary<string, List<string>> OtherSkins_records;

        private static ILHook TextboxRunRoutineHook;
        private static ILHook TempleFallCoroutineHook;

        private static Hook Jungle_Hook_Update;
        private static Hook Jungle_Hook_Update_x2;

        public SkinModHelperModule() {
            Instance = this;
            UI = new SkinModHelperUI();

            skinConfigs = new Dictionary<string, SkinModHelperConfig>();
            OtherskinConfigs = new Dictionary<string, SkinModHelperConfig>();

            SpriteSkin_record = new Dictionary<string, string>();
            SpriteSkins_records = new Dictionary<string, List<string>>()
                ;
            PortraitsSkin_record = new Dictionary<string, string>();
            PortraitsSkins_records = new Dictionary<string, List<string>>();

            OtherSkin_record = new Dictionary<string, string>();
            OtherSkins_records = new Dictionary<string, List<string>>();

            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "JungleHelper", Version = new Version(1, 0, 8) })) {
                JungleHelperInstalled = true;
            }
            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "ExtendedVariantMode", Version = new Version(0, 22, 21) })) {
                ExtendedInstalled = true;
            }
            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "SaveFilePortraits", Version = new Version(1, 0, 0) })) {
                SaveFilePortraits = true;
            }
        }
        bool JungleHelperInstalled = false;
        bool ExtendedInstalled = false;
        bool SaveFilePortraits = false;

        public override void Load() {
            SkinModHelperInterop.Load();

            Everest.Content.OnUpdate += EverestContentUpdateHook;
            On.Celeste.LevelLoader.ctor += on_LevelLoader_ctor;

            On.Celeste.Player.UpdateHair += PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor += PlayerGetTrailColorHook;

            IL.Celeste.Player.UpdateHair += patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate += patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor += patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame += patch_SpriteMode_Silhouette;

            On.Celeste.Player.Render += PlayerRenderHook_x2;
            On.Celeste.PlayerSprite.ctor += on_PlayerSprite_ctor;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOn;

            On.Celeste.Lookout.Interact += on_Lookout_Interact;
            IL.Celeste.Player.Render += PlayerRenderIlHook;

            On.Celeste.Player.Render += PlayerRenderHook;
            On.Celeste.PlayerDeadBody.Render += PlayerDeadBodyRenderHook;
            On.Celeste.PlayerSprite.Render += OnPlayerSpriteRender;
            
            IL.Celeste.Player.Render += PlayerRenderIlHook_LoopReLoad;
            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;

            On.Monocle.SpriteBank.Create += SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOnHook;
            //On.Monocle.Atlas.GetAtlasSubtextures += GetAtlasSubtexturesHook;

            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread += GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup += OuiFileSelectSlotSetupHook;

            IL.Celeste.DeathEffect.Draw += DeathEffectDrawHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += DreamBlockHook;

            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool += FlyFeatherHook;
            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            
            TextboxRunRoutineHook = new ILHook(
                typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                SwapTextboxHook);
            TempleFallCoroutineHook = new ILHook(
                typeof(Player).GetMethod("TempleFallCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                TempleFallCoroutineILHook);

            if (JungleHelperInstalled) {
                JungleHelperInstalled_Hook();
            }
        }


        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);
            ReloadSettings();
        }

        public override void Unload() {
            Everest.Content.OnUpdate -= EverestContentUpdateHook;

            On.Celeste.Player.UpdateHair -= PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor -= PlayerGetTrailColorHook;

            IL.Celeste.Player.UpdateHair -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor -= patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame -= patch_SpriteMode_Silhouette;

            On.Celeste.Player.Render -= PlayerRenderHook_x2;
            On.Celeste.PlayerSprite.ctor -= on_PlayerSprite_ctor;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOn;

            On.Celeste.Lookout.Interact -= on_Lookout_Interact;
            IL.Celeste.Player.Render -= PlayerRenderIlHook;

            On.Celeste.Player.Render -= PlayerRenderHook;
            On.Celeste.PlayerDeadBody.Render -= PlayerDeadBodyRenderHook;
            On.Celeste.PlayerSprite.Render -= OnPlayerSpriteRender;

            IL.Celeste.Player.Render -= PlayerRenderIlHook_LoopReLoad;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;

            On.Monocle.SpriteBank.Create -= SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOnHook;
            //On.Monocle.Atlas.GetAtlasSubtextures -= GetAtlasSubtexturesHook;

            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread -= GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup -= OuiFileSelectSlotSetupHook;
            
            IL.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool -= DreamBlockHook;
            
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool -= FlyFeatherHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            TextboxRunRoutineHook.Dispose();
            TempleFallCoroutineHook.Dispose();

            Jungle_Hook_Update.Dispose();
            Jungle_Hook_Update_x2.Dispose();
        }
        private static void JungleHelperInstalled_Hook() {
            Jungle_Hook_Update = new Hook(typeof(JungleHelperModule).Assembly.GetType("Celeste.Mod.JungleHelper.Entities.EnforceSkinController").GetMethod("ChangePlayerSpriteMode", BindingFlags.Public | BindingFlags.Static),
                                                 typeof(SkinModHelperModule).GetMethod("ChangePlayerSpriteMode", BindingFlags.Public | BindingFlags.Static));

            Jungle_Hook_Update_x2 = new Hook(typeof(JungleHelperModule).Assembly.GetType("Celeste.Mod.JungleHelper.Entities.EnforceSkinController").GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static),
                                                 typeof(SkinModHelperModule).GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static));
        }

        public void SpecificSprite_LoopReload() {
            string skinId = XmlCombineValue();
            if (Xmls_record != skinId) {
                Xmls_record = skinId;

                UpdateParticles();

                bool Selected = false;
                foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                    Selected = Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName];

                    if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                        string spritesXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Sprites.xml";
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, config.SkinName, spritesXmlPath, Selected);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, config.SkinName, portraitsXmlPath, Selected);
                    }
                }
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    Selected = Player_Skinid_verify == config.hashValues[1];

                    if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {
                        string spritesXmlPath = "Graphics/" + config.OtherSprite_Path + "/Sprites.xml";
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_Path + "/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, $"{config.hashValues[1]}", spritesXmlPath, Selected);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, $"{config.hashValues[1]}", portraitsXmlPath, Selected);
                    }
                }
            }
        }





        private void on_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
            orig(self, session, startPosition);
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.Character_ID)) {
                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Load {config.Character_ID}'s Metadata");
                    //Metadata can is null, but Character_ID can't non-existent , 
                    PlayerSprite.CreateFramesMetadata(config.Character_ID);
                }
            }
        }

        public static bool playback = false;
        private static void ExtendedVariant_Silhouette() {
            //playback = ExtendedVariantsModule.Settings.MadelineIsSilhouette;
        }

        public static int Player_Skinid_verify;
        public static bool backpackOn = true;
        private void on_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode) {
            Level level = Engine.Scene as Level ?? (Engine.Scene as LevelLoader)?.Level;

            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self);
            bool isGhost = mode < 0;

            if (!isGhost && level != null) {
                backpackOn = Settings.Backpack == SkinModHelperSettings.BackpackMode.On ||
                    (Settings.Backpack == SkinModHelperSettings.BackpackMode.Default && level.Session.Inventory.Backpack);
            }


            if (!isGhost && (mode == PlayerSpriteMode.Madeline || mode == PlayerSpriteMode.MadelineNoBackpack || mode == PlayerSpriteMode.MadelineAsBadeline) && UniqueSkinSelected()) {
                mode = (PlayerSpriteMode)skinConfigs[Settings.SelectedPlayerSkin].hashValues[1];
                if (!backpackOn && UniqueSkinSelected("_NB")) {
                    mode = (PlayerSpriteMode)skinConfigs[Settings.SelectedPlayerSkin + "_NB"].hashValues[1];
                }
            } else if (!isGhost && mode == PlayerSpriteMode.Playback && UniqueSilhouetteSelected()) {
                mode = (PlayerSpriteMode)skinConfigs[Settings.SelectedSilhouetteSkin].hashValues[1];
                if (!backpackOn && UniqueSilhouetteSelected("_NB")) {
                    mode = (PlayerSpriteMode)skinConfigs[Settings.SelectedSilhouetteSkin + "_NB"].hashValues[1];
                }
            } else if (isGhost) {
                selfData["isGhost"] = true;
            }


            orig(self, mode);
            Logger.Log(LogLevel.Info, "SkinModHelper", $"PlayerModeValue: {mode}");


            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                bool search_out = false;
                foreach (int hash_search in config.hashValues) {
                    if (mode == (PlayerSpriteMode)hash_search || mode == (PlayerSpriteMode)(1 << 31) + hash_search) {
                        mode = (PlayerSpriteMode)config.hashValues[1];
                        break;
                    }
                }
                if (search_out) { break; }
            }
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (mode == (PlayerSpriteMode)config.hashValues[1]) {
                    string id = config.Character_ID;
                    selfData["spriteName"] = id;
                    GFX.SpriteBank.CreateOn(self, id);
                }
            }
            if (isGhost && selfData.Get<string>("spriteName") == "") {
                Logger.Log(LogLevel.Info, "SkinModHelper", $"someone else in CelesteNet uses a Skin-Mod that you don't have");
                string id = "player";
                if (!level.Session.Inventory.Backpack) {
                    id = "player_no_backpack";
                    selfData["spriteName"] = id;
                } else {
                    id = "player";
                    selfData["spriteName"] = id;
                }
                GFX.SpriteBank.CreateOn(self, id);
                return;
            }

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (config.JungleLanternMode == true && (mode == (PlayerSpriteMode)config.hashValues[1])) {

                    // replay the "idle" sprite to make it apply immediately.
                    self.Play("idle", restart: true);

                    // when the look up animation finishes, rewind it to frame 7: this way we are getting 7-11 playing in a loop.
                    self.OnFinish = anim => {
                        if (anim == "lookUp") {
                            self.Play("lookUp", restart: true);
                            self.SetAnimationFrame(5);
                        }
                    };
                }
            }
        }


        private static Sprite SpriteBankCreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            // Prevent mode's non-vanilla value causing the game Error
            if (sprite is PlayerSprite && id == "") {
                return null;
            }
            return orig(self, sprite, id);
        }


        private void patch_SpriteMode_Badeline(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate<Func<PlayerSpriteMode, PlayerSpriteMode>>(orig => {

                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (config.BadelineMode == true) {
                            foreach (int hash_search in config.hashValues) {
                                if (orig == (PlayerSpriteMode)hash_search) {
                                    return (PlayerSpriteMode)3;
                                }
                            }
                        }
                    }
                    return orig;
                });
            }
        }
        private void patch_SpriteMode_Silhouette(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate<Func<PlayerSpriteMode, PlayerSpriteMode>>(orig => {

                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (config.SilhouetteMode == true) {
                            foreach (int hash_search in config.hashValues) {
                                if (orig == (PlayerSpriteMode)hash_search) {
                                    return (PlayerSpriteMode)4;
                                }
                            }
                        }
                    }
                    return orig;
                });
            }
        }

        private static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player) {
            orig(self, player);
            if (Player_Skinid_verify != 0) {
                DynData<Lookout> selfData = new DynData<Lookout>(self);
                if (selfData.Get<string>("animPrefix") == "badeline_" || selfData.Get<string>("animPrefix") == "nobackpack_") {
                    selfData["animPrefix"] = "";
                }
            }
            return;
        }

        private void TempleFallCoroutineILHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("idle"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {
                    if (Player_Skinid_verify != 0) {
                        return "fallPose";
                    }
                    return orig;
                });
            }
        }

        private void PlayerRenderHook_x2(On.Celeste.Player.orig_Render orig, Player self) {
            bool search_out = false;
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                foreach (int hash_search in config.hashValues) {
                    if (self.Sprite.Mode == (PlayerSpriteMode)hash_search) {
                        Player_Skinid_verify = config.hashValues[1];
                        search_out = true;
                    }
                }
                if (search_out) { break; }
            }
            if (!search_out) { Player_Skinid_verify = 0; }
            SpecificSprite_LoopReload();
            orig(self);
        }



        public enum VariantCategory {
            SkinFreeConfig, None
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            //UI.CreateMenu(menu, inGame);
            if (inGame) {
                new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: false, includeRandomizer: false, null, menu, inGame, forceEnabled: false);
                return;
            }
            new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: true, includeRandomizer: true, delegate
            {
                OuiModOptions.Instance.Overworld.Goto<OuiModOptions>();
            }, menu, inGame, forceEnabled: false);
        }
        private void EverestContentUpdateHook(ModAsset oldAsset, ModAsset newAsset) {
            if (newAsset != null && newAsset.PathVirtual.StartsWith("SkinModHelperConfig")) {
                ReloadSettings();
            }
        }

        public void ReloadSettings() {
            skinConfigs.Clear();
            OtherskinConfigs.Clear();

            Instance.LoadSettings();

            foreach (ModContent mod in Everest.Content.Mods) {
                List<SkinModHelperConfig> configs;
                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml)) {
                    configs = LoadConfigFile(configAsset);

                    foreach (SkinModHelperConfig config in configs) {
                        Regex skinIdRegex = new(@"^[a-zA-Z0-9]+_[a-zA-Z0-9]+$");

                        if (string.IsNullOrEmpty(config.SkinName)) {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"Invalid skin name {config.SkinName}, will not register.");
                        }
                        if (OtherskinConfigs.ContainsKey(config.SkinName) || skinConfigs.ContainsKey(config.SkinName)) {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"Duplicate skin name {config.SkinName}, unregister the second {config.SkinName}");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                            Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new non-player skin: {config.SkinName}");
                            OtherskinConfigs.Add(config.SkinName, config);
                        }

                        if (!string.IsNullOrEmpty(config.Character_ID)) {

                            // Default colors taken from vanilla
                            config.GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1]) {
                                [0] = Calc.HexToColor("44B7FF"),
                                [1] = config.BadelineMode ? Calc.HexToColor("9B3FB5") : Calc.HexToColor("AC3232"),
                                [2] = Calc.HexToColor("FF6DEF")
                            };


                            List<bool> changed = new(new bool[MAX_DASHES + 1]);

                            if (config.HairColors != null) {
                                foreach (SkinModHelperConfig.HairColor hairColor in config.HairColors) {
                                    Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");
                                    if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColorRegex.IsMatch(hairColor.Color)) {
                                        config.GeneratedHairColors[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                                        changed[hairColor.Dashes] = true;
                                    } else {
                                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"Invalid hair color or dash count values provided for {config.SkinName}");
                                    }
                                }
                            }
                            // Fill upper dash range with the last customized dash color
                            for (int i = 3; i <= MAX_DASHES; i++) {
                                if (!changed[i]) {
                                    config.GeneratedHairColors[i] = config.GeneratedHairColors[i - 1];
                                }
                            }


                            if (string.IsNullOrEmpty(config.hashSeed)) {
                                config.hashSeed = config.SkinName;
                            }
                            int hashValue = getHash(config.hashSeed);
                            config.hashValues = new(new int[MAX_DASHES + 1]) {
                                [0] = hashValue,
                                [1] = hashValue + 1,
                                [2] = hashValue + 2,
                                [3] = hashValue + 3,
                                [4] = hashValue + 4,
                                [5] = hashValue + 5
                            };

                            

                            if (config.JungleLanternMode) {
                                if (config.Silhouette_List || config.Player_List) {
                                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"{config.SkinName} already set 'JungleLanternMode' to true, ignore it's Player_List or Silhouette_List of setting");
                                }
                                config.Silhouette_List = false;
                                config.Player_List = false;
                            }

                            if (!spritesWithHair.Contains(config.Character_ID)) {
                                spritesWithHair.Add(config.Character_ID);
                            }

                            Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new player skin: {config.SkinName} and {config.hashValues[1]}");
                            skinConfigs.Add(config.SkinName, config);
                        }
                    }
                }
            }

            skinConfigs_MoreDefaultValue();
            RecordSpriteBanks_Start();

            if (Settings.SelectedPlayerSkin == null || !skinConfigs.ContainsKey(Settings.SelectedPlayerSkin)) {
                Settings.SelectedPlayerSkin = DEFAULT;
            }
            if (Settings.SelectedSilhouetteSkin == null || !skinConfigs.ContainsKey(Settings.SelectedSilhouetteSkin)) {
                Settings.SelectedSilhouetteSkin = DEFAULT;
            }
        }
        private static int getHash(string hash_send) {
            int hashValue = hash_send.GetHashCode() >> 4;
            if (hashValue < 0) {
                hashValue += (1 << 31);
            }
            return hashValue;
        }





        private static List<SkinModHelperConfig> LoadConfigFile(ModAsset skinConfigYaml) {
            return skinConfigYaml.Deserialize<List<SkinModHelperConfig>>();
        }




        // ---Custom ColorGrade---
        private void PlayerRenderHook(On.Celeste.Player.orig_Render orig, Player self) {
            int dashCount = self.Dashes < 0 ? 0 : Math.Min(self.Dashes, MAX_DASHES);
            bool MaxDashZero = self.MaxDashes <= 0;

            if (!MaxDashZero) {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    foreach (int hash_search in config.hashValues) {
                        if ((self.Sprite.Mode == (PlayerSpriteMode)hash_search) && config.colorGrade_Path != null) {
                            while (dashCount > 2 && !GFX.ColorGrades.Has(config.colorGrade_Path + "/dash" + dashCount)) {
                                dashCount--;
                            }
                            new DynData<PlayerSprite>(self.Sprite)["DashCount"] = dashCount;
                        }
                    }
                }
            } else {
                new DynData<PlayerSprite>(self.Sprite)["DashCount"] = 1;
            }
            orig(self);
        }

        private void PlayerDeadBodyRenderHook(On.Celeste.PlayerDeadBody.orig_Render orig, PlayerDeadBody self) {
            PlayerSprite sprite = new DynData<PlayerDeadBody>(self).Get<PlayerSprite>("sprite");
            //new DynData<PlayerSprite>(sprite)["DashCount"] = 1;
            orig(self);
        }


        private void OnPlayerSpriteRender(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            orig(self);
            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self);
            if ((selfData.Get<int?>("DashCount") == null) && (selfData.Get<bool?>("isGhost") != true) && (selfData.Get<bool?>("isSilhouette") != true)) {
                //bug-mark if: "Vanilla Chapter-9's internet cafe"s wavedash.ppt Not be filtered
                //then: it will have some UI bug
                return;
            }
            int dashCount = 1;
            if (selfData.Get<int?>("DashCount") != null) {
                dashCount = (int)selfData.Get<int?>("DashCount");

                if (self.Entity is Player && selfData.Get<bool?>("ColorGrade_Jump_Blocking") != true) {
                    Player player = Engine.Scene?.Tracker.GetEntity<Player>();

                    if (player != null && player.StateMachine.State != Player.StStarFly && player.StateMachine.State != Player.StIntroWakeUp) {
                        foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                            foreach (int hash_search in config.hashValues) {
                                if (player.Sprite.Mode == (PlayerSpriteMode)hash_search && player.Sprite.Mode != (PlayerSpriteMode)config.hashValues[dashCount]) {
                                    selfData["ColorGrade_Jump_Blocking"] = true;
                                    RefreshPlayerSpriteMode(config.SkinName, dashCount);
                                }
                            }
                        }
                    }
                }
            }


            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                int search_time = 0;
                foreach (int hash_search in config.hashValues) {
                    if (self.Mode == (PlayerSpriteMode)hash_search && config.colorGrade_Path != null) {

                        if (selfData.Get<int?>("DashCount") != null) {
                            search_time = dashCount;
                        }

                        string colorGrade_Path = config.colorGrade_Path + "/dash" + search_time;

                        if (GFX.ColorGrades.Has(colorGrade_Path)) {
                            Effect colorGradeEffect = GFX.FxColorGrading;
                            colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                            Engine.Graphics.GraphicsDevice.Textures[1] = GFX.ColorGrades[colorGrade_Path].Texture.Texture_Safe;
                            Scene scene = self.Scene ?? Engine.Scene;

                            //bug-mark if: the Silhouette (or CelesteNet's Other?) enabled New ColorGrade, and spawns Dash-Trail with the Player at the same time
                            //then: the player's Dash-Trail will be lost
                            Draw.SpriteBatch.End();
                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, (scene as Level).GameplayRenderer.Camera.Matrix);
                            orig(self);
                            Draw.SpriteBatch.End();
                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, (scene as Level).GameplayRenderer.Camera.Matrix);
                            return;
                        }
                    }
                    search_time++;
                }
            }
        }


        // ---Custom Dash Color---
        private void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
            orig(self, applyGravity);

            int dashCount = self.Dashes < 0 ? 0 : Math.Min(self.Dashes, MAX_DASHES);
            bool MaxDashZero = self.MaxDashes <= 0;

            if (self.StateMachine.State != Player.StStarFly) {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    foreach (int hash_search in config.hashValues) {
                        if (self.Sprite.Mode == (PlayerSpriteMode)hash_search && (config.colorGrade_Path != null || (config.HairColors != null && self.Hair.Color != Player.FlashHairColor))) {
                            if (MaxDashZero) {
                                self.Hair.Color = config.GeneratedHairColors[1];
                            } else {
                                self.Hair.Color = config.GeneratedHairColors[dashCount];
                            }
                            return;
                        }
                    }
                }
            }
        }
        private Color PlayerGetTrailColorHook(On.Celeste.Player.orig_GetTrailColor orig, Player self, bool wasDashB) {
            int dashCount = self.Dashes < 0 ? 0 : Math.Min(self.Dashes, MAX_DASHES);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                foreach (int hash_search in config.hashValues) {
                    if (self.Sprite.Mode == (PlayerSpriteMode)hash_search && (config.colorGrade_Path != null || config.HairColors != null)) {
                        return config.GeneratedHairColors[dashCount];
                    }
                }
            }
            return orig(self, wasDashB);
        }


        private void PlayerRenderIlHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump to the usage of the Red color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_Red"))) {
                Logger.Log("SkinModHelper", $"Patching silhouette hair color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((color, player) => {

                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (config.SilhouetteMode == true) {
                            foreach (int hash_search in config.hashValues) {
                                if (player.Sprite.Mode == (PlayerSpriteMode)hash_search) {
                                    if (player.Dashes == 0) {
                                        color = Calc.HexToColor("348DC1");
                                    }
                                    color = player.Hair.Color;
                                    return color;
                                }
                            }
                        }
                    }
                    return color;
                });
            }

            // jump to the usage of the White-color / Null-color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
                Logger.Log("SkinModHelper", $"Patching silhouette color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((orig, self) => {

                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (config.SilhouetteMode == true) {
                            foreach (int hash_search in config.hashValues) {
                                if (self.Sprite.Mode == (PlayerSpriteMode)hash_search) {
                                    return self.Hair.Color;
                                }
                            }
                        }
                    }
                    return orig;
                });
            }
        }



        // ---Specific Player Sprite---
        private void PlayerRenderIlHook_LoopReLoad(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/startStarFlyWhite"))) {
                Logger.Log("SkinModHelper", $"Changing startStarFlyWhite path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string CustomPath = GetReskinPath(GFX.Game, "startStarFlyWhite", true, false, false, Player_Skinid_verify, true);
                    if (CustomPath != "startStarFlyWhite") {
                        return CustomPath;
                    }
                    return orig;
                });
            }
        }

        private MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index) {
            DynData<PlayerSprite> spriteData = new DynData<PlayerSprite>(self.Sprite);

            string spriteID = (string)spriteData["spriteName"];
            string spriteID_orig = spriteID;
            if (SpriteSkin_record.ContainsKey(spriteID)) {
                spriteID = spriteID + SpriteSkin_record[spriteID];
            }

            if (spritesWithHair.Contains((string)spriteData["spriteName"]) && GFX.SpriteBank.SpriteData.ContainsKey(spriteID)) {

                string SourcesPath = null;
                foreach (SpriteDataSource DataSources in GFX.SpriteBank.SpriteData[spriteID].Sources) {
                    SourcesPath = DataSources.Path;
                }

               
                if (index == 0) {
                    string bangs = "bangs";
                    if (!SpriteSkin_record.ContainsKey((string)spriteData["spriteName"]) || SpriteSkin_record[(string)spriteData["spriteName"]] == null) {
                        bangs = GetReskinPath(GFX.Game, "bangs", true, false, false, (int)self.Sprite.Mode, true);
                    }

                    if (bangs == "bangs") {
                        bangs = SourcesPath + "bangs";
                    }
                    string number = "";
                    while (number != "00" && !GFX.Game.Has(bangs + number)) {
                        number = number + "0";
                    }
                    if (GFX.Game.Has(bangs + number)) {
                        List<MTexture> newbangs = GFX.Game.GetAtlasSubtextures(bangs);
                        return newbangs.Count > self.Sprite.HairFrame ? newbangs[self.Sprite.HairFrame] : newbangs[0];
                    }
                } else {
                    string newhair = "hair00";
                    if (!SpriteSkin_record.ContainsKey((string)spriteData["spriteName"]) || SpriteSkin_record[(string)spriteData["spriteName"]] == null) {
                        newhair = GetReskinPath(GFX.Game, "hair00", true, false, false, (int)self.Sprite.Mode);
                    }

                    if (newhair == "hair00") {
                        newhair = SourcesPath + "hair00";
                    }
                    if (GFX.Game.Has(newhair)) {
                        return GFX.Game[newhair];
                    }
                }
            }
            return orig(self, index);
        }

        private void DeathEffectDrawHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/hair00"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string SpriteID = "SP_death_particle";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                        string SkinId = getOtherSkin_ReskinPath(GFX.Game, "death_particle", SpriteID, OtherSkin_record[SpriteID]);

                        return SkinId == "death_particle" ? orig : SkinId;
                    }
                    return orig;
                });
            }
        }








        //---Other Sprite---
        private static List<MTexture> GetAtlasSubtexturesHook(On.Monocle.Atlas.orig_GetAtlasSubtextures orig, Atlas self, string path) {

            string new_path = null;
            bool number_search = false;
            if (path == "marker/runNoBackpack" || path == "marker/Fall" || path == "marker/runBackpack") {
                new_path = path;
                number_search = true;
            }

            if (new_path != null) {
                path = GetReskinPath(self, new_path, false, true, false, Player_Skinid_verify, number_search);
            }
            return orig(self, path);
        }


        private Sprite SpriteBankCreateOnHook(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            string newId = id;
            if (self == GFX.SpriteBank) {
                if (SpriteSkin_record.ContainsKey(id)) {
                    newId = id + SpriteSkin_record[id];
                }
            }

            if (self.SpriteData.ContainsKey(newId)) {
                id = newId;
            }
            return orig(self, sprite, id);
        }

        private Sprite SpriteBankCreateHook(On.Monocle.SpriteBank.orig_Create orig, SpriteBank self, string id) {
            string newId = id;
            if (self == GFX.SpriteBank) {
                if (SpriteSkin_record.ContainsKey(id)) {
                    newId = id + SpriteSkin_record[id];
                }
            } else if (self == GFX.PortraitsSpriteBank) {
                if (PortraitsSkin_record.ContainsKey(id)) {
                    newId = id + PortraitsSkin_record[id];
                }
            }

            if (self.SpriteData.ContainsKey(newId)) {
                id = newId;
            }
            return orig(self, id);
        }

        private static FancyText.Portrait ReplacePortraitPath(FancyText.Portrait portrait) {

            string skinModPortraitSpriteId = portrait.SpriteId;

            foreach (string SpriteId in PortraitsSkin_record.Keys) {
                //Ignore case of string
                if (string.Compare(SpriteId, skinModPortraitSpriteId, true) == 0) {
                    skinModPortraitSpriteId = SpriteId + PortraitsSkin_record[SpriteId];
                }
            }

            if (GFX.PortraitsSpriteBank.Has(skinModPortraitSpriteId)) {
                portrait.Sprite = skinModPortraitSpriteId.Replace("portrait_", "");
            }
            return portrait;
        }




        private void GameLoaderLoadThreadHook(On.Celeste.GameLoader.orig_LoadThread orig, GameLoader self) {
            orig(self);
            skinConfigs_MoreDefaultValue();
            RecordSpriteBanks_Start();


            if (UniqueSkinSelected()) {
                Player_Skinid_verify = skinConfigs[Settings.SelectedPlayerSkin].hashValues[1];
            }
        }


        // Wait until the main sprite bank is created, then combine with our skin mod banks
        private void LevelLoaderLoadingThreadHook(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {

            //at this Hooking time, The level data has not established, cannot get Default backpack state of Level 
            if (Settings.Backpack != SkinModHelperSettings.BackpackMode.Default) {
                backpackOn = Settings.Backpack != SkinModHelperSettings.BackpackMode.Off;
            }

            if (UniqueSkinSelected()) {
                Player_Skinid_verify = skinConfigs[Settings.SelectedPlayerSkin].hashValues[1];
                if (!backpackOn && UniqueSkinSelected("_NB")) {
                    Player_Skinid_verify = skinConfigs[Settings.SelectedPlayerSkin + "_NB"].hashValues[1];
                }
            }

            Xmls_record = null;
            SpecificSprite_LoopReload();
            orig(self);
        }

        private void OuiFileSelectSlotSetupHook(On.Celeste.OuiFileSelectSlot.orig_Setup orig, OuiFileSelectSlot self) {
            if (self.FileSlot == 0) {
                Xmls_record = null;
                SpecificSprite_LoopReload();

                foreach (string SpriteID in SpriteSkins_records.Keys) {
                    SpriteSkin_record[SpriteID] = null;
                }
                foreach (string SpriteID in PortraitsSkins_records.Keys) {
                    PortraitsSkin_record[SpriteID] = null;
                }

                //Reload the SpriteID registration code of "SaveFilePortraits"
                if (SaveFilePortraits) {
                    Logger.Log("SkinModHelper", $"SaveFilePortraits reload start");
                    SaveFilePortraits_Reload();
                }
            }
            orig(self);
        }


        private static string XmlCombineValue() {
            int sort = 0;
            string identifier = "";
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                if (Settings.ExtraXmlList.ContainsKey(config.SkinName)) {
                    if (sort == 0) {
                        identifier = (sort + "_" + config.SkinName);
                        sort += 1;
                    } else {
                        identifier = (identifier + ", " + sort + "_" + config.SkinName);
                        sort += 1;
                    }
                }
            }
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (sort == 0) {
                    identifier = (sort + "_" + Player_Skinid_verify);
                    sort += 1;
                } else {
                    identifier = (identifier + ", " + sort + "_" + Player_Skinid_verify);
                    sort += 1;
                }
            }

            if (identifier != "") {
                //Logger.Log(LogLevel.Verbose, "SkinModHelper", $"SpriteBank identifier: {identifier}");
                return "_" + identifier;
            }
            return null;
        }



        private static void UpdateParticles() {
            FlyFeather.P_Collect.Source = GFX.Game["particles/feather"];
            FlyFeather.P_Boost.Source = GFX.Game["particles/feather"];

            string CustomPath = "particles/feather";

            string SpriteID = "feather_particles";
            if (OtherSkins_records.ContainsKey(SpriteID)) {
                Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                CustomPath = getOtherSkin_ReskinPath(GFX.Game, "particles/feather", SpriteID, OtherSkin_record[SpriteID]);
            }

            if (CustomPath != null) {
                FlyFeather.P_Collect.Source = GFX.Game[CustomPath];
                FlyFeather.P_Boost.Source = GFX.Game[CustomPath];
            }
        }


        private void DreamBlockHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/dreamblock/particles"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string SpriteID = "dreamblock_particles";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/dreamblock/particles", SpriteID, OtherSkin_record[SpriteID]);
                    }
                    return "objects/dreamblock/particles";
                });
            }
        }

        private void FlyFeatherHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/flyFeather/outline"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    UpdateParticles();
                    string SpriteID = "feather_outline";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/flyFeather/outline", SpriteID, OtherSkin_record[SpriteID]);
                    }
                    return "objects/flyFeather/outline";
                });
            }
        }



        private static string GetReskinPath(Atlas atlas, string orig, bool S_Path, bool N_Path, bool Ex_Path, int mode, bool number_search = false) {
            string number = "";
            string CustomPath = null;
            if (mode != 0) {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    foreach (int hash_search in config.hashValues) {
                        if (mode == hash_search) {
                            if (S_Path && !string.IsNullOrEmpty(config.SpecificPlayerSprite_Path)) {
                                CustomPath = config.SpecificPlayerSprite_Path + "/" + orig;
                            }

                            if (N_Path && !string.IsNullOrEmpty(config.OtherSprite_Path)) {
                                CustomPath = config.OtherSprite_Path + "/" + orig;
                            }

                            if (CustomPath != null) {
                                while (number_search && number != "00000" && !atlas.Has(CustomPath + number)) {
                                    number = number + "0";
                                }
                                if (atlas.Has(CustomPath + number)) {
                                    return CustomPath;
                                }
                            }
                        }
                    }
                }
            }

            if (Ex_Path) {
                CustomPath = null;
                foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                    if (Settings.ExtraXmlList.ContainsKey(config.SkinName)) {
                        if (Settings.ExtraXmlList[config.SkinName] && !string.IsNullOrEmpty(config.OtherSprite_ExPath)) {

                            number = "";
                            while (number_search && number != "00000" && !atlas.Has(config.OtherSprite_ExPath + "/" + orig + number)) {
                                number = number + "0";
                            }
                            if (atlas.Has(config.OtherSprite_ExPath + "/" + orig + number)) {
                                CustomPath = config.OtherSprite_ExPath + "/" + orig;
                            }
                        }
                    }
                }

                if (CustomPath != null) {
                    return atlas.Has(CustomPath + number) ? CustomPath : orig;
                }
            }
            return orig;
        }


        private void SwapTextboxHook(ILContext il) {
            ILCursor cursor = new(il);
            // Move to the last occurence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) {
            }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true) {
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((orig) => {
                    return ReplacePortraitPath(orig);
                });
            }
        }

        // This one requires double hook - for some reason they implemented a tiny version of the Textbox class that behaves differently
        private void CampfireQuestionHook(ILContext il) {
            ILCursor cursor = new(il);
            // Move to the last occurrence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) {
            }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true) {
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((orig) => {
                    return ReplacePortraitPath(orig);
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("_ask"),
                instr => instr.MatchCall(out MethodReference method) && method.Name == "Concat")) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {
                    return ReplaceTextboxPath(orig);
                });
            }
        }

        // ReplacePortraitPath makes textbox path funky, so correct to our real path or revert to vanilla if it does not exist
        private static string ReplaceTextboxPath(string textboxPath) {
            string originalPortraitId = textboxPath.Split('_')[0].Replace("textbox/", ""); // "textbox/[orig portrait id]_[skin id]_ask"

            string CustomPath = null;
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                if (Settings.ExtraXmlList.ContainsKey(config.SkinName)) {
                    if (Settings.ExtraXmlList[config.SkinName] && !string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                        if (GFX.Portraits.Has("textbox/" + config.OtherSprite_ExPath + "/" + originalPortraitId + "_ask")) {
                            CustomPath = config.OtherSprite_ExPath;
                        }
                    }
                }
            }
            if (Player_Skinid_verify != 0) {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    if (Player_Skinid_verify == config.hashValues[1] && !string.IsNullOrEmpty(config.OtherSprite_Path)) {
                        if (GFX.Portraits.Has("textbox/" + config.OtherSprite_Path + "/" + originalPortraitId + "_ask")) {
                            CustomPath = config.OtherSprite_Path;
                        }
                    }
                }
            }

            string newTextboxPath = "textbox/" + CustomPath + "/" + originalPortraitId + "_ask";
            textboxPath = (CustomPath != null && GFX.Portraits.Has(newTextboxPath)) ? newTextboxPath : "textbox/" + originalPortraitId + "_ask";
            return textboxPath;
        }


        // Combine skin mod XML with a vanilla sprite bank
        private void CombineSpriteBanks(SpriteBank origBank, string skinId, string xmlPath, bool Selected) {
            SpriteBank newBank = BuildBank(origBank, skinId, xmlPath);
            if (newBank == null) {
                return;
            }

            // For each overridden sprite, patch it and add it to the original bank with a unique identifier
            foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData) {
                string spriteId = spriteDataEntry.Key;
                SpriteData newSpriteData = spriteDataEntry.Value;

                if (origBank.SpriteData.TryGetValue(spriteId, out SpriteData origSpriteData)) {
                    PatchSprite(origSpriteData.Sprite, newSpriteData.Sprite);

                    string newSpriteId = spriteId + skinId;
                    origBank.SpriteData[newSpriteId] = newSpriteData;

                    if (origBank == GFX.SpriteBank && !string.IsNullOrEmpty(skinId)) {
                        if (Settings.FreeCollocations_OffOn) {
                            if (!Settings.FreeCollocations_Sprites.ContainsKey(spriteId) || Settings.FreeCollocations_Sprites[spriteId] == DEFAULT) {
                                if (Selected) {
                                    SpriteSkin_record[spriteId] = skinId;
                                }
                            } else if (Settings.FreeCollocations_Sprites[spriteId] == skinId) {
                                SpriteSkin_record[spriteId] = skinId;
                            } else {
                                SpriteSkin_record[spriteId] = null;
                            }
                        } else if (Selected) {
                            SpriteSkin_record[spriteId] = skinId;
                        }

                        if (spritesWithHair.Contains(spriteId)) {
                            PlayerSprite.CreateFramesMetadata(newSpriteId);
                        }
                    } else if (origBank == GFX.PortraitsSpriteBank && !string.IsNullOrEmpty(skinId)) {
                        if (Settings.FreeCollocations_OffOn) {
                            if (!Settings.FreeCollocations_Portraits.ContainsKey(spriteId) || Settings.FreeCollocations_Portraits[spriteId] == DEFAULT) {
                                if (Selected) {
                                    PortraitsSkin_record[spriteId] = skinId;
                                }
                            } else if (Settings.FreeCollocations_Portraits[spriteId] == skinId) {
                                PortraitsSkin_record[spriteId] = skinId;
                            } else {
                                PortraitsSkin_record[spriteId] = null;
                            }
                        } else if (Selected) {
                            PortraitsSkin_record[spriteId] = skinId;
                        }
                    }
                }
            }
        }

        private void RecordSpriteBanks_Start() {
            SpriteSkins_records.Clear();
            PortraitsSkins_records.Clear();
            OtherSkins_records.Clear();

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {
                    string spritesXmlPath = "Graphics/" + config.OtherSprite_Path + "/Sprites.xml";
                    string portraitsXmlPath = "Graphics/" + config.OtherSprite_Path + "/Portraits.xml";
                    RecordSpriteBanks(GFX.SpriteBank, DEFAULT, spritesXmlPath);
                    RecordSpriteBanks(GFX.PortraitsSpriteBank, DEFAULT, portraitsXmlPath);

                    if (GFX.Game.Has(config.SpecificPlayerSprite_Path + "/death_particle")) {
                        RecordSpriteBanks(null, DEFAULT, null, "SP_death_particle");
                    }
                    if (GFX.Game.Has(config.OtherSprite_Path + "/objects/dreamblock/particles")) {
                        RecordSpriteBanks(null, DEFAULT, null, "dreamblock_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_Path + "/particles/feather")) {
                        RecordSpriteBanks(null, DEFAULT, null, "feather_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_Path + "/objects/flyFeather/outline")) {
                        RecordSpriteBanks(null, DEFAULT, null, "feather_outline");
                    }
                }
            }

            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {

                    string spritesXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Sprites.xml";
                    string portraitsXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Portraits.xml";
                    RecordSpriteBanks(GFX.SpriteBank, config.SkinName, spritesXmlPath);
                    RecordSpriteBanks(GFX.PortraitsSpriteBank, config.SkinName, portraitsXmlPath);

                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/death_particle")) {
                        RecordSpriteBanks(null, config.SkinName, null, "SP_death_particle");
                    }
                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/objects/dreamblock/particles")) {
                        RecordSpriteBanks(null, config.SkinName, null, "dreamblock_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/particles/feather")) {
                        RecordSpriteBanks(null, config.SkinName, null, "feather_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/objects/flyFeather/outline")) {
                        RecordSpriteBanks(null, config.SkinName, null, "feather_outline");
                    }
                }
            }
        }
        private void RecordSpriteBanks(SpriteBank origBank, string skinId, string xmlPath, string otherSkin = null) {
            if (otherSkin == null) {
                SpriteBank newBank = BuildBank(origBank, skinId, xmlPath);
                if (newBank == null) {
                    return;
                }

                foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData) {
                    string spriteId = spriteDataEntry.Key;
                    if (!string.IsNullOrEmpty(skinId)) {
                        if (origBank == GFX.SpriteBank && origBank.SpriteData.ContainsKey(spriteId)) {

                            if (!SpriteSkins_records.ContainsKey(spriteId)) {
                                SpriteSkins_records.Add(spriteId, new());
                            }
                            if (skinId != DEFAULT && !SpriteSkins_records[spriteId].Contains(skinId)) {
                                SpriteSkins_records[spriteId].Add(skinId);
                            }
                        } else if (origBank == GFX.PortraitsSpriteBank && origBank.SpriteData.ContainsKey(spriteId)) {

                            if (!PortraitsSkins_records.ContainsKey(spriteId)) {
                                PortraitsSkins_records.Add(spriteId, new());
                            }
                            if (skinId != DEFAULT && !PortraitsSkins_records[spriteId].Contains(skinId)) {
                                PortraitsSkins_records[spriteId].Add(skinId);
                            }
                        }
                    }
                }
            } else {
                string spriteId = otherSkin;
                if (!OtherSkins_records.ContainsKey(spriteId)) {
                    OtherSkins_records.Add(spriteId, new());
                }
                if (skinId != DEFAULT && !OtherSkins_records[spriteId].Contains(skinId)) {
                    OtherSkins_records[spriteId].Add(skinId);
                }
            }
        }

        private void skinConfigs_MoreDefaultValue() {
            if (GFX.SpriteBank == null) {
                return;
            }
            Dictionary<string, string> SP_Path = new Dictionary<string, string>();
            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (string.IsNullOrEmpty(config.SpecificPlayerSprite_Path)) {

                    string SourcesPath = null;
                    if (GFX.SpriteBank.Has(config.Character_ID)) {
                        foreach (SpriteDataSource DataSources in GFX.SpriteBank.SpriteData[config.Character_ID].Sources) {
                            SourcesPath = DataSources.Path;
                        }
                        if (SourcesPath.EndsWith("/")) {
                            SourcesPath = SourcesPath.Remove(SourcesPath.LastIndexOf("/"), 1);
                        }

                        SP_Path[config.SkinName] = SourcesPath;
                    } else {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"{config.SkinName}'s '{config.Character_ID}' exist't in Graphics/Sprites.xml, this will make crash the game ");
                    }
                    if (SourcesPath == null) {
                        SP_Path[config.SkinName] = "characters/" + config.Character_ID;
                    }
                }
            }
            foreach (string SkinName in SP_Path.Keys) {
                skinConfigs[SkinName].SpecificPlayerSprite_Path = SP_Path[SkinName];
            }
        }





        private SpriteBank BuildBank(SpriteBank origBank, string skinId, string xmlPath) {
            try {
                SpriteBank newBank = new(origBank.Atlas, xmlPath);
                return newBank;
            } catch (Exception e) {
                return null;
            }
        }

        // Add any missing vanilla animations to an overridden sprite
        private void PatchSprite(Sprite origSprite, Sprite newSprite) {
            Dictionary<string, Sprite.Animation> newAnims = newSprite.GetAnimations();

            // Shallow copy... sometimes new animations get added mid-update?
            Dictionary<string, Sprite.Animation> oldAnims = new(origSprite.GetAnimations());
            foreach (KeyValuePair<string, Sprite.Animation> animEntry in oldAnims) {
                string origAnimId = animEntry.Key;
                Sprite.Animation origAnim = animEntry.Value;
                if (!newAnims.ContainsKey(origAnimId)) {
                    newAnims[origAnimId] = origAnim;
                }
            }
        }




        public static void RefreshPlayerSpriteMode(string SkinName = null, int dashCount = 1) {
            if (Engine.Scene is not Level) {
                return;
            }
            Player player = Engine.Scene.Tracker?.GetEntity<Player>();
            if (player == null) {
                return;
            }

            Player_Skinid_verify = 0;
            if (SkinName != null && skinConfigs.ContainsKey(SkinName)) {

                Player_Skinid_verify = skinConfigs[SkinName].hashValues[1];
                SetPlayerSpriteMode((PlayerSpriteMode)skinConfigs[SkinName].hashValues[dashCount]);

            } else if (SaveData.Instance != null && SaveData.Instance.Assists.PlayAsBadeline) {
                SetPlayerSpriteMode(PlayerSpriteMode.MadelineAsBadeline);
            } else {
                SetPlayerSpriteMode(null);
            }
        }





        // Trigger when we change the setting, store the new one. If in-level, redraw player sprite.
        public static void UpdateSkin(string newSkinId, bool inGame = false) {
            Settings.SelectedPlayerSkin = newSkinId;
            RefreshPlayerSpriteMode();

            if (!inGame) {
                Player_Skinid_verify = skinConfigs[Settings.SelectedPlayerSkin].hashValues[1];
            }
        }
        public static void UpdateSilhouetteSkin(string newSkinId) {
            Settings.SelectedSilhouetteSkin = newSkinId;
        }
        public static void UpdateExtraXml(string SkinId, bool OnOff) {
            Settings.ExtraXmlList[SkinId] = OnOff;
        }

        public static void Update_FreeCollocations_OnOff(bool OnOff, bool inGame) {
            Settings.FreeCollocations_OffOn = OnOff;

            foreach (string SpriteID in SpriteSkins_records.Keys) {
                Update_FreeCollocations_Sprites(SpriteID, null, inGame, true);
            }
            foreach (string SpriteID in PortraitsSkins_records.Keys) {
                Update_FreeCollocations_Portraits(SpriteID, null, inGame, true);
            }
            foreach (string SpriteID in OtherSkins_records.Keys) {
                Update_FreeCollocations_OtherExtra(SpriteID, null, inGame, true);
            }
        }

        public static void Update_FreeCollocations_Sprites(string SpriteID, string SkinId, bool inGame, bool OnOff = false) {
            if (!OnOff) {
                Settings.FreeCollocations_Sprites[SpriteID] = SkinId;
            }

            if (!Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !Settings.FreeCollocations_Sprites.ContainsKey(SpriteID) || Settings.FreeCollocations_Sprites[SpriteID] == DEFAULT) {
                SpriteSkin_record[SpriteID] = getSkinDefaultValues(GFX.SpriteBank, SpriteID);
            } else {
                SpriteSkin_record[SpriteID] = Settings.FreeCollocations_Sprites[SpriteID];
            }
        }

        public static void Update_FreeCollocations_Portraits(string SpriteID, string SkinId, bool inGame, bool OnOff = false) {
            if (!OnOff) {
                Settings.FreeCollocations_Portraits[SpriteID] = SkinId;
            }

            if (!Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !Settings.FreeCollocations_Portraits.ContainsKey(SpriteID) || Settings.FreeCollocations_Portraits[SpriteID] == DEFAULT) {
                PortraitsSkin_record[SpriteID] = getSkinDefaultValues(GFX.PortraitsSpriteBank, SpriteID);
            } else {
                PortraitsSkin_record[SpriteID] = Settings.FreeCollocations_Portraits[SpriteID];
            }
        }
        public static void Update_FreeCollocations_OtherExtra(string SpriteID, string SkinId, bool inGame, bool OnOff = false) {
            if (!OnOff) {
                Settings.FreeCollocations_OtherExtra[SpriteID] = SkinId;
            }

            if (!Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !Settings.FreeCollocations_OtherExtra.ContainsKey(SpriteID) || Settings.FreeCollocations_OtherExtra[SpriteID] == DEFAULT) {
                OtherSkin_record[SpriteID] = DEFAULT;
            } else {
                OtherSkin_record[SpriteID] = Settings.FreeCollocations_OtherExtra[SpriteID];
            }
        }









        public static string getSkinDefaultValues(SpriteBank selfBank, string SpriteID) {

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (Player_Skinid_verify == config.hashValues[1] && selfBank.Has(SpriteID + $"{config.hashValues[1]}")) {
                    return $"{config.hashValues[1]}";
                }
            }
            string SkinID = null;
            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if ((selfBank == GFX.SpriteBank && SpriteSkins_records[SpriteID].Contains(config.SkinName)) ||
                    (selfBank == GFX.PortraitsSpriteBank && PortraitsSkins_records[SpriteID].Contains(config.SkinName))) {
                    if (Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName]) {
                        SkinID = config.SkinName;
                    }
                }
            }
            //Logger.Log(LogLevel.Warn, "SkinModHelper", $"SkinDefaultValues: {SkinID}");
            return SkinID;
        }


        public static string getOtherSkin_ReskinPath(Atlas atlas, string origPath, string SpriteID, string SkinId, bool number_search = false) {
            string number = "";
            string CustomPath = null;
            bool Default = !Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !OtherSkin_record.ContainsKey(SpriteID) || OtherSkin_record[SpriteID] == DEFAULT;
            if (Default) {
                foreach (SkinModHelperConfig config in skinConfigs.Values) {
                    if (Player_Skinid_verify == config.hashValues[1]) {
                        if (SpriteID.StartsWith("SP_")) {
                            CustomPath = config.SpecificPlayerSprite_Path + "/" + origPath;
                        } else if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {
                            CustomPath = config.OtherSprite_Path + "/" + origPath;
                        }

                        if (CustomPath != null) {
                            while (number_search && number != "00000" && !atlas.Has(CustomPath + number)) {
                                number = number + "0";
                            }
                            if (atlas.Has(CustomPath + number)) {
                                return CustomPath;
                            }
                        }
                    }
                }
            }
            CustomPath = null;
            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                    if (SkinId == config.SkinName ||
                       (Default && Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName])) {

                        number = "";
                        while (number_search && number != "00000" && !atlas.Has(config.OtherSprite_ExPath + "/" + origPath + number)) {
                            number = number + "0";
                        }
                        if (atlas.Has(config.OtherSprite_ExPath + "/" + origPath + number)) {
                            CustomPath = config.OtherSprite_ExPath + "/" + origPath;
                        }
                    }
                }
            }
            return atlas.Has(CustomPath + number) ? CustomPath : origPath;
        }




        public static bool UniqueSkinSelected(string skin_suffix = null) {

            string skin_name = Settings.SelectedPlayerSkin + skin_suffix;
            return Settings.SelectedPlayerSkin != null && Settings.SelectedPlayerSkin != DEFAULT && skinConfigs.ContainsKey(skin_name);
        }
        public static bool UniqueSilhouetteSelected(string skin_suffix = null) {

            string skin_name = Settings.SelectedSilhouetteSkin + skin_suffix;
            return Settings.SelectedSilhouetteSkin != null && Settings.SelectedSilhouetteSkin != DEFAULT && skinConfigs.ContainsKey(skin_name);
        }











        public static void SetPlayerSpriteMode(PlayerSpriteMode? mode) {
            if (Engine.Scene is Level level) {
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null) {

                    new DynData<PlayerSprite>(player.Sprite)["ColorGrade_Jump_Blocking"] = true;

                    if (mode == null) {
                        mode = player.DefaultSpriteMode;
                    }
                    if (player.Active) {
                        player.ResetSpriteNextFrame((PlayerSpriteMode)mode);
                    } else {
                        player.ResetSprite((PlayerSpriteMode)mode);
                    }
                }
            }
        }

        // ---SaveFilePortraits---
        private static void SaveFilePortraits_Reload() {
            SaveFilePortraitsModule.ExistingPortraits.Clear();
            List<string> Sources_record = new();

            foreach (string portrait in GFX.PortraitsSpriteBank.SpriteData.Keys) {
                SpriteData sprite = GFX.PortraitsSpriteBank.SpriteData[portrait];

                string SourcesPath = null;
                foreach (SpriteDataSource DataSources in sprite.Sources) {
                    SourcesPath = DataSources.Path;
                }
                if (!Sources_record.Contains(SourcesPath)) {
                    Sources_record.Add(SourcesPath);

                    foreach (string animation in sprite.Sprite.Animations.Keys) {
                        if (animation.StartsWith("idle_") && !animation.Substring(5).Contains("_")
                            && sprite.Sprite.Animations[animation].Frames[0].Height <= 200 && sprite.Sprite.Animations[animation].Frames[0].Width <= 200) {
                            SaveFilePortraitsModule.ExistingPortraits.Add(new Tuple<string, string>(portrait, animation));
                        }
                    }
                } else {
                    //Logger.Log(LogLevel.Info, "SkinModHelper", $"maybe SkinModHelper made some Sources same of ID, will stop them re-register to SaveFilePortraits");
                }
            }
            Logger.Log("SaveFilePortraits", $"Found {SaveFilePortraitsModule.ExistingPortraits.Count} portraits to pick from.");
        }


        // ---JungleHelper---
        public static bool HasLantern(PlayerSpriteMode mode) {
            if (mode == (PlayerSpriteMode)444482 || mode == (PlayerSpriteMode)444483) {
                return true;
            }
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (config.JungleLanternMode == true) {
                    foreach (int hash_search in config.hashValues) {
                        if (mode == (PlayerSpriteMode)hash_search) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        public static void ChangePlayerSpriteMode(Player player, bool hasLantern) {
            new DynData<PlayerSprite>(player.Sprite)["ColorGrade_Jump_Blocking"] = true;
            PlayerSpriteMode mode;

            if (hasLantern) {
                mode = SaveData.Instance.Assists.PlayAsBadeline ? (PlayerSpriteMode)444483 : (PlayerSpriteMode)444482;

                if (UniqueSkinSelected("_lantern")) {
                    string hash_object = Settings.SelectedPlayerSkin + "_lantern";
                    if (!skinConfigs[hash_object].JungleLanternMode) {
                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"{hash_object} unset JungleLanternMode to true, will cancel this jungle-jump");
                    } else {
                        if (!backpackOn && UniqueSkinSelected("_lantern_NB")) {
                            hash_object = Settings.SelectedPlayerSkin + "_lantern_NB";

                            if (!skinConfigs[hash_object].JungleLanternMode) {
                                hash_object = Settings.SelectedPlayerSkin + "_lantern";
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{hash_object + "_NB"} unset JungleLanternMode to true, will jungle-jump to {hash_object}");
                            }
                        }
                        Player_Skinid_verify = skinConfigs[hash_object].hashValues[1];
                        mode = (PlayerSpriteMode)skinConfigs[hash_object].hashValues[1];
                    }
                }
            } else {
                mode = SaveData.Instance.Assists.PlayAsBadeline ? PlayerSpriteMode.MadelineAsBadeline : player.DefaultSpriteMode;
            }

            if (player.Active) {
                player.ResetSpriteNextFrame(mode);
            } else {
                player.ResetSprite(mode);
            }
        }
    }
}