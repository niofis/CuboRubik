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

namespace CuboRubik
{
   
    [ContentProperty("Children")]
    public class CuboC : UIElement3D
    {
        private int x, y, z;
        private string nombre;
        private ArrayList colores;
        private Transform3DGroup transforms;
        private ModelUIElement3D[] lados;
        private Viewport3D viewport;

        public enum Colores
        {
            Gris=0,
            Naranja = 1,
            Verde=2,
            Azul=3,
            Rojo=4,
            Amarillo=5
        }

        public CuboC(int x, int y, int z, string nombre, Viewport3D view)
        {
            Children = new ObservableCollection<ModelUIElement3D>();
            Children.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(children_CollectionChanged);
            this.x = x;
            this.y = y;
            this.z = z;
            this.nombre = nombre;
            colores = new ArrayList();
            transforms = new Transform3DGroup();
            viewport = view;
        }

        #region Children Property

        public static DependencyProperty ChildrenProperty = DependencyProperty.RegisterAttached("Children", typeof(ObservableCollection<ModelUIElement3D>), typeof(CuboC));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ObservableCollection<ModelUIElement3D> Children
        {
            get
            {
                return (ObservableCollection<ModelUIElement3D>)GetValue(ChildrenProperty);
            }
            set
            {
                SetValue(ChildrenProperty, value);
            }
        }

        protected override int Visual3DChildrenCount
        {
            get
            {
                return Children.Count;
            }
        }

        protected override Visual3D GetVisual3DChild(int index)
        {
            return Children[index];
        }

