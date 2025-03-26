using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace HelloWall
{
    class Program
    {
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

                IfcSlabStandardCase wall = CreateSlab(model, 6000, 100, 3000, 0, 0, 0);
                IfcColumnStandardCase column = CreateColumn(model, 50, 50, 2000, 0, 0, 50);

                using (var txn = model.BeginTransaction("Add Wall"))
                {
                    building.AddElement(wall);
                    building.AddElement(column);
                    txn.Commit();
                }

                if (column != null && wall != null)
                {
                    try
                    {
                        Console.WriteLine("Standard Wall successfully created....");
                        model.SaveAs("HelloWallIfc4.ifc", StorageType.Ifc);
                        Console.WriteLine("HelloWallIfc4.ifc has been successfully written");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to save HelloWall.ifc");
                        Console.WriteLine(e.Message);
                    }
                }
            }

            Console.WriteLine("Press any key to exit to view the IFC file....");
            return 0;
        }

        private static IfcBuilding CreateBuilding(IfcStore model, string name)
        {
            using (var txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;
                building.CompositionType = IfcElementCompositionEnum.ELEMENT;

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                building.ObjectPlacement = localPlacement;
                
                var placement = model.Instances.New<IfcAxis2Placement3D>();
                placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));

                localPlacement.RelativePlacement = placement;

                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project?.AddBuilding(building);

                txn.Commit();
                return building;
            }
        }

        private static IfcStore CreateandInitModel(string projectName)
        {
            //first we need to set up some credentials for ownership of data in the new model
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

            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required
            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                //create a project
                var project = model.Instances.New<IfcProject>();
                //set the units to SI (mm and metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }

            return model;
        }

        private static IfcSlabStandardCase CreateSlab(IfcStore model, double length, double width, double height, double x, double y, double z)
        {
            //begin a transaction
            using (var txn = model.BeginTransaction("Create Slab"))
            {
                var wall = model.Instances.New<IfcSlabStandardCase>();
                wall.Name = "A Standard rectangular Slab";

                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = width;
                rectProf.YDim = length;

                //define extrusion
                var body = model.Instances.New<IfcExtrudedAreaSolid>();
                body.Depth = height;
                body.SweptArea = rectProf;
                body.ExtrudedDirection = model.Instances.New<IfcDirection>();
                body.ExtrudedDirection.SetXYZ(0, 0, 1);

                //parameters to insert the geometry in the model
                var origin = model.Instances.New<IfcCartesianPoint>();
                origin.SetXYZ(x, y, z);
                body.Position = model.Instances.New<IfcAxis2Placement3D>();
                body.Position.Location = origin;

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                wall.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = origin;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 1, 0);
                lp.RelativePlacement = ax3D;
                wall.ObjectPlacement = lp;

                txn.Commit();
                return wall;
            }
        }
    
        private static IfcColumnStandardCase CreateColumn(IfcStore model, double length, double width, double height, double x, double y, double z)
        {
            using (var txn = model.BeginTransaction("Create Column"))
            { 
                var column = model.Instances.New<IfcColumnStandardCase>();
                column.Name = "A Standard Column";

                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = width;
                rectProf.YDim = length;

                //define extrusion
                var body = model.Instances.New<IfcExtrudedAreaSolid>();
                body.Depth = height;
                body.SweptArea = rectProf;
                body.ExtrudedDirection = model.Instances.New<IfcDirection>();
                body.ExtrudedDirection.SetXYZ(0, 0, 1);

                //parameters to insert the geometry in the model
                var origin = model.Instances.New<IfcCartesianPoint>();
                origin.SetXYZ(x, y, z);
                body.Position = model.Instances.New<IfcAxis2Placement3D>();
                body.Position.Location = origin;

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                column.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = origin;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(0, 0, 1);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(1, 0, 0);
                lp.RelativePlacement = ax3D;
                column.ObjectPlacement = lp;

                txn.Commit();
                return column;
            }
        }
    }
}
