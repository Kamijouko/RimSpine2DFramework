using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HugsLib;
using Verse;
using RimWorld;


namespace DynamicObject
{
    public class ThisModBase : ModBase
    {
        public override string ModIdentifier { get; } = "DynamicObject.NazunaRei.kamijouko";

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            ModStaticMethod.ThisMod = this;
            LoadAndResolveAllPlanDefs();
            ModStaticMethod.AllLevelsLoaded = true;
        }


        public void LoadAndResolveAllPlanDefs()
        {

        }
    }
}
