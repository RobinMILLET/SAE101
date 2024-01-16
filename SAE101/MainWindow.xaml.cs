﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using WpfAnimatedGif;

namespace SAE101
{
    public partial class MainWindow : Window
    {
        // Gestion boutons
        private bool boutonActif = false;
        private bool[] toutBoutonEnfonce = new bool[6] {false, false, false, false, false, false };
        
        // Personnalisable par l'utilisateur
        private Key[] boutonsValides = new Key[6] { Key.Z, Key.W, Key.Up, Key.Down, Key.Space, Key.Enter };
        
        // Score
        private double score = 0;
        
        // Moteur
        private DispatcherTimer Horloge = new DispatcherTimer();
        private readonly int deltaMoteur = 16;
        private int tickParImage = 1; // 1 = 60FPS, 2 = 30FPS, 3 = 20FPS ...
        private int tick = 0;
        
        // Positionnement, Vitesse et Accélération (X est constant)
        private readonly double X = 250;
        private double pY = 355;
        private double vY = 0;
        private double aY = 0;
       
        // Physique Principale
        private readonly int asymptote = 355;
        private readonly int borneStableP = 20;
        private readonly double frictionAir = 0.1;
        private readonly double frictionEau = 0.15;
        private readonly double frictionSplash = 0.2;
        private readonly double frictionStable = 0.15;
        private readonly double accelerationJoueur = 0.25;
        private readonly double flotaison = 0.80;
        private readonly double graviteA = 0.2;
        private readonly double graviteV = 0.15;
        private bool vaSplash = false;
        
        // Vitesse X et Parallax
        private int tailleDecor = 2480;
        private double vitesse = 10;
        private double vFond = 0.1; // Fond
        private double vSol = 1; // Sol
        private double vVague = 1.25; // Vague
        private (UIElement, double)[] elements;
        
        // Joueur
        private int ratioRotation = 50; // Change selon vitesse de scroll (50:lent ; 75:moyen ; 100:rapide ...)
        private double rotation;
        private Rect collision = new Rect() { Width = 35, Height = 35 };
        private double decaleX = 15;
        private double decaleY = 0;
        private bool estEnCollision = false;
        private (double, double) verifCollision = (-250, +50);
        
        // Images
        private ImageBrush imgJoueur = new ImageBrush(); // Joueur
        private ImageBrush[,] texturesDecor;

        // Monde
        private readonly int nbMondes = 3;
        private int monde = 1;

        // Variables pour le DEBUG
        private string dir;
#if DEBUG
        private Rectangle joueurDebug;
        private readonly int arrondi = 4;
        private bool afficheCollisions = false;
        private DEBUG winDEBUG;
        private Stopwatch chronoDEBUG = new Stopwatch();
        private readonly int tailleMoyenneExec = 100;
        private List<double> moyenneExec = new List<double> { };
#endif
        
        // Obstacles
        private List<Obstacle> obstacles = new List<Obstacle> { };
        Obstacle barque;
        Obstacle bateau;
        Obstacle caillou;
        Obstacle oiseau;
        private ImageBrush[] textureCaillou = new ImageBrush[3];
        private BitmapImage[] textureOiseau = new BitmapImage[2];
        private int limiteGauche = -500;
        private int limiteDroite = 1500;

        // Zindex

        // Fond = 0
        // Sol = 5
        // Joueur = 25
        private int zOiseau = 50;
        private int zBarque = 75;
        private int zBateau = 85;
        private int zCaillou = 85;
        // Vague = 100
        // Labels = 100
        // Collisions = 999

        // FIN DES VARIABLES

