using Gorge;
using GorgeFramework;
namespace Reincal;

class BeeBooNativeResources
{
    @InstantAudio(name = "RespondA")
    static AudioAsset BeeBooNativeResources()
    {
        return (AudioAsset) Environment.GetAssetByName("audio:BeeBoo/FormAsset/RespondEffect");
    }
}