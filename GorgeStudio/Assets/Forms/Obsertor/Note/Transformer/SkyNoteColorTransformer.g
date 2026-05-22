using Gorge;
using GorgeFramework;
namespace Obsertor;

class SkyNoteColorTransformer :: ITransformer
{
    SkyNote note;
    
    SkyNoteColorTransformer(SkyNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        string automatonState = note.automaton.GetState();
        if (automatonState == "Accepted" || automatonState == "Denied")
        {
            ColorArgb c = note.graphNode.color;
            note.graphNode.color = new ColorArgb(0, c.r, c.g, c.b);
            ColorArgb c2 = note.indicatorNode.color;
            note.indicatorNode.color = new ColorArgb(0, c2.r, c2.g, c2.b);
            return null;
        }

        float t = now - note.hitTime;

        if(t > 0)
        {
            note.indicatorNode.color = new ColorArgb(0,1,1,1);
        }
        else
        {
            float alpha = 0.2 * (t + 1);
            note.indicatorNode.color = new ColorArgb(alpha,1,1,1);
        }
        return null;
    }
}