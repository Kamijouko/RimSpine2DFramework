using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.IO;

namespace DynamicObject
{
    public class DynamicObjectDef : Def
    {
        public ImportMode importMode = ImportMode.File;
        public SpineSet spine;

        public Material[] materials = new Material[] { };

        public class SpineSet 
        {
            public string ver = "3.8";

            public string assetBundleName = "";

            public string atlasPath = "";

            public string skeletonPath = "";

            public string shaderName = "Spine-Skeleton.shader";

            //这里的GraphicClass填Graphic_single
            public List<TexturePath> textures = new List<TexturePath>();

            public List<string> materialNames = new List<string>();
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
        }

    }
}
