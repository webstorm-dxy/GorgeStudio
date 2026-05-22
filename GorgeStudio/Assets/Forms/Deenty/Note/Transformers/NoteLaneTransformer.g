using Gorge;
using GorgeFramework;
namespace Deenty;

class NoteLaneTransformer :: ITransformer
{
    DeentyNote note;
    
    @Inject
    string laneType = ^laneType;
    
    NoteLaneTransformer(DeentyNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        Element laneElement = Environment.FindAliveLane(laneType, note.laneName);
        if (laneElement == null)
        {
            return null;
        }
        DeentyLane lane = (DeentyLane) laneElement;
        note.graphNode.positionReference = lane.positionNode;
        // 原版如果是Line，则会重置rotation.z到0，目前已经不确定这个意义是什么
        
        return null;
    }
}