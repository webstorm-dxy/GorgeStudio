using Gorge;
using GorgeFramework;
namespace Reincal;

class Reincal
{
    @Form(name = "Reincal", version = "0.1")
    static string[] ElementTypeList()
    {
        return new string[7]{"Reincal.ReincalSliderTap", "Reincal.ReincalSliderHold", "Reincal.Tap", "Reincal.Catch", "Reincal.Hold", "Reincal.ReincalSliderLane", "Reincal.NormalLane"};
    }
}