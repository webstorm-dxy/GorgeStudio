using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

class DeentyStoryboard
{
    @Form(name = "DeentyStoryboard", version = "0.1")
    static string[] ElementTypeList()
    {
        return new string[2]{"DeentyStoryboard.Image", "DeentyStoryboard.StoryboardVideo"};
    }
}