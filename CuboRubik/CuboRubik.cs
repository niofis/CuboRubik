using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using _3DTools;

using System.ComponentModel;
using System.Windows.Markup;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media.Animation;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

using System.Threading;

namespace CuboRubik
{
    class Cubo
    {
        private Viewport3D viewport;
        private MS3DLoader loader;
        private CuboC[][][] cubo_c;
        private ModelUIElement3D[][][] lados;
        private CuboC.Colores[][][] colores_lados;
        private ContainerUIElement3D container;
        private const int t_lado=3;
        private Vector3D eje_x, eje_y, eje_z;
        private Point punto_anterior;
        private bool locked;
        private ArrayList bloque;
        private Eje eje_activo;
        private int valor_activo;
        private Direccion direccion_activa;
        private Lados lado_activo;
        private ModelUIElement3D sector_activo;
        private Point coords_activas;
        private TextBlock msg;
        private RotateTransform3D current_rotation;
        private AxisAngleRotation3D rotation;
        private Flechas flechas;
        private HitData cubo_sel;
        private HitData cubo_act;
        private ArrayList movimientos;
        
        public enum Eje
        {
            X,
            Y,
            Z
        };

        public enum Direccion
        {
            Arriba=0,
            Abajo=1,
            Izquierda=2,
            Derecha=3
        }

        public enum Lados
        {
            Arriba=0,
            Frente=1,
            Derecho=2,
            Izquierdo=3,
            Atras=4,
            Abajo=5
        }

        public Cubo(Viewport3D v3d, TextBlock label)
        {
            movimientos = new ArrayList();
            msg = label;
            viewport = v3d;
            loader = new MS3DLoader();
            container = new ContainerUIElement3D();
            eje_x = new Vector3D(1, 0, 0);
            eje_y = new Vector3D(0, 1, 0);
            eje_z = new Vector3D(0, 0, 1);
            //arrastre = false;
            IniciaEscena();
            CargaFlechas();
            GeneraCuboC();
            
        }

        #region Region Abrir y Guardar
        public void Guardar(Stream f)
        {
            XmlTextWriter xmlw = new XmlTextWriter(f, Encoding.Unicode);
            xmlw.Formatting = Formatting.Indented;
            xmlw.WriteStartDocument();
            xmlw.WriteStartElement("Movimientos");
            xmlw.WriteAttributeString("GT", "100");
            foreach(Registro r in movimientos)
            {
                xmlw.WriteStartElement("Registro");
                xmlw.WriteAttributeString("Lado", ((int)r.Lado).ToString());
                xmlw.WriteAttributeString("X", ((int)r.SectorX).ToString());
                xmlw.WriteAttributeString("Y", ((int)r.SectorY).ToString());
                xmlw.WriteAttributeString("Direccion", ((int)r.Dir).ToString());
                xmlw.WriteEndElement();
            }
            xmlw.WriteEndElement();
            xmlw.WriteEndDocument();
            xmlw.Flush();
            xmlw.Close();
        }

        public void Abrir(Stream f)
        {
            XmlTextReader xmlr = new XmlTextReader(f);
            xmlr.Read();
            while (!xmlr.IsStartElement())
               if(!xmlr.Read())
                   return; ;

            XmlReader read = xmlr.ReadSubtree();

            while(read.Name != "Movimientos")
                if (!read.Read())
                    return; 

            
            int mins = Int32.Parse(read.GetAttribute("GT"));
            Registro r;
            while (read.Read())
            {
                if (read.Name == "Registro")
                {
                    r = new Registro();
                    r.Lado = (Lados)Int32.Parse(read.GetAttribute("Lado"));
                    r.SectorX = Int32.Parse(read.GetAttribute("X"));
                    r.SectorY = Int32.Parse(read.GetAttribute("Y"));
                    r.Dir = (Direccion)Int32.Parse(read.GetAttribute("Direccion"));
                    movimientos.Add(r);
                }
            }
            xmlr.Close();
            PlayMoves();
        }

        private void PlayMoves()
        {
            foreach(Registro r in movimientos)
            {
                GiraBloque(r.Lado, new Point(r.SectorX, r.SectorY), r.Dir, 0, true, false);
            }
        }
        #endregion

        #region Region Registro
        class Registro
        {
            private Lados l;
            private int x;
            private int y;
            private Direccion d;
            public Lados Lado
            {
                get
                {
                    return l;
                }
                set
                {
                    l = value;
                }
            }
            public int SectorX
            {
                get
                {
                    return x;
                }
                set
                {
                    x = value;
                }
            }
            public int SectorY
            {
                get
                {
                    return y;
                }
                set
                {
                    y = value;
                }
            }
            public Direccion Dir
            {
                get
                {
                    return d;
                }
                set
                {
                    d = value;
                }
            }
            public Registro() { }
            public Registro(Lados lado, int sector_x, int sector_y, Direccion direccion)
            {
                l = lado;
                x = sector_x;
                y = sector_y;
                d = direccion;
            }
            //Serializar
            //Deserializar
        }

        private void GuardaRegistro(Lados lado, int sector_x, int sector_y, Direccion direccion)
        {
            movimientos.Add(new Registro(lado, sector_x, sector_y, direccion));
        }

        private Registro NegateReg(Registro registro)
        {
            Registro sal=new Registro(registro.Lado,registro.SectorX,registro.SectorY,registro.Dir);
            switch (sal.Dir)
            {
                case Direccion.Arriba:
                    sal.Dir = Direccion.Abajo;
                    break;
                case Direccion.Abajo:
                    sal.Dir = Direccion.Arriba;
                    break;
                case Direccion.Izquierda:
                    sal.Dir = Direccion.Derecha;
                    break;
                case Direccion.Derecha:
                    sal.Dir = Direccion.Izquierda;
                    break;
            }
            return sal;
        }

        #endregion

        #region Region Flechas
        private void CargaFlechas()
        {
            flechas = new Flechas();
            container.Children.Add(flechas);
            flechas.OnArriba += new RoutedEventHandler(FlechaArriba);
            flechas.OnAbajo += new RoutedEventHandler(FlechaAbajo);
            flechas.OnIzquierda += new RoutedEventHandler(FlechaIzquierda);
            flechas.OnDerecha += new RoutedEventHandler(FlechaDerecha);
        }

        private void FlechaArriba(object sender, RoutedEventArgs e)
        {
            flechas.Mostrar(false);
        }

        private void FlechaAbajo(object sender, RoutedEventArgs e)
        {

        }

        private void FlechaIzquierda(object sender, RoutedEventArgs e)
        {

        }

