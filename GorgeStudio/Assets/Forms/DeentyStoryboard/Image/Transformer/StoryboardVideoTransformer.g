using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

class StoryboardVideoTransformer :: ITransformer
{
    StoryboardVideo image;
    VideoAsset nowAsset;
    
    StoryboardVideoTransformer(StoryboardVideo image)
    {
        this.image = image;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        image.graphNode.SetTime((now - image.startMoment) * image.speed);
        VideoAsset newAsset = (VideoAsset) Environment.GetAssetByName(image.assetName);
        // 材质不变则不执行替换
        if (newAsset == nowAsset)
        {
            return null;
        }
        
        nowAsset = newAsset;
        
        if (nowAsset == null)
        {
            image.graphNode.video = null;
            return null;
        }
        
        image.graphNode.video = nowAsset.GetAsset();
        
        return null;
    }
}