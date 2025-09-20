using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld
{
	// Token: 0x02002545 RID: 9541
	public sealed class ThoughtHandler : IExposable
	{
		// Token: 0x0600D0CC RID: 53452 RVA: 0x003D6A25 File Offset: 0x003D4C25
		public ThoughtHandler(Pawn pawn)
		{
			this.pawn = pawn;
			this.memories = new MemoryThoughtHandler(pawn);
			this.situational = new SituationalThoughtHandler(pawn);
		}

		// Token: 0x0600D0CD RID: 53453 RVA: 0x003D6A4C File Offset: 0x003D4C4C
		public void ExposeData()
		{
			Scribe_Deep.Look<MemoryThoughtHandler>(ref this.memories, "memories", new object[] { this.pawn });
		}

		// Token: 0x0600D0CE RID: 53454 RVA: 0x003D6A6D File Offset: 0x003D4C6D
		public void ThoughtInterval()
		{
			this.situational.SituationalThoughtInterval();
			this.memories.MemoryThoughtInterval();
		}

		// Token: 0x0600D0CF RID: 53455 RVA: 0x003D6A88 File Offset: 0x003D4C88
		public void GetMoodThoughtsFor(ThoughtDef def, List<Thought> thoughts)
		{
			thoughts.Clear();
			for (int i = 0; i < this.memories.Memories.Count; i++)
			{
				Thought_Memory thought_Memory = this.memories.Memories[i];
				if (thought_Memory.def == def && thought_Memory.MoodOffset() != 0f)
				{
					thoughts.Add(thought_Memory);
				}
			}
			this.situational.AppendMoodThoughts(def, thoughts);
		}

		// Token: 0x0600D0D0 RID: 53456 RVA: 0x003D6AF4 File Offset: 0x003D4CF4
		public void GetAllMoodThoughts(List<Thought> outThoughts)
		{
			outThoughts.Clear();
			List<Thought_Memory> list = this.memories.Memories;
			for (int i = 0; i < list.Count; i++)
			{
				Thought_Memory thought_Memory = list[i];
				if (thought_Memory.MoodOffset() != 0f)
				{
					outThoughts.Add(thought_Memory);
				}
			}
			this.situational.AppendMoodThoughts(outThoughts);
		}

		// Token: 0x0600D0D1 RID: 53457 RVA: 0x003D6B4C File Offset: 0x003D4D4C
		public void GetMoodThoughts(Thought group, List<Thought> outThoughts)
		{
			this.GetAllMoodThoughts(outThoughts);
			for (int i = outThoughts.Count - 1; i >= 0; i--)
			{
				if (!outThoughts[i].GroupsWith(group))
				{
					outThoughts.RemoveAt(i);
				}
			}
		}

		// Token: 0x0600D0D2 RID: 53458 RVA: 0x003D6B8C File Offset: 0x003D4D8C
		public float MoodOffsetOfGroup(Thought group)
		{
			this.GetMoodThoughts(group, ThoughtHandler.tmpThoughts);
			if (!ThoughtHandler.tmpThoughts.Any<Thought>())
			{
				return 0f;
			}
			float num = 0f;
			float num2 = 1f;
			float num3 = 0f;
			for (int i = 0; i < ThoughtHandler.tmpThoughts.Count; i++)
			{
				Thought thought = ThoughtHandler.tmpThoughts[i];
				num += thought.MoodOffset();
				num3 += num2;
				num2 *= thought.def.stackedEffectMultiplier;
			}
			float num4 = num / (float)ThoughtHandler.tmpThoughts.Count;
			ThoughtHandler.tmpThoughts.Clear();
			return num4 * num3;
		}

		// Token: 0x0600D0D3 RID: 53459 RVA: 0x003D6C24 File Offset: 0x003D4E24
		public void GetDistinctMoodThoughtGroups(List<Thought> outThoughts)
		{
			this.GetAllMoodThoughts(outThoughts);
			for (int i = outThoughts.Count - 1; i >= 0; i--)
			{
				Thought thought = outThoughts[i];
				for (int j = 0; j < i; j++)
				{
					if (outThoughts[j].GroupsWith(thought))
					{
						outThoughts.RemoveAt(i);
						break;
					}
				}
			}
		}

		// Token: 0x0600D0D4 RID: 53460 RVA: 0x003D6C78 File Offset: 0x003D4E78
		public float TotalMoodOffset()
		{
			this.GetDistinctMoodThoughtGroups(ThoughtHandler.tmpTotalMoodOffsetThoughts);
			float num = 0f;
			for (int i = 0; i < ThoughtHandler.tmpTotalMoodOffsetThoughts.Count; i++)
			{
				num += this.MoodOffsetOfGroup(ThoughtHandler.tmpTotalMoodOffsetThoughts[i]);
			}
			ThoughtHandler.tmpTotalMoodOffsetThoughts.Clear();
			return num;
		}

		// Token: 0x0600D0D5 RID: 53461 RVA: 0x003D6CCC File Offset: 0x003D4ECC
		public void GetSocialThoughts(Pawn otherPawn, List<ISocialThought> outThoughts)
		{
			outThoughts.Clear();
			List<Thought_Memory> list = this.memories.Memories;
			for (int i = 0; i < list.Count; i++)
			{
				ISocialThought socialThought = list[i] as ISocialThought;
				if (socialThought != null && socialThought.OtherPawn() == otherPawn)
				{
					outThoughts.Add(socialThought);
				}
			}
			this.situational.AppendSocialThoughts(otherPawn, outThoughts);
		}

		// Token: 0x0600D0D6 RID: 53462 RVA: 0x003D6D2C File Offset: 0x003D4F2C
		public void GetSocialThoughts(Pawn otherPawn, ISocialThought group, List<ISocialThought> outThoughts)
		{
			this.GetSocialThoughts(otherPawn, outThoughts);
			for (int i = outThoughts.Count - 1; i >= 0; i--)
			{
				if (!((Thought)outThoughts[i]).GroupsWith((Thought)group))
				{
					outThoughts.RemoveAt(i);
				}
			}
		}

		// Token: 0x0600D0D7 RID: 53463 RVA: 0x003D6D74 File Offset: 0x003D4F74
		public int OpinionOffsetOfGroup(ISocialThought group, Pawn otherPawn)
		{
			this.GetSocialThoughts(otherPawn, group, ThoughtHandler.tmpSocialThoughts);
			for (int i = ThoughtHandler.tmpSocialThoughts.Count - 1; i >= 0; i--)
			{
				if (ThoughtHandler.tmpSocialThoughts[i].OpinionOffset() == 0f)
				{
					ThoughtHandler.tmpSocialThoughts.RemoveAt(i);
				}
			}
			if (!ThoughtHandler.tmpSocialThoughts.Any<ISocialThought>())
			{
				return 0;
			}
			ThoughtDef def = ((Thought)group).def;
			if (def.IsMemory && def.stackedEffectMultiplier != 1f)
			{
				ThoughtHandler.tmpSocialThoughts.Sort((ISocialThought a, ISocialThought b) => ((Thought_Memory)a).age.CompareTo(((Thought_Memory)b).age));
			}
			float num = 0f;
			float num2 = 1f;
			for (int j = 0; j < ThoughtHandler.tmpSocialThoughts.Count; j++)
			{
				num += ThoughtHandler.tmpSocialThoughts[j].OpinionOffset() * num2;
				num2 *= ((Thought)ThoughtHandler.tmpSocialThoughts[j]).def.stackedEffectMultiplier;
			}
			ThoughtHandler.tmpSocialThoughts.Clear();
			if (num == 0f)
			{
				return 0;
			}
			if (num > 0f)
			{
				return Mathf.Max(Mathf.RoundToInt(num), 1);
			}
			return Mathf.Min(Mathf.RoundToInt(num), -1);
		}

		// Token: 0x0600D0D8 RID: 53464 RVA: 0x003D6EB0 File Offset: 0x003D50B0
		public void GetDistinctSocialThoughtGroups(Pawn otherPawn, List<ISocialThought> outThoughts)
		{
			this.GetSocialThoughts(otherPawn, outThoughts);
			for (int i = outThoughts.Count - 1; i >= 0; i--)
			{
				ISocialThought socialThought = outThoughts[i];
				for (int j = 0; j < i; j++)
				{
					if (((Thought)outThoughts[j]).GroupsWith((Thought)socialThought))
					{
						outThoughts.RemoveAt(i);
						break;
					}
				}
			}
		}

		// Token: 0x0600D0D9 RID: 53465 RVA: 0x003D6F10 File Offset: 0x003D5110
		public int TotalOpinionOffset(Pawn otherPawn)
		{
			this.GetDistinctSocialThoughtGroups(otherPawn, ThoughtHandler.tmpTotalOpinionOffsetThoughts);
			int num = 0;
			for (int i = 0; i < ThoughtHandler.tmpTotalOpinionOffsetThoughts.Count; i++)
			{
				num += this.OpinionOffsetOfGroup(ThoughtHandler.tmpTotalOpinionOffsetThoughts[i], otherPawn);
			}
			ThoughtHandler.tmpTotalOpinionOffsetThoughts.Clear();
			return num;
		}

		// Token: 0x04008E2D RID: 36397
		public Pawn pawn;

		// Token: 0x04008E2E RID: 36398
		public MemoryThoughtHandler memories;

		// Token: 0x04008E2F RID: 36399
		public SituationalThoughtHandler situational;

		// Token: 0x04008E30 RID: 36400
		private static List<Thought> tmpThoughts = new List<Thought>();

		// Token: 0x04008E31 RID: 36401
		private static List<Thought> tmpTotalMoodOffsetThoughts = new List<Thought>();

		// Token: 0x04008E32 RID: 36402
		private static List<ISocialThought> tmpSocialThoughts = new List<ISocialThought>();

		// Token: 0x04008E33 RID: 36403
		private static List<ISocialThought> tmpTotalOpinionOffsetThoughts = new List<ISocialThought>();
	}
}
