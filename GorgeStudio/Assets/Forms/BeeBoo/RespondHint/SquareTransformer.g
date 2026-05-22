using Gorge;
using GorgeFramework;
namespace BeeBoo;

class SquareTransformer :: ITransformer
{
    Square square;
    
    FunctionCurve progressCurve = new CubicHermiteSpline()
    {
        startPoint : Vector2 : {x : 0.0, y : 0.0},
        startTangent : 0.0,
        startWeight : 0.0,
        endPoint : Vector2 : {x : 1.0, y : 1.0},
        endTangent : 0.1,
        endWeight : 0.8,
    };
    
    Vector2 direction;
    float rotation;
    float distance;
    float finalSize;
    float keepTime;
    
    SquareTransformer(Square square)
    {
        this.square = square;

        direction = Random.RandomNormalized();
        rotation = Random.RandomFloat(0.0, 360.0);
        distance = Random.RandomFloat(0.6, 1.2);
        finalSize = Random.RandomFloat(0.0, 0.1);
        keepTime = Random.RandomFloat(0.3, 0.8);
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - square.startTime;
        float curveTime = t / keepTime;
        Vector2 direction = direction;
        float distance = distance;
        float progress = progressCurve.Evaluate(curveTime);
        float finalSize = finalSize;
        Sprite nowSprite = square.squareSprite;
        nowSprite.rotation.z = rotation;
        nowSprite.position.x = direction.x * distance * progress + square.respondPosition.x;
        nowSprite.position.y = direction.y * distance * progress + square.respondPosition.y;
        nowSprite.size.x = Math.Lerp(0.15, finalSize, curveTime);
        nowSprite.size.y = Math.Lerp(0.15, finalSize, curveTime);
        
        if (curveTime >= 1)
        {
            ColorArgb color = nowSprite.color;
            if (color.a != 0.0)
            {
                nowSprite.color = new ColorArgb(0.0, color.r, color.g, color.b);
            }
        }
        else if (curveTime > 0.93)
        {
            nowSprite.color = square.flashColor;
        }
        
        return null;
    }
}