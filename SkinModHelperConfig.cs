using Celeste;
using System;

namespace SkinModHelper
{
    public class SkinModHelperConfig
    {
        public String SkinId { get; set; } = null;
        public String SkinDialogKey { get; set; } = null;

        public static string DEFAULT_SKIN = "default_skin";
        public string GetUniquePath()
        {
            return SkinId.Replace('_', '/') + '/';
        }
    }
}
