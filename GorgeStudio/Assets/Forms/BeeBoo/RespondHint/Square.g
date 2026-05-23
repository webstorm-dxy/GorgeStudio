using Gorge;
using GorgeFramework;
namespace BeeBoo;

class Square : Element
{
    float startTime;
    
    float keepTime;
    
    ColorArgb color;
    
    ColorArgb flashColor;
    
    Sprite squareSprite;
    
    Vector2 respondPosition;
    
    Square(Vector2 respondPosition, float startTime, Node reference, ColorArgb color1, ColorArgb color2)
    {
        int squareCount = 7;
        this.startTime = startTime;
        this.keepTime = 0.8;
        this.respondPosition = respondPosition;
        color = color1;
        flashColor = color2;

        // GorgeGraphics
        ImageAsset heartImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/Square");
        squareSprite = new Sprite(heartImage.texture);
        squareSprite.position.x = respondPosition.x;
        squareSprite.position.y = respondPosition.y;
        squareSprite.position.z = -1;
        squareSprite.size.x = 0.15;
        squareSprite.size.y = 0.15;
        squareSprite.color = color1;
        squareSprite.positionReference = reference;

        nodes = new Node[1];
        nodes[0] = squareSprite;
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new SquareTransformer(this);
        simulator = new ElementSimulator(transformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return startTime + keepTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return startTime;
    }
}