using Gorge;
using GorgeFramework;
namespace BeeBoo;

class WordTransformer :: ITransformer
{
    Word respondHint;
    
    float expandSpeed = 5.0;
    
    float alphaChangeSpeed = 0.2;
    
    WordTransformer(Word respondHint)
    {
        this.respondHint = respondHint;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float curveTime = (now - respondHint.startTime) / respondHint.keepTime;
        float process = respondHint.respondHintProcessCurve.Evaluate(curveTime);
        respondHint.graphNode.position.y = process * 0.2 + respondHint.respondPosition.y;
        ColorArgb color = respondHint.color;
        respondHint.graphNode.color = new ColorArgb(Math.Atan(((1 - process) * respondHint.keepTime) / alphaChangeSpeed) * 2 / Math.Pi(), color.r, color.g, color.b);
        return null;
    }
}