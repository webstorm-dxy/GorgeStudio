using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

class EntityColorTransformer :: ITransformer
{
    Image image;
    
    EntityColorTransformer(Image image)
    {
        this.image = image;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float curveTime;
        if (image.useChartTime)
        {
            curveTime = now - image.startMoment;
        }
        else
        {
            curveTime = (now - image.startMoment) / image.keepTime;
        }
        image.graphNode.color.a = image.alpha.EvaluateDoubleLerp(curveTime, 0.0, 1.0);
        
        return null;
    }
}