        private void children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //primitive collection change handler to manage Visual 3D children
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.OldItems == null)
                {
                    AddVisual3DChild((ModelUIElement3D)e.NewItems[0]);
                    colores.Add(((SolidColorBrush)((DiffuseMaterial)((GeometryModel3D)(((ModelUIElement3D)e.NewItems[0]).Model)).Material).Brush).Color);
                    ((ModelUIElement3D)e.NewItems[0]).Transform = transforms;
                    if (Children.Count == 6)
                        AcomodaLados();
                }
                else
                {
                    int i = e.NewStartingIndex - 1;
                    AddVisual3DChild((ModelUIElement3D)e.NewItems[i]);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RemoveVisual3DChild((ModelUIElement3D)e.OldItems[e.OldStartingIndex]);
            }
        }


        #endregion

        #region Otras Propiedades
        public int X
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

        public int Y
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

        public int Z
        {
            get
            {
                return z;
            }
            set
            {
                z = value;
            }
        }

        public string Nombre
        {
            get
            {
                return nombre;
            }
            set
            {
                nombre = value;
            }
        }

        public Transform3DGroup Transforms
        {
            get
            {
                return transforms;
            }
        }

        public ModelUIElement3D[] Lados
        {
            get
            {
                return lados;
            }
        }

        public ArrayList ColoresLados
        {
            get
            {
                return colores;
            }
        }
        #endregion

        #region Manejo de Eventos
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mouseposition = e.GetPosition(this);
                Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
                Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 100);
                PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
                RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
                VisualTreeHelper.HitTest(viewport, null, HTEventTest, pointparams);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

        }

        public HitTestResultBehavior HTEventTest(System.Windows.Media.HitTestResult rawresult)
        {
            RayHitTestResult result = rawresult as RayHitTestResult;
            if (result != null)
            {
                ModelUIElement3D hit = (ModelUIElement3D)result.VisualHit;
                int x;
                for (x = 0; x < 6; x++)
                {
                    if (hit == lados[x])
                        break;
                }
                RiseOnClickLado(x, result.PointHit);
            }
            return HitTestResultBehavior.Stop;
        }


        public delegate void ClickLadoEventHandler(object sender, CuboCEventArgs e);

        public static readonly RoutedEvent EvClickLado = EventManager.RegisterRoutedEvent(
        "OnClickLado", RoutingStrategy.Bubble, typeof(ClickLadoEventHandler), typeof(Flechas));

        public event ClickLadoEventHandler OnClickLado
        {
            add { AddHandler(EvClickLado, value); }
            remove { RemoveHandler(EvClickLado, value); }
        }

        private void RiseOnClickLado(int lado, Point3D point)
        {
            CuboCEventArgs args = new CuboCEventArgs(CuboC.EvClickLado);
            args.Cubo = this;
            args.HitPoint = point;
            args.Lado = lado;
            RaiseEvent(args);
        }

        #endregion

        public void ActualizaCoords(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public ModelUIElement3D TraeLado(Colores color)
        {
            return lados[(int)color];
        }
        public ModelUIElement3D TraeLado(int lado)
        {
            return lados[lado];
        }

        private void AcomodaLados()
        {
            Point3D testpoint3D = new Point3D(0, 0, 0);
            lados = new ModelUIElement3D[6];

            //Arriba - 1
            Vector3D testdirection = new Vector3D(0, -1, 0); 
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
            VisualTreeHelper.HitTest(this, null, HTResult, rayparams);

            //Abajo - 6
            testdirection = new Vector3D(0, 1, 0);
            rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
            VisualTreeHelper.HitTest(this, null, HTResult, rayparams);

            //Izquierda - 4
            testdirection = new Vector3D(-1, 0, 0);
            rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
            VisualTreeHelper.HitTest(this, null, HTResult, rayparams);

            //Derecha - 3
            testdirection = new Vector3D(1, 0, 0);
            rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
            VisualTreeHelper.HitTest(this, null, HTResult, rayparams);

            //Frente - 2
            testdirection = new Vector3D(0, 0, -1);
            rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
            VisualTreeHelper.HitTest(this, null, HTResult, rayparams);

            //Atras - 5
            testdirection = new Vector3D(0, 0, 1);
            rayparams = new RayHitTestParameters(testpoint3D, testdirection);    //test for a result in the Viewport3D    
            VisualTreeHelper.HitTest(this, null, HTResult, rayparams);
        }

        public HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            RayHitTestResult rayResult = rawresult as RayHitTestResult;
            if (rayResult != null)
            {
                RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;
                if (rayMeshResult != null)
                {
                    ModelUIElement3D hitgeo = rayMeshResult.VisualHit as ModelUIElement3D;
                    if (rayResult.PointHit.X > 0)
                    {
                        lados[3] = hitgeo;
                    }
                    else if (rayResult.PointHit.X < 0)
                    {
                        lados[2] = hitgeo;
                    }
                    else if (rayResult.PointHit.Y > 0)
                    {
                        lados[5] = hitgeo;
                    }
                    else if (rayResult.PointHit.Y < 0)
                    {
                        lados[0] = hitgeo;
                    }
                    else if (rayResult.PointHit.Z > 0)
                    {
                        lados[4] = hitgeo;
                    }
                    else if (rayResult.PointHit.Z < 0)
                    {
                        lados[1] = hitgeo;
                    }
                    // do something with the model hit, like change     
                    // colors or start an animation storyboard        
                }
            }
            return HitTestResultBehavior.Stop;
        }

        public void SetColorLado(int lado, Color color)
        {
            int x=0;
            foreach(ModelUIElement3D m in Children)
            {
                if (m == lados[lado])
                    colores[x] = color;
                x++;
            }
            UnHighlight();
        }

        public void Highlight()
        {
            int x = 0;
            Color c;
            foreach (ModelUIElement3D mod in Children)
            {
                c = (Color)colores[x++];
                ((GeometryModel3D)mod.Model).Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb((byte)(c.R | 63), (byte)(c.G | 63), (byte)(c.B | 63))));
            }
        }

        public void UnHighlight()
        {
            int x = 0;
            Color c;
            foreach (ModelUIElement3D mod in Children)
            {
                c = (Color)colores[x++];
                ((GeometryModel3D)mod.Model).Material = new DiffuseMaterial(new SolidColorBrush(c));
            }
        }

        #region Otras Clases
        #endregion


        #region HitTest Functions
        public HitData GetHitData(Point punto)
        {
            RayMeshGeometry3DHitTestResult result = (RayMeshGeometry3DHitTestResult)VisualTreeHelper.HitTest(viewport, punto);
            if (result != null)
            {
                ModelUIElement3D hit = (ModelUIElement3D)result.VisualHit;
                int x;
                for (x = 0; x < 6; x++)
                {
                    if (hit == lados[x])
                        break;
                }
                return new HitData(this,result.PointHit,x);
            }
            return null;
        }
        #endregion

    }

    public class HitData
    {
        private CuboC c;
        private Point3D p;
        private int l;
        private CuboC.Colores col;

        public HitData(CuboC cubo, Point3D punto, int lado)
        {
            c = cubo;
            p = punto;
            l = lado;
            col = (CuboC.Colores)lado;
        }

        public CuboC Cubo
        {
            get
            {
                return c;// return (CuboC)GetValue(CuboProperty);
            }
            set
            {
                c = (CuboC)value;// SetValue(CuboProperty, value);
            }
        }

        public Point3D HitPoint
        {
            get
            {
                return p;// return (Point3D)GetValue(HitPointProperty);
            }
            set
            {
                p = (Point3D)value;// SetValue(HitPointProperty, value);
            }
        }

        public int Lado
        {
            get
            {
                return l;// return (int)GetValue(LadoProperty);
            }
            set
            {
                l = value;// SetValue(LadoProperty, value);
            }
        }

        public CuboC.Colores ColorLado
        {
            get
            {
                return col;
            }
        }

        
    }

}
