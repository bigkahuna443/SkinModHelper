﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using YamlDotNet.Serialization;

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperConfig
    {
        public string Options { get; set; }
        public bool Player_List { get; set; }
        public bool Silhouette_List { get; set; }



        public int SpriteModeValue { get; set; }
        public string Character_ID { get; set; }



        public bool BadelineMode { get; set; }
        public bool SilhouetteMode { get; set; }
        public bool JungleLanternMode { get; set; }



        public int SpriteMode_hasBackPack { get; set; }
        public int SpriteMode_NoBackPack { get; set; }
        public int SpriteMode_JungleLantern { get; set; }
        public int SpriteMode_JungleLantern_NoBackPack { get; set; }



        public string SpecificPlayerSprite_Path { get; set; }
        public string colorGrade_Path { get; set; }
        public string OtherSprite_Path { get; set; }
        public string OtherSprite_ExPath { get; set; }



        public List<HairColor> HairColors { get; set; }
        public class HairColor
        {
            public int Dashes { get; set; }
            public string Color { get; set; }
        }
        public List<Color> GeneratedHairColors { get; set; }
    }
}
