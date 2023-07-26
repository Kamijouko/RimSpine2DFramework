using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using Spine35;
using Spine35.Unity;
using Spine38;
using Spine38.Unity;

namespace RimSpine2DFramework
{
    public static class ModDynamicObjectManager
    {
        public static Dictionary<string, Shader> spineShaderDatabase = new Dictionary<string, Shader>();
        public static Dictionary<string, SpineTextAssetData> spine35Database = new Dictionary<string, SpineTextAssetData>();
        public static Dictionary<string, SpineTextAssetData> spine38Database = new Dictionary<string, SpineTextAssetData>();
        public static Dictionary<string, SpineTextAssetData> spine40Database = new Dictionary<string, SpineTextAssetData>();
        public static Dictionary<string, SpineTextAssetData> spine41Database = new Dictionary<string, SpineTextAssetData>();

        public static Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

        public static Dictionary<string, GameObject> DynamicStoryTellerDatabase = new Dictionary<string, GameObject>();
    }
}
