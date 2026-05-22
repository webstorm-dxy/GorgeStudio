using Gorge;
using GorgeFramework;
namespace Obsertor;

class NativeResources
{
    @InstantAudio(name = "RespondA")
    static AudioAsset NativeResources()
    {
        return (AudioAsset) Environment.GetAssetByName("audio:Obsertor/FormAsset/RespondEffect");
    }
}