using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace RimSpine2DFramework
{
    public class DynamicObjectPlanDef : Def
    {
        public List<PawnKindDef> pawnKindDefs = new List<PawnKindDef>();
        public List<DynamicObjectDef> dynamicObjectDefs = new List<DynamicObjectDef>();
    }
}
