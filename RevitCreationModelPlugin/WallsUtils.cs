using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitCreationModelPlugin
{
   public class WallsUtils
    {
        public static List<WallType> GetWallTypes(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var wallTypes =
                new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .ToList();
            return wallTypes;

            //находим все сстены в модели
            var res1 = new FilteredElementCollector(doc)
                   .OfClass(typeof(WallType))//объектно-ориентированное представление типа (быстрый фильтр)
                                             //.Cast<Wall>()
                   .OfType<WallType>()//фильтрация по типу(медленный фильтр)
                   .ToList();


            var res2 = new FilteredElementCollector(doc)
                   .OfClass(typeof(FamilyInstance))
                   .OfCategory(BuiltInCategory.OST_Doors)//конкретный тип загружаемого семейства
                   .OfType<FamilyInstance>()
                   .Where(x => x.Name.Equals("Имя"))
                   .ToList();


            var res3 = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToList();

        }
    }
}
