using System;

namespace RimSpine2DFramework
{
    public interface ISpineRuntimeAdapter
    {
        string Version { get; }

        bool HasSkeleton(DynamicObjectInstance instance);

        void EnsureSkeleton(DynamicObjectInstance instance);

        ISpineAnimationStateAdapter GetAnimationState(DynamicObjectInstance instance);

        void SetSkin(DynamicObjectInstance instance, string skinName);
    }

    public interface ISpineAnimationStateAdapter
    {
        ISpineTrackEntryAdapter SetAnimation(int trackIndex, string animationName, bool loop);

        ISpineTrackEntryAdapter AddAnimation(int trackIndex, string animationName, bool loop, float delay);

        ISpineTrackEntryAdapter SetEmptyAnimation(int trackIndex, float mixDuration);

        ISpineTrackEntryAdapter AddEmptyAnimation(int trackIndex, float mixDuration, float delay);
    }

    public interface ISpineTrackEntryAdapter
    {
        string AnimationName { get; }

        void OnComplete(Action action);

        void SetMixDuration(float mixDuration);
    }
}
