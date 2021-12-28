using System;

namespace SkinModHelper
{
    public class SkinModHelperConfig
    {
        public String SkinId;
        public String SkinDialogKey;

        public string GetUniquePath()
        {
            return SkinId.Replace('_', '/') + '/';
        }
    }
}
