using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace HelloWall
{
    class Program
    {
        private static IfcBuilding CreateBuilding(IfcStore model, string name)
        {
            using (var txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;
                building.CompositionType = IfcElementCompositionEnum.ELEMENT;

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var placement = model.Instances.New<IfcAxis2Placement3D>();

                placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                localPlacement.RelativePlacement = placement;
                building.ObjectPlacement = localPlacement;

                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project?.AddBuilding(building);

                txn.Commit();
                return building;
            }
        }

        private static IfcStore CreateandInitModel(string projectName)
        {
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "Ryan Wu",
                ApplicationFullName = "Hello Wall Application",
                ApplicationIdentifier = "HelloWall.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "xbim",
                EditorsOrganisationName = "xbim developer"
            };

            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                var project = model.Instances.New<IfcProject>();
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                txn.Commit();
            }

            return model;
        }

        private static IfcSlabStandardCase CreateSlab(IfcStore model, double length, double width, double height, 
                                                                    double x, double y, double z)
        {
            using (var txn = model.BeginTransaction("Create Standard Slab"))
            {
                var slab = model.Instances.New<IfcSlabStandardCase>();
                slab.Name = "A Standard rectangular Slab";

                // Create rectangular profile definition
                var profile = model.Instances.New<IfcRectangleProfileDef>();
                profile.ProfileType = IfcProfileTypeEnum.AREA;
                profile.XDim = width;
                profile.YDim = length;

                // Create extrusion geometry
                var extrusion = model.Instances.New<IfcExtrudedAreaSolid>();
                extrusion.SweptArea = profile;
                extrusion.Depth = height;
                extrusion.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));

                // Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(extrusion);

                // Create a Product Definition and add the model geometry to the slab
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                slab.Representation = rep;

                // Create and set local placement
                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var placementAxis = model.Instances.New<IfcAxis2Placement3D>();

                placementAxis.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(x, y, z));
                placementAxis.Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
                placementAxis.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(1, 0, 0));

                localPlacement.RelativePlacement = placementAxis;
                slab.ObjectPlacement = localPlacement;

                txn.Commit();
                return slab;
            }
        }

        private static IfcColumnStandardCase CreateColumn(IfcStore model, double length, double width, double height, 
                                                                        double x, double y, double z)
        {
            using (var txn = model.BeginTransaction("Create Standard Column"))
            {
                var column = model.Instances.New<IfcColumnStandardCase>();
                column.Name = "A Standard Column";

                // Create rectangular profile definition
                var profile = model.Instances.New<IfcRectangleProfileDef>();
                profile.ProfileType = IfcProfileTypeEnum.AREA;
                profile.XDim = width;
                profile.YDim = length;

                // Create extrusion geometry
                var extrusion = model.Instances.New<IfcExtrudedAreaSolid>();
                extrusion.SweptArea = profile;
                extrusion.Depth = height;
                extrusion.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));

                // Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(extrusion);

                // Create a Product Definition and add the model geometry to the column
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                column.Representation = rep;

                // Create and set local placement
                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var placementAxis = model.Instances.New<IfcAxis2Placement3D>();

                placementAxis.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(x, y, z));
                placementAxis.Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
                placementAxis.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(1, 0, 0));

                localPlacement.RelativePlacement = placementAxis;
                column.ObjectPlacement = localPlacement;

                txn.Commit();
                return column;
            }
        }
        static int Main()
        {
            Console.WriteLine("Initialising the IFC Project....");
            using (var model = CreateandInitModel("HelloWall"))
            {
                if (model == null)
                {
                    Console.WriteLine("Failed to initialise the model");
                    return 0;
                }

                IfcBuilding building = CreateBuilding(model, "Default Building");

                IfcSlabStandardCase slab = CreateSlab(model, 3000, 1500, 100, 0, 0, 0);
                
                // 4 columns at each corner
                IfcColumnStandardCase column1 = CreateColumn(model, 25, 25, 1000, 725, 1475, 100);
                IfcColumnStandardCase column2 = CreateColumn(model, 25, 25, 1000, -725, 1475, 100);
                IfcColumnStandardCase column3 = CreateColumn(model, 25, 25, 1000, 725, -1475, 100);
                IfcColumnStandardCase column4 = CreateColumn(model, 25, 25, 1000, -725, -1475, 100);

                // 4 columns between corners
                IfcColumnStandardCase column5 = CreateColumn(model, 25, 25, 1000, -725, -475, 100);
                IfcColumnStandardCase column6 = CreateColumn(model, 25, 25, 1000, -725, 475, 100);
                IfcColumnStandardCase column7 = CreateColumn(model, 25, 25, 1000, 725, -475, 100);
                IfcColumnStandardCase column8 = CreateColumn(model, 25, 25, 1000, 725, 475, 100);

                if (column1 == null || column2 == null || column3 == null ||
                    column4 == null || column5 == null || column6 == null ||
                    column7 == null || column8 == null || slab == null)
                {
                    Console.WriteLine("Failed to initialise shape");
                    return 0;
                }

                using (var txn = model.BeginTransaction("Add slab and columns"))
                {
                    building.AddElement(slab);
                    building.AddElement(column1);
                    building.AddElement(column2);
                    building.AddElement(column3);
                    building.AddElement(column4);
                    building.AddElement(column5);
                    building.AddElement(column6);
                    building.AddElement(column7);
                    building.AddElement(column8);
                    txn.Commit();
                }

                try
                {
                    model.SaveAs("HelloWallIfc4.ifc", StorageType.Ifc);
                    Console.WriteLine("HelloWallIfc4.ifc has been successfully written");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to save HelloWall.ifc");
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine("Press any key to exit to view the IFC file....");
            return 0;
        }
    }
}
