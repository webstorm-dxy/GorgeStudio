using Gorge;
using GorgeFramework;
namespace Reincal;

class DremuNativeResources
{
    @InstantAudio(name = "RespondA")
    static AudioAsset ReincalNativeResources()
    {
        return (AudioAsset) Environment.GetAssetByName("audio:Reincal/FormAsset/RespondEffect");
    }
}