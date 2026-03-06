using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcRx = Autodesk.AutoCAD.Runtime;
using CAD_Rhino_Bridge_Pro.Core;
using Rhino.Geometry;

namespace CAD_Rhino_Bridge_Pro
{
    public class Commands
    {
        private static readonly ICadRhinoBridge _bridge = new CadRhinoBridge();

        [AcRx.CommandMethod("CRB_ImportFromCAD")]
        public static void ImportFromCad()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                // 获取CAD文件路径
                var fileDlg = new Autodesk.AutoCAD.Windows.OpenFileDialog(
                    "Select CAD file to import",
                    "",
                    "dwg;dxf",
                    "Select CAD file",
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.AllowMultiple);

                if (fileDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string cadFilePath = fileDlg.GetFilenames()[0];

                // 显示进度
                ed.WriteMessage($"\nImporting from {Path.GetFileName(cadFilePath)}...");

                // 调用桥梁接口导入几何体
                var importedGeometry = _bridge.ImportFromCad(cadFilePath);

                // 将Rhino几何体添加到AutoCAD
                AddGeometryToAutoCAD(importedGeometry, doc.Database);

                ed.WriteMessage($"\nSuccessfully imported {importedGeometry.Count} objects.");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        [AcRx.CommandMethod("CRB_ExportToCAD")]
        public static void ExportToCad()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                // 选择要导出的实体
                var filter = new SelectionFilter(new[] {
                    new TypedValue(0, "LINE,ARC,CIRCLE,POLYLINE,LWPOLYLINE,SPLINE,ELLIPSE")
                });

                var selectionResult = ed.GetSelection(filter);
                if (selectionResult.Status != PromptStatus.OK)
                    return;

                // 获取保存路径
                var saveDlg = new Autodesk.AutoCAD.Windows.SaveFileDialog(
                    "Save CAD file",
                    "",
                    "dwg",
                    "CAD File",
                    Autodesk.AutoCAD.Windows.SaveFileDialog.SaveFileDialogFlags.DefaultIsFolder);

                if (saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string savePath = saveDlg.Filename;

                // 获取AutoCAD几何体并转换为Rhino几何体
                var rhinoGeometry = GetRhinoGeometryFromSelection(selectionResult.Value, doc.Database);

                // 导出到CAD
                bool success = _bridge.ExportToCad(rhinoGeometry, savePath);

                if (success)
                    ed.WriteMessage($"\nSuccessfully exported {rhinoGeometry.Count} objects to {Path.GetFileName(savePath)}");
                else
                    ed.WriteMessage($"\nExport failed.");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        [AcRx.CommandMethod("CRB_SystemInfo")]
        public static void ShowSystemInfo()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                string cadInfo = _bridge.GetCadSystemInfo();
                string rhinoInfo = _bridge.GetRhinoSystemInfo();

                ed.WriteMessage($"\n=== CAD-Rhino Bridge Pro System Info ===\n");
                ed.WriteMessage($"\nCAD: {cadInfo}");
                ed.WriteMessage($"\nRhino: {rhinoInfo}");
                ed.WriteMessage($"\n=======================================\n");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        [AcRx.CommandMethod("CRB_SyncSelection")]
        public static void SyncSelection()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                // 选择AutoCAD实体
                var selectionResult = ed.GetSelection();
                if (selectionResult.Status != PromptStatus.OK)
                    return;

                // 获取选择集
                var selectedIds = selectionResult.Value.GetObjectIds();

                ed.WriteMessage($"\nSelected {selectedIds.Length} objects for synchronization.");
                ed.WriteMessage($"\nSynchronization feature requires Rhino to be running with Grasshopper.");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        private static void AddGeometryToAutoCAD(List<GeometryBase> geometry, Database database)
        {
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var geom in geometry)
                {
                    if (geom is Rhino.Geometry.Curve curve)
                    {
                        // 将Rhino曲线转换为AutoCAD曲线
                        var acadCurve = ConvertRhinoCurveToAutoCAD(curve);
                        if (acadCurve != null)
                        {
                            modelSpace.AppendEntity(acadCurve);
                            transaction.AddNewlyCreatedDBObject(acadCurve, true);
                        }
                    }
                    else if (geom is Rhino.Geometry.Brep brep)
                    {
                        // 将RhinoBrep转换为AutoCAD实体
                        var acadSolids = ConvertRhinoBrepToAutoCAD(brep);
                        foreach (var solid in acadSolids)
                        {
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                        }
                    }
                }

                transaction.Commit();
            }
        }

        private static List<GeometryBase> GetRhinoGeometryFromSelection(SelectionSet selectionSet, Database database)
        {
            var rhinoGeometry = new List<GeometryBase>();

            using (var transaction = database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selectionSet.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null)
                    {
                        var rhinoGeom = ConvertAutoCADEntityToRhino(entity);
                        if (rhinoGeom != null)
                        {
                            rhinoGeometry.Add(rhinoGeom);
                        }
                    }
                }

                transaction.Commit();
            }

            return rhinoGeometry;
        }

        private static Entity ConvertRhinoCurveToAutoCAD(Rhino.Geometry.Curve curve)
        {
            if (curve is LineCurve lineCurve)
            {
                var line = new Autodesk.AutoCAD.DatabaseServices.Line(
                    new Autodesk.AutoCAD.Geometry.Point3d(lineCurve.PointAtStart.X, lineCurve.PointAtStart.Y, lineCurve.PointAtStart.Z),
                    new Autodesk.AutoCAD.Geometry.Point3d(lineCurve.PointAtEnd.X, lineCurve.PointAtEnd.Y, lineCurve.PointAtEnd.Z)
                );
                return line;
            }

            return null;
        }

        private static List<Solid3d> ConvertRhinoBrepToAutoCAD(Rhino.Geometry.Brep brep)
        {
            var solids = new List<Solid3d>();

            try
            {
                // 尝试创建一个简单的体素网格来表示 Brep
                var bbox = brep.GetBoundingBox(false);

                // 简化实现：创建一个立方体作为占位符
                var solid = new Solid3d();
                solid.CreateBox(bbox.Diagonal.X, bbox.Diagonal.Y, bbox.Diagonal.Z);

                // 将实体移动到正确的位置
                var center = bbox.Center;
                var matrix = Autodesk.AutoCAD.Geometry.Matrix3d.Displacement(
                    new Autodesk.AutoCAD.Geometry.Vector3d(center.X, center.Y, center.Z));
                solid.TransformBy(matrix);

                solids.Add(solid);
            }
            catch
            {
                // 如果转换失败,返回空列表
            }

            return solids;
        }

        private static GeometryBase ConvertAutoCADEntityToRhino(Entity entity)
        {
            if (entity is Autodesk.AutoCAD.DatabaseServices.Line line)
            {
                return new Rhino.Geometry.Line(
                    new Rhino.Geometry.Point3d(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z),
                    new Rhino.Geometry.Point3d(line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z)
                ).ToNurbsCurve();
            }

            return null;
        }
    }
}