        private void FlechaDerecha(object sender, RoutedEventArgs e)
        {

        }
        public void MostrarFlechas(bool visible)
        {
            if (visible && !container.Children.Contains(flechas))
            {
                container.Children.Add(flechas);
            }
            else if (!visible && container.Children.Contains(flechas))
            {
                container.Children.Remove(flechas);
            }
        }
        #endregion

        public void Revolver()
        {
            Random rnd = new Random();
            for (int x = 0; x < 20; x++)
            {
                GiraBloque((Lados)rnd.Next(6), new Point(rnd.Next(t_lado), rnd.Next(t_lado)), (Direccion)rnd.Next(4), 0, true, true);
            }
        }

        private void IniciaEscena()
        {
            Model3DGroup group = new Model3DGroup();
            ModelVisual3D visual3d = new ModelVisual3D();
            PerspectiveCamera camera = new PerspectiveCamera();


            camera.Position = new Point3D(0, 0, 100);
            camera.LookDirection = new Vector3D(0, 0, -1);
            camera.FieldOfView = 40;
            camera.FarPlaneDistance = 5000;
            camera.UpDirection = new Vector3D(0, 1, 0);
            camera.NearPlaneDistance = 0.1;

            viewport.Camera = camera;

            //OrthographicCamera myOCamera = new OrthographicCamera(new Point3D(0, 0, -3), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0), 3);
            //viewport.Camera = myOCamera;


            /*DirectionalLight light = new DirectionalLight();
            light.Color = Colors.White;
            light.Direction = new Vector3D(-0.61, -0.5, -0.61);
             * */

            PointLight pl = new PointLight();
            pl.Position = new Point3D(0, 0, 50);
            pl.Color = Brushes.White.Color;
            pl.Range = 310;
            pl.ConstantAttenuation = 1.0;

            AmbientLight al = new AmbientLight();
            al.Color = Brushes.DarkGray.Color;

            visual3d.Content = group;

            viewport.Children.Add(container);
            group.Children.Add(pl);
            group.Children.Add(al);
            container.Children.Add(visual3d);
        }

        private void GeneraCuboC()
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();

            System.IO.Stream f =
                thisExe.GetManifestResourceStream("CuboRubik.meshes.cubo.ms3d");

            TranslateTransform3D translate;

            lados = new ModelUIElement3D[6][][];
            colores_lados = new CuboC.Colores[6][][];
            for (int l = 0; l < 6; l++)
            {
                lados[l] = new ModelUIElement3D[3][];
                colores_lados[l] = new CuboC.Colores[3][];
                for (int y = 0; y < 3; y++)
                {
                    lados[l][y] = new ModelUIElement3D[3];
                    colores_lados[l][y] = new CuboC.Colores[3];
                }
            }

            cubo_c = new CuboC[t_lado][][];
            for (int z = 0; z < t_lado; z++)
            {
                cubo_c[z] = new CuboC[t_lado][];
                for (int y = 0; y < t_lado; y++)
                {
                    cubo_c[z][y] = new CuboC[t_lado];
                    for (int x = 0; x < t_lado; x++)
                    {
                        //cubo_c[z][y][x] = new ContainerUIElement3D();
                        cubo_c[z][y][x] = new CuboC(x,y,z,"",viewport);

                        foreach (ModelUIElement3D model in loader.Load(f).Children)
                        {
                            cubo_c[z][y][x].Children.Add(model);
                        }
                        f.Position = 0;

                        translate = new TranslateTransform3D(x*7.05-7, -y*7.05+7, -z*7.05+7);
                        cubo_c[z][y][x].Transforms.Children.Add(translate);

                        container.Children.Add(cubo_c[z][y][x]);
                        
                        cubo_c[z][y][x].MouseDown += new MouseButtonEventHandler(CuboMouseDown);
                        cubo_c[z][y][x].MouseUp += new MouseButtonEventHandler(CuboMouseUp);
                        cubo_c[z][y][x].MouseEnter += new MouseEventHandler(CuboMouseEnter);
                        cubo_c[z][y][x].MouseLeave += new MouseEventHandler(CuboMouseLeave);
                        cubo_c[z][y][x].MouseMove += new MouseEventHandler(CuboMouseMove);
                    }
                }
            }
            f.Close();
            ArreglaColores();
            IdentificaLados();
        }

