using Gorge;
using GorgeFramework;
namespace Deenty;

class NoteColorTransformer :: ITransformer
{
    DeentyNote note;
    
    @Inject
    string type = ^type;
    
    NoteColorTransformer(DeentyNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        // TODO 暂时只考虑LineNote，需要重新考虑SkyNote的两图元间关系
        if (GetVisible(now, note.respondMoment, note.respondTime))
        {
            float curveTime = 1 - (note.respondMoment - now) / note.respondTime;
            float alpha = note.alpha.EvaluateDoubleLerp(curveTime, 0.0, 1.0);
            float hue = note.hue.EvaluateAdd(curveTime);
            float saturation = note.saturation.EvaluateAdd(curveTime);
            float brightness = note.brightness.EvaluateAdd(curveTime);
            SetColor(note.graphNode, alpha, hue, saturation, brightness);
            if (type == "SkyTap" || type == "Slider")
            {
                SetColor(((DeentySkySingleNote) note).loopNode, alpha, hue, saturation, brightness);
            }
        }
        else
        {
            note.graphNode.color.a = 0.0;
            if (type == "SkyTap" || type == "Slider")
            {
                NineSliceSprite loopNode = ((DeentySkySingleNote) note).loopNode;
                loopNode.color.a = 0.0;
            }
        }
        
        return null;
    }
    
    void SetColor(NineSliceSprite node, float alpha, float hue, float saturation, float brightness)
    {
        if (note.isDark)
        {
            node.color.r = 0.5;
            node.color.g = 0.5;
            node.color.b = 0.5;
        }
        else
        {
            node.color.r = 1.0;
            node.color.g = 1.0;
            node.color.b = 1.0;
        }
        node.color.a = alpha;
        node.hsl.x = hue;
        node.hsl.y = saturation;
        node.hsl.z = brightness;
    }
    
    bool GetVisible(float now, float respondMoment, float respondTime)
    {
        float showMoment = respondMoment - respondTime;
        float hideMoment = note.HideMoment();
        if (now < showMoment || now > hideMoment)
        {
            return false;
        }
        
        string automatonState = note.automaton.GetState();
        if (automatonState == "Accepted")
        {
            if (type == "Hold")
            {
                if (now >= respondMoment + ((Hold) note).holdTime)
                {
                    return false;
                }
            }
            else if (type == "Catch" || type == "Slider")
            {
                if (now > respondMoment)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        else if (automatonState == "Denied")
        {
            if (type != "Hold")
            {
                return false;
            }
        }
        return true;
    }
}