        public MainWindow()
        {
            // Répertoire courrant
            dir = AppDomain.CurrentDomain.BaseDirectory;
            dir = dir.Remove(dir.IndexOf("\\bin\\"));
            InitializeComponent();
#if DEBUG
            // Créer le rectangle de collision du Joueur
            joueurDebug = new Rectangle() { Width = collision.Width, Height = collision.Height,
                Stroke = new SolidColorBrush(Colors.Red)};

            // Instancier la fenêtre DEBUG
            winDEBUG = new DEBUG();
            winDEBUG.Show();
#endif
            // Apparence du Joueur
            imgJoueur.ImageSource = new BitmapImage(new Uri(dir + "/img/poisson.png"));
            Joueur.Fill = imgJoueur;
            Canvas.SetLeft(Joueur, X);

            // Collision
            verifCollision = (verifCollision.Item1 + X, verifCollision.Item2 + X);

            // Préparation du Décor
            elements = new (UIElement, double)[]
            {
                // ("nom_de_l'objet", vitesse_multiplicatrice)
                (fond1, vFond), (fond2, vFond),
                (sol1, vSol), (sol2, vSol),
                (vague1, vVague), (vague2, vVague)
            };
            ObtenirTextures();
            PlacerTextures();

            // Obstacles par défaut
            barque = ObstacleUsine("/img/monde1/obstacles/barque.png",
                new Rect[] { new Rect(12, 45, 250, 75),
                    new Rect(125, 0, 75, 60)});

            bateau = ObstacleUsine("/img/monde2/obstacles/bateau.png",
                new Rect[] { new Rect(15, 5, 325, 125),
                    new Rect(75, 125, 250, 75)});

            caillou = ObstacleUsine("/img/monde1/obstacles/caillou/caillou1.png",
                new Rect[] { new Rect(5, 0, 100, 100)});

            oiseau = ObstacleUsine("/img/monde2/obstacles/oiseau.gif",
                new Rect[] { new Rect(25, 35, 125, 75) });


            // Moteur
            Horloge.Tick += MoteurDeJeu;
            Horloge.Interval = TimeSpan.FromMilliseconds(deltaMoteur);
            Horloge.Start();
        }


