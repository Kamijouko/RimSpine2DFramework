using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.Threading;
using HarmonyLib;
using System.Collections;

namespace RimSpine2DFramework
{
    public class ThisModBase : Mod
    {
        //public override string ModIdentifier { get; } = "RimSpine2DFramework.NazunaRei.kamijouko";

        public ThisModBase(ModContentPack content) : base(content) 
        {
            Instance = this;
            ModStaticMethod.ThisMod = this;
        }

        public static ThisModBase Instance; 
    }
}
