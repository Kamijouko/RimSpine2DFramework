using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using Spine35;
using Spine35.Unity;

namespace DynamicObject
{
    public class DynamicObjectInstance : MonoBehaviour
    {
        public string Ver { get; set; } = "3.5";

        void Start()
        {
            if (Ver == "3.5")
            {
                TextAsset atlasTxt = TextAsset.
                Spine35.Unity.AtlasAsset runtimeAtlasAsset = AtlasAsset.CreateRuntimeInstance(atlasTxt, textures, materialPropertySource, true);
                Spine35.Unity.SkeletonDataAsset runtimeSkeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(skeletonJson, runtimeAtlasAsset, true);
            }
            
        }

    }
}
