using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

enum ImagePositionMode
{
    @DisplayName(name = "标准坐标系 - 以GamePlayPanel本地坐标为基础，position和size有效")
    GamePlayPanel,
    @DisplayName(name = "屏幕拉伸 - 以屏幕为基础，position和size有效，且为屏幕比例，position范围是+-1，size范围是0-1")
    ScreenXY,
    @DisplayName(name = "等比屏幕宽 - 以屏幕为基础，position和size.x有效，且为屏幕比例，保持原图长宽比，position范围是+-1，size范围是0-1")
    ScreenX,
    @DisplayName(name = "等比屏幕高 - 以屏幕为基础，position和size.y有效，且为屏幕比例，保持原图长宽比，position范围是+-1，size范围是0-1")
    ScreenY,
    @DisplayName(name = "全屏内嵌 - 屏幕内最大内嵌，position有效，保持原图长宽比，position范围是+-1")
    ScreenContain,
    @DisplayName(name = "全屏覆盖 - 屏幕内最小覆盖，position有效，保持原图长宽比，position范围是+-1")
    ScreenCover
}