#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;


namespace Gorge.GorgeCompiler.ProgressMerger
{
    public class ParallelProgressMerger
    {
        private readonly IProgress<float> _parentProgress;
        private readonly ConcurrentDictionary<int, float> _childProgresses;
        private readonly ConcurrentDictionary<int, float> _childWeights;
        private readonly object _lockObject = new();
        private int _nextChildId = 0;

        public ParallelProgressMerger(IProgress<float> parentProgress)
        {
            _parentProgress = parentProgress ?? throw new ArgumentNullException(nameof(parentProgress));
            _childProgresses = new ConcurrentDictionary<int, float>();
            _childWeights = new ConcurrentDictionary<int, float>();
        }

        public IProgress<float> CreateChildProgress(float weight = 1)
        {
            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "权重必须大于0");
            }

            var childId = Interlocked.Increment(ref _nextChildId);
            _childProgresses[childId] = 0;
            _childWeights[childId] = weight;

            return new ChildProgress(this, childId);
        }

        private void UpdateChildProgress(int childId, float progress)
        {
            _childProgresses[childId] = Math.Max(0, Math.Min(1, progress));
            UpdateParentProgress();
        }

        private void UpdateParentProgress()
        {
            lock (_lockObject)
            {
                if (_childProgresses.IsEmpty)
                {
                    _parentProgress.Report(0);
                    return;
                }

                var totalWeightedProgress = 0f;
                var totalWeight = 0f;

                foreach (var (childId, progress) in _childProgresses)
                {
                    if (_childWeights.TryGetValue(childId, out var weight))
                    {
                        totalWeightedProgress += progress * weight;
                        totalWeight += weight;
                    }
                }

                var overallProgress = totalWeight > 0 ? totalWeightedProgress / totalWeight : 0.0f;
                _parentProgress.Report(overallProgress);
            }
        }

        private class ChildProgress : IProgress<float>
        {
            private readonly ParallelProgressMerger _merger;
            private readonly int _childId;

            public ChildProgress(ParallelProgressMerger merger, int childId)
            {
                _merger = merger;
                _childId = childId;
            }

            public void Report(float value)
            {
                _merger.UpdateChildProgress(_childId, value);
            }
        }
    }
}