using System;
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
        private int stage = 1;
        // 1: Menu, 2: Transition, 3: Jeu, 4: Mort, 5: Fin
        
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
        private readonly int tailleDecor = 2480;
        private double vitesse = 10;
        private readonly double vFond = 0.1; // Fond
        private readonly double vSol = 1; // Sol
        private readonly double vVague = 1.25; // Vague
        private (UIElement, double)[] elements;
        
        // Joueur
        private int ratioRotation = 50; // Change selon vitesse de scroll (50:lent ; 75:moyen ; 100:rapide ...)
        private double rotation;
        private Rect collision = new Rect() { Width = 35, Height = 35 };
        private readonly double decaleX = 15;
        private readonly double decaleY = 0;
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
        private readonly int limiteGauche = -500;
        private int limiteDroite = 1500;
        private readonly (int, int) borneApparition = (2, 5);
        private int prochaineApparition = 5;

        // Zindex

        // Fond = 0 ; Sol = 5 ; Joueur = 25
        private readonly Dictionary<string, int> zSelonNom = new Dictionary<string, int>()
        {
            {"barque", 75 },
            {"bateau", 80 },
            {"caillou", 85 },
            {"oiseau", 50 },
        };
        // Vague = 100 ; Labels = 100 ; Collisions = 999

        // Personnalisation
        private readonly Dictionary<string, int[]> persoSelonNom = new Dictionary<string, int[]>()
        {
            // min Y, max Y, min taille/100, max taille/100, min dx, max dx, min dy/10, max dy/10
            {"barque", new int[8] { 355, 365, 60, 80, -5, 0, 0, 0} },
            {"bateau", new int[8] { 358, 368, 50, 75, -5, 0, 0, 0} },
            {"caillou", new int[8] { 25, 25, 50, 100, 0, 0, 0, 0} },
            {"oiseau", new int[8] { 500, 600, 25, 50, -5, 2, -5, 5} },
        };

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
            barque = ObstacleUsine("barque", "/img/monde1/obstacles/barque.png",
                new Rect[] { new Rect(12, 45, 250, 75),
                    new Rect(125, 0, 75, 60)});

            bateau = ObstacleUsine("bateau", "/img/monde2/obstacles/bateau.png",
                new Rect[] { new Rect(15, 5, 325, 125),
                    new Rect(75, 125, 250, 75)});

            caillou = ObstacleUsine("caillou", "/img/monde1/obstacles/caillou/caillou1.png",
                new Rect[] { new Rect(5, 0, 100, 100)});

            oiseau = ObstacleUsine("oiseau", "/img/monde2/obstacles/oiseau.gif",
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
            
            if (stage == 3) score += vitesse / 100;

            if (stage != 1)
            {
                DetecteAppui();
                GereObstacles();
                PhysiqueJoueur();
                Collision();
                Apparition();
            }

            if (tick >= tickParImage)
            {
                tick = 0;
                if (stage == 2) { Transition2(); }
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
                    // Retirer de l'écran
                    if (!obstacles[nObstacle].Anime)
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


        private void Apparition()
        {
            if (prochaineApparition > score) return;
            Random rd = new Random();
            prochaineApparition += rd.Next(borneApparition.Item1, borneApparition.Item2);
            // Faire apparaître
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


        private Obstacle ObstacleUsine(string nom, string source, Rect[] collisions)
        {
            if (source.EndsWith(".png"))
            {
                ImageBrush img = new ImageBrush(new BitmapImage(new Uri(dir + source)));
                Rectangle visuel = new Rectangle()
                { Fill = img, Width = img.ImageSource.Width, Height = img.ImageSource.Height };
                return new Obstacle(nom, visuel, collisions);
            }
            else if (source.EndsWith(".gif"))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(dir + source);
                image.EndInit();
                Image visuel = new Image()
                { Width = image.Width, Height = image.Height, Source = image };
                ImageBehavior.SetAnimatedSource(visuel, image);
                return new Obstacle(nom, visuel, collisions);
            }
            throw new NotImplementedException();
        }


        private void GenereObstacle(Obstacle usine)
        {
            Obstacle obst = usine.GenereObstacle();

            // Changer les références pour permettre l'existence parallele de plusieurs instances
            Cloner(ref obst);

            // Personnalisation de l'obstacle (comprendre randomisation)
            Personnalise(ref obst, persoSelonNom[obst.Nom]);

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


        private void Cloner(ref Obstacle obst)
        {
            obst.Collisions = (Rect[]) obst.Collisions.Clone();
            if (!obst.Anime)
            {
                Rectangle r = obst.Visuel;
                Rectangle visuel = new Rectangle()
                { Width = r.Width, Height = r.Height, Fill = r.Fill };
                obst.Visuel = visuel;
                Canvas.Children.Add(visuel);
                Canvas.SetZIndex(visuel, zSelonNom[obst.Nom]);
            }
            else
            {
                Image i = obst.Animation;
                Image animation = new Image()
                { Source = i.Source };
                ImageBehavior.SetAnimatedSource(animation, i.Source);
                obst.Animation = animation;
                Canvas.Children.Add(animation);
                Canvas.SetZIndex(animation, zSelonNom[obst.Nom]);
            }
        }


        private void Personnalise(ref Obstacle obst, int[] param)
        {
            Random rd = new Random();
            obst.ChangeTaille(rd.Next(param[2], param[3])/100.0);
            obst.Place(limiteDroite, rd.Next(param[0], param[1]));
            obst.DX = rd.Next(param[4], param[5]);
            obst.DY = rd.Next(param[6], param[7])/10.0;

            // Personnalisation spécifique
            switch (obst.Nom)
            {
                case "caillou": obst.Visuel.Fill = textureCaillou[rd.Next(0, 2)]; break;
            }
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

            // Caillou
            for (int i = 0; i < 3; i++)
            {
                textureCaillou[i] = new ImageBrush(new BitmapImage(new Uri(dir + $"/img/monde1/obstacles/caillou/caillou{i + 1}.png")));
            }        
        }


        private void PlacerTextures()
        {
            // 0: Fond
            // 1: Sol
            ImageBrush textureFond = texturesDecor[monde - 1, 0];
            fond1.Fill = textureFond; fond2.Fill = textureFond;
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
                    $"estEnCollision : {estEnCollision}\n" +
                    $"stageDeJeu : {stage}\n" +
                    $"numeroDeMonde : {monde}\n"
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
            if (e.Key == Key.NumPad1) GenereObstacle(barque);
            if (e.Key == Key.NumPad2) GenereObstacle(bateau);
            if (e.Key == Key.NumPad3) GenereObstacle(caillou);
            if (e.Key == Key.NumPad4) GenereObstacle(oiseau);
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


        private void Quitter(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }


        private void Jouer(object sender, MouseButtonEventArgs e)
        {
            if (stage == 1) stage = 2;
        }


        private void Transition2()
        {
            Menu.Opacity -= 0.01 * tickParImage;
            if (Menu.Opacity <= 0)
            {
                Menu.Visibility = Visibility.Hidden;
                stage = 3;
            }
        }
    }
}
