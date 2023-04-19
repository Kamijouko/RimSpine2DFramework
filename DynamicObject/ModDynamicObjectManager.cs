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

namespace DynamicObject
{
    public static class ModDynamicObjectManager
    {
        public static Dictionary<string, Shader> spineShaderDatabase = new Dictionary<string, Shader>();
        public static Dictionary<string, Spine35.Unity.SkeletonAnimation> spine35Database = new Dictionary<string, Spine35.Unity.SkeletonAnimation>();
        public static Dictionary<string, Spine38.Unity.SkeletonAnimation> spine38Database = new Dictionary<string, Spine38.Unity.SkeletonAnimation>();
    }
}
