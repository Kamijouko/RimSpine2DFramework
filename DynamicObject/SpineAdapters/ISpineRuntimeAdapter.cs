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

        void OnEvent(Action<SpineEventArgs> action);

        void SetMixDuration(float mixDuration);
    }

    public sealed class SpineEventArgs
    {
        public SpineEventArgs(string name, float time, int intValue, float floatValue, string stringValue, float? volume, float? balance)
        {
            Name = name;
            Time = time;
            IntValue = intValue;
            FloatValue = floatValue;
            StringValue = stringValue;
            Volume = volume;
            Balance = balance;
        }

        public string Name { get; }

        public float Time { get; }

        public int IntValue { get; }

        public float FloatValue { get; }

        public string StringValue { get; }

        public float? Volume { get; }

        public float? Balance { get; }
    }
}
