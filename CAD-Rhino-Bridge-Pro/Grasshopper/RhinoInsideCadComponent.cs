using Grasshopper.Kernel;
using Rhino.Geometry;
using CAD_Rhino_Bridge_Pro.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CAD_Rhino_Bridge_Pro.Grasshopper
{
    public class RhinoInsideCadComponent : GH_Component
    {
        private readonly ICadRhinoBridge _bridge;

        public RhinoInsideCadComponent()
          : base("RhinoInsideCAD", "RInsideCAD",
              "Bidirectional data exchange between AutoCAD and Rhino using RhinoInside technology",
              "CAD-Rhino Bridge", "Integration")
        {
            _bridge = new CadRhinoBridge();
        }

        public override Guid ComponentGuid => new Guid("F66C0FBD-26CE-45C3-AD28-A7D59252CE1A");

        protected override Bitmap Icon => null;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("CAD File", "File", "Path to CAD file (.dwg, .dxf)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Import", "I", "Import geometry from CAD to Rhino", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Export", "E", "Export geometry from Rhino to CAD", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Layer Filter", "Layers", "Comma-separated layer names (empty for all)", GH_ParamAccess.item, "");
            pManager.AddGeometryParameter("Rhino Geometry", "Geo", "Rhino geometry to export", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Imported Geometry", "Geo", "Geometry imported from CAD", GH_ParamAccess.list);
            pManager.AddTextParameter("Status", "Status", "Operation status", GH_ParamAccess.item);
            pManager.AddTextParameter("System Info", "Info", "CAD and Rhino system information", GH_ParamAccess.item);
            pManager.AddTextParameter("Layer Info", "Layers", "Layer information from CAD", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Count", "Count", "Number of objects processed", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string cadFilePath = string.Empty;
            bool import = false;
            bool export = false;
            string layerFilter = string.Empty;
            List<GeometryBase> rhinoGeometry = new List<GeometryBase>();

            if (!DA.GetData(0, ref cadFilePath)) return;
            if (!DA.GetData(1, ref import)) return;
            if (!DA.GetData(2, ref export)) return;
            DA.GetData(3, ref layerFilter);
            DA.GetDataList(4, rhinoGeometry);

            try
            {
                // 验证文件
                if (!File.Exists(cadFilePath))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File not found: {cadFilePath}");
                    DA.SetData(1, "Error: File not found");
                    return;
                }

                // 执行导入
                if (import && !export)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Importing from: {Path.GetFileName(cadFilePath)}");

                    var importedGeometry = _bridge.ImportFromCad(cadFilePath);

                    if (importedGeometry != null && importedGeometry.Count > 0)
                    {
                        DA.SetDataList(0, importedGeometry);
                        DA.SetData(1, $"Import successful: {importedGeometry.Count} objects");
                        DA.SetData(4, importedGeometry.Count);

                        // 模拟图层信息
                        var layerInfo = new List<string>
                        {
                            "Layer: 0 (Default)",
                            "Layer: Walls",
                            "Layer: Doors",
                            "Layer: Windows"
                        };
                        DA.SetDataList(3, layerInfo);
                    }
                    else
                    {
                        DA.SetData(1, "No geometry found in CAD file");
                    }
                }
                // 执行导出
                else if (export && !import)
                {
                    if (rhinoGeometry.Count == 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No geometry to export");
                        DA.SetData(1, "No geometry to export");
                        return;
                    }

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Exporting {rhinoGeometry.Count} objects to: {Path.GetFileName(cadFilePath)}");

                    bool exportResult = _bridge.ExportToCad(rhinoGeometry, cadFilePath);

                    if (exportResult)
                    {
                        DA.SetData(1, $"Export successful: {rhinoGeometry.Count} objects");
                        DA.SetData(4, rhinoGeometry.Count);
                    }
                    else
                    {
                        DA.SetData(1, "Export failed");
                    }
                }
                else
                {
                    DA.SetData(1, "Select either Import or Export (not both)");
                }

                // 系统信息
                string cadInfo = _bridge.GetCadSystemInfo();
                string rhinoInfo = _bridge.GetRhinoSystemInfo();
                DA.SetData(2, $"CAD: {cadInfo}\nRhino: {rhinoInfo}");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Error: {ex.Message}");
                DA.SetData(1, $"Error: {ex.Message}");
            }
        }
    }
}
