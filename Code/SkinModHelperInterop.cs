using MonoMod.ModInterop;

namespace Celeste.Mod.SkinModHelper {
    [ModExportName("SkinModHelper")]
    public static class SkinModHelperInterop {
        internal static void Load() {
            typeof(SkinModHelperInterop).ModInterop();
        }
    }
}
