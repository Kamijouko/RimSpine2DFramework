using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpine2DFramework
{
    public static class ModStaticMethod
    {
        public static bool AllLevelsLoaded { get; set; } = false;

        public static string message = "not load";

        public static string RootDir
        {
            get
            {
                return ThisMod == null ? ModLister.GetActiveModWithIdentifier("RimSpine2DFramework.NazunaRei.kamijouko").RootDir.ToString() : ThisMod.ModContentPack.RootDir;
            }
        }

        public static ThisModBase ThisMod { get; set; } = null;
    }
}
