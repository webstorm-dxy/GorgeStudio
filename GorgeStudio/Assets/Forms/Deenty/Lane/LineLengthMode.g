using Gorge;
using GorgeFramework;
namespace Deenty;

enum LineLengthMode
{
    // length代表GamePlayPanel本地坐标
    @DisplayName(name = "标准坐标系")
    GamePlayPanel,
    
    // length以屏幕高度为单位
    @DisplayName(name = "屏幕宽度")
    ScreenX,
    
    // length以屏幕宽度为单位
    @DisplayName(name = "屏幕高度")
    ScreenY,
    
    // 判定线足够长，确保贯穿屏幕，length无效
    @DisplayName(name = "贯穿屏幕")
    Enough
}