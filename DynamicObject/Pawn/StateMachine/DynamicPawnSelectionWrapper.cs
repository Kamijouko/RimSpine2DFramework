using System;
using System.Runtime.CompilerServices;
using Verse;

namespace RimSpine2DFramework
{
    /// <summary>
    /// Wraps a pawn instance in an object that can be queried when deciding
    /// whether the pawn should be selectable through mouse targeting.
    /// </summary>
    internal sealed class DynamicPawnSelectionWrapper
    {
        private static readonly ConditionalWeakTable<Pawn, DynamicPawnSelectionWrapper> Wrappers = new ConditionalWeakTable<Pawn, DynamicPawnSelectionWrapper>();

        private Pawn pawn;
        private bool blockMouseTargeting;

        private DynamicPawnSelectionWrapper(Pawn pawn)
        {
            this.pawn = pawn ?? throw new ArgumentNullException(nameof(pawn));
        }

        public Pawn Pawn => pawn;

        public bool BlockMouseTargeting
        {
            get => blockMouseTargeting;
            set => blockMouseTargeting = value;
        }

        public static DynamicPawnSelectionWrapper Wrap(Pawn pawn)
        {
            if (pawn == null)
            {
                throw new ArgumentNullException(nameof(pawn));
            }

            return Wrappers.GetValue(pawn, p => new DynamicPawnSelectionWrapper(p));
        }

        public static bool ShouldBlock(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (Wrappers.TryGetValue(pawn, out DynamicPawnSelectionWrapper wrapper))
            {
                return wrapper.blockMouseTargeting;
            }

            return false;
        }

        public static void Clear(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            Wrappers.Remove(pawn);
        }
    }
}
