using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    双押贴图变换器
*/
class NoteChordTransformer :: ITransformer
{
    DeentyNote note;
    bool savedIsChord = false;
    
    Graph normalGraph;
    Graph chordGraph;
    
    Graph normalLoopGraph;
    Graph chordLoopGraph;
    
    @Inject
    string type = ^type;
    
    NoteChordTransformer(DeentyNote note)
    {
        this.note = note;
        this.normalGraph = ((ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/" + type)).texture;
        this.chordGraph = ((ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/" + type + "Chord")).texture;
        if (type == "SkyTap" || type == "Slider")
        {
            this.normalLoopGraph = ((ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/" + type + "Loop")).texture;
            this.chordLoopGraph = ((ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/" + type + "LoopChord")).texture;
        }
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        if (savedIsChord == note.isChord)
        {
            return null;
        }
        
        savedIsChord = note.isChord;
        note.graphNode.graph = savedIsChord ? chordGraph : normalGraph;
        if (type == "SkyTap" || type == "Slider")
        {
            NineSliceSprite loopNode = ((DeentySkySingleNote) note).loopNode;
            loopNode.graph = savedIsChord ? chordLoopGraph : normalLoopGraph;
        }
        return null;
    }
}