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

using Celeste.Mod.JungleHelper;

namespace Celeste.Mod.SkinModHelper
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
        public static Dictionary<string, SkinModHelperConfig> OtherskinConfigs;
        private static ILHook TextboxRunRoutineHook;

        private static Hook Jungle_Hook_Update;
        private static Hook Jungle_Hook_Update_x2;

        public SkinModHelperModule()
        {
            Instance = this;
            UI = new SkinModHelperUI();
            skinConfigs = new Dictionary<string, SkinModHelperConfig>();
            OtherskinConfigs = new Dictionary<string, SkinModHelperConfig>();


            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "JungleHelper", Version = new Version(1, 0, 8) }))
            {
                JungleHelperInstalled = true;
            }
            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "ExtendedVariantMode", Version = new Version(0, 22, 21) }))
            {
                ExtendedInstalled = true;
            }
        }
        bool ExtendedInstalled = false;
        bool JungleHelperInstalled = false;

        public override void Load()
        {
            SkinModHelperInterop.Load();

            Everest.Content.OnUpdate += EverestContentUpdateHook;

            On.Celeste.LevelLoader.ctor += on_LevelLoader_ctor;

            On.Celeste.Player.UpdateHair += PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor += PlayerGetTrailColorHook;

            IL.Celeste.Player.UpdateHair += patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate += patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor += patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame += patch_SpriteMode_Silhouette;

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

            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread += GameLoaderLoadThreadHook;

            IL.Celeste.DeathEffect.Draw += DeathEffectDrawHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += DreamBlockHook;
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool += FlyFeatherHook;

            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            TextboxRunRoutineHook = new ILHook(
                typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                SwapTextboxHook);

            if (JungleHelperInstalled) {
                JungleHelperInstalled_Hook();
            }
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);
            ReloadSettings();
            UpdateParticles();
        }

        public override void Unload()
        {
            Everest.Content.OnUpdate -= EverestContentUpdateHook;

            On.Celeste.Player.UpdateHair -= PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor -= PlayerGetTrailColorHook;

            IL.Celeste.Player.UpdateHair -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor -= patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame -= patch_SpriteMode_Silhouette;

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

            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread -= GameLoaderLoadThreadHook;

            IL.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool -= DreamBlockHook;
            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool -= FlyFeatherHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            TextboxRunRoutineHook.Dispose();

            Jungle_Hook_Update.Dispose();
            Jungle_Hook_Update_x2.Dispose();
        }
        private static void JungleHelperInstalled_Hook()
        {
            Jungle_Hook_Update = new Hook(typeof(JungleHelperModule).Assembly.GetType("Celeste.Mod.JungleHelper.Entities.EnforceSkinController").GetMethod("ChangePlayerSpriteMode", BindingFlags.Public | BindingFlags.Static),
                                                 typeof(SkinModHelperModule).GetMethod("ChangePlayerSpriteMode", BindingFlags.Public | BindingFlags.Static));

            Jungle_Hook_Update_x2 = new Hook(typeof(JungleHelperModule).Assembly.GetType("Celeste.Mod.JungleHelper.Entities.EnforceSkinController").GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static),
                                                 typeof(SkinModHelperModule).GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static));
        }

        public void SpecificSprite_LoopReload()
        {
            IL.Celeste.Player.Render -= PlayerRenderIlHook_LoopReLoad;
            if (Player_Skinid_verify != 0)
            {
                IL.Celeste.Player.Render += PlayerRenderIlHook_LoopReLoad;
            }

            string skinId = XmlCombineValue();
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                    {
                        string spritesXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Sprites.xml";
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, skinId, spritesXmlPath);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (config.OtherSprite_Path != null && Player_Skinid_verify == config.SpriteModeValue)
                    {
                        string spritesXmlPath = "Graphics/" + config.OtherSprite_Path + "/Sprites.xml";
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_Path + "/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, skinId, spritesXmlPath);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
                    }
                }
            }
        }






        private void on_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition)
        {
            orig(self, session, startPosition);
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                if (config.Character_ID != null)
                {
                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Load {config.Character_ID}'s Metadata");
                    //Metadata can is null, but Character_ID can't non-existent , 
                    PlayerSprite.CreateFramesMetadata(config.Character_ID);
                }
            }
        }

        public static bool playback = false;
        private static void ExtendedVariant_Silhouette()
        {
            //playback = ExtendedVariantsModule.Settings.MadelineIsSilhouette;
        }

        public static bool PlayerSelf = false;
        public static int Player_Skinid_verify;
        public static bool backpackOn = true;
        private void on_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode)
        {
            Level level = Engine.Scene as Level ?? (Engine.Scene as LevelLoader)?.Level;

            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self);
            bool isGhost = mode < 0;
            bool isSilhouette = false;

            if (!isGhost && level != null)
            {
                backpackOn = Settings.Backpack == SkinModHelperSettings.BackpackMode.On ||
                    (Settings.Backpack == SkinModHelperSettings.BackpackMode.Default && level.Session.Inventory.Backpack);
            }

            if (!isGhost && UniqueSkinSelected() && (mode == PlayerSpriteMode.Madeline || mode == PlayerSpriteMode.MadelineNoBackpack || mode == PlayerSpriteMode.MadelineAsBadeline))
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (config.Options == Settings.SelectedPlayerSkin)
                    {
                        PlayerSelf = true;
                        if (!backpackOn)
                        {
                            mode = (PlayerSpriteMode)(config.SpriteModeValue + config.SpriteMode_NoBackPack);
                        }
                        else
                        {
                            mode = (PlayerSpriteMode)(config.SpriteModeValue + config.SpriteMode_hasBackPack);
                        }
                        break;
                    }
                }
            }
            else if (!isGhost && UniqueSilhouetteSelected() && (mode == PlayerSpriteMode.Playback))
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (config.Options == Settings.SelectedSilhouetteSkin)
                    {
                        selfData["isSilhouette"] = true;
                        if (playback){ PlayerSelf = true;}
                        if (!backpackOn)
                        {
                            mode = (PlayerSpriteMode)(config.SpriteModeValue + config.SpriteMode_NoBackPack);
                        }
                        else
                        {
                            mode = (PlayerSpriteMode)(config.SpriteModeValue + config.SpriteMode_hasBackPack);
                        }
                        break;
                    }
                }
            }
            else if (isGhost)
            {
                selfData["isGhost"] = true;
            }


            orig(self, mode);


            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                if (mode == (PlayerSpriteMode)config.SpriteModeValue || mode == (PlayerSpriteMode)(1 << 31) + config.SpriteModeValue)
                {
                    string id = config.Character_ID;
                    selfData["spriteName"] = id;
                    GFX.SpriteBank.CreateOn(self, id);
                    break;
                }
            }
            if (PlayerSelf && !isGhost && !isSilhouette && mode != (PlayerSpriteMode)2)
            {
                PlayerSelf = false;
                Player_Skinid_verify = (int)mode;
                SpecificSprite_LoopReload();
                UpdateParticles();
            }

            if (isGhost && selfData["spriteName"] == "")
            {
                Logger.Log(LogLevel.Info, "SkinModHelper", $"someone else in CelesteNet uses a Skin-Mod that you don't have");
                string id = "player";
                if (!level.Session.Inventory.Backpack)
                {
                    id = "player_no_backpack";
                    selfData["spriteName"] = id;
                }
                else
                {
                    id = "player";
                    selfData["spriteName"] = id;
                }
                GFX.SpriteBank.CreateOn(self, id);
                return;
            }


            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                if (config.JungleLanternMode == true && (mode == (PlayerSpriteMode)config.SpriteModeValue || mode == (PlayerSpriteMode)(1 << 31) + config.SpriteModeValue))
                {
                    // replay the "idle" sprite to make it apply immediately.
                    self.Play("idle", restart: true);

                    // when the look up animation finishes, rewind it to frame 7: this way we are getting 7-11 playing in a loop.
                    self.OnFinish = anim =>
                    {
                        if (anim == "lookUp")
                        {
                            self.Play("lookUp", restart: true);
                            self.SetAnimationFrame(5);
                        }
                    };
                }
            }
        }




        private static Sprite SpriteBankCreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id)
        {
            // Prevent mode's non-vanilla value causing the game Error
            if (sprite is PlayerSprite && id == "")
            {
                return null;
            }
            return orig(self, sprite, id);
        }


        private void patch_SpriteMode_Badeline(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate<Func<PlayerSpriteMode, PlayerSpriteMode>>(orig => 
                {
                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (orig == (PlayerSpriteMode)config.SpriteModeValue && config.BadelineMode == true) {
                            return (PlayerSpriteMode)3;
                        }
                    }
                    return orig;
                });
            }
        }
        private void patch_SpriteMode_Silhouette(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate<Func<PlayerSpriteMode, PlayerSpriteMode>>(orig =>
                {
                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (orig == (PlayerSpriteMode)config.SpriteModeValue && config.SilhouetteMode == true) {
                            return (PlayerSpriteMode)4;
                        }
                    }
                    return orig;
                });
            }
        }

        private static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player)
        {
            orig(self, player);
            if (Player_Skinid_verify != 0)
            {
                DynData<Lookout> selfData = new DynData<Lookout>(self);
                selfData["animPrefix"] = "";
            }
            else
            {
                return;
            }
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
            OtherskinConfigs.Clear();
            Instance.LoadSettings();

            foreach (ModContent mod in Everest.Content.Mods)
            {
                List<SkinModHelperConfig> configs;
                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml))
                {
                    configs = LoadConfigFile(configAsset);

                    foreach (SkinModHelperConfig config in configs)
                    {
                        Regex skinIdRegex = new(@"^[a-zA-Z0-9]+_[a-zA-Z0-9]+$");
                        if (config.Options == null)
                        {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"Some SkinModHelperConfig.yaml unset 'Options'?");
                            continue;
                        }
                        if (OtherskinConfigs.ContainsKey(config.Options) || skinConfigs.ContainsKey(config.Options))
                        {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"{config.Options} appear twice in the ConfigFile, unregister the second {config.Options}");
                            continue;
                        }
                        if (config.SpriteModeValue > 4)
                        {
                            bool Whether_continue = false;
                            foreach (SkinModHelperConfig config_x2 in SkinModHelperModule.skinConfigs.Values)
                            {
                                if (config.SpriteModeValue == config_x2.SpriteModeValue)
                                {
                                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"{config.Options} and {config_x2.Options} has the same SpriteModeValue, unregister the {config.Options}'s skin ID");
                                    Whether_continue = true;
                                }
                            }
                            if (Whether_continue)
                            {
                                continue;
                            }
                        }


                        if (config.OtherSprite_ExPath != null)
                        {
                            OtherskinConfigs.Add(config.Options, config);
                        }
                        if (config.Character_ID == null || config.SpriteModeValue < 5)
                        {
                            if (config.OtherSprite_ExPath == null || config.SpriteModeValue != 0) { Logger.Log(LogLevel.Warn, "SkinModHelper", $"invalid skin ID {config.Options}, will not register."); }
                        }
                        else
                        {
                            if (config.SpecificPlayerSprite_Path == null && config.Character_ID != null)
                            {
                                config.SpecificPlayerSprite_Path = "characters/" + config.Character_ID;
                            }


                            List<bool> changed = new(new bool[MAX_DASHES + 1]);
                            if (config.HairColors != null)
                            {
                                config.GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1])
                                {
                                    // Default colors taken from vanilla
                                    [0] = Calc.HexToColor("44B7FF"),
                                    [1] = Calc.HexToColor("AC3232"),
                                    [2] = Calc.HexToColor("FF6DEF")
                                };
                                foreach (SkinModHelperConfig.HairColor hairColor in config.HairColors)
                                {
                                    Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");
                                    if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColorRegex.IsMatch(hairColor.Color))
                                    {
                                        config.GeneratedHairColors[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                                        changed[hairColor.Dashes] = true;
                                    }
                                    else
                                    {
                                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"Invalid hair color or dash count values provided for {config.Options}.");
                                    }
                                }
                            }
                            // Fill upper dash range with the last customized dash color
                            for (int i = 3; i <= MAX_DASHES; i++)
                            {
                                if (!changed[i] && config.HairColors != null)
                                {
                                    config.GeneratedHairColors[i] = config.GeneratedHairColors[i - 1];
                                }
                            }

                            if (config.SpriteMode_JungleLantern_NoBackPack == 0 && config.SpriteMode_JungleLantern != 0)
                            {
                                config.SpriteMode_JungleLantern_NoBackPack = config.SpriteMode_JungleLantern;
                            }

                            Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new skin: {config.Options}");
                            skinConfigs.Add(config.Options, config);
                        }
                    }
                }
            }

            if (Settings.SelectedPlayerSkin == null || !skinConfigs.ContainsKey(Settings.SelectedPlayerSkin))
            {
                Settings.SelectedPlayerSkin = DEFAULT;
            }
            if (Settings.SelectedSilhouetteSkin == null || !skinConfigs.ContainsKey(Settings.SelectedSilhouetteSkin))
            {
                Settings.SelectedSilhouetteSkin = DEFAULT;
            }
        }


        private static List<SkinModHelperConfig> LoadConfigFile(ModAsset skinConfigYaml)
        {
            return skinConfigYaml.Deserialize<List<SkinModHelperConfig>>();
        }





        // ---Custom ColorGrade---
        private void PlayerRenderHook(On.Celeste.Player.orig_Render orig, Player self)
        {
            int dashCount = self.Dashes < 0 ? 0 : Math.Min(self.Dashes, MAX_DASHES);
            bool MaxDashZero = self.MaxDashes <= 0;

            if (!MaxDashZero)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if ((self.Sprite.Mode == (PlayerSpriteMode)config.SpriteModeValue) && config.colorGrade_Path != null)
                    {
                        while (dashCount > 2 && !GFX.ColorGrades.Has(config.colorGrade_Path + "/dash" + dashCount))
                        {
                            dashCount--;
                        }
                        new DynData<PlayerSprite>(self.Sprite)["DashCount"] = dashCount;
                    }
                }
            }
            else 
            { 
                new DynData<PlayerSprite>(self.Sprite)["DashCount"] = 1; 
            }
            orig(self);
        }

        private void PlayerDeadBodyRenderHook(On.Celeste.PlayerDeadBody.orig_Render orig, PlayerDeadBody self)
        {
            PlayerSprite sprite = new DynData<PlayerDeadBody>(self).Get<PlayerSprite>("sprite");
            new DynData<PlayerSprite>(sprite)["DashCount"] = 1;
            orig(self);
        }

        private void OnPlayerSpriteRender(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self)
        {
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
            }

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if ((self.Mode == (PlayerSpriteMode)config.SpriteModeValue) && config.colorGrade_Path != null) {
                    string colorGrade_Path = config.colorGrade_Path + "/dash" + dashCount;

                    if (GFX.ColorGrades.Has(colorGrade_Path) && colorGrade_Path != null) {
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
                    }
                    break;
                }
            }
        }

        // ---Custom Dash Color---
        private void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity)
        {
            orig(self, applyGravity);

            int dashCount = self.Dashes < 0 ? 0 : Math.Min(self.Dashes, MAX_DASHES);
            bool MaxDashZero = self.MaxDashes <= 0;

            List <Color> GeneratedHairColors = null;
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (self.Sprite.Mode == (PlayerSpriteMode)config.SpriteModeValue && config.HairColors != null) {
                    GeneratedHairColors = config.GeneratedHairColors;
                }
            }

            if (self.StateMachine.State != Player.StStarFly && self.Hair.Color != Color.White && GeneratedHairColors != null) {
                if (MaxDashZero) {
                    self.Hair.Color = GeneratedHairColors[1];
                }
                else {
                    self.Hair.Color = GeneratedHairColors[dashCount];
                }
            }
        }

        private Color PlayerGetTrailColorHook(On.Celeste.Player.orig_GetTrailColor orig, Player self, bool wasDashB)
        {
            int dashCount = self.Dashes < 0 ? 0 : Math.Min(self.Dashes, MAX_DASHES);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (self.Sprite.Mode == (PlayerSpriteMode)config.SpriteModeValue && config.HairColors != null) {
                    return config.GeneratedHairColors[dashCount];
                }
            }
            return orig(self, wasDashB);
        }

        private void PlayerRenderIlHook(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // jump to the usage of the Red color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_Red")))
            {
                Logger.Log("SkinModHelper", $"Patching silhouette hair color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((color, player) => {
                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (player.Sprite.Mode == (PlayerSpriteMode)config.SpriteModeValue && config.SilhouetteMode == true) {
                            if (player.Dashes == 0) {
                                color = Calc.HexToColor("348DC1");
                            }
                            color = player.Hair.Color;
                        }
                        player.Hair.Color = color;
                    }
                    return color;
                });
            }

            // jump to the usage of the White-color / Null-color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White")))
            {
                Logger.Log("SkinModHelper", $"Patching silhouette color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((orig, self) => {
                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                        if (self.Sprite.Mode == (PlayerSpriteMode)config.SpriteModeValue && config.SilhouetteMode == true) {
                            return self.Hair.Color;
                        }
                    }
                    return orig;
                });
            }
        }



        // ---Specific Player Sprite---
        private void PlayerRenderIlHook_LoopReLoad(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/startStarFlyWhite")))
            {
                Logger.Log("SkinModHelper", $"Changing startStarFlyWhite path at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitDelegate<Func<string, string>>((orig) =>
                {
                    string CustomPath = null;
                    foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
                    {
                        if (Settings.ExtraXmlList.ContainsKey(config.Options))
                        {
                            if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                            {
                                if (GFX.Game.Has(config.OtherSprite_ExPath + "/startStarFlyWhite00"))
                                {
                                    CustomPath = config.OtherSprite_ExPath + "/startStarFlyWhite";
                                }
                            }
                        }
                    }
                    if (Player_Skinid_verify != 0)
                    {
                        foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                        {
                            if (Player_Skinid_verify == config.SpriteModeValue && config.SpecificPlayerSprite_Path != null)
                            {
                                if (GFX.Game.Has(config.SpecificPlayerSprite_Path + "/startStarFlyWhite00"))
                                {
                                    CustomPath = config.SpecificPlayerSprite_Path + "/startStarFlyWhite";
                                }
                            }
                        }
                    }

                    if (CustomPath != null)
                    {
                        return CustomPath;
                    }
                    return orig;
                });
            }
        }
        private MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index)
        {
            string bangs = null;
            string hair = null;
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                if (self.Sprite.Mode == (PlayerSpriteMode)2 && config.OtherSprite_Path != null && Player_Skinid_verify == config.SpriteModeValue)
                {
                    if (index == 0 && GFX.Game.Has(config.OtherSprite_Path + "/bangs00"))
                    {
                        bangs = config.OtherSprite_Path + "/badeline_bangs";
                    }
                    else if(GFX.Game.Has(config.OtherSprite_Path + "/hair00"))
                    {
                        hair = config.OtherSprite_Path + "/badeline_hair00";
                    }
                }
                else if (self.Sprite.Mode == (PlayerSpriteMode)config.SpriteModeValue && config.SpecificPlayerSprite_Path != null)
                {
                    if (index == 0 && GFX.Game.Has(config.SpecificPlayerSprite_Path + "/bangs00"))
                    {
                        bangs = config.SpecificPlayerSprite_Path + "/bangs";
                    }
                    else if (GFX.Game.Has(config.SpecificPlayerSprite_Path + "/hair00"))
                    {
                        hair = config.SpecificPlayerSprite_Path + "/hair00";
                    }
                }
            }


            if (index == 0 && bangs != null)
            {
                List<MTexture> CustomBangs = GFX.Game.GetAtlasSubtextures(bangs);
                return CustomBangs.Count > self.Sprite.HairFrame ? CustomBangs[self.Sprite.HairFrame] : CustomBangs[0];
            }
            else if (hair != null)
            {
                return GFX.Game[hair];
            }

            return orig(self, index);
        }

        private void DeathEffectDrawHook(ILContext il)
        {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/hair00")))
            {
                cursor.EmitDelegate<Func<string, string>>((orig) =>
                {
                    string CustomPath = null;
                    foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
                    {
                        if (Settings.ExtraXmlList.ContainsKey(config.Options))
                        {
                            if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                            {
                                if (GFX.Game.Has(config.OtherSprite_ExPath + "/death_particle"))
                                {
                                    CustomPath = config.OtherSprite_ExPath + "/death_particle";
                                }
                            }
                        }
                    }
                    if (Player_Skinid_verify != 0)
                    {
                        foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                        {
                            if (Player_Skinid_verify == config.SpriteModeValue && config.SpecificPlayerSprite_Path != null)
                            {
                                if (GFX.Game.Has(config.SpecificPlayerSprite_Path + "/death_particle"))
                                {
                                    CustomPath = config.SpecificPlayerSprite_Path + "/death_particle";
                                }
                            }
                        }
                    }

                    if (CustomPath != null)
                    {
                        return CustomPath;
                    }
                    return orig;
                });
            }
        }






        //---Other Sprite---
        private Sprite SpriteBankCreateOnHook(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id)
        {
            string newId = id + "_" + XmlCombineValue();
            if (self.SpriteData.ContainsKey(newId))
            {
                id = newId;
            }
            return orig(self, sprite, id);
        }

        private Sprite SpriteBankCreateHook(On.Monocle.SpriteBank.orig_Create orig, SpriteBank self, string id)
        {
            string newId = id + "_" + XmlCombineValue();
            if (self.SpriteData.ContainsKey(newId))
            {
                id = newId;
            }
            return orig(self, id);
        }

        // We only need this for file select : )
        private void GameLoaderLoadThreadHook(On.Celeste.GameLoader.orig_LoadThread orig, GameLoader self)
        {
            orig(self);
            string skinId = XmlCombineValue();

            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                    {
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Portraits.xml";
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (Player_Skinid_verify == config.SpriteModeValue && config.OtherSprite_Path != null)
                    {
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_Path + "/Portraits.xml";
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
                    }
                }
            }
        }

        // Wait until the main sprite bank is created, then combine with our skin mod banks
        private void LevelLoaderLoadingThreadHook(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
        {            
            if (Settings.Backpack != SkinModHelperSettings.BackpackMode.Default)
            {
                //at this Hooking time, The level data has not established, cannot get Default backpack state of Level 
                backpackOn = Settings.Backpack != SkinModHelperSettings.BackpackMode.Off;
            }

            if (UniqueSkinSelected())
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (config.Options == Settings.SelectedPlayerSkin)
                    {
                        PlayerSelf = true;
                        if (!backpackOn)
                        {
                            Player_Skinid_verify = (config.SpriteModeValue + config.SpriteMode_NoBackPack);
                        }
                        else
                        {
                            Player_Skinid_verify = (config.SpriteModeValue + config.SpriteMode_hasBackPack);
                        }
                    }
                }
            }


            string skinId = XmlCombineValue();
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                    {
                        string spritesXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Sprites.xml";
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_ExPath + "/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, skinId, spritesXmlPath);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (Player_Skinid_verify == config.SpriteModeValue && config.OtherSprite_Path != null)
                    {
                        string spritesXmlPath = "Graphics/" + config.OtherSprite_Path + "/Sprites.xml";
                        string portraitsXmlPath = "Graphics/" + config.OtherSprite_Path + "/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, skinId, spritesXmlPath);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, skinId, portraitsXmlPath);
                    }
                }
            }
            orig(self);
        }


        private static string XmlCombineValue()
        {
            int sort = 0;
            string identifier = "";
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && sort == 0)
                    {
                        identifier = (sort + "_" + config.Options);
                        sort += 1;
                    }
                    else if (Settings.ExtraXmlList[config.Options])
                    {
                        identifier = (identifier + ", " + sort + "_" + config.Options);
                        sort += 1;
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (Player_Skinid_verify == config.SpriteModeValue && sort == 0)
                    {
                        identifier = (sort + "_" + Player_Skinid_verify);
                        sort += 1;
                    }
                    else if (Player_Skinid_verify == config.SpriteModeValue)
                    {
                        identifier = (identifier + ", " + sort + "_" + Player_Skinid_verify);
                        sort += 1;
                    }
                }
            }

            if (identifier != "")
            {
                //Logger.Log(LogLevel.Verbose, "SkinModHelper", $"SpriteBank identifier: {identifier}");
                return identifier;
            }
            return null;
        }




        private static void UpdateParticles()
        {
            FlyFeather.P_Collect.Source = GFX.Game["particles/feather"];
            FlyFeather.P_Boost.Source = GFX.Game["particles/feather"];

            string CustomPath = null;
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                    {
                        if (GFX.Game.Has(config.OtherSprite_ExPath + "/particles/feather"))
                        {
                            CustomPath = config.OtherSprite_ExPath + "/particles/feather";
                        }
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (Player_Skinid_verify == config.SpriteModeValue && config.OtherSprite_Path != null)
                    {
                        if (GFX.Game.Has(config.OtherSprite_Path + "/particles/feather"))
                        {
                            CustomPath = config.OtherSprite_Path + "/particles/feather";
                        }
                    }
                }
            }

            if (CustomPath != null)
            {
                FlyFeather.P_Collect.Source = GFX.Game[CustomPath];
                FlyFeather.P_Boost.Source = GFX.Game[CustomPath];
            }
        }


        private void DreamBlockHook(ILContext il)
        {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/dreamblock/particles")))
            {
                cursor.EmitDelegate<Func<string, string>>((orig) =>
                {
                    return GetReskinPath("objects/dreamblock/particles");
                });
            }
        }

        private void FlyFeatherHook(ILContext il)
        {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/flyFeather/outline")))
            {
                cursor.EmitDelegate<Func<string, string>>((orig) =>
                {
                    return GetReskinPath("objects/flyFeather/outline");
                });
            }
        }

        private static string GetReskinPath(string orig)
        {
            string CustomPath = null;
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                    {
                        if (GFX.Game.Has(config.OtherSprite_ExPath + "/" + orig))
                        {
                            CustomPath = config.OtherSprite_ExPath + "/" + orig;
                        }
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (Player_Skinid_verify == config.SpriteModeValue && config.OtherSprite_Path != null)
                    {
                        if (GFX.Game.Has(config.OtherSprite_Path + "/" + orig))
                        {
                            CustomPath = config.OtherSprite_Path + "/" + orig;
                        }
                    }
                }
            }

            if (CustomPath != null)
            {
                return GFX.Game.Has(CustomPath) ? CustomPath : orig;
            }
            return orig;
        }




        private void SwapTextboxHook(ILContext il)
        {
            ILCursor cursor = new(il);
            // Move to the last occurence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>()))
            {
            }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true)
            {
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((orig) =>
                {
                    return ReplacePortraitPath(orig);
                });
            }
        }

        // This one requires double hook - for some reason they implemented a tiny version of the Textbox class that behaves differently
        private void CampfireQuestionHook(ILContext il)
        {
            ILCursor cursor = new(il);
            // Move to the last occurrence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>()))
            {
            }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true)
            {
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((orig) =>
                {
                    return ReplacePortraitPath(orig);
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("_ask"),
                instr => instr.MatchCall(out MethodReference method) && method.Name == "Concat"))
            {
                cursor.EmitDelegate<Func<string, string>>((orig) =>
                {
                    return ReplaceTextboxPath(orig);
                });
            }
        }

        private static FancyText.Portrait ReplacePortraitPath(FancyText.Portrait portrait)
        {
            string skinModPortraitSpriteId = portrait.SpriteId + "_" + XmlCombineValue();
            if (GFX.PortraitsSpriteBank.Has(skinModPortraitSpriteId))
            {
                portrait.Sprite = skinModPortraitSpriteId.Replace("portrait_", "");
            }

            return portrait;
        }

        // ReplacePortraitPath makes textbox path funky, so correct to our real path or revert to vanilla if it does not exist
        private static string ReplaceTextboxPath(string textboxPath)
        {
            string originalPortraitId = textboxPath.Split('_')[0].Replace("textbox/", ""); // "textbox/[orig portrait id]_[skin id]_ask"

            string CustomPath = null;
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values)
            {
                if (Settings.ExtraXmlList.ContainsKey(config.Options))
                {
                    if (Settings.ExtraXmlList[config.Options] && config.OtherSprite_ExPath != null)
                    {
                        if (GFX.Portraits.Has("textbox/" + config.OtherSprite_ExPath + "/" + originalPortraitId + "_ask"))
                        {
                            CustomPath = config.OtherSprite_ExPath;
                        }
                    }
                }
            }
            if (Player_Skinid_verify != 0)
            {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                {
                    if (Player_Skinid_verify == config.SpriteModeValue && config.OtherSprite_Path != null)
                    {
                        if (GFX.Portraits.Has("textbox/" + config.OtherSprite_Path + "/" + originalPortraitId + "_ask"))
                        {
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
                }
            }
        }


        private SpriteBank BuildBank(SpriteBank origBank, string xmlPath)
        {
            try
            {
                SpriteBank newBank = new(origBank.Atlas, xmlPath);
                Logger.Log(LogLevel.Verbose, "SkinModHelper", $"Built sprite bank for {xmlPath}.");
                return newBank;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Could not build sprite bank for {xmlPath}: {e.Message}.");
                return null;
            }
        }

        // Add any missing vanilla animations to an overridden sprite
        private void PatchSprite(Sprite origSprite, Sprite newSprite)
        {
            Dictionary<string, Sprite.Animation> newAnims = newSprite.GetAnimations();

            // Shallow copy... sometimes new animations get added mid-update?
            Dictionary<string, Sprite.Animation> oldAnims = new(origSprite.GetAnimations());
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




        static bool RefreshXml = false;
        public static void RefreshExtraXml()
        {
            if (RefreshXml)
            {
                RefreshPlayerSpriteMode();
            }
        }
        public static void RefreshPlayerSpriteMode()
        {
            if (Engine.Scene is not Level)
            {
                return;
            }
            Player player = Engine.Scene.Tracker?.GetEntity<Player>();
            if (player == null)
            {
                return;
            }

            if (SaveData.Instance != null && SaveData.Instance.Assists.PlayAsBadeline)
            {
                SetPlayerSpriteMode(PlayerSpriteMode.MadelineAsBadeline);
            }
            else
            {
                SetPlayerSpriteMode(null);
            }
        }





        // Trigger when we change the setting, store the new one. If in-level, redraw player sprite.
        public static void UpdateSkin(string newSkinId)
        {
            Settings.SelectedPlayerSkin = newSkinId;
            RefreshPlayerSpriteMode();
        }
        public static void UpdateSilhouetteSkin(string newSkinId)
        {
            Settings.SelectedSilhouetteSkin = newSkinId;
        }
        public static void UpdateExtraXml(string SkinId, bool OnOff)
        {
            Settings.ExtraXmlList[SkinId] = OnOff;
            RefreshXml = true;
        }


        public static bool UniqueSkinSelected()
        {
            return Settings.SelectedPlayerSkin != null && Settings.SelectedPlayerSkin != DEFAULT;
        }
        public static bool UniqueSilhouetteSelected()
        {
            return Settings.SelectedSilhouetteSkin != null && Settings.SelectedSilhouetteSkin != DEFAULT;
        }











        public static void SetPlayerSpriteMode(PlayerSpriteMode? mode)
        {
            if (Engine.Scene is Level level)
            {
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null)
                {
                    PlayerSelf = true;
                    Player_Skinid_verify = 0;
                    if (mode == null)
                    {
                        mode = player.DefaultSpriteMode;
                    }
                    if (player.Active)
                    {
                        player.ResetSpriteNextFrame((PlayerSpriteMode)mode);
                    }
                    else
                    {
                        player.ResetSprite((PlayerSpriteMode)mode);
                    }
                }
            }
        }





        // ---JungleHelper---
        public static bool HasLantern(PlayerSpriteMode mode)
        {
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
            {
                if (config.JungleLanternMode == true && mode == (PlayerSpriteMode)config.SpriteModeValue)
                {
                    return true;
                }
            }
            return mode == (PlayerSpriteMode)444482 || mode == (PlayerSpriteMode)444483;
        }


        public static void ChangePlayerSpriteMode(Player player, bool hasLantern)
        {
            if (Engine.Scene is not Level)
            {
                return;
            }
            player = Engine.Scene.Tracker?.GetEntity<Player>();
            if (player == null)
            {
                return;
            }

            if (!hasLantern)
            {
                if (SaveData.Instance != null && SaveData.Instance.Assists.PlayAsBadeline)
                {
                    SetPlayerSpriteMode(PlayerSpriteMode.MadelineAsBadeline);
                }
                else
                {
                    SetPlayerSpriteMode(null);
                }
            }
            else if (hasLantern)
            {
                PlayerSpriteMode mode = (PlayerSpriteMode)444482;
                if (UniqueSkinSelected())
                {
                    foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values)
                    {
                        if (config.SpriteMode_JungleLantern != 0 && config.Options == Settings.SelectedPlayerSkin)
                        {
                            if (!backpackOn)
                            {
                                mode = (PlayerSpriteMode)config.SpriteModeValue + config.SpriteMode_JungleLantern_NoBackPack;
                            }
                            else
                            {
                                mode = (PlayerSpriteMode)config.SpriteModeValue + config.SpriteMode_JungleLantern;
                            }
                            break;
                        }
                    }
                }
                if (mode == (PlayerSpriteMode)444482 && SaveData.Instance != null && SaveData.Instance.Assists.PlayAsBadeline)
                {
                    mode = (PlayerSpriteMode)444483;
                }
                SetPlayerSpriteMode(mode);
            }
        }

    }
}