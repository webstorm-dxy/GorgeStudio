using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public static class ModifierTypeExtension
    {
        public static string DisplayName(this ModifierType modifierType)
        {
            return modifierType switch
            {
                ModifierType.Native => "native",
                ModifierType.Static => "static",
                ModifierType.Injector => "injector",
                _ => throw new UnexpectedSwitchConditionException(modifierType)
            };
        }
    }
}