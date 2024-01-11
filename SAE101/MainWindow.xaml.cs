using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SAE101
{
    public partial class MainWindow : Window
    {
        // Gestion boutons
        private bool boutonActif = false;
        private bool[] toutBoutonEnfonce = new bool[6] {false, false, false, false, false, false };
        // Personnalisable par l'utilisateur
        private Key[] boutonsValides = new Key[6] { Key.Z, Key.W, Key.Up, Key.Down, Key.Space, Key.Enter };
        // Score et Temps
        private int score = 0;
        private int temps = 0;
        // Moteur
        private DispatcherTimer Horloge = new DispatcherTimer();
        private int deltaIPS = 16;
        private bool pause = true;
        // Positionnement, Vitesse et Accélération (X est constant)
        private readonly double X = 250;
        private double pY = 400;
        private double vY = 0;
        private double aY = 0;
        // Physique Principale
        private readonly int asymptote = 400;
        private readonly int borneStableP = 25;
        private readonly double frictionAir = 0.1;
        private readonly double frictionEau = 0.15;
        private readonly double frictionSplash = 0.2;
        private readonly double frictionStable = 0.2;
        private readonly double accelerationJoueur = 0.5;
        private readonly double flotaison = 0.75;
        private readonly double gravité = 0.3;
        private bool vaSplash = false;
        // Vitesse fond
        private int frequenceDecaleFond = 8; // vitesse du fond (1 : plus rapide)

        // Images
        // Joueur
        private ImageBrush imgJoueur= new ImageBrush();
        private int ratioRotation = 50; // Change selon vitesse de scroll (50:lent ; 75:moyen ; 100:rapide ...)
        private double rotation;
        // Fond
        private ImageBrush textureFond = new ImageBrush();
        int tickDecaleFond = 0;
        // Sol
        private ImageBrush textureSol = new ImageBrush();
        // Vague
        private ImageBrush textureVague = new ImageBrush();


        // Variables pour le DEBUG
        private readonly int arrondi = 4;
        public string dir;
#if DEBUG
        private DEBUG winDEBUG;
#endif
        // Obstacles
        private  List<Obstacle> obstacles;
        // FIN DES VARIABLES

        public MainWindow()
        {
            dir = AppDomain.CurrentDomain.BaseDirectory;
            dir = dir.Remove(dir.IndexOf("\\bin\\"));
            InitializeComponent();
#if DEBUG
            // Instancier la fenêtre DEBUG
            winDEBUG = new DEBUG();
            winDEBUG.Show();
#endif
            imgJoueur.ImageSource = new BitmapImage(new Uri(dir + "\\img\\poisson.png"));
            Joueur.Fill = imgJoueur;
            Canvas.SetLeft(Joueur, X);

            Horloge.Tick += MoteurDeJeu;
            Horloge.Interval = TimeSpan.FromMilliseconds(deltaIPS);
            Horloge.Start();

            textureFond.ImageSource = new BitmapImage(new Uri(dir + "/img/monde1/fond.png"));
            fond1.Background = textureFond;
            fond2.Background = textureFond;

            textureSol.ImageSource = new BitmapImage(new Uri(dir + "/img/monde1/sol.png"));
            sol1.Fill = textureSol;
            sol2.Fill = textureSol;

            textureVague.ImageSource = new BitmapImage(new Uri(dir + "/img/vague.png"));
            vague1.Fill = textureVague;
            vague2.Fill = textureVague;
        }


        private void MoteurDeJeu(object objet, EventArgs e)
        {
            tickDecaleFond += 1;
            if (tickDecaleFond >= frequenceDecaleFond)
            {
                tickDecaleFond = 0;
                BougerFond();
            }
            BougerSol();
            BougerVague();

            DetecteAppui();
            PhysiqueJoueur();
            Affiche();
#if DEBUG
            // Afficher les variables d'environement
            if (winDEBUG != null)
            {
                winDEBUG.Content =
                    $" dir = {dir}\n" +
                    $" pY : {Math.Round(pY, arrondi)}\n" +
                    $" vY : {Math.Round(vY, arrondi)}\n" +
                    $" aY : {Math.Round(aY, arrondi)}\n" +
                    $" r : {rotation}";
            }
#endif
        }


        private void DetecteAppui()
        {
            if (toutBoutonEnfonce.Any(x => x))
            {
                boutonActif = true;
            }
            else
            {
                boutonActif = false;
            }
        }


        private void PhysiqueJoueur()
        {
            // Utilisation du bouton
            if (boutonActif && pY <= asymptote) { aY -= accelerationJoueur; }
            // Relativité
            double friction;
            bool dansBorne = false;
            if (pY <= asymptote + borneStableP && pY >= asymptote - borneStableP) dansBorne = true;
            if (pY > asymptote) // Air
            {
                friction = frictionAir;
                aY -= gravité;
            }
            else // Eau
            {
                friction = frictionEau;
                if (!boutonActif && !dansBorne) { aY += flotaison; }
            }
            // Réentrée dans l'eau
            if (pY > asymptote) { vaSplash = true; }
            else if (vaSplash)
            {
                vaSplash = false;
                if (!boutonActif) { friction += frictionSplash; }
            }
            // Stabilisation
            if (vY <= 0 && dansBorne && !boutonActif) { friction += frictionStable; }
            if (friction < 0) friction = 0; // Pas de friction négative
            vY *= 1 - friction;
            aY *= 1 - friction;
            // Calcul de Vitesse
            vY += aY;
            // Calcul de Position
            pY += vY;
        }

        private void Affiche()
        {
            // Joueur
            Canvas.SetBottom(Joueur, pY);
            rotation = Math.Round( -90 * Math.Atanh(Math.Round(vY, 2) / ratioRotation), 1);
            Joueur.RenderTransform = new RotateTransform(rotation);
            // Score
            lbDistance.Content = $"Distance : {score}m";
            lbTemps.Content = $"Temps : {temps}s";
        }


        private void ClavierAppui(object objet, KeyEventArgs e)
        {
            for (int i = 0; i < boutonsValides.Length; i++)
            {
                if (e.Key == boutonsValides[i])
                {
                    toutBoutonEnfonce[i] = true;
                }
            }
        }


        private void ClavierRelache(object objet, KeyEventArgs e)
        {
            for (int i = 0; i < boutonsValides.Length; i++)
            {
                if (e.Key == boutonsValides[i])
                {
                    toutBoutonEnfonce[i] = false;
                }
            }
        }

        private void StopTout(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }



        private void BougerFond()
        {
            // si le fond dépasse la limite, on le renvoie tout à droite
            if (Canvas.GetLeft(fond1) <= -2480) Canvas.SetLeft(fond1, 2480);
            else if (Canvas.GetLeft(fond2) <= -2480) Canvas.SetLeft(fond2, 2480);
            // déplacer de 10px le fond vers la gauche
            Canvas.SetLeft(fond1, Canvas.GetLeft(fond1) - 10);
            Canvas.SetLeft(fond2, Canvas.GetLeft(fond2) - 10);
        }

        private void BougerSol()
        {
            // si le sol dépasse la limite, on le renvoie tout à droite
            if (Canvas.GetLeft(sol1) <= -2480) Canvas.SetLeft(sol1, 2480);
            else if (Canvas.GetLeft(sol2) <= -2480) Canvas.SetLeft(sol2, 2480);
            // déplacer de 10px le sol vers la gauche
            Canvas.SetLeft(sol1, Canvas.GetLeft(sol1) - 10);
            Canvas.SetLeft(sol2, Canvas.GetLeft(sol2) - 10);
        }
        private void BougerVague()
        {
            // si le sol dépasse la limite, on le renvoie tout à droite
            if (Canvas.GetLeft(vague1) <= -2480) Canvas.SetLeft(vague1, 2480);
            else if (Canvas.GetLeft(vague2) <= -2480) Canvas.SetLeft(vague2, 2480);
            // déplacer de 10px le sol vers la gauche
            Canvas.SetLeft(vague1, Canvas.GetLeft(vague1) - 10);
            Canvas.SetLeft(vague2, Canvas.GetLeft(vague2) - 10);
        }
    }
}
