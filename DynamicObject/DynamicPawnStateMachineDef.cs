using System;
using System.Collections.Generic;
using System.ComponentModel;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimSpine2DFramework
{
    public class DynamicPawnStateMachineDef : Def
    {
        public DynamicObjectDef targetDynamicObject;

        public PawnBinding binding;

        public List<PawnAnimationState> states = new List<PawnAnimationState>();

        public string defaultStateId;

        public string interactionStateId;

        public int priority;

        public PawnSkeletonSettings skeleton = new PawnSkeletonSettings();

        public bool hideVanillaPawn = false;

        public class PawnBinding
        {
            public List<PawnKindDef> pawnKinds;

            public List<FactionDef> factions;

            public bool colonistOnly;

            public bool allowAnimals = true;

            public bool allowNonHumanlike;

            public bool allowNonPlayer = true;

            public bool Matches(Pawn pawn)
            {
                if (pawn == null)
                {
                    return false;
                }

                if (!allowAnimals && pawn.RaceProps?.Animal == true)
                {
                    return false;
                }

                if (!allowNonHumanlike && pawn.RaceProps != null && !pawn.RaceProps.Humanlike)
                {
                    return false;
                }

                if (!allowNonPlayer)
                {
                    if (pawn.Faction == null || pawn.Faction != Faction.OfPlayer)
                    {
                        return false;
                    }
                }

                if (colonistOnly && !pawn.IsColonist)
                {
                    return false;
                }

                if (pawnKinds != null && pawnKinds.Count > 0)
                {
                    if (pawn.kindDef == null || !pawnKinds.Contains(pawn.kindDef))
                    {
                        return false;
                    }
                }

                if (factions != null && factions.Count > 0)
                {
                    FactionDef pawnFaction = pawn.Faction?.def;
                    if (pawnFaction == null || !factions.Contains(pawnFaction))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public class PawnAnimationState
        {
            public string stateId;

            public string animationName;

            public string skin = "default";

            public bool loop = true;

            public bool holdPoseOnComplete = false;

            public string verbEventName;

            public bool useQueue;

            public int trackIndex;

            public float mixDuration = 0.2f;

            public float delay;

            public bool clearTrack = true;

            public float clearMixDuration = 0.1f;

            public bool forceRestart;

            public bool isFallback;

            public int priority;

            public List<PawnStateTrigger> triggers = new List<PawnStateTrigger>();
        }

        public class PawnStateTrigger
        {
            public PawnStateTriggerSource source = PawnStateTriggerSource.Job;

            public Def def;

            public string defName;

            public int stageIndex = -1;

            public string stageName;

            public bool? isMoving;

            public bool? isDead;

            public bool? isDowned;

            public float threshold = float.NaN;

            public bool thresholdGreaterOrEqual = true;

            public override string ToString()
            {
                string resolvedDef = def?.defName ?? defName ?? string.Empty;
                return $"{source}({resolvedDef})";
            }
        }

        public enum PawnStateTriggerSource
        {
            Job,
            Verb,
            Need,
            Hediff,
            Thought,
            Duty,
            MentalState,
            Movement,
            LifeState
        }

        public class PawnSkeletonSettings
        {
            public string defaultSkin = "default";

            public Vector2 scale = Vector2.one;

            public Vector2 offset = Vector2.zero;

            public Vector3 rotation = Vector3.zero;

            public float cameraDistance = 0f;
        }
    }
}
