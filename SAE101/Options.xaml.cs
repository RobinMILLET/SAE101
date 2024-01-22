using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAE101
{
    public partial class Options : Window
    {
        private bool attendBouton = false;
        private string dir;
        private readonly string fichierBouton = "/bouton.txt";
        private readonly string fichierIPS = "/ips.txt";
        private readonly string fichierScore = "/score.txt";
        private RadioButton[] paramIPS;

        public Options()
        {
            InitializeComponent();

            BtnReinitScore.IsEnabled = true;
            paramIPS = new RadioButton[] { IPS60, IPS30, IPS20, IPS15, IPS12, IPS10 };

            // Répertoire courrant
            dir = AppDomain.CurrentDomain.BaseDirectory;
            dir = dir.Remove(dir.IndexOf("\\bin\\"));

            // Init
            BtnPerso.Content = ObtenirParams(fichierBouton);
            if (string.IsNullOrEmpty(BtnPerso.Content.ToString())) BtnPerso.Content = "-";

            string indexIPS = ObtenirParams(fichierIPS);
            if (indexIPS == "") IPS60.IsChecked = true;
            else
            {
                int temp = 0;
                int.TryParse(indexIPS, out temp);
                paramIPS[temp].IsChecked = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (attendBouton)
            {
                if (e.Key == Key.Escape)
                {
                    MessageBox.Show("Ne peut pas utiliser Échap en bouton personnalisé.", "Erreur");
                    attendBouton = false;
                    BtnPerso.Content = "-";
                }
                else
                {
                    BtnPerso.Content = e.Key.ToString();
                    EcrireParams(fichierBouton, e.Key.ToString());
                    attendBouton = false;
                }
            }
        }

        private void BtnPerso_Click(object sender, RoutedEventArgs e)
        {
            if (!attendBouton) 
            {
                attendBouton = true;
                BtnPerso.Content = "...";
            }
            else
            {
                attendBouton = false;
                BtnPerso.Content = "-";
            }
            
        }

        private void InitBtnPerso_Click(object sender, RoutedEventArgs e)
        {
            attendBouton = false;
            BtnPerso.Content = "-";
            File.Delete(dir + fichierBouton);
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            attendBouton = false;
            if (BtnPerso.Content.ToString() == "...") BtnPerso.Content = "-";
            this.Visibility = Visibility.Hidden;
        }

        private string ObtenirParams(string fichier)
        {
            if (File.Exists(dir + fichier))
            {
                return File.ReadAllText(dir + fichier);
            }
            return "";
        }


        private void EcrireParams(string fichier, string txt)
        {
            using (StreamWriter document = new StreamWriter(dir + fichier))
            {
                document.WriteLine(txt);
            }
        }

        private void IPS60_Checked(object sender, RoutedEventArgs e)
        {
            EcrireParams(fichierIPS, "0");
        }

        private void IPS30_Checked(object sender, RoutedEventArgs e)
        {
            EcrireParams(fichierIPS, "1");
        }

        private void IPS20_Checked(object sender, RoutedEventArgs e)
        {
            EcrireParams(fichierIPS, "2");
        }

        private void IPS15_Checked(object sender, RoutedEventArgs e)
        {
            EcrireParams(fichierIPS, "3");
        }

        private void IPS12_Checked(object sender, RoutedEventArgs e)
        {
            EcrireParams(fichierIPS, "4");
        }

        private void IPS10_Checked(object sender, RoutedEventArgs e)
        {
            EcrireParams(fichierIPS, "5");
        }

        private void BtnReinitScore_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(dir + fichierScore);
            BtnReinitScore.IsEnabled = false;
        }
    }
}
