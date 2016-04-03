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
    public class Flechas : UIElement3D
    {
        private ContainerUIElement3D[] flechas;
        private Color color;
        private Direcciones flecha_sel;
        enum Direcciones
        {
            Ninguna = -1,
            Izquierda = 0,
            Arriba = 1,
            Derecha = 2,
            Abajo = 3
        }

        public Flechas()
        {
            Children = new ObservableCollection<Visual3D>();
            Children.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(children_CollectionChanged);
            //color = Colors.CornflowerBlue;
            //color.A = 128;
            flecha_sel = (Direcciones)(-1);
            CargaFlechas();
        }

        #region Children Property

        public static DependencyProperty ChildrenProperty = DependencyProperty.RegisterAttached("Children", typeof(ObservableCollection<Visual3D>), typeof(Flechas));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ObservableCollection<Visual3D> Children
        {
            get
            {
                return (ObservableCollection<Visual3D>)GetValue(ChildrenProperty);
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
                    AddVisual3DChild((Visual3D)e.NewItems[0]);
                }
                else
                {
                    int i = e.NewStartingIndex - 1;
                    AddVisual3DChild((Visual3D)e.NewItems[i]);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RemoveVisual3DChild((Visual3D)e.OldItems[e.OldStartingIndex]);
            }
        }


        #endregion

        #region Propiedades
        

        public Double Alfa
        {
            get 
            { 
                return (Double)this.GetValue(PropiedadAlfa); 
            }
            set 
            { 
                this.SetValue(PropiedadAlfa, value); 
                ActualizaColor();
                 
            }
        }

        public static readonly DependencyProperty PropiedadAlfa = DependencyProperty.RegisterAttached(
  "Alfa", typeof(Double), typeof(Flechas));

        #endregion

        private void CargaFlechas()
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();

            System.IO.Stream f =
                thisExe.GetManifestResourceStream("CuboRubik.meshes.flechas.ms3d");
            MS3DLoader loader = new MS3DLoader();
            flechas = new ContainerUIElement3D[3];
            for (int x = 0; x < 3; x++)
            {
                flechas[x] = loader.Load(f);
                
                flechas[x].MouseEnter += new MouseEventHandler(FlechaMouseEnter);
                flechas[x].MouseLeave += new MouseEventHandler(FlechaMouseLeave);
                flechas[x].MouseDown += new MouseButtonEventHandler(FlechaMouseDown);
                flechas[x].MouseUp += new MouseButtonEventHandler(FlechaMouseUp);
                this.Children.Add(flechas[x]);
                f.Position = 0;
            }

            ((GeometryModel3D)((ModelUIElement3D)flechas[0].Children[0]).Model).Material = new DiffuseMaterial(new SolidColorBrush(Colors.CornflowerBlue));
            ((GeometryModel3D)((ModelUIElement3D)flechas[1].Children[0]).Model).Material = new DiffuseMaterial(new SolidColorBrush(Colors.LightSalmon));
            ((GeometryModel3D)((ModelUIElement3D)flechas[2].Children[0]).Model).Material = new DiffuseMaterial(new SolidColorBrush(Colors.LimeGreen));

            RotateTransform3D rotate = new RotateTransform3D();
            AxisAngleRotation3D aar = new AxisAngleRotation3D();
            aar.Axis = new Vector3D(0, 0, 1);
            aar.Angle = -90;
            rotate.Rotation = aar;
            flechas[1].Transform = rotate;

            rotate = new RotateTransform3D();
            aar = new AxisAngleRotation3D();
            aar.Axis = new Vector3D(1, 0, 0);
            aar.Angle = 90;
            rotate.Rotation = aar;
            flechas[2].Transform = rotate;

        }

        private void FlechaMouseDown(object sender, MouseButtonEventArgs e)
        {
            for (int x = 0; x < 4; x++)
            {
                if (sender == flechas[x])
                {
                    flecha_sel = (Direcciones)x;
                    break;
                }
            }
        }

        private void FlechaMouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (flecha_sel)
            {
                case Direcciones.Izquierda:
                    RaiseEvIzquierda();
                    break;
                case Direcciones.Arriba:
                    RaiseEvArriba();
                    break;
                case Direcciones.Derecha:
                    RaiseEvDerecha();
                    break;
                case Direcciones.Abajo:
                    RaiseEvAbajo();
                    break;

            }
            flecha_sel = Direcciones.Ninguna;
        }

        private void FlechaMouseEnter(object sender, MouseEventArgs e)
        {
            Highlight((ContainerUIElement3D)sender);
        }

        private void FlechaMouseLeave(object sender, MouseEventArgs e)
        {
            UnHighlight((ContainerUIElement3D)sender);
        }

        private void Highlight(ContainerUIElement3D flecha)
        {
            //((GeometryModel3D)((ModelUIElement3D)flecha.Children[0]).Model).Material = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
        }

        private void UnHighlight(ContainerUIElement3D flecha)
        {
            //((GeometryModel3D)((ModelUIElement3D)flecha.Children[0]).Model).Material = new DiffuseMaterial(new SolidColorBrush(color));
        }

        public void Mostrar(bool visible)
        {
            this.Alfa = (visible) ? 1 : 0;
            
            /*DoubleAnimation da = new DoubleAnimation();
            if (color.A > 0)
            {
                da.From = 1;
                da.To = 0;
            }
            else
            {
                da.From = (double)color.A / 255;
                da.To = 0;
            }
            da.Duration = new Duration(TimeSpan.FromSeconds(2));
            da.RepeatBehavior = RepeatBehavior.Forever;
            this.BeginAnimation(Flechas.PropiedadAlfa, da);
            da.Completed += new EventHandler(Completo);*/
        }
        public void Completo(object sender, EventArgs e)
        {
            MessageBox.Show("Aki");
        }

        public void ActualizaColor()
        {
            color.A = (byte)(this.Alfa * 255);
            for (int x = 0; x < 4; x++)
            {
                ((GeometryModel3D)((ModelUIElement3D)flechas[x].Children[0]).Model).Material = new DiffuseMaterial(new SolidColorBrush(color));
            }
        }

        private static void OnAlfaChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Flechas f = (Flechas)o;
            f.ActualizaColor();
        }

        #region Eventos Propios
        public static readonly RoutedEvent EvIzquierda = EventManager.RegisterRoutedEvent(
        "OnIzquierda", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Flechas));
        public static readonly RoutedEvent EvArriba = EventManager.RegisterRoutedEvent(
        "OnArriba", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Flechas));
        public static readonly RoutedEvent EvDerecha = EventManager.RegisterRoutedEvent(
        "OnDerecha", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Flechas));
        public static readonly RoutedEvent EvAbajo = EventManager.RegisterRoutedEvent(
        "OnAbajo", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Flechas));


        public event RoutedEventHandler OnIzquierda
        {
            add { AddHandler(EvIzquierda, value); }
            remove { RemoveHandler(EvIzquierda, value);}
        }

        public event RoutedEventHandler OnArriba
        {
            add { AddHandler(EvArriba, value); }
            remove { RemoveHandler(EvArriba, value); }
        }

        public event RoutedEventHandler OnDerecha
        {
            add { AddHandler(EvDerecha, value); }
            remove { RemoveHandler(EvDerecha, value); }
        }

        public event RoutedEventHandler OnAbajo
        {
            add { AddHandler(EvAbajo, value); }
            remove { RemoveHandler(EvAbajo, value); }
        }

        void RaiseEvIzquierda()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(Flechas.EvIzquierda);
            RaiseEvent(newEventArgs);
        }

        void RaiseEvArriba()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(Flechas.EvArriba);
            RaiseEvent(newEventArgs);
        }



        void RaiseEvDerecha()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(Flechas.EvDerecha);
            RaiseEvent(newEventArgs);
        }



        void RaiseEvAbajo()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(Flechas.EvAbajo);
            RaiseEvent(newEventArgs);
        }
        #endregion

        
    }
}