        private void ArreglaColores()
        {
            //Centro
            cubo_c[1][1][1].SetColorLado(0,Colors.Black);
            cubo_c[1][1][1].SetColorLado(1,Colors.Black);
            cubo_c[1][1][1].SetColorLado(2,Colors.Black);
            cubo_c[1][1][1].SetColorLado(3,Colors.Black);
            cubo_c[1][1][1].SetColorLado(4,Colors.Black);
            cubo_c[1][1][1].SetColorLado(5,Colors.Black);

            //Esquinas Frontales
            cubo_c[0][0][0].SetColorLado(5,Colors.Black);
            cubo_c[0][0][0].SetColorLado(4,Colors.Black);
            cubo_c[0][0][0].SetColorLado(2,Colors.Black);

            cubo_c[0][0][2].SetColorLado(5,Colors.Black);
            cubo_c[0][0][2].SetColorLado(4,Colors.Black);
            cubo_c[0][0][2].SetColorLado(3,Colors.Black);

            cubo_c[0][2][0].SetColorLado(4,Colors.Black);
            cubo_c[0][2][0].SetColorLado(0,Colors.Black);
            cubo_c[0][2][0].SetColorLado(2,Colors.Black);

            cubo_c[0][2][2].SetColorLado(4,Colors.Black);
            cubo_c[0][2][2].SetColorLado(0,Colors.Black);
            cubo_c[0][2][2].SetColorLado(3,Colors.Black);
            
            //Esquinas Posteriores
            cubo_c[2][0][0].SetColorLado(1,Colors.Black);
            cubo_c[2][0][0].SetColorLado(5,Colors.Black);
            cubo_c[2][0][0].SetColorLado(2,Colors.Black);

            cubo_c[2][0][2].SetColorLado(1,Colors.Black);
            cubo_c[2][0][2].SetColorLado(5,Colors.Black);
            cubo_c[2][0][2].SetColorLado(3,Colors.Black);

            cubo_c[2][2][0].SetColorLado(1,Colors.Black);
            cubo_c[2][2][0].SetColorLado(0,Colors.Black);
            cubo_c[2][2][0].SetColorLado(2,Colors.Black);

            cubo_c[2][2][2].SetColorLado(1,Colors.Black);
            cubo_c[2][2][2].SetColorLado(0,Colors.Black);
            cubo_c[2][2][2].SetColorLado(3,Colors.Black);

            //Centros
            cubo_c[0][1][1].SetColorLado(0,Colors.Black);
            cubo_c[0][1][1].SetColorLado(2,Colors.Black);
            cubo_c[0][1][1].SetColorLado(3,Colors.Black);
            cubo_c[0][1][1].SetColorLado(4,Colors.Black);
            cubo_c[0][1][1].SetColorLado(5,Colors.Black);

            cubo_c[1][1][2].SetColorLado(0,Colors.Black);
            cubo_c[1][1][2].SetColorLado(1,Colors.Black);
            cubo_c[1][1][2].SetColorLado(3,Colors.Black);
            cubo_c[1][1][2].SetColorLado(4,Colors.Black);
            cubo_c[1][1][2].SetColorLado(5,Colors.Black);

            cubo_c[2][1][1].SetColorLado(0,Colors.Black);
            cubo_c[2][1][1].SetColorLado(2,Colors.Black);
            cubo_c[2][1][1].SetColorLado(3,Colors.Black);
            cubo_c[2][1][1].SetColorLado(1,Colors.Black);
            cubo_c[2][1][1].SetColorLado(5,Colors.Black);

            cubo_c[1][1][0].SetColorLado(0,Colors.Black);
            cubo_c[1][1][0].SetColorLado(1,Colors.Black);
            cubo_c[1][1][0].SetColorLado(2,Colors.Black);
            cubo_c[1][1][0].SetColorLado(4,Colors.Black);
            cubo_c[1][1][0].SetColorLado(5,Colors.Black);

            cubo_c[1][0][1].SetColorLado(1,Colors.Black);
            cubo_c[1][0][1].SetColorLado(2,Colors.Black);
            cubo_c[1][0][1].SetColorLado(3,Colors.Black);
            cubo_c[1][0][1].SetColorLado(4,Colors.Black);
            cubo_c[1][0][1].SetColorLado(5,Colors.Black);

            cubo_c[1][2][1].SetColorLado(0,Colors.Black);
            cubo_c[1][2][1].SetColorLado(1,Colors.Black);
            cubo_c[1][2][1].SetColorLado(2,Colors.Black);
            cubo_c[1][2][1].SetColorLado(3,Colors.Black);
            cubo_c[1][2][1].SetColorLado(4,Colors.Black);



            //Otros Frente
            cubo_c[0][0][1].SetColorLado(2,Colors.Black);
            cubo_c[0][0][1].SetColorLado(3,Colors.Black);
            cubo_c[0][0][1].SetColorLado(4,Colors.Black);
            cubo_c[0][0][1].SetColorLado(5,Colors.Black);

            cubo_c[0][1][0].SetColorLado(0,Colors.Black);
            cubo_c[0][1][0].SetColorLado(2,Colors.Black);
            cubo_c[0][1][0].SetColorLado(4,Colors.Black);
            cubo_c[0][1][0].SetColorLado(5,Colors.Black);

            cubo_c[0][2][1].SetColorLado(0,Colors.Black);
            cubo_c[0][2][1].SetColorLado(2,Colors.Black);
            cubo_c[0][2][1].SetColorLado(3,Colors.Black);
            cubo_c[0][2][1].SetColorLado(4,Colors.Black);

            cubo_c[0][1][2].SetColorLado(0,Colors.Black);
            cubo_c[0][1][2].SetColorLado(3,Colors.Black);
            cubo_c[0][1][2].SetColorLado(4,Colors.Black);
            cubo_c[0][1][2].SetColorLado(5,Colors.Black);

            //Otros Izquierda
            cubo_c[1][0][0].SetColorLado(1,Colors.Black);
            cubo_c[1][0][0].SetColorLado(2,Colors.Black);
            cubo_c[1][0][0].SetColorLado(4,Colors.Black);
            cubo_c[1][0][0].SetColorLado(5,Colors.Black);

            cubo_c[1][2][0].SetColorLado(0,Colors.Black);
            cubo_c[1][2][0].SetColorLado(2,Colors.Black);
            cubo_c[1][2][0].SetColorLado(1,Colors.Black);
            cubo_c[1][2][0].SetColorLado(4,Colors.Black);

            cubo_c[2][1][0].SetColorLado(0,Colors.Black);
            cubo_c[2][1][0].SetColorLado(1,Colors.Black);
            cubo_c[2][1][0].SetColorLado(2,Colors.Black);
            cubo_c[2][1][0].SetColorLado(5,Colors.Black);

            //Otros Derecha
            cubo_c[1][0][2].SetColorLado(1,Colors.Black);
            cubo_c[1][0][2].SetColorLado(3,Colors.Black);
            cubo_c[1][0][2].SetColorLado(4,Colors.Black);
            cubo_c[1][0][2].SetColorLado(5,Colors.Black);

            cubo_c[1][2][2].SetColorLado(0,Colors.Black);
            cubo_c[1][2][2].SetColorLado(1,Colors.Black);
            cubo_c[1][2][2].SetColorLado(3,Colors.Black);
            cubo_c[1][2][2].SetColorLado(4,Colors.Black);

            cubo_c[2][1][2].SetColorLado(0,Colors.Black);
            cubo_c[2][1][2].SetColorLado(1,Colors.Black);
            cubo_c[2][1][2].SetColorLado(5,Colors.Black);
            cubo_c[2][1][2].SetColorLado(3,Colors.Black);


            //Otros Arriba
            cubo_c[2][0][1].SetColorLado(1,Colors.Black);
            cubo_c[2][0][1].SetColorLado(2,Colors.Black);
            cubo_c[2][0][1].SetColorLado(3,Colors.Black);
            cubo_c[2][0][1].SetColorLado(5,Colors.Black);

            //Otros Abajo
            cubo_c[2][2][1].SetColorLado(0,Colors.Black);
            cubo_c[2][2][1].SetColorLado(1,Colors.Black);
            cubo_c[2][2][1].SetColorLado(2,Colors.Black);
            cubo_c[2][2][1].SetColorLado(3,Colors.Black);


        }

        private void IdentificaLados()
        {
            int i, j;
            for(j=0;j<t_lado;j++)
                for (i = 0; i < t_lado; i++)
                {
                    lados[(int)Lados.Frente][j][i]=cubo_c[0][j][i].TraeLado(CuboC.Colores.Naranja);
                    lados[(int)Lados.Arriba][j][i] = cubo_c[2 - j][0][i].TraeLado(CuboC.Colores.Gris);
                    lados[(int)Lados.Abajo][j][i] = cubo_c[j][2][i].TraeLado(CuboC.Colores.Amarillo);
                    lados[(int)Lados.Atras][j][i] = cubo_c[2][j][2-i].TraeLado(CuboC.Colores.Rojo);
                    lados[(int)Lados.Derecho][j][i] = cubo_c[i][j][2].TraeLado(CuboC.Colores.Verde);
                    lados[(int)Lados.Izquierdo][j][i] = cubo_c[2-i][j][0].TraeLado(CuboC.Colores.Azul);

                    colores_lados[(int)Lados.Frente][j][i] = CuboC.Colores.Naranja;
                    colores_lados[(int)Lados.Arriba][j][i] = CuboC.Colores.Gris;
                    colores_lados[(int)Lados.Abajo][j][i] = CuboC.Colores.Amarillo;
                    colores_lados[(int)Lados.Atras][j][i] = CuboC.Colores.Rojo;
                    colores_lados[(int)Lados.Derecho][j][i] = CuboC.Colores.Verde;
                    colores_lados[(int)Lados.Izquierdo][j][i] = CuboC.Colores.Azul;
                }
                
        }

