using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalSliderRespondHint : Element
{
    float startTime;
    
    float keepTime;
    
    float arcLength;
    
    float height;
    
    float moveSpeed;
    
    MeshedSprite graphNode;
    
    float position;
    
    // 使用二次函数映射前的判定线半径
    float judgementLineUnmappedRadius;
    
    ReincalSliderRespondHint(float position, float judgementLineRadius, float startTime, Node reference)
    {
        this.startTime = startTime;
        this.keepTime = 0.8;
        this.position = position;
        this.arcLength = 2;
        this.height = 0.4;
        this.moveSpeed = 40.0;
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new ReincalSliderRespondHintPositionTransformer(this);
        //transformers[1] = new ReincalRespondHintHeartTransformer(this);
        
        // 计算网格变形器参数
        // Note落在判定线上时的弧长
        float finalArcLength = this.arcLength;
        // Note落在判定线上的径向高度
        float finalHeight = this.height;
        
        // 计算弧长圆心角
        float centerAngleR = finalArcLength / judgementLineRadius;
        float halfCenterAngleR = centerAngleR / 2;
        
        // GorgeGraphics
        ImageAsset sliderTapImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/SliderTap");
        graphNode = new MeshedSprite(sliderTapImage.texture);
        graphNode.width = finalArcLength;
        graphNode.height = finalHeight;
        graphNode.horizontalSegments = 10;
        graphNode.verticalSegments = 1;
        graphNode.size.y = 1;
        graphNode.size.x = 1;
        graphNode.position.z = -0.95;
        graphNode.positionReference = reference;
        graphNode.color = new ColorArgb(0.3, 1, 1, 1);
        
        // 计算映射前判定线距离
        judgementLineUnmappedRadius = Math.Sqrt(2 * judgementLineRadius * judgementLineRadius);
        AnnulusMeshTransformer meshTransformer = new AnnulusMeshTransformer();
        meshTransformer.yRadius = new QuadraticFunctionCurve(0.5 / judgementLineRadius, 0, 0);
        meshTransformer.xAngle = new LinearCurve(-1, -halfCenterAngleR + position + 4.71233889, 1, halfCenterAngleR + position + 4.71233889);
        
        graphNode.AddMeshTransformer(meshTransformer);
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
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