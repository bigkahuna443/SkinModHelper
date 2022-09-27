using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperConfig {
        public string SkinId { get; set; }
        public string SkinDialogKey { get; set; }
        public List<HairColor> HairColors { get; set; }

        public class HairColor {
            public int Dashes { get; set; }
            public string Color { get; set; }
        }

        public List<Color> GeneratedHairColors { get; set; }

        public string GetUniquePath() {
            return SkinId.Replace('_', '/') + '/';
        }
    }
}