        private void CuboMouseMove(object sender, MouseEventArgs e)
        {
            Point pt;
           // Vector3D pt3d;
            double dx, dy;

            if (((IInputElement)sender).IsMouseCaptured && punto_anterior != null)
            {
                //punto3D_anterior = new Vector3D(cubo_sel.HitPoint.X, cubo_sel.HitPoint.Y, cubo_sel.HitPoint.Z);
                cubo_act = ((CuboC)sender).GetHitData(e.GetPosition(viewport));
                pt = e.GetPosition((IInputElement)sender);
                
                if (!locked)
                {
                    /*dx = Math.Abs(cubo_sel.HitPoint.X - cubo_act.HitPoint.X);
                    dy = Math.Abs(cubo_sel.HitPoint.Y - cubo_act.HitPoint.Y);
                    dz = Math.Abs(cubo_sel.HitPoint.Z - cubo_act.HitPoint.Z);
                    //msg.Content = dx.ToString() + "," + dy.ToString() + "," + dz.ToString();
                    if(dx==0 && dy==0 && dz==0)
                    {
                        return;
                    }

                    if (dz < 0.00001)
                    {
                        if (dx < dy)
                        {
                            eje_activo = Eje.X;
                        }
                        else
                        {
                            eje_activo = Eje.Y;
                        }
                    }
                    else if (dy < 0.00001)
                    {
                        
                        if (dx < dz)
                        {
                            eje_activo = Eje.X;
                        }
                        else
                        {
                            eje_activo = Eje.Z;
                        }
                    }
                    else if (dx < 0.00001)
                    {
                        
                        if (dz < dy)
                        {
                            eje_activo = Eje.Z;
                        }
                        else
                        {
                            eje_activo = Eje.Y;
                        }
                    }*/


                    dx = pt.X - punto_anterior.X;
                    dy = pt.Y - punto_anterior.Y;
                    if (dx == 0 && dy == 0)
                    {
                        return;
                    }

                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        switch (lado_activo)
                        {
                            case Lados.Frente:
                                eje_activo = Eje.Y;
                                break;
                            case Lados.Atras:
                                eje_activo = Eje.Y;
                                break;
                            case Lados.Izquierdo:
                                eje_activo = Eje.Y;
                                break;
                            case Lados.Derecho:
                                eje_activo = Eje.Y;
                                break;
                            case Lados.Arriba:
                                eje_activo = Eje.Z;
                                break;
                            case Lados.Abajo:
                                eje_activo = Eje.Z;
                                break;
                        }
                    }
                    else
                    {
                        switch (lado_activo)
                        {
                            case Lados.Frente:
                                eje_activo = Eje.X;
                                break;
                            case Lados.Atras:
                                eje_activo = Eje.X;
                                break;
                            case Lados.Izquierdo:
                                eje_activo = Eje.Z;
                                break;
                            case Lados.Derecho:
                                eje_activo = Eje.Z;
                                break;
                            case Lados.Arriba:
                                eje_activo = Eje.X;
                                break;
                            case Lados.Abajo:
                                eje_activo = Eje.X;
                                break;
                        }
                    }


                    switch (eje_activo)
                    {
                        case Eje.X:
                            SeleccionaBloque(Eje.X, ((CuboC)sender).X);
                            break;
                        case Eje.Y:
                            SeleccionaBloque(Eje.Y, ((CuboC)sender).Y);
                            break;
                        case Eje.Z:
                            SeleccionaBloque(Eje.Z, ((CuboC)sender).Z);
                            break;
                    }

                    current_rotation = new RotateTransform3D();
                    Vector3D axis = new Vector3D();
                    switch (eje_activo)
                    {
                        case Eje.X:
                            axis.Y = 0;
                            axis.Z = 0;
                            axis.X = 1;// (cubo_act.HitPoint.X - cubo_sel.HitPoint.X) / dx;
                            break;
                        case Eje.Y:
                            axis.X = 0;
                            axis.Z = 0;
                            //if(lado_activo==Lados.Frente)
                            axis.Y = 1;//(cubo_sel.HitPoint.Y - cubo_act.HitPoint.Y) / dy;
                            break;
                        case Eje.Z:
                            axis.X = 0;
                            axis.Y = 0;
                            axis.Z = 1;//(cubo_act.HitPoint.Z - cubo_sel.HitPoint.Z) / dz;
                            break;
                    }
                    rotation = new AxisAngleRotation3D();
                    rotation.Axis = axis;
                    current_rotation.Rotation = rotation;
                    AplicaTransfBloque(current_rotation);

                    //Encuentra Direccion Actual
                    /*dx = cubo_act.HitPoint.X - cubo_sel.HitPoint.X;
                    dy = cubo_act.HitPoint.Y - cubo_sel.HitPoint.Y;
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        if (dx < 0)
                            direccion_activa = Direccion.Izquierda;
                        //Horizontal
                        else
                            direccion_activa = Direccion.Derecha;
                    }
                    else
                    {
                        //Vertical
                        if (dy < 0)
                            direccion_activa = Direccion.Abajo;
                        else
                            direccion_activa = Direccion.Arriba;
                    }*/
                }

                double dist = 0;// (pt.X - punto_anterior.X) * dir_original.X + (pt.Y - punto_anterior.Y) * dir_original.Y;
                /*if (eje_activo == Eje.Y)
                {
                    dist = pt.X - punto_anterior.X;
                    dist /= viewport.ActualWidth;
                    dist *= 180;
                }
                if (eje_activo == Eje.X)
                {
                    dist = pt.Y - punto_anterior.Y;
                    dist /= viewport.ActualHeight;
                    dist *= 180;
                }
                if (eje_activo == Eje.Z)
                {*/
                    dist = pt.Y - punto_anterior.Y;
                    dist += pt.X - punto_anterior.X;

                    dist /= viewport.ActualHeight;
                    dist *= 180;
                //}
                //msg.Content = dist;
                rotation.Angle = dist;

                //Track(rotation,pt);

                //punto_anterior = pt;

                locked = true;
                
            }
            else
            {
                locked = false;
                punto_anterior = e.GetPosition((IInputElement)sender);                
            }
        }

        private void AplicaTransfBloque(Transform3D transformacion)
        {
            foreach(CuboC c in bloque)
            {
                c.Transforms.Children.Add(transformacion);
            }
        }

        private void RemueveTransfBloque(Transform3D transformacion)
        {
            foreach (CuboC c in bloque)
            {
                c.Transforms.Children.Remove(transformacion);
            }
        }

        private void SeleccionaBloque(Eje e, int valor)
        {
            valor_activo = valor;
            bloque = new ArrayList();
            for (int z = 0; z < t_lado; z++)
                for (int y = 0; y < t_lado; y++)
                    for (int x = 0; x < t_lado; x++)
                    {
                        switch (e)
                        {
                            case Eje.X:
                                if (cubo_c[z][y][x].X == valor)
                                    bloque.Add(cubo_c[z][y][x]);
                                break;
                            case Eje.Y:
                                if (cubo_c[z][y][x].Y == valor)
                                    bloque.Add(cubo_c[z][y][x]);
                                break;
                            case Eje.Z:
                                if (cubo_c[z][y][x].Z == valor)
                                    bloque.Add(cubo_c[z][y][x]);
                                break;
                        }
                    }

        }

        private void GiraBloque()
        {
            switch (eje_activo)
            {
                case Eje.X:
                    if (lado_activo == Lados.Atras)
                    {
                        if (rotation.Angle > 0)
                            direccion_activa = Direccion.Arriba;
                        else
                            direccion_activa = Direccion.Abajo;
                        break;
                    }
                    else
                    {
                        if (rotation.Angle < 0)
                            direccion_activa = Direccion.Arriba;
                        else
                            direccion_activa = Direccion.Abajo;
                        break;
                    }
                case Eje.Y:
                    if (rotation.Angle < 0)
                        direccion_activa = Direccion.Izquierda;
                    else
                        direccion_activa = Direccion.Derecha;
                    break;
                case Eje.Z:
                    if (lado_activo == Lados.Arriba)
                    {
                        if (rotation.Angle < 0)
                            direccion_activa = Direccion.Derecha;
                        else
                            direccion_activa = Direccion.Izquierda;
                    }
                    else if (lado_activo == Lados.Abajo)
                    {
                        if (rotation.Angle > 0)
                            direccion_activa = Direccion.Derecha;
                        else
                            direccion_activa = Direccion.Izquierda;
                    }
                    else if(lado_activo==Lados.Izquierdo)
                    {
                        if (rotation.Angle < 0)
                            direccion_activa = Direccion.Arriba;
                        else
                            direccion_activa = Direccion.Abajo;
                    }
                    else if (lado_activo == Lados.Derecho)
                    {
                        if (rotation.Angle > 0)
                            direccion_activa = Direccion.Arriba;
                        else
                            direccion_activa = Direccion.Abajo;
                    }
                    break;
                        
            }
            msg.Text = "Lado= " + lado_activo + " Direccion=" + direccion_activa + " Eje=" + eje_activo;
            //GiraBloque(eje_activo, valor_activo,coords_activas, direccion_activa, rotation.Angle, true);
            GiraBloque(lado_activo, coords_activas, direccion_activa, rotation.Angle, true,true);
            
            //GiraBloque(eje_activo,valor_activo

        }

        //private void GiraBloque(Eje e,int valor,Point cords_sector,Direccion dir,double angulo, bool animar)
        private void GiraBloque(Lados lado_sel,Point cords_sector,Direccion dir,double angulo, bool animar, bool guardar)
        {
            if(guardar)
                GuardaRegistro(lado_sel,(int)cords_sector.X,(int)cords_sector.Y,dir);
            Eje e=Eje.X;
            int valor=0;
            switch (lado_sel)
            {
                case Lados.Arriba:

                    if (dir == Direccion.Derecha)
                    {
                        lado_sel = Lados.Derecho;
                        dir = Direccion.Abajo;
                        e = Eje.Z;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Arriba, Lados.Derecho);
                    }
                    else if (dir == Direccion.Izquierda)
                    {
                        lado_sel = Lados.Derecho;
                        dir = Direccion.Arriba;
                        e = Eje.Z;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Arriba, Lados.Derecho);
                    }
                    else if (dir == Direccion.Abajo)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Abajo;
                        e = Eje.X;
                    }
                    else if (dir == Direccion.Arriba)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Arriba;
                        e = Eje.X;
                    }
                    break;
                case Lados.Abajo:
                    if (dir == Direccion.Derecha)
                    {
                        lado_sel = Lados.Derecho;
                        dir = Direccion.Arriba;
                        e = Eje.Z;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Abajo, Lados.Derecho);
                    }
                    else if (dir == Direccion.Izquierda)
                    {
                        lado_sel = Lados.Derecho;
                        dir = Direccion.Abajo;
                        e = Eje.Z;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Abajo, Lados.Derecho);
                    }
                    else if (dir == Direccion.Abajo)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Abajo;
                        e = Eje.X;
                    }
                    else if (dir == Direccion.Arriba)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Arriba;
                        e = Eje.X;
                    }
                    break;
                case Lados.Izquierdo:
                    if (dir == Direccion.Abajo)
                    {
                        lado_sel = Lados.Derecho;
                        dir = Direccion.Arriba;
                        e = Eje.Z;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Izquierdo, Lados.Derecho);
                    }
                    else if (dir == Direccion.Arriba)
                    {
                        lado_sel = Lados.Derecho;
                        dir = Direccion.Abajo;
                        e = Eje.Z;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Izquierdo, Lados.Derecho);
                    }
                    else if (dir == Direccion.Izquierda)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Izquierda;
                        e = Eje.Z;
                    }
                    else if (dir == Direccion.Derecha)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Derecha;
                        e = Eje.Z;
                    }
                    break;
                case Lados.Atras:
                    if (dir == Direccion.Arriba)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Abajo;
                        e = Eje.X;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Atras, Lados.Frente);
                    }
                    else if (dir == Direccion.Abajo)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Arriba;
                        e = Eje.X;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Atras, Lados.Frente);
                    }
                    else if (dir == Direccion.Izquierda)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Derecha;
                        e = Eje.Y;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Atras, Lados.Frente);
                    }
                    else if (dir == Direccion.Derecha)
                    {
                        lado_sel = Lados.Frente;
                        dir = Direccion.Izquierda;
                        e = Eje.Y;
                        cords_sector = ConvierteCoordenadas(cords_sector, Lados.Atras, Lados.Frente);
                    }
                    break;
            }

            switch (lado_sel)
            {
                case Lados.Frente:
                    if (dir == Direccion.Arriba || dir == Direccion.Abajo)
                    { e = Eje.X; valor = (int)cords_sector.X; }
                    else if (dir == Direccion.Izquierda || dir == Direccion.Derecha)
                    {e = Eje.Y; valor = (int)cords_sector.Y; }
                    break;
                case Lados.Atras:
                    if (dir == Direccion.Arriba || dir == Direccion.Abajo)
                    { e = Eje.X; valor = 2-(int)cords_sector.X; }
                    else if (dir == Direccion.Izquierda || dir == Direccion.Derecha)
                    { e = Eje.Y; valor = (int)cords_sector.Y; }
                    break;
                case Lados.Derecho:
                    if (dir == Direccion.Arriba || dir == Direccion.Abajo)
                    {e = Eje.Z; valor = (int)cords_sector.X; }
                    else if (dir == Direccion.Izquierda || dir == Direccion.Derecha)
                    {e = Eje.Y; valor = (int)cords_sector.Y; }
                    break;
                case Lados.Izquierdo:
                    if (dir == Direccion.Arriba || dir == Direccion.Abajo)
                    {e = Eje.Z; valor = 2-(int)cords_sector.X; }
                    else if (dir == Direccion.Izquierda || dir == Direccion.Derecha)
                    { e = Eje.Y; valor = (int)cords_sector.Y; }
                    break;
                case Lados.Arriba:
                    if (dir == Direccion.Arriba || dir == Direccion.Abajo)
                    {e = Eje.X; valor = (int)cords_sector.X; }
                    else if (dir == Direccion.Izquierda || dir == Direccion.Derecha)
                    {e = Eje.Z; valor = 2-(int)cords_sector.Y; }
                    break;
                case Lados.Abajo:
                    if (dir == Direccion.Arriba || dir == Direccion.Abajo)
                    { e = Eje.X; valor = (int)cords_sector.X; }
                    else if (dir == Direccion.Izquierda || dir == Direccion.Derecha)
                    { e = Eje.Z; valor = (int)cords_sector.Y; }
                    break;
                
            }
            SeleccionaBloque(e, valor);
            RotateTransform3D rotate=new RotateTransform3D();
            AxisAngleRotation3D axis_rotation =  new AxisAngleRotation3D();
            DoubleAnimation da = new DoubleAnimation();
            AplicaTransfBloque(rotate);
            rotate.Rotation = axis_rotation;
            int x=0, y=0, z=0;
            Vector3D ax=new Vector3D();
            switch (e)
            {
                case Eje.X:
                    ax = new Vector3D(1, 0, 0);
                    x = valor;
                    break;
                case Eje.Y:
                    ax = new Vector3D(0, 1, 0);
                    y = valor;
                    break;
                case Eje.Z:
                    ax = new Vector3D(0, 0, 1);
                    z = valor;
                    break;
            }
            switch (lado_sel)
            {
                case Lados.Frente:
                    switch (dir)
                    {
                        case Direccion.Arriba:
                            ax.X *= -1;
                            ax.Y *= -1;
                            ax.Z *= -1;
                            break;
                        case Direccion.Izquierda:
                            ax.X *= -1;
                            ax.Y *= -1;
                            ax.Z *= -1;
                            break;
                    }
                    break;
                case Lados.Derecho:
                    switch (dir)
                    {
                        case Direccion.Abajo:
                            ax.X *= -1;
                            ax.Y *= -1;
                            ax.Z *= -1;
                            break;
                        case Direccion.Izquierda:
                            ax.X *= -1;
                            ax.Y *= -1;
                            ax.Z *= -1;
                            break;
                    }
                    break;
            }
            axis_rotation.Axis = ax;
            if (animar)
            {
                da.Duration = TimeSpan.FromSeconds(0.25);
                da.From = Math.Abs(angulo);
                da.To = 90;
                axis_rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, da);
            }
            else
            {
                axis_rotation.Angle = 90;
            }
            CuboC t_cubo;
            int i;            
            switch (dir)
            {
                case Direccion.Derecha:
                    for (i = 0; i < t_lado-1; i++)
                    {
                        t_cubo = cubo_c[0][y][i];
                        cubo_c[0][y][i] = cubo_c[2-i][y][0];
                        cubo_c[2 - i][y][0] = cubo_c[2][y][2 - i];
                        cubo_c[2][y][2 - i] = cubo_c[i][y][2];
                        cubo_c[i][y][2] = t_cubo;
                    }
                    break;
                case Direccion.Izquierda:
                    for (i = 0; i < t_lado-1; i++)
                    {
                        t_cubo = cubo_c[0][y][i];
                        cubo_c[0][y][i] = cubo_c[i][y][2];
                        cubo_c[i][y][2] = cubo_c[2][y][2 - i];
                        cubo_c[2][y][2 - i] = cubo_c[2-i][y][0];
                        cubo_c[2 - i][y][0] = t_cubo;
                    }
                    break;
                case Direccion.Arriba:
                    switch (e)
                    {
                        case Eje.X:
                            for (i = 0; i < t_lado-1; i++)
                            {
                                t_cubo=cubo_c[0][i][x];
                                cubo_c[0][i][x] = cubo_c[i][2][x];
                                cubo_c[i][2][x] = cubo_c[2][2-i][x];
                                cubo_c[2][2 - i][x] = cubo_c[2-i][0][x];
                                cubo_c[2 - i][0][x] = t_cubo;
                            }
                            break;
                        case Eje.Z:
                            for (i = 0; i < t_lado-1; i++)
                            {
                                t_cubo = cubo_c[z][i][2];
                                cubo_c[z][i][2] = cubo_c[z][2][2-i];
                                cubo_c[z][2][2 - i] = cubo_c[z][2-i][0];
                                cubo_c[z][2 - i][0] = cubo_c[z][0][i];
                                cubo_c[z][0][i] = t_cubo;
                            }
                            break;
                    }
                    break;
                case Direccion.Abajo:
                    switch (e)
                    {
                        case Eje.X:
                            for (i = 0; i < t_lado-1; i++)
                            {
                                t_cubo = cubo_c[0][i][x];
                                cubo_c[0][i][x] = cubo_c[2-i][0][x];
                                cubo_c[2 - i][0][x] = cubo_c[2][2 - i][x];
                                cubo_c[2][2 - i][x] = cubo_c[i][2][x];
                                cubo_c[i][2][x] = t_cubo;
                            }
                            break;
                        case Eje.Z:
                            for (i = 0; i < t_lado-1; i++)
                            {
                                t_cubo = cubo_c[z][i][2];
                                cubo_c[z][i][2] = cubo_c[z][0][i];
                                cubo_c[z][0][i] = cubo_c[z][2 - i][0];
                                cubo_c[z][2 - i][0] = cubo_c[z][2][2 - i];
                                cubo_c[z][2][2 - i] = t_cubo;
                            }
                            break;
                    }
                    break;
            }
            
            for (z = 0; z < t_lado; z++)
                for (y = 0; y < t_lado; y++)
                    for (x = 0; x < t_lado; x++)
                        cubo_c[z][y][x].ActualizaCoords(x, y, z);

            
            ModelUIElement3D lado;
            CuboC.Colores t_color;            
            
            x = (int)cords_sector.X;
            y = (int)cords_sector.Y;
            msg.Text = "Giro Eje=" + e + " Dir=" + dir + " Lado " + lado_sel;
            switch (dir)
            {
                case Direccion.Derecha:
                    for (i = 0; i < t_lado; i++)
                    {
                        lado = lados[(int)Lados.Frente][y][i];
                        lados[(int)Lados.Frente][y][i] = lados[(int)Lados.Izquierdo][y][i];
                        lados[(int)Lados.Izquierdo][y][i] = lados[(int)Lados.Atras][y][i];
                        lados[(int)Lados.Atras][y][i] = lados[(int)Lados.Derecho][y][i];
                        lados[(int)Lados.Derecho][y][i] = lado;

                        t_color = colores_lados[(int)Lados.Frente][y][i];
                        colores_lados[(int)Lados.Frente][y][i] = colores_lados[(int)Lados.Izquierdo][y][i];
                        colores_lados[(int)Lados.Izquierdo][y][i] = colores_lados[(int)Lados.Atras][y][i];
                        colores_lados[(int)Lados.Atras][y][i] = colores_lados[(int)Lados.Derecho][y][i];
                        colores_lados[(int)Lados.Derecho][y][i] = t_color;
                    }
                
                    if (y == 0)
                    {
                        GirarLado(Lados.Arriba, Direccion.Izquierda);
                    }
                    else if (y == 2)
                    {
                        GirarLado(Lados.Abajo, Direccion.Derecha);
                    }
                    break;
                case Direccion.Izquierda:
                    for (i = 0; i < t_lado; i++)
                    {
                        lado = lados[(int)Lados.Frente][y][i];
                        lados[(int)Lados.Frente][y][i] = lados[(int)Lados.Derecho][y][i];
                        lados[(int)Lados.Derecho][y][i] = lados[(int)Lados.Atras][y][i];
                        lados[(int)Lados.Atras][y][i] = lados[(int)Lados.Izquierdo][y][i];
                        lados[(int)Lados.Izquierdo][y][i] = lado;

                        t_color = colores_lados[(int)Lados.Frente][y][i];
                        colores_lados[(int)Lados.Frente][y][i] = colores_lados[(int)Lados.Derecho][y][i];
                        colores_lados[(int)Lados.Derecho][y][i] = colores_lados[(int)Lados.Atras][y][i];
                        colores_lados[(int)Lados.Atras][y][i] = colores_lados[(int)Lados.Izquierdo][y][i];
                        colores_lados[(int)Lados.Izquierdo][y][i] = t_color;
                    }
                    if (y == 0)
                    {
                        GirarLado(Lados.Arriba, Direccion.Derecha);
                    }
                    else if (y == 2)
                    {
                        GirarLado(Lados.Abajo, Direccion.Izquierda);
                    }
                    break;
                case Direccion.Arriba:
                    switch(e)
                    {
                        case Eje.X:
                            for (i = 0; i < t_lado; i++)
                            {
                                lado = lados[(int)Lados.Frente][i][x];
                                lados[(int)Lados.Frente][i][x] = lados[(int)Lados.Abajo][i][x];
                                lados[(int)Lados.Abajo][i][x] = lados[(int)Lados.Atras][2-i][2 - x];
                                lados[(int)Lados.Atras][2-i][2 - x] = lados[(int)Lados.Arriba][i][x];
                                lados[(int)Lados.Arriba][i][x] = lado;

                                t_color = colores_lados[(int)Lados.Frente][i][x];
                                colores_lados[(int)Lados.Frente][i][x] = colores_lados[(int)Lados.Abajo][i][x];
                                colores_lados[(int)Lados.Abajo][i][x] = colores_lados[(int)Lados.Atras][2-i][2 - x];
                                colores_lados[(int)Lados.Atras][2-i][2 - x] = colores_lados[(int)Lados.Arriba][i][x];
                                colores_lados[(int)Lados.Arriba][i][x] = t_color;
                            }
                            if (x == 0)
                            {
                                GirarLado(Lados.Izquierdo, Direccion.Izquierda);
                            }
                            else if (x == 2)
                            {
                                GirarLado(Lados.Derecho, Direccion.Derecha);
                            }
                            break;


                        case Eje.Z:
                            for (i = 0; i < t_lado; i++)
                            {
                                lado = lados[(int)Lados.Derecho][i][x];
                                lados[(int)Lados.Derecho][i][x] = lados[(int)Lados.Abajo][x][2 - i];
                                lados[(int)Lados.Abajo][x][2 - i] = lados[(int)Lados.Izquierdo][2-i][2 - x];
                                lados[(int)Lados.Izquierdo][2-i][2 - x] = lados[(int)Lados.Arriba][2 - x][i];
                                lados[(int)Lados.Arriba][2 - x][i] = lado;

                                t_color = colores_lados[(int)Lados.Derecho][i][x];
                                colores_lados[(int)Lados.Derecho][i][x] = colores_lados[(int)Lados.Abajo][x][2 - i];
                                colores_lados[(int)Lados.Abajo][x][2 - i] = colores_lados[(int)Lados.Izquierdo][i][2 - x];
                                colores_lados[(int)Lados.Izquierdo][i][2 - x] = colores_lados[(int)Lados.Arriba][2 - x][i];
                                colores_lados[(int)Lados.Arriba][2 - x][i] = t_color;
                            }
                            if (x == 0)
                            {
                                GirarLado(Lados.Frente, Direccion.Izquierda);
                            }
                            else if (x == 2)
                            {
                                GirarLado(Lados.Atras, Direccion.Derecha);
                            }
                            break;
                        
                    }
                    break;
                case Direccion.Abajo:
                    switch (e)
                    {
                        case Eje.X:
                            for (i = 0; i < t_lado; i++)
                            {
                                lado = lados[(int)Lados.Frente][i][x];
                                lados[(int)Lados.Frente][i][x] = lados[(int)Lados.Arriba][i][x];
                                lados[(int)Lados.Arriba][i][x] = lados[(int)Lados.Atras][2-i][2-x];
                                lados[(int)Lados.Atras][2-i][2-x] = lados[(int)Lados.Abajo][i][x];
                                lados[(int)Lados.Abajo][i][x] = lado;

                                t_color = colores_lados[(int)Lados.Frente][i][x];
                                colores_lados[(int)Lados.Frente][i][x] = colores_lados[(int)Lados.Arriba][i][x];
                                colores_lados[(int)Lados.Arriba][i][x] = colores_lados[(int)Lados.Atras][2-i][2 - x];
                                colores_lados[(int)Lados.Atras][2-i][2 - x] = colores_lados[(int)Lados.Abajo][i][x];
                                colores_lados[(int)Lados.Abajo][i][x] = t_color;
                            }
                            if (x == 0)
                            {
                                GirarLado(Lados.Izquierdo, Direccion.Derecha);
                            }
                            else if (x == 2)
                            {
                                GirarLado(Lados.Derecho, Direccion.Izquierda);
                            }
                            break;

                        case Eje.Z:
                            for (i = 0; i < t_lado; i++)
                            {
                                lado = lados[(int)Lados.Derecho][i][x];
                                lados[(int)Lados.Derecho][i][x] = lados[(int)Lados.Arriba][2 - x][i];
                                lados[(int)Lados.Arriba][2 - x][i] = lados[(int)Lados.Izquierdo][2-i][2 - x];
                                lados[(int)Lados.Izquierdo][2-i][2 - x] = lados[(int)Lados.Abajo][x][2 - i];
                                lados[(int)Lados.Abajo][x][2 - i] = lado;

                                t_color = colores_lados[(int)Lados.Derecho][i][x];
                                colores_lados[(int)Lados.Derecho][i][x] = colores_lados[(int)Lados.Arriba][2 - x][i];
                                colores_lados[(int)Lados.Arriba][2 - x][i] = colores_lados[(int)Lados.Izquierdo][2-i][2 - x];
                                colores_lados[(int)Lados.Izquierdo][2-i][2 - x] = colores_lados[(int)Lados.Abajo][x][2 - i];
                                colores_lados[(int)Lados.Abajo][x][2 - i] = t_color;
                            }
                            if (x == 0)
                            {
                                GirarLado(Lados.Frente, Direccion.Derecha);
                            }
                            else if (x == 2)
                            {
                                GirarLado(Lados.Atras, Direccion.Izquierda);
                            }
                            break;

                    }
                    break;
            }
        }

        //Solo frente o derecha en destino
        private Point ConvierteCoordenadas(Point coords, Lados origen, Lados destino)
        {
            int xo = (int)coords.X;
            int yo = (int)coords.Y;
            int xn=xo, yn=xo;
            switch (origen)
            {
                case Lados.Arriba:
                    switch (destino)
                    {
                        case Lados.Derecho:
                            xn = 2 - xo;
                            yn = 2 - yo;
                            break;
                    }
                    break;
                case Lados.Abajo:
                    switch (destino)
                    {
                        case Lados.Derecho:
                            xn = yo;
                            yn = 2-xo;
                            break;
                    }
                    break;
                case Lados.Izquierdo:
                    switch (destino)
                    {
                        case Lados.Derecho:
                            xn = 2-xo;
                            yn = yo;
                            break;
                    }
                    break;
                case Lados.Atras:
                    switch (destino)
                    {
                        case Lados.Frente:
                            xn = 2 - xo;
                            yn = yo;
                            break;
                    }
                    break;
            }
            return new Point(xn, yn);
        }

        private void GirarLado(Lados lado, Direccion dir)
        {
            int l = (int)lado;
            int i;
            ModelUIElement3D t_l;
            CuboC.Colores t_color;
            switch (dir)
            {
                case Direccion.Derecha:
                    for (i = 0; i < t_lado-1; i++)
                    {
                        t_l = lados[l][i][0];
                        lados[l][i][0] = lados[l][2][i];
                        lados[l][2][i] = lados[l][2-i][2];
                        lados[l][2-i][2] = lados[l][0][2-i];
                        lados[l][0][2 - i] = t_l;

                        t_color = colores_lados[l][0][i];
                        colores_lados[l][0][i] = colores_lados[l][2 - i][0];
                        colores_lados[l][2 - i][0] = colores_lados[l][2][2 - i];
                        colores_lados[l][2][2 - i] = colores_lados[l][i][2];
                        colores_lados[l][i][2] = t_color;
                    }
                    break;
                case Direccion.Izquierda:
                    for (i = 0; i < t_lado-1; i++)
                    {
                        t_l = lados[l][0][i];
                        lados[l][0][i] = lados[l][i][2];
                        lados[l][i][2] = lados[l][2][2 - i];
                        lados[l][2][2 - i] = lados[l][2 - i][0];
                        lados[l][2 - i][0] = t_l;

                        t_color = colores_lados[l][0][i];
                        colores_lados[l][0][i] = colores_lados[l][i][2];
                        colores_lados[l][i][2] = colores_lados[l][2][2 - i];
                        colores_lados[l][2][2 - i] = colores_lados[l][2 - i][0];
                        colores_lados[l][2 - i][0] = t_color;
                    }
                    
                    break;
            }
        }

        private void CuboMouseDown(object sender, MouseButtonEventArgs e)
        {
            
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                cubo_sel = ((CuboC)sender).GetHitData(e.GetPosition(viewport));
                //msg.Content = "X= " + ((CuboC)sender).X + " Y=" + ((CuboC)sender).Y + " Z=" + ((CuboC)sender).Z;
                IdentificaLado(((CuboC)sender));
                Mouse.Capture(((IInputElement)sender), CaptureMode.Element);
            }
        }

        private void IdentificaLado(CuboC cubo)
        {
            sector_activo = cubo.TraeLado(cubo_sel.Lado);
            for (int l = 0; l < 6; l++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        if (lados[l][y][x] == sector_activo)
                        {
                            coords_activas = new Point(x, y);
                            lado_activo = (Lados)l;
                            msg.Text = "Cubo(" + cubo.X + "," + cubo.Y + "," + cubo.Z + ") Lado " + lado_activo.ToString() + " (" + x + "," + y + ") " + colores_lados[l][y][x];
                        }
                    }
                }
            }
        }

        private void CuboMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (((IInputElement)sender).IsMouseCaptured)
            {
                Mouse.Capture(((IInputElement)sender), CaptureMode.None);
                cubo_sel = null;
                if (current_rotation != null)
                {
                    RemueveTransfBloque(current_rotation);
                    if (Math.Abs(rotation.Angle) > 30)
                        GiraBloque();
                }
                current_rotation = null;
                //GiraBloque(Eje);
            }
        }

        private void CuboMouseEnter(object sender, MouseEventArgs e)
        {
            
            ((CuboC)sender).Highlight();
            
        }

        private void CuboMouseLeave(object sender, MouseEventArgs e)
        {
            ((CuboC)sender).UnHighlight();
        }
    }
}
