using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
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
        private readonly int deltaMoteur = 16;
        private int tickParImage = 1; // 1 = 60FPS, 2 = 30FPS, 3 = 20FPS ...
        private int tick = 0;
        
        // Positionnement, Vitesse et Accélération (X est constant)
        private readonly double X = 250;
        private double pY = 350;
        private double vY = 0;
        private double aY = 0;
       
        // Physique Principale
        private readonly int asymptote = 350;
        private readonly int borneStableP = 25;
        private readonly double frictionAir = 0.1;
        private readonly double frictionEau = 0.15;
        private readonly double frictionSplash = 0.2;
        private readonly double frictionStable = 0.2;
        private readonly double accelerationJoueur = 0.25;
        private readonly double flotaison = 0.75;
        private readonly double gravité = 0.25;
        private bool vaSplash = false;
        
        // Vitesse X et Parallax
        private int tailleDecor = 2480;
        private double vitesse = 7.5;
        private double vFond = 0.1; // Fond
        private double vSol = 1; // Sol
        private double vVague = 1.25; // Vague
        private (UIElement, double)[] elements;
        
        // Rotation Joueur
        private int ratioRotation = 50; // Change selon vitesse de scroll (50:lent ; 75:moyen ; 100:rapide ...)
        private double rotation;
        
        // Images
        private ImageBrush imgJoueur = new ImageBrush(); // Joueur
        private ImageBrush textureFond = new ImageBrush(); // Fond
        private ImageBrush textureSol = new ImageBrush(); // Sol
        private ImageBrush textureVague = new ImageBrush(); // Vague
        
        // Variables pour le DEBUG
        private readonly int arrondi = 4;
        public string dir;
#if DEBUG
        private DEBUG winDEBUG;
        private Stopwatch chrono = new Stopwatch();
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

            imgJoueur.ImageSource = new BitmapImage(new Uri(dir + "/img/poisson.png"));
            Joueur.Fill = imgJoueur;
            Canvas.SetLeft(Joueur, X);

            // Génération du décor 
            elements = new (UIElement, double)[]
            {
                // ("nom_de_l'objet", vitesse_multiplicatrice)
                (fond1, vFond), (fond2, vFond),
                (sol1, vSol), (sol2, vSol),
                (vague1, vVague), (vague2, vVague)
            };
            
            // Fond
            textureFond.ImageSource = new BitmapImage(new Uri(dir + "/img/monde1/fond.png"));
            fond1.Background = textureFond;
            fond2.Background = textureFond;

            // Sol
            textureSol.ImageSource = new BitmapImage(new Uri(dir + "/img/monde1/sol.png"));
            sol1.Fill = textureSol;
            sol2.Fill = textureSol;

            // Vague
            textureVague.ImageSource = new BitmapImage(new Uri(dir + "/img/vague.png"));
            vague1.Fill = textureVague;
            vague2.Fill = textureVague;

            // Moteur
            Horloge.Tick += MoteurDeJeu;
            Horloge.Interval = TimeSpan.FromMilliseconds(deltaMoteur);
            Horloge.Start();
        }


        private void MoteurDeJeu(object objet, EventArgs e)
        {
#if DEBUG
            chrono.Restart();
#endif
            DetecteAppui();
            PhysiqueJoueur();
            tick++;
            if (tick >= tickParImage) tick = 0;
            if (tick == 0)
            {
                BougerDecor();
                Affiche();
#if DEBUG
                // Afficher les variables d'environement
                if (winDEBUG != null)
                {
                    winDEBUG.Content =
                        $" dir = {dir}\n" +
                        $" Vitesse = {vitesse} px/t\n" +
                        $" pos Y Joueur : {Math.Round(pY, arrondi).ToString($"F{arrondi}")} px\n" +
                        $" vit Y Joueur : {Math.Round(vY, arrondi).ToString($"F{arrondi}")} px/t\n" +
                        $" acc Y Joueur : {Math.Round(aY, arrondi).ToString($"F{arrondi}")} px/t²\n" +
                        $" rotat Joueur : {rotation} deg\n" +
                        $" tempsExecMoteur : {Math.Round(chrono.Elapsed.TotalMilliseconds, arrondi).ToString($"F{arrondi}")} ms"
                        ;
                }
#endif
            }
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


        private void BougerDecor()
        {
            foreach (var doublon in elements)
            {
                BougerDecor(doublon.Item1, doublon.Item2);
            }
        }


        private void BougerDecor(UIElement objet, double vit)
        {
            double posX = Canvas.GetLeft(objet);
            posX -= vitesse * vit * tickParImage;
            if (posX < -tailleDecor) posX += 2 * tailleDecor;
            Canvas.SetLeft(objet, posX);
        }
    }
}
