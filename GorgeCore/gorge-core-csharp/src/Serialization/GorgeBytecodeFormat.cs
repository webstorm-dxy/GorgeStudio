namespace Gorge.GorgeLanguage.Serialization;

public static class GorgeBytecodeFormat
{
    // File identification
    public static readonly byte[] Magic = { 0x47, 0x4F, 0x52, 0x47 }; // "GORG"
    public const uint CurrentVersion = 1;

    // Operand discriminators
    public const byte OperandTagNull = 0;
    public const byte OperandTagAddress = 1;
    public const byte OperandTagImmediate = 2;

    // Immediate value type tags
    public const byte ValueTagInt = 0;
    public const byte ValueTagFloat = 1;
    public const byte ValueTagBool = 2;
    public const byte ValueTagString = 3;
    public const byte ValueTagObject = 4;

    // Constant pool object type tags
    public const byte ConstantTagInjector = 0;

    // Sentinel for null indices
    public const int NullIndex = -1;
}
