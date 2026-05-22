using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

class EntityTextureTransformer :: ITransformer
{
    Image image;
    ImageAsset nowAsset;
    
    EntityTextureTransformer(Image image)
    {
        this.image = image;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        ImageAsset newAsset = (ImageAsset) Environment.GetAssetByName(image.assetName);
        // 材质不变则不执行替换
        if (newAsset == nowAsset)
        {
            return null;
        }
        
        nowAsset = newAsset;
        
        if (nowAsset == null)
        {
            image.graphNode.graph = null;
            return null;
        }
        
        image.graphNode.graph = nowAsset.texture;
        
        return null;
    }
}