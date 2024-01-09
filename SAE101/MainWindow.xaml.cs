using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SAE101
{
    public partial class MainWindow : Window
    {
        // Gestion boutons
        private bool boutonActif = false;
        private bool[] toutBoutonEnfonce = new bool[5] {false, false, false, false, false };
        // Personnalisable par l'utilisateur
        private Key[] boutonsValides = new Key[5] { Key.Z, Key.W, Key.Up, Key.Space, Key.Enter };
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
        private int asymptote = 400;
        private int borneStableP = 50;
        private bool vaSplash = false;
        private double frictionAir = 0.1;
        private double frictionEau = 0.2;
        private double frictionSplash = 0.4;
        private double frictionStable = 0.3;
        private double accelerationJoueur = 1;
        private double flotaison = 1;
        private double gravité = 0.25;


        public MainWindow()
        {
            InitializeComponent();
            Canvas.SetLeft(Joueur, X);
            Horloge.Tick += MoteurDeJeu;
            Horloge.Interval = TimeSpan.FromMilliseconds(deltaIPS);
            Horloge.Start();
        }


        private void MoteurDeJeu(object objet, EventArgs e)
        {
            DetecteAppui();
            PhysiqueJoueur();
            BougeJoueur();
            AfficheScore();
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
            if (pY > asymptote) // Air
            {
                friction = frictionAir;
                aY -= gravité;
            }
            else // Eau
            {
                friction = frictionEau;
                if (!boutonActif) { aY += flotaison; }
            }
            // Réentrée dans l'eau
            if (pY > asymptote) { vaSplash = true; }
            else if (vaSplash)
            {
                vaSplash = false;
                if (!boutonActif) { friction += frictionSplash; }
            }
            if (vY <= 0 && pY <= asymptote+borneStableP && pY >= asymptote-borneStableP && !boutonActif) { friction += frictionStable; }
            vY *= 1 - friction;
            aY *= 1 - friction;
            // Calcul de Vitesse
            vY += aY;
            // Calcul de Position
            pY += vY;
        }


        private void BougeJoueur()
        {
            Canvas.SetBottom(Joueur, pY);
        }


        private void AfficheScore()
        {
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
    }
}