        private void MoteurDeJeu(object objet, EventArgs e)
        {
#if DEBUG
            chronoDEBUG.Restart();
#endif
            score += vitesse / 100;
            DetecteAppui();
            GereObstacles();
            PhysiqueJoueur();
            Collision();

            if (tick >= tickParImage)
            {
                tick = 0;
                BougerDecor();
                Affiche();
#if DEBUG
                CollisionsDebug();
                AfficheDebug();
#endif
            }

#if DEBUG
            // Mesure d'execution
            moyenneExec.Add(chronoDEBUG.Elapsed.TotalMilliseconds);
            if (moyenneExec.Count > tailleMoyenneExec) moyenneExec.RemoveAt(0);
#endif
            tick++;
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


        private void GereObstacles()
        {
            int nObstacle = 0;
            while (nObstacle < obstacles.Count)
            {
                // Bouger les obstacles
                obstacles[nObstacle].Mouvement(-vitesse);

                // Tester pour les supprimer
                if (obstacles[nObstacle].Sorti(Canvas, limiteGauche))
                {
                    if (obstacles[nObstacle].Animation == null)
                    {
                        Canvas.Children.Remove(obstacles[nObstacle].Visuel);
                    }
                    else
                    {
                        Canvas.Children.Remove(obstacles[nObstacle].Animation);
                    }
                    obstacles.RemoveAt(nObstacle);
                }
                else nObstacle++;
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
                aY -= graviteA;
                vY -= graviteV;
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


        private void Collision()
        {
            collision.X = X + decaleX;
            collision.Y = pY + decaleY;
            estEnCollision = obstacles.Any(obst => obst.EstEnCollision(collision, verifCollision));
        }


        private void Affiche()
        {
            // Joueur
            Canvas.SetBottom(Joueur, pY);
            rotation = Math.Round( -90 * Math.Atanh(Math.Round(vY, 2) / ratioRotation), 1);
            Joueur.RenderTransform = new RotateTransform(rotation);
            // Score
            lbDistance.Content = Math.Round(score, 0);
            // Obstacle
            foreach (Obstacle obst in obstacles)
            {
                obst.AfficheObstacle();
            }
        }


        private void BougerDecor()
        {
            // Déplacer le décor avec (élément, dX)
            foreach (var doublon in elements)
            {
                BougerDecor(doublon.Item1, doublon.Item2);
            }
        }


        private void BougerDecor(UIElement objet, double vit)
        {
            // Comme le décor ne contient pas de collisions par lui même,
            // On met l'affichage au même endroit que le calcul de position,
            // Puis on le bouge en fonction du ratio 'tickParImage'
            double posX = Canvas.GetLeft(objet);
            posX -= vitesse * vit * tickParImage;
            // Faire la boucle
            if (posX < -tailleDecor) posX += 2 * tailleDecor;
            // Afficher
            Canvas.SetLeft(objet, posX);
        }


        private Obstacle ObstacleUsine(string source, Rect[] collisions)
        {
            ImageBrush img = new ImageBrush(new BitmapImage(new Uri(dir + source)));
            Rectangle visuel = new Rectangle();
            // Obstacle automatiquement de la même taille que son image
            visuel.Width = img.ImageSource.Width;
            visuel.Height = img.ImageSource.Height;
            visuel.Fill = img;
            return new Obstacle(visuel, collisions);
        }


        private void GenereObstacle(string nom, Obstacle usine)
        {
            Obstacle obst = usine.GenereObstacle();
            Rectangle r1 = obst.Visuel;
            Rectangle r2 = new Rectangle() { Width = r1.Width, Height = r1.Height, Fill = r1.Fill };
            obst.Collisions = (Rect[]) obst.Collisions.Clone();
            obst.Visuel = r2;
            // Personnalisation de l'obstacle (comprendre randomisation)
            switch (nom)
            {
                case "barque": PersonnaliseBarque(ref obst); break;
                case "bateau": PersonnaliseBateau(ref obst); break;
                case "caillou": PersonnaliseCaillou(ref obst); break;
                case "oiseau": PersonnaliseOiseau(ref obst); break;
            }

            if (obst.Animation == null)
            {
                Canvas.Children.Add(r2);
            }
            else
            {
                Canvas.Children.Add(obst.Animation);
            }
            obst.PlaceCollisions();
            obst.AfficheObstacle();
#if DEBUG
            if (afficheCollisions)
            {
                obst.AfficheCollisions(Canvas);
            }
#endif
            obstacles.Add(obst);
        }


        private void PersonnaliseBarque(ref Obstacle obst)
        {
            Random rd = new Random();
            double taille = rd.Next(60,80) / 100.0;
            obst.ChangeTaille(taille, taille);
            // Changer le Y en fonction de la taille, car le 0 est sous la pagaie
            // alors qu'il faut aligner la vague et la coque
            obst.Place(limiteDroite, 370 - taille * 15);
            obst.DX = rd.Next(-5, 0);
            Canvas.SetZIndex(obst.Visuel, zBarque);
        }


        private void PersonnaliseBateau(ref Obstacle obst)
        {
            Random rd = new Random();
            double taille = rd.Next(50, 75) / 100.0;
            obst.ChangeTaille(taille, taille);
            // Changer le Y en fonction de la taille, car le 0 est sous la pagaie
            // alors qu'il faut aligner la vague et la coque
            obst.Place(limiteDroite, 370 - taille * 15);
            obst.DX = rd.Next(-5, 0);
            Canvas.SetZIndex(obst.Visuel, zBateau);
        }

        private void PersonnaliseCaillou(ref Obstacle obst)
        {
            Random rd = new Random();
            obst.Visuel.Fill = textureCaillou[rd.Next(0, 2)];
            double taille = rd.Next(25, 100) / 100.0;
            obst.ChangeTaille(taille, taille);
            obst.Place(limiteDroite, 25);
            Canvas.SetZIndex(obst.Visuel, zCaillou);
        }


        private void PersonnaliseOiseau(ref Obstacle obst)
        {
            Random rd = new Random();
            Image img = new Image();
            ImageBehavior.SetAnimatedSource(img, textureOiseau[0]);
            obst.Animation = img;
            double taille = rd.Next(25, 50) / 100.0;
            obst.ChangeTaille(taille, taille);
            obst.Place(limiteDroite, rd.Next(500, 600));
            obst.DY = rd.Next(-5, 5) / 10.0;
            Canvas.SetZIndex(obst.Animation, zOiseau);
        }


        private void ObtenirTextures()
        {
            ImageBrush[,] textures = new ImageBrush[nbMondes, 2];
            // 0: Fond
            // 1: Sol
            for (int i = 0; i < nbMondes; i++)
            {
                textures[i, 0] = new ImageBrush()
                {
                    ImageSource = new BitmapImage(new Uri(dir + $"/img/monde{i + 1}/fond.png"))
                };
                textures[i, 1] = new ImageBrush()
                {
                    ImageSource = new BitmapImage(new Uri(dir + $"/img/monde{i + 1}/sol.png"))
                };
            }
            texturesDecor = textures;

            for (int i = 0; i < 3; i++)
            {
                textureCaillou[i] = new ImageBrush(new BitmapImage(new Uri(dir + $"/img/monde1/obstacles/caillou/caillou{i + 1}.png")));
            }

            for (int i = 0;i < 2; i++)
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(dir + $"/img/monde{i + 2}/obstacles/oiseau.gif");
                image.EndInit();
                textureOiseau[i] = image;
            }
            
        }


        private void PlacerTextures()
        {
            // 0: Fond
            // 1: Sol
            ImageBrush textureFond = texturesDecor[monde - 1, 0];
            fond1.Background = textureFond; fond2.Background = textureFond;
            ImageBrush textureSol = texturesDecor[monde - 1, 1];
            sol1.Fill = textureSol; sol2.Fill = textureSol;
        }



#if DEBUG
        private void CollisionsDebug()
        {
            if (winDEBUG.Col.IsChecked != afficheCollisions)
            {
                if (afficheCollisions) // Si true, alors il faut mettre à false
                {
                    afficheCollisions = false;
                    obstacles.ForEach(obstacle => obstacle.CacheCollisions(Canvas));
                    Canvas.Children.Remove(joueurDebug);
                }
                else // Si false, alors il faut mettre à true
                {
                    afficheCollisions = true;
                    obstacles.ForEach(obstacle => obstacle.AfficheCollisions(Canvas));
                    Canvas.Children.Add(joueurDebug);
                    Canvas.SetZIndex(joueurDebug, 999);
                }
            }
            if (afficheCollisions) // En continu
            {
                Canvas.SetLeft(joueurDebug, collision.X);
                Canvas.SetBottom(joueurDebug, collision.Y);
            }
        }


        private void AfficheDebug()
        {
            // Uniquement si la fenêtre winDEBUG exist (pas fermée, etc)
            if (winDEBUG != null)
            {
                winDEBUG.Text.Content =
                    $"dir = {dir}\n" +
                    $"vitesse = {FormatDebug(vitesse)} px/t\n" +
                    $"pos Y Joueur : {FormatDebug(pY)} px\n" +
                    $"vit Y Joueur : {FormatDebug(vY)} px/t\n" +
                    $"acc Y Joueur : {FormatDebug(aY)} px/t²\n" +
                    $"rotat Joueur : {rotation.ToString("F1")} deg\n" +
                    $"tempsExecMoteur(/1) : {FormatDebug(moyenneExec.Last())} ms\n" +
                    $"tempsExecMoteur(/{tailleMoyenneExec}) : {FormatDebug(moyenneExec.Average())} ms\n" +
                    $"elementsCanvas : {Canvas.Children.Count}\n" +
                    $"estEnCollision : {estEnCollision}\n"
                    ;
            }
        }


        private string FormatDebug(double valeur)
        {
            return Math.Round(valeur, arrondi).ToString($"F{arrondi}");
        }
#endif


        private void ClavierAppui(object objet, KeyEventArgs e)
        {
            for (int i = 0; i < boutonsValides.Length; i++)
            {
                if (e.Key == boutonsValides[i])
                {
                    toutBoutonEnfonce[i] = true;
                }
            }
#if DEBUG
            // Commandes
            if (e.Key == Key.NumPad1) GenereObstacle("barque", barque);
            if (e.Key == Key.NumPad2) GenereObstacle("bateau", bateau);
            if (e.Key == Key.NumPad3) GenereObstacle("caillou", caillou);
            if (e.Key == Key.NumPad4) GenereObstacle("oiseau", oiseau);
#endif
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
    }
}
