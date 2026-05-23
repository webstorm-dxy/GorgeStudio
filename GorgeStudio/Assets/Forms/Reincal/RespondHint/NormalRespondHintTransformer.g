using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalRespondHintTransformer :: ITransformer
{
    NormalRespondHint note;
    
    float speed1 = -360;
    float speed2 = 360;
    float speed3 = -360;
    float speed4 = 480;
    float speed5 = -720;
    FunctionCurve respondHintProcessCurve;
    float baseSize;
    float sizeFinal1;
    float sizeFinal2;
    float sizeFinal3;
    float sizeFinal4;
    float sizeFinal5;
    float alphaChangeSpeed = 0.2;
    
    NormalRespondHintTransformer(NormalRespondHint note, float baseSize)
    {
        this.note = note;
        this.baseSize = baseSize;
        
        respondHintProcessCurve = new CubicHermiteSpline()
        {
            startPoint : Vector2 : {x : 0.0, y : 0.0},
            startTangent : 0.0,
            startWeight : 0.0,
            endPoint : Vector2 : {x : 1.0, y : 1.0},
            endTangent : 0.1,
            endWeight : 0.8,
        };
        
        sizeFinal1 = (515.0 / 447.0) * baseSize;
        sizeFinal2 = (580.0 / 447.0) * baseSize;
        sizeFinal3 = (691.0 / 447.0) * baseSize;
        sizeFinal4 = (790.0 / 447.0) * baseSize;
        sizeFinal5 = (980.0 / 447.0) * baseSize;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float curveTime = (now - note.startTime) / note.keepTime;
        float process = respondHintProcessCurve.Evaluate(curveTime);

        float alpha = Math.Atan(((1 - process) * note.keepTime) / alphaChangeSpeed) * 2 / Math.Pi();
        
        float rotation1 = process * speed1;
        float rotation2 = process * speed2;
        float rotation3 = process * speed3;
        float rotation4 = process * speed4;
        float rotation5 = process * speed5;
        
        note.respondHintCircle1.rotation.z = rotation1;
        float size1 = (sizeFinal1 - baseSize) * process + baseSize;
        note.respondHintCircle1.size.x = size1;
        note.respondHintCircle1.size.y = size1;
        note.respondHintCircle1.color.a = alpha;
        
        note.respondHintCircle2.rotation.z = rotation2;
        float size2 = (sizeFinal2 - baseSize) * process + baseSize;
        note.respondHintCircle2.size.x = size2;
        note.respondHintCircle2.size.y = size2;
        note.respondHintCircle2.color.a = alpha;
        
        note.respondHintCircle3.rotation.z = rotation3;
        float size3 = (sizeFinal3 - baseSize) * process + baseSize;
        note.respondHintCircle3.size.x = size3;
        note.respondHintCircle3.size.y = size3;
        note.respondHintCircle3.color.a = alpha;
        
        note.respondHintCircle4.rotation.z = rotation4;
        float size4 = (sizeFinal4 - baseSize) * process + baseSize;
        note.respondHintCircle4.size.x = size4;
        note.respondHintCircle4.size.y = size4;
        note.respondHintCircle4.color.a = alpha;
        
        note.respondHintCircle5.rotation.z = rotation5;
        float size5 = (sizeFinal5 - baseSize) * process + baseSize;
        note.respondHintCircle5.size.x = size5;
        note.respondHintCircle5.size.y = size5;
        note.respondHintCircle5.color.a = alpha;
        
        return null;
    }
}