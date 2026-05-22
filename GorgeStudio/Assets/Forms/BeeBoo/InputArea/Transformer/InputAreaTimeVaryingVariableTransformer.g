using Gorge;
using GorgeFramework;
namespace BeeBoo;

class InputAreaTimeVaryingVariableTransformer :: ITransformer
{
    InputArea inputArea;

    InputAreaTimeVaryingVariableTransformer(InputArea inputArea)
    {
        this.inputArea = inputArea;
    }

    IAutomatonCommand[] Transform(float now)
    {
        float t = now - inputArea.startTime;
        inputArea.localTime = t;
        inputArea.nowPositionX = inputArea.positionX.EvaluateAdd(t);
        inputArea.nowPositionY = inputArea.positionY.EvaluateAdd(t);
        inputArea.nowWidth = inputArea.width.EvaluateAdd(t);
        inputArea.nowHeight = inputArea.height.EvaluateAdd(t);
        inputArea.nowRotation = inputArea.rotation.EvaluateAdd(t);
        inputArea.nowAlpha = inputArea.alpha.EvaluateAdd(t);
        return null;
    }

}