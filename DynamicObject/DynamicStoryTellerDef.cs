using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace RimSpine2DFramework
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

        public Vector2 windowScale = new Vector2(540, 620);

        public Vector2 scale = new Vector2(1f, 1f);

        public float cameraDistance = 1f;

        public Vector2 offset = Vector2.zero;

        public Vector3 rotation = Vector3.zero;

        public string defaultStateId;

        public List<DynamicObjectStateNode> stateNodes = new List<DynamicObjectStateNode>();

        public class DynamicObjectStateNode
        {
            public string id;

            public string animation;

            public bool loop = true;

            public bool queue;

            public int trackIndex = 0;

            public float delay = 0f;

            public float mixDuration = 0.2f;

            public string fallbackStateId;

            public int priority;

            public List<DynamicObjectStateTrigger> triggers = new List<DynamicObjectStateTrigger>();
        }

        public class DynamicObjectStateTrigger
        {
            public DynamicObjectStateTriggerType triggerType = DynamicObjectStateTriggerType.Job;

            public string defName;

            public bool invert;

            public bool useThreshold;

            public float threshold;

            public DynamicObjectStateComparison comparison = DynamicObjectStateComparison.GreaterOrEqual;

            public bool useUpperThreshold;

            public float upperThreshold;

            public int thoughtStageIndex = -1;
        }

        public enum DynamicObjectStateTriggerType
        {
            Job,
            Verb,
            Need,
            Hediff,
            Thought,
            Always
        }

        public enum DynamicObjectStateComparison
        {
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
            Equal,
            NotEqual
        }
    }
}
