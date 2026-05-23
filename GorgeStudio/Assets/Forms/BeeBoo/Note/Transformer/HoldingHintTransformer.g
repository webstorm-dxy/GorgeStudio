using Gorge;
using GorgeFramework;
namespace BeeBoo;

class HoldingHintTransformer :: ITransformer
{
    Hold note;
    float step = 0.10;
    bool first = true;
    float lastTime = 0;

    HoldingHintTransformer(Hold note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        string automatonState = note.automaton.GetState();
        if(automatonState == "Holding")
        {
            if(first)
            {
                lastTime = now;
                first = false;
                return GenerateSquare(1, now);
            }
            else if(lastTime < now)
            {
                lastTime = lastTime + step;
                return GenerateSquare(1, now);
            }
        }

        return null;
    }

    IAutomatonCommand[] GenerateSquare(int number, float now)
    {
        ColorArgb color = note.channel.nowColor;
        Vector3 globalRespondPosition = note.tracker.trackerSprite.GlobalPosition();
        IAutomatonCommand[] commands = new IAutomatonCommand[number];
        for(int i = 0; i < number; i = i + 1)
        {
            Square square = new Square(new Vector2(globalRespondPosition.x, globalRespondPosition.y), now, null, new ColorArgb(color.a,color.r,color.g,color.b), new ColorArgb(1,1,1,1));
            commands[i] = new DeriveElementCommand(square, false);
        }
        return commands;
    }

}