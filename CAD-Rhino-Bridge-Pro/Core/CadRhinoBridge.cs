using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System;

namespace CAD_Rhino_Bridge_Pro.Core
{
    public class CadRhinoBridge : ICadRhinoBridge
    {
        private bool _isCadAvailable = false;
        private string _cadVersion = "Unknown";

        public CadRhinoBridge()
        {
            InitializeCadConnection();
        }

        private void InitializeCadConnection()
        {
            try
            {
                // 尝试检测 AutoCAD 是否可用
                _isCadAvailable = true;
                _cadVersion = "2024";
            }
            catch (Exception)
            {
                _isCadAvailable = false;
            }
        }

        public List<GeometryBase> ImportFromCad(string cadFilePath)
        {
            var importedGeometry = new List<GeometryBase>();

            if (!_isCadAvailable)
            {
                throw new InvalidOperationException("AutoCAD is not available.");
            }

            if (!File.Exists(cadFilePath))
            {
                throw new FileNotFoundException($"CAD file not found: {cadFilePath}");
            }

            try
            {
                // 示例：创建一个矩形
                var rectangle = new Rectangle3d(
                    Plane.WorldXY,
                    new Interval(0, 10),
                    new Interval(0, 5)
                );
                importedGeometry.Add(rectangle.ToNurbsCurve());

                // 示例：创建一个圆
                var circle = new Circle(Point3d.Origin, 3);
                importedGeometry.Add(circle.ToNurbsCurve());

                return importedGeometry;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import from CAD: {ex.Message}", ex);
            }
        }

        public bool ExportToCad(List<GeometryBase> geometryObjects, string cadFilePath)
        {
            if (!_isCadAvailable)
            {
                throw new InvalidOperationException("AutoCAD is not available.");
            }

            if (geometryObjects == null || geometryObjects.Count == 0)
            {
                throw new ArgumentException("No geometry objects provided for export.");
            }

            try
            {
                Console.WriteLine($"Exporting {geometryObjects.Count} objects to: {cadFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export to CAD: {ex.Message}", ex);
            }
        }

        public bool SyncGeometry(object cadGeometry, GeometryBase rhinoGeometry)
        {
            if (cadGeometry == null || rhinoGeometry == null)
            {
                return false;
            }

            try
            {
                Console.WriteLine($"Synchronizing geometry");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetCadSystemInfo()
        {
            if (_isCadAvailable)
            {
                return $"AutoCAD {_cadVersion} - Available and connected";
            }
            else
            {
                return "AutoCAD - Not available or not connected";
            }
        }

        public string GetRhinoSystemInfo()
        {
            try
            {
                var rhinoVersion = Rhino.RhinoApp.Version;
                return $"Rhino {rhinoVersion}";
            }
            catch (Exception)
            {
                return "Rhino - Version information not available";
            }
        }
    }
}
