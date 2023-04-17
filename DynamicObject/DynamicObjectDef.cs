using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace DynamicObject
{
    public class DynamicObjectDef : Def
    {
        public SpineSet spine;

        public class SpineSet 
        {
            public string ver = "3.5";

            public string skeletonPath = "";

            public string atlasPath = "";
        }

    }
}
