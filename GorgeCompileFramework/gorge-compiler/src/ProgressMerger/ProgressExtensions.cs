#nullable enable
using System;

namespace Gorge.GorgeCompiler.ProgressMerger
{
    public static class ProgressExtensions
    {
        public static ParallelProgressMerger ParallelMerger(this IProgress<float> parentProgress)
        {
            return new ParallelProgressMerger(parentProgress);
        }
    }
}