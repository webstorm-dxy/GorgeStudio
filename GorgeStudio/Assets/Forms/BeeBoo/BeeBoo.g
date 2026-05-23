using Gorge;
using GorgeFramework;
namespace BeeBoo;

class BeeBoo
{
    @Form(name = "汐梦之歌", version = "0.1")
    static string[] ElementTypeList()
    {
        return new string[6]
        {
            "BeeBoo.Tap",
            "BeeBoo.Hold",
            "BeeBoo.InputArea",
            "BeeBoo.FreeLaneSet",
            "BeeBoo.Tracker",
            "BeeBoo.Channel"
        };
    }
}