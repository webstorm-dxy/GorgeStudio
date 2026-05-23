using Gorge;
using GorgeFramework;
namespace Deenty;

[
    delegate<float:SkyTap^> display = string:(SkyTap^ skyTapInjector) ->
    {
        return skyTapInjector.^respondMoment + " [" + skyTapInjector.^respondTime + "] | " + skyTapInjector.^laneName + ":" +
               skyTapInjector.^positionX.^baseValue + "," + skyTapInjector.^positionY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2392157, g : 0.3981045, b : 0.8},
    delegate<ElementLine:SkyTap^> elementLine = ElementLine:(SkyTap^ skyTapInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(skyTapInjector.^respondMoment, 0.3, 0.2);
        return new ElementLine(new ColorArgb(1, 0.176471, 0.54902, 0.921569), points);
    }
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.ReInject)
class SkyTap : DeentySkySingleNote
{
    [
        delegate<float:SkyTap^> time = float:(SkyTap^ noteInjector) ->
        {
            return DeentySkySingleNote.InjectorGenerateTime(noteInjector);
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:SkyTap^> time = float:(SkyTap^ noteInjector) ->
        {
            return DeentySkySingleNote.InjectorDestroyTime(noteInjector);
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    SkyTap(bool isAutoPlay, bool isReverse) : super()
    {
        ITransformer[] transformers = new ITransformer[7];
        
        transformers[0] = new NotePositionZTransformer(this);
        transformers[1] = new SkyNotePositionTransformer(this);
        transformers[2] = new SkyNoteSizeTransformer(this);
        transformers[3] = new SkySingleNoteLoopTransformer(this);
        transformers[4] = new NoteLaneTransformer(this){laneType : "SkyArea"};
        transformers[5] = new NoteColorTransformer(this){type : "SkyTap"};
        transformers[6] = new NoteChordTransformer(this){type : "SkyTap"};
        
        // GorgeGraphics
        ImageAsset baseImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/SkyTap");
        graphNode = new NineSliceSprite(baseImage.texture, new Vector2(169, 169), new Vector2(169, 169), new Vector2(0.2, 0.2));
        graphNode.size.x = 1;
        graphNode.size.y = 1;
        
        ImageAsset loopImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/SkyTapLoop");
        loopNode = new NineSliceSprite(loopImage.texture, new Vector2(169, 169), new Vector2(169, 169), new Vector2(0.2, 0.2));
        loopNode.size.x = 2.5;
        loopNode.size.y = 2.5;
        loopNode.existenceReference = graphNode;
        loopNode.positionReference = graphNode;
        loopNode.rotationReference = graphNode;
        loopNode.sizeReference = graphNode;
        
        nodes = new Node[2];
        nodes[0] = graphNode;
        nodes[1] = loopNode;
        
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
    void ReInjectSkyTap(SkyTap^ newInjector)
    {
        ReInjectDeentySkyNote(newInjector);
    }
    
    @EditTryGenerate
    static bool TryGenerate(SkyTap^ newInjector, float chartTime)
    {
        float beginTime = DeentySkySingleNote.InjectorGenerateTime(newInjector);
        float destroyTime = DeentySkySingleNote.InjectorDestroyTime(newInjector);
        return chartTime >= beginTime && chartTime <= destroyTime;
    }
}