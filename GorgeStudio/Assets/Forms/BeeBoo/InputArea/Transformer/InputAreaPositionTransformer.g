using Gorge;
using GorgeFramework;
namespace BeeBoo;

class InputAreaPositionTransformer :: ITransformer
{
    InputArea inputArea;

    InputAreaPositionTransformer(InputArea inputArea)
    {
        this.inputArea = inputArea;
    }

    IAutomatonCommand[] Transform(float now)
    {
        inputArea.sprite.position.x = inputArea.nowPositionX;
        inputArea.sprite.position.y = inputArea.nowPositionY;
        inputArea.sprite.size.x = inputArea.nowWidth;
        inputArea.sprite.size.y = inputArea.nowHeight;
        inputArea.sprite.rotation.z = inputArea.nowRotation;
        ColorArgb color = inputArea.channel.nowColor;
        inputArea.sprite.color = new ColorArgb(color.a * inputArea.nowAlpha, color.r, color.g, color.b);
        return null;
    }

}