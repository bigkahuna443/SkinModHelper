using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace SkinModHelper
{
    public class SkinModHelperConfig
    {
        public String SkinId { get; set; }
        public String SkinDialogKey { get; set; }
        public List<HairColor> HairColors { get; set; }

        public class HairColor
        {
            public int Dashes { get; set; }
            public String Color { get; set; }
        }

        public List<Color> GeneratedHairColors { get; set; }

        public string GetUniquePath()
        {
            return SkinId.Replace('_', '/') + '/';
        }
    }
}
