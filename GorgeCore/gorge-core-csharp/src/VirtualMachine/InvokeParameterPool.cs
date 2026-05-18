using Gorge.Native;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using System;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    public static class InvokeParameterPool
    {
        /// <summary>
        /// 参数池大小（编译期可计算，当前硬编码）
        /// </summary>
        public const int PoolSize = 256;

        #region 调用参数池

        public static int[] Int = new int[PoolSize];
        public static float[] Float = new float[PoolSize];
        public static bool[] Bool = new bool[PoolSize];
        public static string[] String = new string[PoolSize];
        public static GorgeObject[] Object = new GorgeObject[PoolSize];

        /// <summary>
        /// 构造方法传递Injector的专用位
        /// Injector作为参数传递使用Object，而不是用Injector位
        /// </summary>
        public static Injector Injector;

        #endregion

        #region 返回值池

        public static int IntReturn;
        public static float FloatReturn;
        public static bool BoolReturn;
        public static string StringReturn;
        public static GorgeObject ObjectReturn;

        #endregion

        #region Span 访问器（零分配快速访问）

        public static Span<int> IntSpan() => Int.AsSpan();
        public static Span<float> FloatSpan() => Float.AsSpan();
        public static Span<bool> BoolSpan() => Bool.AsSpan();
        public static Span<string> StringSpan() => String.AsSpan();
        public static Span<GorgeObject> ObjectSpan() => Object.AsSpan();

        #endregion
    }
}
