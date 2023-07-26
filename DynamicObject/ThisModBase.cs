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
using System.Threading;
using HarmonyLib;
using System.Collections;

namespace RimSpine2DFramework
{
    public class ThisModBase : ModBase
    {
        public override string ModIdentifier { get; } = "RimSpine2DFramework.NazunaRei.kamijouko";

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            ModStaticMethod.ThisMod = this;
            //Logger.Message("已读取"+ModContentPack.assetBundles.loadedAssetBundles.Count.ToString()+"个AB包");    
        }
    }
}
