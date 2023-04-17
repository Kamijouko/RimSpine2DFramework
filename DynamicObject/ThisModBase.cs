using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HugsLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace DynamicObject
{
    public class ThisModBase : ModBase
    {
        public override string ModIdentifier { get; } = "DynamicObject.NazunaRei.kamijouko";

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            ModStaticMethod.ThisMod = this;
            LoadAndResolveAllDynamicDefs();
            ModStaticMethod.AllLevelsLoaded = true;
        }


        public void LoadAndResolveAllDynamicDefs()
        {
            List<DynamicObjectDef> list = DefDatabase<DynamicObjectDef>.AllDefsListForReading;
            foreach (DynamicObjectDef def in list)
            {
                if (def.spine == null)
                    continue;
                if (def.spine.ver == "3.5")
                {
                    string txt = File.ReadAllText(Path.Combine(ModStaticMethod.RootDir, def.spine.atlasPath));
                    TextAsset atlasTxt = new TextAsset(txt);
                    byte[] bytes = File.ReadAllBytes(Path.Combine(ModStaticMethod.RootDir, def.spine.skeletonPath));
                    TextAsset skeletonByte = new TextAsset(Convert.ToBase64String(bytes));
                    Spine35.Unity.AtlasAsset runtimeAtlasAsset = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(atlasTxt, textures, materialPropertySource, true);
                    Spine35.Unity.SkeletonDataAsset runtimeSkeletonDataAsset = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(skeletonByte, runtimeAtlasAsset, true);
                }
            }
            
        }
    }
}
