using Gorge;
using GorgeFramework;
namespace Deenty;

class Deenty
{
    @Form(name = "Deenty", version = "0.1")
    static string[] ElementTypeList()
    {
        return new string[7]{"Tap", "Catch", "Hold", "SkyTap", "Slider", "Line", "SkyArea"};
    }
}