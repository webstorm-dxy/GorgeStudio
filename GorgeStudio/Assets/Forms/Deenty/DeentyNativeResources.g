using Gorge;
using GorgeFramework;
namespace Deenty;

class DeentyNativeResources
{
    @InstantAudio(name = "NormalRespond")
    static AudioAsset RespondEffect()
    {
        return (AudioAsset) Environment.GetAssetByName("audio:Deenty/FormAsset/RespondEffect");
    }
}