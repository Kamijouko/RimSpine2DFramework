using System;

namespace Verse.AI
{
	// Token: 0x0200100B RID: 4107
	public class ThinkNode_Subtree : ThinkNode
	{
		// Token: 0x060064DB RID: 25819 RVA: 0x001FEF90 File Offset: 0x001FD190
		public override ThinkNode DeepCopy(bool resolve = true)
		{
			ThinkNode_Subtree thinkNode_Subtree = (ThinkNode_Subtree)base.DeepCopy(false);
			thinkNode_Subtree.treeDef = this.treeDef;
			if (resolve)
			{
				thinkNode_Subtree.ResolveSubnodesAndRecur();
				thinkNode_Subtree.subtreeNode = thinkNode_Subtree.subNodes[this.subNodes.IndexOf(this.subtreeNode)];
			}
			return thinkNode_Subtree;
		}

		// Token: 0x060064DC RID: 25820 RVA: 0x001FEFE2 File Offset: 0x001FD1E2
		protected override void ResolveSubnodes()
		{
			this.subtreeNode = this.treeDef.thinkRoot.DeepCopy(true);
			this.subNodes.Add(this.subtreeNode);
		}

		// Token: 0x060064DD RID: 25821 RVA: 0x001FF00C File Offset: 0x001FD20C
		public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
		{
			return this.subtreeNode.TryIssueJobPackage(pawn, jobParams);
		}

		// Token: 0x04004843 RID: 18499
		private ThinkTreeDef treeDef;

		// Token: 0x04004844 RID: 18500
		[Unsaved(false)]
		public ThinkNode subtreeNode;
	}
}
