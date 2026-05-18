using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 类成员编号和索引计数器
    /// </summary>
    public class ClassMemberCounter
    {
        /// <summary>
        /// 该计数器是否被冻结。
        /// 如果被冻结，则不再接受任何计数。
        /// </summary>
        public bool Frozen { get; private set; } = false;

        /// <summary>
        /// 字段索引计数
        /// </summary>
        public readonly TypeCount FieldIndex;

        /// <summary>
        /// 注入器字段索引计数
        /// </summary>
        public readonly TypeCount InjectorFieldIndex;

        /// <summary>
        /// 注入器字段默认值索引计数
        /// </summary>
        public readonly TypeCount InjectorFieldDefaultValueIndex;

        /// <summary>
        /// 字段编号
        /// </summary>
        public int FieldId;

        /// <summary>
        /// 注入器字段编号
        /// </summary>
        public int InjectorFieldId;

        /// <summary>
        /// 方法编号
        /// </summary>
        public int MethodId;

        /// <summary>
        /// 静态方法编号
        /// </summary>
        public int StaticMethodId;

        /// <summary>
        /// 构造方法编号
        /// </summary>
        public int ConstructorId;

        /// <summary>
        /// 注入器构造方法编号
        /// </summary>
        public int InjectorConstructorId;

        /// <summary>
        /// 构造计数器，初始化为0
        /// </summary>
        public ClassMemberCounter()
        {
            FieldIndex = new TypeCount();
            InjectorFieldIndex = new TypeCount();
            InjectorFieldDefaultValueIndex = new TypeCount();
            FieldId = 0;
            InjectorFieldId = 0;
            MethodId = 0;
            StaticMethodId = 0;
            ConstructorId = 0;
            InjectorConstructorId = 0;
        }

        /// <summary>
        /// 从现有计数器的基础上派生新计数器，初始化为原计数器的计数值
        /// </summary>
        /// <param name="superCounter">原计数器，必须已冻结</param>
        private ClassMemberCounter(ClassMemberCounter superCounter)
        {
            FieldIndex = new TypeCount(superCounter.FieldIndex);
            InjectorFieldIndex = new TypeCount(superCounter.InjectorFieldIndex);
            InjectorFieldDefaultValueIndex = new TypeCount(superCounter.InjectorFieldDefaultValueIndex);
            FieldId = superCounter.FieldId;
            InjectorFieldId = superCounter.InjectorFieldId;
            MethodId = superCounter.MethodId;
            StaticMethodId = superCounter.StaticMethodId;
            ConstructorId = superCounter.ConstructorId;
            InjectorConstructorId = superCounter.InjectorConstructorId;
        }

        /// <summary>
        /// 创建子计数器，从本计数器的计数结果上进一步计数。
        /// 必须冻结本计数器后才能创建子计数器，从而保证垂直方向ID不冲突。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="GorgeCompilerException"></exception>
        public ClassMemberCounter GetChildCounter()
        {
            if (!Frozen)
            {
                throw new GorgeCompilerException("尝试使用未冻结的计数器创建新计数器");
            }

            return new ClassMemberCounter(this);
        }

        /// <summary>
        /// 为字段分配编号和索引
        /// </summary>
        /// <param name="fieldType"></param>
        /// <param name="id"></param>
        /// <param name="index"></param>
        public void CountField(GorgeType fieldType, out int id, out int index)
        {
            id = FieldId++;
            index = FieldIndex.Count(fieldType.BasicType);
        }

        /// <summary>
        /// 为方法分配编号
        /// </summary>
        /// <param name="id"></param>
        public void CountMethod(out int id)
        {
            id = MethodId++;
        }

        /// <summary>
        /// 为静态方法分配编号
        /// </summary>
        /// <param name="id"></param>
        public void CountStaticMethod(out int id)
        {
            id = StaticMethodId++;
        }

        /// <summary>
        /// 为构造方法分配编号
        /// </summary>
        /// <param name="id"></param>
        public void CountConstructor(out int id)
        {
            id = ConstructorId++;
        }
        
        /// <summary>
        /// 为注入器构造方法分配编号
        /// </summary>
        /// <param name="id"></param>
        public void CountInjectorConstructor(out int id)
        {
            id = InjectorConstructorId++;
        }

        /// <summary>
        /// 为注入器字段分配编号和索引
        /// </summary>
        /// <param name="fieldType"></param>
        /// <param name="id"></param>
        /// <param name="index"></param>
        public void CountInjectorField(GorgeType fieldType, out int id, out int index)
        {
            id = InjectorFieldId++;
            index = InjectorFieldIndex.Count(fieldType.BasicType);
        }

        /// <summary>
        /// 为注入器字段默认值分配索引
        /// </summary>
        /// <param name="fieldType"></param>
        /// <param name="index"></param>
        public void CountInjectorFieldDefaultValue(GorgeType fieldType, out int index)
        {
            index = InjectorFieldDefaultValueIndex.Count(fieldType.BasicType);
        }

        /// <summary>
        /// 冻结本计数器
        /// </summary>
        public void Freeze()
        {
            Frozen = true;
        }
    }
}