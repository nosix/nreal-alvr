using UnityEngine;

namespace Alvr
{
    public class HeadPoseHistory
    {
        private struct Entry
        {
            public long FrameIndex;
            public Pose HeadPose;
        }

        private readonly Entry[] _history = new Entry[64];

        public void Add(long frameIndex, Pose headPose)
        {
            _history[frameIndex % _history.Length] = new Entry
            {
                FrameIndex = frameIndex,
                HeadPose = headPose
            };
        }

        public bool Has(long frameIndex)
        {
            return _history[frameIndex % _history.Length].FrameIndex == frameIndex;
        }

        public Pose Get(long frameIndex)
        {
            return _history[frameIndex % _history.Length].HeadPose;
        }
    }
}