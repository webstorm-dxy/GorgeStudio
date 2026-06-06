using System.Threading;
using System.Threading.Tasks;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services.ChartService;

/// <summary>
/// 谱面服务接口。负责从编译结果构建可编辑的 <see cref="SimulationScore"/>。
/// </summary>
public interface IChartService
{
    /// <summary>
    /// 从编译结果构建 <see cref="SimulationScore"/>。
    /// 遍历 ClassDeclarations 识别 @ElementStaff / @AudioStaff 注解，
    /// 创建对应的 Staff → Period 层级结构。
    /// </summary>
    /// <param name="result">编译结果，必须包含 Project、ClassDeclarations 和 AssetFiles。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>填充了 Staff/Period 层级和资产文件的 SimulationScore。</returns>
    Task<SimulationScore> BuildChartDocumentAsync(CompileResult result, CancellationToken ct = default);
}
