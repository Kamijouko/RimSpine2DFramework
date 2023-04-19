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
        public ImportMode importMode = ImportMode.File;
        public SpineSet spine;

        public class SpineSet 
        {
            public string ver = "3.8";

            public string assetBundleName = "";

            public string atlasPath = "";

            public string skeletonPath = "";

            //这里的GraphicClass填Graphic_single
            public List<DynamicGraphicData> textures = new List<DynamicGraphicData>();

            public List<string> materialNames = new List<string>();
        }

    }
}
