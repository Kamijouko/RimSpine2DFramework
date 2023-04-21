using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace DynamicObject
{
    public class DynamicStoryTellerDef : Def
    {
        public StorytellerDef storyTeller;

        public DynamicObjectDef dynamicObject;

        public string idleAnimationName;

        public string specialAnimationName;

        public string interactAnimationName;

        public int specialAnimationLoopForIdleAnimationTimes = 3;

        public string skin = "default";

        public bool loop = true;

        public Vector2 windowScale = new Vector2(580, 620);

        public Vector2 scale = new Vector2(1f, 1f);

        public float cameraDistance = 1f;

        public Vector2 offset = Vector2.zero;

        public Vector3 rotation = Vector3.zero;
    }
}
