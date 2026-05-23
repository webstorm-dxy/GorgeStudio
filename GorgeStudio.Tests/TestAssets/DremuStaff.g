using Gorge;
using GorgeFramework;
using Dremu;

[
    string form = "Dremu",
    string displayName = "Dremu谱表"
]
@ElementStaff
class DremuStaff
{
    [
        PeriodConfig^ config = PeriodConfig : {
            timeOffset : 0.0,
        }
    ]
    @Chart
    static Element^[] Period()
    {
        return new Element^[4]{
            DremuMainLane : {
                name : "mainLine01",
                laneLines : FunctionCurve^ : {
                    ConstantFunctionCurve : {
                        value : 0.0,
                    },
                },
                animation : LinearFunctionCurve : {
                    k : 1.0,
                    b : -10.0,
                },
                color : LerpColorCurve : { : },
                positionY : VariableFloat : {
                    baseValue : 0.0,
                    variationCurve : PiecewiseFunctionCurve : {
                        functionPieces : FunctionPiece^ : {
                            FunctionPiece : {
                                functionCurve : CubicHermiteSpline : {
                                    endPoint : Vector2 : {
                                        x : 1.0,
                                        y : -3.5,
                                    },
                                },
                            },
                            FunctionPiece : {
                                functionCurve : ConstantFunctionCurve : {
                                    value : -3.5,
                                },
                                startX : 1.0,
                                endX : 5.0,
                            },
                            FunctionPiece : {
                                functionCurve : CubicHermiteSpline : {
                                    startPoint : Vector2 : {
                                        x : 5.0,
                                        y : 0.0,
                                    },
                                    endPoint : Vector2 : {
                                        x : 6.0,
                                        y : 3.5,
                                    },
                                },
                                startX : 5.0,
                                endX : 6.0,
                            },
                            FunctionPiece : {
                                functionCurve : ConstantFunctionCurve : {
                                    value : 3.5,
                                },
                                startX : 0.0,
                                endX : 10.0,
                            },
                        },
                    },
                },
            },
            DremuTap : {
                laneName : "mainLine01",
                hitTime : 2.842105,
                color : LerpColorCurve : {
                    colorPoints : ColorArgb^ : {
                        ColorArgb : {
                            r : 0.0,
                            g : 0.8,
                            b : 1.0,
                        },
                    },
                },
            },
            DremuTap : {
                laneName : "mainLine01",
                hitTime : 7.31579,
                distance : LinearFunctionCurve : {
                    k : 8.0,
                    b : 0.0,
                },
            },
            DremuGuideLane : {
                name : "mainGuideLine01",
                animation : LinearFunctionCurve : {
                    k : 1.0,
                    b : 0.0,
                },
                drawStartX : VariableFloat : {
                    baseValue : 0.0,
                    variationCurve : PiecewiseFunctionCurve : {
                        functionPieces : FunctionPiece^ : {
                            FunctionPiece : {
                                functionCurve : ConstantFunctionCurve : { : },
                                startX : 0.0,
                                endX : 4.0,
                            },
                            FunctionPiece : {
                                functionCurve : ConstantFunctionCurve : {
                                    value : -10.0,
                                },
                                startX : 4.0,
                                endX : 8.0,
                            },
                        },
                    },
                },
                drawEndX : VariableFloat : {
                    baseValue : 10.0,
                    variationCurve : PiecewiseFunctionCurve : {
                        functionPieces : FunctionPiece^ : {
                            FunctionPiece : {
                                functionCurve : ConstantFunctionCurve : {
                                    value : 10.0,
                                },
                                startX : 0.0,
                                endX : 4.0,
                            },
                            FunctionPiece : {
                                functionCurve : ConstantFunctionCurve : {
                                    value : -10.0,
                                },
                                startX : 4.0,
                                endX : 8.0,
                            },
                        },
                    },
                },
                color : LerpColorCurve : {
                    colorPoints : ColorArgb^ : {
                        ColorArgb : {
                            a : 0.3,
                        },
                    },
                },
                mainLaneName : "mainLine01",
            },
        };
    }


}
