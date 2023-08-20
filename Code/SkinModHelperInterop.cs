using MonoMod.ModInterop;

namespace Celeste.Mod.SkinModHelper {
    [ModExportName("SkinModHelper")]
    public static class SkinModHelperInterop {
        internal static void Load() {
            typeof(SkinModHelperInterop).ModInterop();
        }

        public static void ApplySkinGlobal(string newSkin) {
            SkinModHelperModule.UpdateSkin(newSkin);
        }

        public static void ApplySkinSession(string newSkin) {
            SkinModHelperModule.SetSessionSkin(newSkin);
        }
    }
}
