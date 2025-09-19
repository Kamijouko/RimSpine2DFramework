using System;

namespace RimSpine2DFramework
{
    public interface ISpineRuntimeAdapter
    {
        string Version { get; }

        bool HasSkeleton(DynamicObjectInstance instance);

        void EnsureSkeleton(DynamicObjectInstance instance);

        ISpineAnimationStateAdapter GetAnimationState(DynamicObjectInstance instance);
    }

    public interface ISpineAnimationStateAdapter
    {
        ISpineTrackEntryAdapter SetAnimation(int trackIndex, string animationName, bool loop);

        ISpineTrackEntryAdapter AddAnimation(int trackIndex, string animationName, bool loop, float delay);
    }

    public interface ISpineTrackEntryAdapter
    {
        string AnimationName { get; }

        void OnComplete(Action action);
    }
}
