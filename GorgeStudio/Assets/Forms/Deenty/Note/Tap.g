using Gorge;
using GorgeFramework;
namespace Deenty;

[
    delegate<float:Tap^> display = string:(Tap^ tapInjector) ->
    {
        return tapInjector.^respondMoment + " [" + tapInjector.^respondTime + "] | " + tapInjector.^laneName + ":" + tapInjector.^positionY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:Tap^> elementLine = ElementLine:(Tap^ tapInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(tapInjector.^respondMoment, 0.9, 0.2);
        return new ElementLine(new ColorArgb(1, 0.447059, 0.603922, 0.8), points);
    }
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.ReInject)
class Tap : DeentyLineSingleNote
{
    [
        delegate<float:Tap^> time = float:(Tap^ noteInjector) ->
        {
            return DeentyLineSingleNote.InjectorGenerateTime(noteInjector);
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Tap^> time = float:(Tap^ noteInjector) ->
        {
            return DeentyLineSingleNote.InjectorDestroyTime(noteInjector);
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Tap(bool isAutoPlay, bool isReverse) : super()
    {
        ITransformer[] transformers = new ITransformer[7];
        
        transformers[0] = new LineNotePositionYTransformer(this);
        transformers[1] = new LineSingleNotePositionXTransformer(this);
        transformers[2] = new NotePositionZTransformer(this);
        transformers[3] = new LineNoteLengthTransformer(this);
        transformers[4] = new NoteLaneTransformer(this){laneType : "Line"};
        transformers[5] = new NoteColorTransformer(this){type : "Tap"};
        transformers[6] = new NoteChordTransformer(this){type : "Tap"};
        
        // GorgeGraphics
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/Tap");
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
            TapNormal initializer = new TapNormal(this, isReverse);
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
    void ReInjectTap(Tap^ newInjector)
    {
        ReInjectDeentyLineSingleNote(newInjector);
    }
    
    @EditTryGenerate
    static bool TryGenerate(Tap^ newInjector, float chartTime)
    {
        float beginTime = DeentyLineSingleNote.InjectorGenerateTime(newInjector);
        float destroyTime = DeentyLineSingleNote.InjectorDestroyTime(newInjector);
        return chartTime >= beginTime && chartTime <= destroyTime;
    }
    
    [
        delegate<void:Tap^,float> onPointMove = ElementLine:(Tap^ noteInjector, float timeOffset) ->
        {
            noteInjector.^respondMoment = noteInjector.^respondMoment + timeOffset;
        }
    ]
    @PianoRollPoint
    static float PianoRollPointTime(Tap^ noteInjector)
    {
        return noteInjector.^respondMoment;
    }
}