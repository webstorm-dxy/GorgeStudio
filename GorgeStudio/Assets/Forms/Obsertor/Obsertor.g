using Gorge;
using GorgeFramework;
namespace Obsertor;

class Obsertor
{
    @Form(name = "Obsertor", version = "0.1")
    static string[] ElementTypeList()
    {
        return new string[7]{
                   "Obsertor.Tap",
                   "Obsertor.Catch",
                   "Obsertor.Flick",
                   "Obsertor.Hold",
                   "Obsertor.SkyTap",
                   "Obsertor.SkyCatch",
                   "Obsertor.LineLane"
               };
    }
}