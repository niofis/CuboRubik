using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace CuboRubik
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Cubo cubo_rubik;
        public MainWindow()
        {
            InitializeComponent();
            cubo_rubik = new Cubo(V3DCubo, lblMensaje);
        }
        private void chkFlechas_Checked(object sender, RoutedEventArgs e)
        {
            if (cubo_rubik != null)
                cubo_rubik.MostrarFlechas((bool)chkFlechas.IsChecked);
        }

        private void btnRevolver_Click(object sender, RoutedEventArgs e)
        {
            cubo_rubik.Revolver();
        }

        private void btnSolucionar_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.Title = "Guardar Cubo...";
            dlg.Filter = "Archivo XML|*.xml";
            if ((bool)dlg.ShowDialog())
            {
                cubo_rubik.Guardar(dlg.OpenFile());
            }
        }

        private void btnAbrir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.Title = "Abrir Cubo...";
            dlg.Filter = "Archivo XMl|*.xml";
            if ((bool)dlg.ShowDialog())
            {
                btnNuevo_Click(sender, e);
                cubo_rubik.Abrir(dlg.OpenFile());
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            V3DTrackball.Reset();
            V3DCubo.Children.Clear();
            cubo_rubik = new Cubo(V3DCubo, lblMensaje);
            chkFlechas.IsChecked = true;

        }

    }
}
