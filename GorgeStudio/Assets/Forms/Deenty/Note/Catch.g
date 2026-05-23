using Gorge;
using GorgeFramework;
namespace Deenty;

[
    delegate<float:Catch^> display = string:(Catch^ catchInjector) ->
    {
        return catchInjector.^respondMoment + " [" + catchInjector.^respondTime + "] | " + catchInjector.^laneName + ":" + catchInjector.^positionY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.8, g : 0.6598039, b : 0.2392157},
    delegate<ElementLine:Catch^> elementLine = ElementLine:(Catch^ catchInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(catchInjector.^respondMoment, 0.7, 0.2);
        return new ElementLine(new ColorArgb(1, 0.8, 0.713725, 0.447059), points);
    }
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.ReInject)
class Catch : DeentyLineSingleNote
{
    [
        delegate<float:Catch^> time = float:(Catch^ noteInjector) ->
        {
            return DeentyLineSingleNote.InjectorGenerateTime(noteInjector);
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Catch^> time = float:(Catch^ noteInjector) ->
        {
            return DeentyLineSingleNote.InjectorDestroyTime(noteInjector);
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Catch(bool isAutoPlay, bool isReverse) : super()
    {
        ITransformer[] transformers = new ITransformer[7];
        
        transformers[0] = new LineNotePositionYTransformer(this);
        transformers[1] = new LineSingleNotePositionXTransformer(this);
        transformers[2] = new NotePositionZTransformer(this);
        transformers[3] = new LineNoteLengthTransformer(this);
        transformers[4] = new NoteLaneTransformer(this){laneType : "Line"};
        transformers[5] = new NoteColorTransformer(this){type : "Catch"};
        transformers[6] = new NoteChordTransformer(this){type : "Catch"};
        
        // GorgeGraphics
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/Catch");
        graphNode = new NineSliceSprite(lineImage.texture, new Vector2(169, 169), new Vector2(169, 169), new Vector2(0.2, 0.2));
        graphNode.size.x = 0.2;
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        if (isAutoPlay)
        {
            SingleAuto initializer = new SingleAuto(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        else
        {
            CatchNormal initializer = new CatchNormal(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        
        simulator = new ElementSimulator(transformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return DestroyTime();
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return GenerateTime();
    }
    
    @EditReInject
    void ReInjectCatch(Catch^ newInjector)
    {
        ReInjectDeentyLineSingleNote(newInjector);
    }
    
    @EditTryGenerate
    static bool TryGenerate(Catch^ newInjector, float chartTime)
    {
        float beginTime = DeentyLineSingleNote.InjectorGenerateTime(newInjector);
        float destroyTime = DeentyLineSingleNote.InjectorDestroyTime(newInjector);
        return chartTime >= beginTime && chartTime <= destroyTime;
    }
}