using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimSpine2DFramework
{
    public class DynamicPawnStateController
    {
        private static readonly PropertyInfo NeedCurCategoryProperty = AccessTools.Property(typeof(Need), "CurCategory");
        private static readonly PropertyInfo NeedCurLevelCategoryProperty = AccessTools.Property(typeof(Need), "CurLevelCategory");
        private static readonly MemberInfo NeedDefCategoriesMember = ResolveNeedDefMember(new[] { "needCategories", "categories" });
        private static readonly MemberInfo NeedDefStagesMember = ResolveNeedDefMember(new[] { "stages", "needStages" });
        private static readonly PropertyInfo ThoughtHandlerMemoriesProperty = AccessTools.Property(typeof(ThoughtHandler), "memories");
        private static readonly FieldInfo ThoughtHandlerMemoriesField = ThoughtHandlerMemoriesProperty == null ? AccessTools.Field(typeof(ThoughtHandler), "memories") : null;
        private static readonly Type ThoughtHandlerMemoriesType = ThoughtHandlerMemoriesProperty?.PropertyType ?? ThoughtHandlerMemoriesField?.FieldType;
        private static readonly PropertyInfo MemoryHandlerMemoriesListProperty = ResolveMemoriesListProperty();
        private static readonly FieldInfo MemoryHandlerMemoriesListField = MemoryHandlerMemoriesListProperty == null ? ResolveMemoriesListField() : null;
        private static readonly MethodInfo MemoryHandlerMemoriesListMethod = MemoryHandlerMemoriesListProperty == null && MemoryHandlerMemoriesListField == null ? ResolveMemoriesListMethod() : null;

        private readonly DynamicObjectInstance instance;
        private readonly DynamicPawnStateMachineDef definition;
        private readonly Pawn pawn;

        private string currentStateId;
        private int? currentTrackIndex;
        private string forcedStateId;

        private int currentAnimationPriority = int.MinValue;
        private bool currentStateFromVerb;
        private ISpineTrackEntryAdapter currentTrackEntry;
        private bool waitingForAnimationCompletion;

        private string lastJobDefName;
        private int lastJobStageIndex = -1;
        private string lastJobStageLabel;

        private string lastVerbIdentifier;
        private string lastVerbAbility;
        private bool needsRefresh = true;
        private bool disposed;
        private string currentSkin;

        public DynamicPawnStateController(DynamicObjectInstance instance, Pawn pawn, DynamicPawnStateMachineDef definition)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.pawn = pawn ?? throw new ArgumentNullException(nameof(pawn));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));

            instance.AttachPawn(pawn);
            instance.AttachStateController(this);
            instance.ApplyPawnSkeletonSettings(definition?.skeleton);
        }

        public DynamicObjectInstance Instance => instance;

        public DynamicPawnStateMachineDef Definition => definition;

        public Pawn Pawn => pawn;

        public bool HideVanillaPawn => definition?.hideVanillaPawn == true;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            instance.DetachPawn(this.Pawn);
            instance.DetachStateController(this);
            instance.ClearPawnSkeletonSettings();
            DynamicPawnStateRegistry.NotifyControllerDisposed(this);
        }

        public void RefreshNow()
        {
            needsRefresh = true;
            Tick();
        }

        public void Tick()
        {
            if (disposed)
            {
                return;
            }

            if (pawn == null || pawn.DestroyedOrNull())
            {
                Dispose();
                return;
            }

            if (!needsRefresh)
            {
                return;
            }

            needsRefresh = false;
            EvaluateAndApply();

        }

        public void NotifyJobUpdated(Job job, int stageIndex, string stageLabel)
        {
            if (disposed)
            {
                return;
            }

            string newJobDef = job?.def?.defName;
            int newStageIndex = stageIndex;
            string newStageLabel = stageLabel;

            if (lastJobDefName == newJobDef && lastJobStageIndex == newStageIndex && lastJobStageLabel == newStageLabel)
            {
                return;
            }

            lastJobDefName = newJobDef;
            lastJobStageIndex = newStageIndex;
            lastJobStageLabel = newStageLabel;
            RequestRefresh();
        }

        public void NotifyJobEnded()
        {
            if (disposed)
            {
                return;
            }

            if (lastJobDefName == null && lastJobStageIndex == -1 && lastJobStageLabel == null)
            {
                return;
            }

            lastJobDefName = null;
            lastJobStageIndex = -1;
            lastJobStageLabel = null;
            RequestRefresh();
        }

        public void NotifyVerbUsed(Verb verb)
        {
            if (disposed)
            {
                return;
            }

            lastVerbIdentifier = ResolveVerbSource(verb, out string abilityDef);
            lastVerbAbility = abilityDef;
            RequestRefresh();
        }

        public void NotifyNeedsChanged()
        {
            RequestRefresh();
        }

        public void NotifyHediffsChanged()
        {
            RequestRefresh();
        }

        public void NotifyThoughtsChanged()
        {
            RequestRefresh();
        }

        public void TriggerInteraction()
        {
            if (!string.IsNullOrEmpty(definition.interactionStateId))
            {
                forcedStateId = definition.interactionStateId;
            }

            RequestRefresh();
        }

        private void RequestRefresh()
        {
            needsRefresh = true;
        }

        private void EvaluateAndApply()
        {
            if (!TryEnsureSkeleton(out ISpineRuntimeAdapter adapter))
            {
                return;
            }

            DynamicPawnStateMachineDef.PawnAnimationState state = SelectState(out bool isForced);
            if (state == null)
            {
                return;
            }

            if (waitingForAnimationCompletion && !isForced && state.priority < currentAnimationPriority)
            {
                return;
            }

            bool shouldReplay = isForced || state.forceRestart || !string.Equals(currentStateId, state.stateId, StringComparison.OrdinalIgnoreCase);
            if (!shouldReplay)
            {
                return;
            }

            ApplyState(adapter, state, isForced);
        }

        private DynamicPawnStateMachineDef.PawnAnimationState SelectState(out bool isForced)
        {
            isForced = false;

            if (!string.IsNullOrEmpty(forcedStateId))
            {
                DynamicPawnStateMachineDef.PawnAnimationState forced = definition.states?.FirstOrDefault(s => s != null && string.Equals(s.stateId, forcedStateId, StringComparison.OrdinalIgnoreCase));
                if (forced != null)
                {
                    isForced = true;
                    return forced;
                }
            }

            IEnumerable<DynamicPawnStateMachineDef.PawnAnimationState> states = definition.states?.Where(s => s != null);
            if (states == null)
            {
                return null;
            }

            DynamicPawnStateMachineDef.PawnAnimationState fallback = null;
            foreach (DynamicPawnStateMachineDef.PawnAnimationState state in states.OrderByDescending(s => s.priority))
            {
                if (state.isFallback)
                {
                    if (fallback == null)
                    {
                        fallback = state;
                    }
                    continue;
                }

                if (state.triggers.NullOrEmpty())
                {
                    return state;
                }

                if (state.triggers.All(MatchesTrigger))
                {
                    return state;
                }
            }

            if (!string.IsNullOrEmpty(definition.defaultStateId))
            {
                DynamicPawnStateMachineDef.PawnAnimationState defaultState = states.FirstOrDefault(s => string.Equals(s.stateId, definition.defaultStateId, StringComparison.OrdinalIgnoreCase));
                if (defaultState != null)
                {
                    return defaultState;
                }
            }

            return fallback;
        }

        private bool MatchesTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            if (trigger == null)
            {
                return true;
            }

            switch (trigger.source)
            {
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Job:
                    return MatchesJobTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Verb:
                    return MatchesVerbTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Need:
                    return MatchesNeedTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Hediff:
                    return MatchesHediffTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Thought:
                    return MatchesThoughtTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Duty:
                    return MatchesDutyTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.MentalState:
                    return MatchesMentalStateTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Movement:
                    return MatchesMovementTrigger(trigger);
                default:
                    return false;
            }
        }

        private bool MatchesJobTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (!string.IsNullOrEmpty(target))
            {
                if (!string.Equals(lastJobDefName, target, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (trigger.stageIndex >= 0 && lastJobStageIndex != trigger.stageIndex)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(trigger.stageName))
            {
                if (string.IsNullOrEmpty(lastJobStageLabel) || !string.Equals(lastJobStageLabel, trigger.stageName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesVerbTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(lastVerbAbility) && string.Equals(lastVerbAbility, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(lastVerbIdentifier) && string.Equals(lastVerbIdentifier, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private bool MatchesNeedTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            Need need = pawn.needs?.AllNeeds?.FirstOrDefault(n => string.Equals(n.def?.defName, target, StringComparison.OrdinalIgnoreCase));
            if (need == null)
            {
                return false;
            }

            if (!float.IsNaN(trigger.threshold))
            {
                float value = need.CurLevel;
                bool comparison = trigger.thresholdGreaterOrEqual ? value >= trigger.threshold : value <= trigger.threshold;
                if (!comparison)
                {
                    return false;
                }
            }

            bool categoryAvailable = TryGetNeedCategory(need, out object categoryValue);

            if (trigger.stageIndex >= 0)
            {
                bool? stageIndexMatches = null;

                if (categoryAvailable)
                {
                    stageIndexMatches = MatchNeedStageIndex(need, categoryValue, trigger.stageIndex);
                }

                if (stageIndexMatches == null)
                {
                    stageIndexMatches = MatchNeedStageIndexFromDef(need.def, trigger.stageIndex);
                }

                if (stageIndexMatches == false)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(trigger.stageName))
            {
                bool? stageNameMatches = null;

                if (categoryAvailable)
                {
                    stageNameMatches = MatchNeedStageName(need, categoryValue, trigger.stageName);
                }

                if (stageNameMatches == null)
                {
                    stageNameMatches = MatchNeedStageNameFromDef(need.def, trigger.stageName);
                }

                if (stageNameMatches == false)
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesHediffTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            Hediff hediff = pawn.health?.hediffSet?.hediffs?.FirstOrDefault(h => string.Equals(h.def?.defName, target, StringComparison.OrdinalIgnoreCase));
            if (hediff == null)
            {
                return false;
            }

            if (!float.IsNaN(trigger.threshold))
            {
                float severity = hediff.Severity;
                bool comparison = trigger.thresholdGreaterOrEqual ? severity >= trigger.threshold : severity <= trigger.threshold;
                if (!comparison)
                {
                    return false;
                }
            }

            if (trigger.stageIndex >= 0 && hediff.CurStageIndex != trigger.stageIndex)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(trigger.stageName))
            {
                string label = hediff.CurStage?.label;
                if (string.IsNullOrEmpty(label) || !string.Equals(label, trigger.stageName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesThoughtTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            ThoughtHandler handler = pawn.needs?.mood?.thoughts;
            IEnumerable memories = GetMemoriesForReading(handler);
            if (memories == null)
            {
                return false;
            }

            foreach (object memoryObject in memories)
            {
                Thought_Memory memory = memoryObject as Thought_Memory;
                if (memory?.def == null)
                {
                    continue;
                }

                if (!string.Equals(memory.def.defName, target, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (trigger.stageIndex >= 0 && memory.CurStageIndex != trigger.stageIndex)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(trigger.stageName))
                {
                    string label = memory.CurStage?.label;
                    if (string.IsNullOrEmpty(label) || !string.Equals(label, trigger.stageName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        private static IEnumerable GetMemoriesForReading(ThoughtHandler handler)
        {
            if (handler == null)
            {
                return null;
            }

            object memories = null;
            try
            {
                if (ThoughtHandlerMemoriesProperty != null)
                {
                    memories = ThoughtHandlerMemoriesProperty.GetValue(handler);
                }
                else if (ThoughtHandlerMemoriesField != null)
                {
                    memories = ThoughtHandlerMemoriesField.GetValue(handler);
                }
            }
            catch
            {
                return null;
            }

            if (memories == null)
            {
                return null;
            }

            object list = null;
            try
            {
                if (MemoryHandlerMemoriesListProperty != null)
                {
                    list = MemoryHandlerMemoriesListProperty.GetValue(memories);
                }
                else if (MemoryHandlerMemoriesListField != null)
                {
                    list = MemoryHandlerMemoriesListField.GetValue(memories);
                }
                else if (MemoryHandlerMemoriesListMethod != null)
                {
                    list = MemoryHandlerMemoriesListMethod.Invoke(memories, Array.Empty<object>());
                }
            }
            catch
            {
                return null;
            }

            if (list is IEnumerable enumerable)
            {
                return enumerable;
            }

            return memories as IEnumerable;
        }

        private bool MatchesDutyTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            PawnDuty duty = pawn.mindState?.duty;
            DutyDef dutyDef = duty?.def;
            if (dutyDef == null)
            {
                return false;
            }

            return string.Equals(dutyDef.defName, target, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesMentalStateTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            if (!pawn.InMentalState)
            {
                return false;
            }

            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return true;
            }

            MentalState mentalState = pawn.MentalState;
            if (mentalState?.def == null)
            {
                return false;
            }

            return string.Equals(mentalState.def.defName, target, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesMovementTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            bool isMoving = pawn?.pather?.Moving == true;
            if (!trigger.isMoving.HasValue)
            {
                return !isMoving;
            }

            return isMoving == trigger.isMoving.Value;
        }

        private static bool TryGetNeedCategory(Need need, out object categoryValue)
        {
            categoryValue = null;
            if (need == null)
            {
                return false;
            }

            PropertyInfo[] properties =
            {
                NeedCurCategoryProperty,
                NeedCurLevelCategoryProperty
            };

            foreach (PropertyInfo property in properties)
            {
                if (property == null)
                {
                    continue;
                }

                try
                {
                    categoryValue = property.GetValue(need);
                    return true;
                }
                catch
                {
                    // Continue trying other potential category properties.
                }
            }

            return false;
        }

        private static bool? MatchNeedStageIndex(Need need, object categoryValue, int stageIndex)
        {
            if (categoryValue == null)
            {
                return null;
            }

            int? directIndex = ConvertCategoryToInt(categoryValue);
            if (directIndex.HasValue)
            {
                return directIndex.Value == stageIndex;
            }

            NeedDef needDef = need?.def;
            if (needDef != null)
            {
                IList categories = GetNeedDefCategories(needDef);
                if (categories != null && categories.Count > 0)
                {
                    int index = FindIndex(categories, categoryValue);
                    if (index >= 0)
                    {
                        return index == stageIndex;
                    }
                }

                IList stages = GetNeedDefStages(needDef);
                if (stages != null && stages.Count > 0)
                {
                    int index = FindIndex(stages, categoryValue);
                    if (index >= 0)
                    {
                        return index == stageIndex;
                    }
                }
            }

            return null;
        }

        private static bool? MatchNeedStageIndexFromDef(NeedDef needDef, int stageIndex)
        {
            if (needDef == null)
            {
                return null;
            }

            IList categories = GetNeedDefCategories(needDef);
            if (categories != null)
            {
                if (categories.Count == 0)
                {
                    return null;
                }

                return stageIndex >= 0 && stageIndex < categories.Count;
            }

            IList stages = GetNeedDefStages(needDef);
            if (stages != null)
            {
                if (stages.Count == 0)
                {
                    return null;
                }

                return stageIndex >= 0 && stageIndex < stages.Count;
            }

            return null;
        }

        private static bool? MatchNeedStageName(Need need, object categoryValue, string stageName)
        {
            if (categoryValue == null)
            {
                return null;
            }

            string resolvedName = ExtractCategoryName(categoryValue);
            if (!string.IsNullOrEmpty(resolvedName))
            {
                return string.Equals(resolvedName, stageName, StringComparison.OrdinalIgnoreCase);
            }

            NeedDef needDef = need?.def;
            if (needDef != null)
            {
                IList categories = GetNeedDefCategories(needDef);
                if (categories != null && categories.Count > 0)
                {
                    int index = FindIndex(categories, categoryValue);
                    if (index >= 0)
                    {
                        string name = ExtractCategoryName(categories[index]);
                        if (!string.IsNullOrEmpty(name))
                        {
                            return string.Equals(name, stageName, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                IList stages = GetNeedDefStages(needDef);
                if (stages != null && stages.Count > 0)
                {
                    int index = FindIndex(stages, categoryValue);
                    if (index >= 0)
                    {
                        string name = ExtractCategoryName(stages[index]);
                        if (!string.IsNullOrEmpty(name))
                        {
                            return string.Equals(name, stageName, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }

            string fallback = categoryValue.ToString();
            if (!string.IsNullOrEmpty(fallback))
            {
                return string.Equals(fallback, stageName, StringComparison.OrdinalIgnoreCase);
            }

            return null;
        }

        private static bool? MatchNeedStageNameFromDef(NeedDef needDef, string stageName)
        {
            if (needDef == null)
            {
                return null;
            }

            IList categories = GetNeedDefCategories(needDef);
            if (categories != null)
            {
                if (categories.Count == 0)
                {
                    return null;
                }

                foreach (object category in categories)
                {
                    if (CategoryMatchesName(category, stageName))
                    {
                        return true;
                    }
                }

                return false;
            }

            IList stages = GetNeedDefStages(needDef);
            if (stages != null)
            {
                if (stages.Count == 0)
                {
                    return null;
                }

                foreach (object stage in stages)
                {
                    if (CategoryMatchesName(stage, stageName))
                    {
                        return true;
                    }
                }

                return false;
            }

            return null;
        }

        private static int? ConvertCategoryToInt(object categoryValue)
        {
            if (categoryValue == null)
            {
                return null;
            }

            if (categoryValue is int intValue)
            {
                return intValue;
            }

            if (categoryValue is Enum enumValue)
            {
                try
                {
                    return Convert.ToInt32(enumValue);
                }
                catch
                {
                    return null;
                }
            }

            if (categoryValue is IConvertible convertible)
            {
                try
                {
                    return convertible.ToInt32(null);
                }
                catch
                {
                    // Ignore and try parsing the string representation instead.
                }
            }

            if (int.TryParse(categoryValue.ToString(), out int parsed))
            {
                return parsed;
            }

            return null;
        }

        private static IList GetNeedDefCategories(NeedDef needDef)
        {
            return GetListFromMember(NeedDefCategoriesMember, needDef);
        }

        private static IList GetNeedDefStages(NeedDef needDef)
        {
            return GetListFromMember(NeedDefStagesMember, needDef);
        }

        private static PropertyInfo ResolveMemoriesListProperty()
        {
            if (ThoughtHandlerMemoriesType == null)
            {
                return null;
            }

            return AccessTools.Property(ThoughtHandlerMemoriesType, "MemoriesListForReading");
        }

        private static FieldInfo ResolveMemoriesListField()
        {
            if (ThoughtHandlerMemoriesType == null)
            {
                return null;
            }

            return AccessTools.Field(ThoughtHandlerMemoriesType, "MemoriesListForReading");
        }

        private static MethodInfo ResolveMemoriesListMethod()
        {
            if (ThoughtHandlerMemoriesType == null)
            {
                return null;
            }

            return AccessTools.Method(ThoughtHandlerMemoriesType, "MemoriesListForReading")
                   ?? AccessTools.Method(ThoughtHandlerMemoriesType, "get_MemoriesListForReading");
        }

        private static MemberInfo ResolveNeedDefMember(IEnumerable<string> names)
        {
            if (names == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                PropertyInfo property = AccessTools.Property(typeof(NeedDef), name);
                if (property != null)
                {
                    return property;
                }

                FieldInfo field = AccessTools.Field(typeof(NeedDef), name);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static IList GetListFromMember(MemberInfo member, object instance)
        {
            if (member == null || instance == null)
            {
                return null;
            }

            try
            {
                object value = null;
                if (member is FieldInfo field)
                {
                    value = field.GetValue(instance);
                }
                else if (member is PropertyInfo property)
                {
                    value = property.GetValue(instance);
                }

                if (value is IList list)
                {
                    return list;
                }

                if (value is IEnumerable enumerable)
                {
                    List<object> buffer = new List<object>();
                    foreach (object item in enumerable)
                    {
                        buffer.Add(item);
                    }

                    return buffer;
                }
            }
            catch
            {
                // Ignore reflection failures and fall back to other strategies.
            }

            return null;
        }

        private static int FindIndex(IList list, object value)
        {
            if (list == null)
            {
                return -1;
            }

            for (int i = 0; i < list.Count; i++)
            {
                object candidate = list[i];
                if (CategoryValuesEqual(candidate, value))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool CategoryValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (Equals(left, right))
            {
                return true;
            }

            string leftName = ExtractCategoryName(left);
            string rightName = ExtractCategoryName(right);
            if (!string.IsNullOrEmpty(leftName) && !string.IsNullOrEmpty(rightName))
            {
                return string.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool CategoryMatchesName(object value, string stageName)
        {
            if (string.IsNullOrEmpty(stageName))
            {
                return false;
            }

            string resolved = ExtractCategoryName(value);
            return !string.IsNullOrEmpty(resolved) && string.Equals(resolved, stageName, StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractCategoryName(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is string strValue && !string.IsNullOrEmpty(strValue))
            {
                return strValue;
            }

            if (value is Def defValue)
            {
                if (!string.IsNullOrEmpty(defValue.defName))
                {
                    return defValue.defName;
                }

                if (!string.IsNullOrEmpty(defValue.label))
                {
                    return defValue.label;
                }

                string labelCap = defValue.LabelCap;
                if (!string.IsNullOrEmpty(labelCap))
                {
                    return labelCap;
                }
            }

            Type type = value.GetType();

            PropertyInfo property = AccessTools.Property(type, "label") ?? AccessTools.Property(type, "Label");
            if (property != null)
            {
                try
                {
                    object propertyValue = property.GetValue(value);
                    if (propertyValue is string label && !string.IsNullOrEmpty(label))
                    {
                        return label;
                    }
                }
                catch
                {
                    // Ignore and continue to other options.
                }
            }

            PropertyInfo labelCapProperty = AccessTools.Property(type, "LabelCap") ?? AccessTools.Property(type, "labelCap");
            if (labelCapProperty != null)
            {
                try
                {
                    object propertyValue = labelCapProperty.GetValue(value);
                    if (propertyValue is string labelCap && !string.IsNullOrEmpty(labelCap))
                    {
                        return labelCap;
                    }

                    if (propertyValue != null)
                    {
                        string converted = propertyValue.ToString();
                        if (!string.IsNullOrEmpty(converted))
                        {
                            return converted;
                        }
                    }
                }
                catch
                {
                    // Ignore and continue to other options.
                }
            }

            PropertyInfo defNameProperty = AccessTools.Property(type, "defName");
            if (defNameProperty != null)
            {
                try
                {
                    object propertyValue = defNameProperty.GetValue(value);
                    if (propertyValue is string defName && !string.IsNullOrEmpty(defName))
                    {
                        return defName;
                    }
                }
                catch
                {
                    // Ignore and fall back to the string representation.
                }
            }

            FieldInfo labelField = AccessTools.Field(type, "label");
            if (labelField != null)
            {
                try
                {
                    object fieldValue = labelField.GetValue(value);
                    if (fieldValue is string fieldLabel && !string.IsNullOrEmpty(fieldLabel))
                    {
                        return fieldLabel;
                    }
                }
                catch
                {
                    // Ignore and continue to the next option.
                }
            }

            FieldInfo defNameField = AccessTools.Field(type, "defName");
            if (defNameField != null)
            {
                try
                {
                    object fieldValue = defNameField.GetValue(value);
                    if (fieldValue is string fieldDefName && !string.IsNullOrEmpty(fieldDefName))
                    {
                        return fieldDefName;
                    }
                }
                catch
                {
                    // Ignore and continue.
                }
            }

            string text = value.ToString();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private void ApplyState(ISpineRuntimeAdapter adapter, DynamicPawnStateMachineDef.PawnAnimationState state, bool isForced)
        {
            if (string.IsNullOrEmpty(state.animationName))
            {
                Log.Warning($"[RimSpine2D] State '{state.stateId}' on '{definition.defName}' has no animationName.");
                return;
            }

            ISpineAnimationStateAdapter animationState = adapter.GetAnimationState(instance);
            if (animationState == null)
            {
                Log.Warning($"[RimSpine2D] Unable to resolve animation state for '{definition.defName}'.");
                return;
            }

            if (currentTrackIndex.HasValue && currentTrackIndex.Value != state.trackIndex)
            {
                animationState.SetEmptyAnimation(currentTrackIndex.Value, state.clearMixDuration);
            }

            if (state.clearTrack)
            {
                animationState.SetEmptyAnimation(state.trackIndex, state.clearMixDuration);
            }

            ISpineTrackEntryAdapter entry = state.useQueue
                ? animationState.AddAnimation(state.trackIndex, state.animationName, state.loop, state.delay)
                : animationState.SetAnimation(state.trackIndex, state.animationName, state.loop);

            if (entry == null)
            {
                Log.Warning($"[RimSpine2D] Failed to play animation '{state.animationName}' for state '{state.stateId}' on '{definition.defName}'.");
                return;
            }

            if (state.mixDuration > 0f)
            {
                entry.SetMixDuration(state.mixDuration);
            }

            string targetSkin = state.skin;
            if (targetSkin.NullOrEmpty())
            {
                targetSkin = instance.GetDefaultSkinName();
            }

            if (!targetSkin.NullOrEmpty() && !string.Equals(currentSkin, targetSkin, StringComparison.OrdinalIgnoreCase))
            {
                adapter.SetSkin(instance, targetSkin);
                currentSkin = targetSkin;
            }

            currentStateId = state.stateId;
            currentTrackIndex = state.trackIndex;
            currentAnimationPriority = state.priority;

            bool newStateFromVerb = StateHasVerbTrigger(state);
            if (!newStateFromVerb && currentStateFromVerb)
            {
                ClearVerbTrigger();
            }

            currentStateFromVerb = newStateFromVerb;

            bool shouldWait = ShouldWaitForCompletion(state);
            currentTrackEntry = entry;
            waitingForAnimationCompletion = shouldWait;
            entry.OnComplete(() => OnTrackEntryComplete(entry));

            if (!shouldWait)
            {
                currentTrackEntry = null;
                if (currentStateFromVerb)
                {
                    ClearVerbTrigger();
                }
            }

            if (isForced)
            {
                forcedStateId = null;
            }
        }

        private static bool ShouldWaitForCompletion(DynamicPawnStateMachineDef.PawnAnimationState state)
        {
            return state != null && !state.loop;
        }

        private static bool StateHasVerbTrigger(DynamicPawnStateMachineDef.PawnAnimationState state)
        {
            if (state?.triggers == null)
            {
                return false;
            }

            return state.triggers.Any(trigger => trigger != null && trigger.source == DynamicPawnStateMachineDef.PawnStateTriggerSource.Verb);
        }

        private void OnTrackEntryComplete(ISpineTrackEntryAdapter entry)
        {
            if (entry == null || !ReferenceEquals(entry, currentTrackEntry))
            {
                return;
            }

            currentTrackEntry = null;
            waitingForAnimationCompletion = false;

            if (currentStateFromVerb)
            {
                ClearVerbTrigger();
            }

            RequestRefresh();
        }

        private void ClearVerbTrigger()
        {
            lastVerbIdentifier = null;
            lastVerbAbility = null;
            currentStateFromVerb = false;
        }

        private bool TryEnsureSkeleton(out ISpineRuntimeAdapter adapter)
        {
            adapter = null;
            if (!instance.TryGetSpineAdapter(out adapter))
            {
                Log.Warning($"[RimSpine2D] Unable to resolve Spine adapter for instance '{instance.name ?? instance.gameObject?.name}'.");
                return false;
            }

            if (!adapter.HasSkeleton(instance))
            {
                adapter.EnsureSkeleton(instance);
                currentSkin = instance.GetDefaultSkinName();
            }
            else if (currentSkin.NullOrEmpty())
            {
                currentSkin = instance.GetDefaultSkinName();
            }

            return true;
        }

        private static string ResolveTriggerDefName(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            return trigger.def?.defName ?? trigger.defName;
        }

        private static string ResolveVerbSource(Verb verb, out string abilityDef)
        {
            abilityDef = null;
            if (verb == null)
            {
                return null;
            }

            /*if (verb is Verb_CastAbility abilityVerb && abilityVerb.ability?.def != null)
            {
                abilityDef = abilityVerb.ability.def.defName;
                return abilityDef;
            }

            if (verb.EquipmentSource != null && verb.EquipmentSource.def != null)
            {
                return verb.EquipmentSource.def.defName;
            }

            if (verb.HediffCompSource != null && verb.HediffCompSource.parent?.def != null)
            {
                return verb.HediffCompSource.parent.def.defName;
            }*/

            return verb.GetType().Name;
        }
    }
}
