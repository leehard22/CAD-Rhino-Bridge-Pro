using Rhino.Geometry;
using System.Collections.Generic;

namespace CAD_Rhino_Bridge_Pro.Core
{
    /// <summary>
    /// CAD 与 Rhino 之间的桥梁接口，定义核心功能契约
    /// </summary>
    public interface ICadRhinoBridge
    {
        /// <summary>
        /// 从 CAD 导入几何对象到 Rhino
        /// </summary>
        /// <param name="cadFilePath">CAD 文件路径</param>
        /// <returns>导入的几何对象列表</returns>
        List<GeometryBase> ImportFromCad(string cadFilePath);

        /// <summary>
        /// 从 Rhino 导出几何对象到 CAD
        /// </summary>
        /// <param name="geometryObjects">要导出的 Rhino 几何对象列表</param>
        /// <param name="cadFilePath">目标 CAD 文件路径</param>
        /// <returns>导出是否成功</returns>
        bool ExportToCad(List<GeometryBase> geometryObjects, string cadFilePath);

        /// <summary>
        /// 同步 CAD 与 Rhino 之间的几何对象
        /// </summary>
        /// <param name="cadGeometry">CAD 几何对象</param>
        /// <param name="rhinoGeometry">Rhino 几何对象</param>
        /// <returns>同步是否成功</returns>
        bool SyncGeometry(object cadGeometry, GeometryBase rhinoGeometry);

        /// <summary>
        /// 获取 CAD 系统信息
        /// </summary>
        /// <returns>CAD 系统信息字符串</returns>
        string GetCadSystemInfo();

        /// <summary>
        /// 获取 Rhino 系统信息
        /// </summary>
        /// <returns>Rhino 系统信息字符串</returns>
        string GetRhinoSystemInfo();
    }
}
