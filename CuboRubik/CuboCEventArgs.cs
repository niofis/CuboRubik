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
    public class CuboCEventArgs : RoutedEventArgs
    {
        private CuboC c;
        private Point3D p;
        private int l;

        public CuboCEventArgs(RoutedEvent evento)
        {
            base.RoutedEvent = evento;
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

        //public static DependencyProperty CuboProperty = DependencyProperty.RegisterAttached("Cubo", typeof(CuboC), typeof(CuboCEventArgs));
        //public static DependencyProperty HitPointProperty = DependencyProperty.RegisterAttached("HitPoint", typeof(Point3D), typeof(CuboCEventArgs));
        //public static DependencyProperty LadoProperty = DependencyProperty.RegisterAttached("Lado", typeof(int), typeof(CuboCEventArgs));

    }

}
