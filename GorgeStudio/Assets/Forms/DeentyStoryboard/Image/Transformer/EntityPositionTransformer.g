using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

class EntityPositionTransformer :: ITransformer
{
    Image image;
    
    EntityPositionTransformer(Image image)
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
        
        // 设置角度
        image.graphNode.rotation.z = image.rotationZ.EvaluateAdd(curveTime);
        
        if (image.positionMode == ImagePositionMode.GamePlayPanel)
        {
            // 直接设置尺寸和位置
            image.graphNode.size.x = image.scaleX.EvaluateAdd(curveTime);
            image.graphNode.size.y = image.scaleY.EvaluateAdd(curveTime);
            image.graphNode.position.x = image.positionX.EvaluateAdd(curveTime);
            image.graphNode.position.y = image.positionY.EvaluateAdd(curveTime);
            image.graphNode.position.z = image.positionZ.EvaluateAdd(curveTime);
            return null;
        }
        
        Vector2 baseSize = Environment.ViewportSize();
        
        // 设置位置，xy为屏幕比例，全屏+-1
        image.graphNode.position.x = image.positionX.EvaluateAdd(curveTime) * baseSize.x / 2;
        image.graphNode.position.y = image.positionY.EvaluateAdd(curveTime) * baseSize.y / 2;
        image.graphNode.position.z = image.positionZ.EvaluateAdd(curveTime);
        
        // 设置尺寸
        switch (image.positionMode)
        {
            case ImagePositionMode.ScreenXY:
                // 设置尺寸，xy为屏幕比例，0-1
                image.graphNode.size.x = image.scaleX.EvaluateAdd(curveTime) * baseSize.x;
                image.graphNode.size.y = image.scaleY.EvaluateAdd(curveTime) * baseSize.y;
                break;
            case ImagePositionMode.ScreenX:
                // 设置尺寸，x为屏幕比例，0-1，y按正比保持
                image.graphNode.size.x = image.scaleX.EvaluateAdd(curveTime) * baseSize.x;
                image.graphNode.size.y = (image.graphNode.size.x * image.graphNode.graph.height) / image.graphNode.graph.width;
                break;
            case ImagePositionMode.ScreenY:
                // 设置尺寸，y为屏幕比例，0-1，x按正比保持
                image.graphNode.size.y = image.scaleY.EvaluateAdd(curveTime) * baseSize.y;
                image.graphNode.size.x = (image.graphNode.size.y * image.graphNode.graph.width) / image.graphNode.graph.height;
                return null;
            case ImagePositionMode.ScreenContain:
                // 渲染基宽于图片的情况，y填满，x按比例保持
                if (baseSize.x * image.graphNode.graph.height > baseSize.y * image.graphNode.graph.width)
                {
                    image.graphNode.size.y = baseSize.y;
                    image.graphNode.size.x = (image.graphNode.size.y * image.graphNode.graph.width) / image.graphNode.graph.height;
                }
                // 渲染基高于图片的情况，x填满，y按比例保持
                else
                {
                    image.graphNode.size.x = baseSize.x;
                    image.graphNode.size.y = (image.graphNode.size.x * image.graphNode.graph.height) / image.graphNode.graph.width;
                }
                break;
            case ImagePositionMode.ScreenCover:
                // 渲染基宽于图片的情况，x填满，y按比例保持
                if (baseSize.x * image.graphNode.graph.height > baseSize.y * image.graphNode.graph.width)
                {
                    image.graphNode.size.x = baseSize.x;
                    image.graphNode.size.y = (image.graphNode.size.x * image.graphNode.graph.height) / image.graphNode.graph.width;
                }
                // 渲染基高于图片的情况，y填满，x按比例保持
                else
                {
                    image.graphNode.size.y = baseSize.y;
                    image.graphNode.size.x = (image.graphNode.size.y * image.graphNode.graph.width) / image.graphNode.graph.height;
                }
                break;
        }
        return null;
    }
}