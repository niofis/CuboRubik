using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools
{
    public class MS3DLoader
    {
        public ContainerUIElement3D Load(Stream file)
        {
            CMS3DFile ms3dfile = new CMS3DFile();
            ms3dfile.LoadFromFile(file);

            ms3d_triangle_t ms3dtriangle = new ms3d_triangle_t();
            ms3d_vertex_t ms3dvertex = new ms3d_vertex_t();
            ms3d_group_t ms3dgroup = new ms3d_group_t();
            ms3d_material_t ms3dmaterial = new ms3d_material_t();
            ms3d_joint_t ms3djoint = new ms3d_joint_t();

            int i, j, k, l;

            GeometryModel3D Model = new GeometryModel3D(); ;
            MeshGeometry3D Mesh;

            Vector3DCollection Normals;
            Point3DCollection Vertices;
            Int32Collection TriangleIndices;
            ModelUIElement3D modelui = new ModelUIElement3D();
            ContainerUIElement3D container = new ContainerUIElement3D();

            PlanarTextureCoordinateGenerator tx = new PlanarTextureCoordinateGenerator();

            k = ms3dfile.GetNumGroups();
            for (l = 0; l < k; l++)
            {
                ms3dfile.GetGroupAt(l, ref ms3dgroup);
                modelui = new ModelUIElement3D();
                Model = new GeometryModel3D();
                Mesh = new MeshGeometry3D();

                Vertices = new Point3DCollection();
                Normals = new Vector3DCollection();

                TriangleIndices = new Int32Collection();


                j = ms3dgroup.numtriangles;
                for (i = 0; i < j; i++)
                {
                    ms3dfile.GetTriangleAt(ms3dgroup.triangleIndices[i], ref ms3dtriangle);

                    ms3dfile.GetVertexAt(ms3dtriangle.vertexIndices[0], ref ms3dvertex);
                    Vertices.Add(new Point3D(ms3dvertex.vertex[0], ms3dvertex.vertex[1], ms3dvertex.vertex[2]));
                    TriangleIndices.Add(Vertices.Count - 1);

                    ms3dfile.GetVertexAt(ms3dtriangle.vertexIndices[1], ref ms3dvertex);
                    Vertices.Add(new Point3D(ms3dvertex.vertex[0], ms3dvertex.vertex[1], ms3dvertex.vertex[2]));
                    TriangleIndices.Add(Vertices.Count - 1);

                    ms3dfile.GetVertexAt(ms3dtriangle.vertexIndices[2], ref ms3dvertex);
                    Vertices.Add(new Point3D(ms3dvertex.vertex[0], ms3dvertex.vertex[1], ms3dvertex.vertex[2]));
                    TriangleIndices.Add(Vertices.Count - 1);


                    Normals.Add(new Vector3D(ms3dtriangle.vertexNormals[0][0], ms3dtriangle.vertexNormals[0][1], ms3dtriangle.vertexNormals[0][2]));
                    Normals.Add(new Vector3D(ms3dtriangle.vertexNormals[1][0], ms3dtriangle.vertexNormals[1][1], ms3dtriangle.vertexNormals[1][2]));
                    Normals.Add(new Vector3D(ms3dtriangle.vertexNormals[2][0], ms3dtriangle.vertexNormals[2][1], ms3dtriangle.vertexNormals[2][2]));

                }
                Mesh.Normals = Normals;
                Mesh.TriangleIndices = TriangleIndices;
                Mesh.Positions = Vertices;
                Model.Geometry = Mesh;

                ms3dfile.GetMaterialAt(ms3dgroup.materialIndex, ref ms3dmaterial);
 
                SolidColorBrush solid_color = new SolidColorBrush();
                solid_color.Color = Color.FromRgb((byte)(ms3dmaterial.diffuse[0] * 255), (byte)(ms3dmaterial.diffuse[1] * 255), (byte)(ms3dmaterial.diffuse[2] * 255));

                DiffuseMaterial Material = new DiffuseMaterial(solid_color);
                Model.Material = Material;

                modelui.Model = Model;
                container.Children.Add(modelui);
            }

            ms3dfile.Clear();

            return container;

        }
    }
}
