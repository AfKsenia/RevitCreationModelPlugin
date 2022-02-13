using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace RevitCreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            var level1 = FindLevel(doc, "Уровень 1");
            var level2 = FindLevel(doc, "Уровень 2");

            List<Wall> walls = CreateWall(doc, level1, level2);



            Transaction transaction = new Transaction(doc, "Расстановка FamilySymbol");
            transaction.Start("1");
            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1], 500);
            AddWindow(doc, level1, walls[2], 500);
            AddWindow(doc, level1, walls[3], 500);
            AddRoof2(doc, level2, walls);
            transaction.Commit();
            return Result.Succeeded;
        }



        public List<Wall> CreateWall(Document doc, Level level1, Level level2)
        {
            //задаем размеры сооружения
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depht = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depht / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            //массив стен
            List<Wall> walls = new List<Wall>();
            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start("Копирование группы объектов");

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);

            }
            transaction.Commit();
            return walls;
        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                   .OfClass(typeof(FamilySymbol))
                   .OfCategory(BuiltInCategory.OST_Doors)//конкретный тип загружаемого семейства
                   .OfType<FamilySymbol>()
                   .Where(x => x.Name.Equals("0915 x 2134 мм"))
                   .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                   .FirstOrDefault();


            //определяем точку, в которую добавим дверь
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;


            if (!doorType.IsActive)
            {
                doorType.Activate();
            }
            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
        private void AddWindow(Document doc, Level level1, Wall wall, double windowSill)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                   .OfClass(typeof(FamilySymbol))
                   .OfCategory(BuiltInCategory.OST_Windows)//конкретный тип загружаемого семейства
                   .OfType<FamilySymbol>()
                   .Where(x => x.Name.Equals("0610 x 1830 мм"))
                   .Where(x => x.FamilyName.Equals("Фиксированные"))
                   .FirstOrDefault();

            //определяем точку, в которую добавим окно
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;


            if (!windowType.IsActive)
            {
                windowType.Activate();
            }

            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            //высота нижнего бруса
            double windowSillValue = UnitUtils.ConvertToInternalUnits(windowSill, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(windowSillValue);
        }

        public Level FindLevel(Document doc, string level)
        {
            List<Level> listlevel = new FilteredElementCollector(doc)
                  .OfClass(typeof(Level))
                  .OfType<Level>()
                  .ToList();

            Level level1 = listlevel
                .Where(x => x.Name.Equals(level))
                .FirstOrDefault();

            return level1;
        }


        private void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                   .OfType<RoofType>()
                   .Where(x => x.Name.Equals("Типовой - 400мм"))
                   .Where(x => x.FamilyName.Equals("Базовая крыша"))
                   .FirstOrDefault();

            double wallWidth = walls[0].Width; //узнаем толщину любой стены
            double dt = wallWidth / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));


            Application application = doc.Application;
            CurveArray footprint = application.Create.NewCurveArray(); //отпечаток границы дома

            for (int i = 0; i < 4; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);//линия со смещением на толщину стены
                footprint.Append(line);

            }
            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);
            //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
            //iterator.Reset();
            //while (iterator.MoveNext())
            //{
            //    ModelCurve modelCurve = iterator.Current as ModelCurve;
            //    footprintRoof.set_DefinesSlope(modelCurve, true);
            //    footprintRoof.set_SlopeAngle(modelCurve, 0.5);
            //}
            foreach (ModelCurve m in footPrintToModelCurveMapping)
            {
                footprintRoof.set_DefinesSlope(m, true);
                footprintRoof.set_SlopeAngle(m, 0.5);
            }


        }
        private void AddRoof2(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                   .OfType<RoofType>()
                   .Where(x => x.Name.Equals("Типовой - 400мм"))
                   .Where(x => x.FamilyName.Equals("Базовая крыша"))
                   .FirstOrDefault();

            double wallWidth = walls[0].Width; 
            double dt = wallWidth / 2;


            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depht = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depht / 2;
            double extrusionStart = -dx - dt;
            double extrusionEnd = dx + dt;


            CurveArray curvearray = new CurveArray();
            curvearray.Append(Line.CreateBound(new XYZ(0, -dy - dt, level2.Elevation), new XYZ(0, 0, level2.Elevation + dy)));
            curvearray.Append(Line.CreateBound(new XYZ(0, 0, level2.Elevation + dy), new XYZ(0, dy + dt, level2.Elevation)));
            
            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            var roof= doc.Create.NewExtrusionRoof(curvearray, plane, level2, roofType, extrusionStart, extrusionEnd);
            roof.get_Parameter(BuiltInParameter.ROOF_EAVE_CUT_PARAM).Set(33619);
        }
    }
}
    