using Gorge;
using GorgeFramework;
namespace Deenty;

class RespondHintTransformer :: ITransformer
{
    RespondHint respondHint;
    
    float expandSpeed = 5.0;
    
    float alphaChangeSpeed = 0.2;
    
    RespondHintTransformer(RespondHint respondHint)
    {
        this.respondHint = respondHint;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float curveTime = (now - respondHint.startTime) / respondHint.keepTime;
        float process = respondHint.respondHintProcessCurve.Evaluate(curveTime);
        float scale = respondHint.respondHintSize.EvaluateAdd(curveTime);
        
        float newScale = Math.Atan(process * respondHint.keepTime * expandSpeed) * 2 * scale / Math.Pi();
        
        respondHint.graphNode.size.x = newScale;
        respondHint.graphNode.size.y = newScale;
        
        respondHint.graphNode.color.a = Math.Atan(((1 - process) * respondHint.keepTime) / alphaChangeSpeed) * 2 / Math.Pi();
        
        return null;
    }
}