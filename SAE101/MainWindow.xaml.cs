using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
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
        private Key[] boutonsValides = new Key[6] { Key.Z, Key.W, Key.Up, Key.Down, Key.Space, Key.Enter };
        
        // Options
        private Options options;
        private bool btnUtilise = false;
        private bool btnAppui = false;
        private Key btnPerso = Key.None;
        private RadioButton[] paramsIPS;
        private readonly string fichierIPS = "/ips.txt";

        // Pause
        private bool pause = false;

        // Score
        private double score = 0;
        private int stage = 0;
        // 0: Menu, 1,2: Menu->Jeu, 3: Jeu, 4,5: Jeu->Menu
        private readonly string fichierScore = "/score.txt";
        private int recordJoueur;
        
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
        private readonly int fond = 50;
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
        private readonly double vitesseInit = 7.5;
        private double vitesse;
        private readonly double vFond = 0.1; // Fond
        private readonly double vSol = 1; // Sol
        private readonly double vVague = 1.25; // Vague
        private (UIElement, double)[] elements;
        
        // Joueur
        private readonly int ratioRotation = 50;
        private double rotation;
        private Rect collision = new Rect() { Width = 35, Height = 35 };
        private readonly double decaleX = 15;
        private readonly double decaleY = 0;
        private bool estEnCollision = false;
        private (double, double) verifCollision = (-250, +50);

        // Vie
        private readonly int vieMax = 3;
        private int vie;
        private readonly int tempsInvincible = 100; // *16ms
        private int invincible = 0;
        private readonly int nbFlashInvinc = 5;
        private int freqFlashInvinc;
        private Rectangle[] coeurs;

        // Images
        private ImageBrush imgJoueur = new ImageBrush(); // Joueur
        private ImageBrush[,] texturesDecor;

        // Transition
        double positionXtransition;

        // Monde
        private readonly int nbMondes = 4;
        private int monde = 1;
        private int iconeMondeMenu = 1;
        private ImageBrush[] iconesMonde;
        private ImageBrush iconeMonde = new ImageBrush();
        private readonly int[] paliersDebloquageMonde = new int[4] { 0, 100, 200, 500 };

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
        private readonly int limiteGauche = -500;
        private int limiteDroite = 1500;
        private readonly (int, int) borneApparition = (25, 100);
        private double[] prochaineApparition;

        private List<Obstacle> obstacles = new List<Obstacle> { };

        Obstacle pigeon;
        Obstacle corbeau;
        Obstacle barque;
        Obstacle bateau;
        Obstacle meduse;
        Obstacle coquillage;
        Obstacle crabe;
        Obstacle corail;
        Obstacle encre;
        Obstacle caillou;

        private ImageBrush[] textureCaillou = new ImageBrush[3];
        private ImageBrush[] textureCorail = new ImageBrush[3];

        // Zindex

        // Fond = 0 ; Sol = 5 ; Joueur = 25
        private readonly Dictionary<string, int> zSelonNom = new Dictionary<string, int>()
        {
            {"meduse", 45 },
            {"corbeau", 48 },
            {"pigeon", 50 },
            {"barque", 75 },
            {"bateau", 80 },
            {"caillou", 85 },
            {"encre", 86 },
            {"coquillage", 88 },
            {"crabe", 90 },
            {"corail", 95 },
        };
        // Vague = 100 ; Labels = 100 ; Collisions = 999

        // Personnalisation
        private readonly Dictionary<string, int[]> persoSelonNom = new Dictionary<string, int[]>()
        {
            // min Y, max Y, min taille/100, max taille/100, min dx/10, max dx/10, min dy/10, max dy/100
            {"pigeon", new int[8] { 450, 600, 20, 30, -100, -25, -50, 50} },
            {"corbeau", new int[8] { 450, 600, 20, 30, -100, -25, -50, 50} },
            {"barque", new int[8] { 355, 365, 60, 80, -50, 0, 0, 0} },
            {"bateau", new int[8] { 358, 368, 50, 75, -50, 0, 0, 0} },
            {"meduse", new int[8] { 50, 250, 25, 50, -25, 25, -25, 25} },
            {"coquillage", new int[8] { 25, 30, 10, 20, 0, 0, 0, 0} },
            {"crabe", new int[8] { 20, 30, 20, 35, -25, 25, 0, 0} },
            {"corail", new int[8] { 20, 30, 15, 30, 0, 0, 0, 0} },
            {"encre", new int[8] { 20, 30, 10, 30, 0, 0, 0, 0} },
            {"caillou", new int[8] { 20, 30, 50, 75, 0, 0, 0, 0} },
        };

        // FIN DES VARIABLES

        public MainWindow()
        {
            InitializeComponent();

            // Répertoire courrant
            dir = AppDomain.CurrentDomain.BaseDirectory;
            dir = dir.Remove(dir.IndexOf("\\bin\\"));

            tickParImage = Obtenir(fichierIPS) + 1;

            // Score et Vitesse
            recordJoueur = Obtenir(fichierScore);
            lbRecord.Content = recordJoueur;
            vitesse = vitesseInit;

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

            // Vie
            vie = vieMax;
            freqFlashInvinc = tempsInvincible / (2 * nbFlashInvinc);
            coeurs = new Rectangle[] { coeur1, coeur2, coeur3 };

            // Collision
            verifCollision = (verifCollision.Item1 + X, verifCollision.Item2 + X);
            iconesMonde = new ImageBrush[nbMondes + 1];

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

            pigeon = ObstacleUsine("pigeon", "/img/obstacles/air/pigeon.gif",
                new Rect[] { new Rect(25, 35, 125, 75) });

            corbeau = ObstacleUsine("corbeau", "/img/obstacles/air/corbeau.gif",
                new Rect[] { new Rect(25, 35, 125, 75) });

            barque = ObstacleUsine("barque", "/img/obstacles/surface/barque.png",
                new Rect[] { new Rect(20, 45, 240, 75),
                    new Rect(125, 0, 75, 60)});

            bateau = ObstacleUsine("bateau", "/img/obstacles/surface/bateau.png",
                new Rect[] { new Rect(30, 10, 310, 120),
                    new Rect(75, 125, 250, 75)});

            meduse = ObstacleUsine("meduse", "/img/obstacles/eau/meduse.gif",
                new Rect[] { new Rect(75, 50, 100, 175) });

            coquillage = ObstacleUsine("coquillage", "/img/obstacles/fond/coquillage.png",
                new Rect[] { new Rect(25, 0, 300, 400),
                    new Rect(325, 0, 200, 200)});

            crabe = ObstacleUsine("crabe", "/img/obstacles/fond/crabe.gif",
                new Rect[] { new Rect(25, 0, 175, 150) });

            corail = ObstacleUsine("corail", "/img/obstacles/fond/corail/corail1.png",
                new Rect[] { new Rect(25, 0, 275, 275) });

            encre = ObstacleUsine("encre", "/img/obstacles/fond/encre.png",
                new Rect[] { new Rect(25, 0, 300, 300),
                    new Rect(150, 150, 225, 225)});

            caillou = ObstacleUsine("caillou", "/img/obstacles/fond/caillou/caillou1.png",
                new Rect[] { new Rect(5, 0, 100, 100)});

            ProchaineApparition();

            // Moteur
            Horloge.Tick += MoteurDeJeu;
            Horloge.Interval = TimeSpan.FromMilliseconds(deltaMoteur);
            Horloge.Start();
        }


        private void MoteurDeJeu(object objet, EventArgs e)
        {
            if (pause) return;
#if DEBUG
            chronoDEBUG.Restart();
#endif

            if (stage == 2 || stage == 3) DetecteAppui();
            else { boutonActif = false; }
 
            GereObstacles();
            PhysiqueJoueur();

            if (stage == 3)
            {
                vitesse += 0.001;
                Collision();
                Danger();
            }

            if (stage != 2) Apparition();
            if (stage == 0) ReinitScore();

            if (stage == 0 || stage == 3) score += vitesse / 100;

            if (tick >= tickParImage)
            {
                tick = 0;
                if (stage == 1 || stage == 2) { Transition(1); }
                else if (stage == 4 || stage == 5) { Transition(-1); }

                BougerDecor();
                Affiche();
                AfficheInvinc();
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
            if (toutBoutonEnfonce.Any(x => x) || btnAppui)
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
                    obstacles[nObstacle].RetireObstacle(Canvas);
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
            if (friction > 1) friction = 1; // Pas de friction négative
            vY *= 1 - friction;
            aY *= 1 - friction;
            // Limite au sol
            LimiteAuSol();
            // Calcul de Vitesse
            vY += aY;
            // Calcul de Position
            pY += vY;
        }


        private void LimiteAuSol()
        {
            if (pY < fond)
            {
                vY += (fond - pY) / 10.0;
            }
        }


        private void Collision()
        {
            collision.X = X + decaleX;
            collision.Y = pY + decaleY;
            estEnCollision = obstacles.Any(obst => obst.EstEnCollision(collision, verifCollision));
        }


        private void Danger()
        {
            if (estEnCollision)
            {
#if DEBUG
                if (winDEBUG.Dieu.IsChecked.GetValueOrDefault(false)) { return; }
#endif
                if (invincible == 0)
                {
                    vie--;
                    AfficheVie();
                    if (vie > 0) invincible = tempsInvincible;
                    else
                    {
                        TesterScore();
                        stage = 4;
                    }
                    
                }
            }
            if (invincible > 0) invincible--;
        }


        private void AfficheInvinc()
        {
            if (invincible % freqFlashInvinc < freqFlashInvinc / 2)
            {
                Joueur.Opacity = 1;
            }
            else
            {
                Joueur.Opacity = 0.5;
            }
        }



        private void Apparition()
        {
            for (int i = 0; i < 4; i++)
            {
                if (prochaineApparition[i] < score)
                {
                    Random rd = new Random();
                    prochaineApparition[i] += rd.Next(borneApparition.Item1, borneApparition.Item2) / 10.0;
                    switch (i)
                    {
                        case 0: ApparitionAir(); break; // Air
                        case 1: ApparitionSurface(); break; // Surface
                        case 2: ApparitionEau(); break; // Eau
                        case 3: ApparitionFond();  break; // Sol
                    }
                }
            }
        }


        private void ApparitionAir()
        {
            switch (new Random().Next(0, 2))
            {
                case 0: GenereObstacle(pigeon); break;
                case 1: GenereObstacle(corbeau); break;
            }
        }


        private void ApparitionSurface()
        {
            switch (new Random().Next(0, 2))
            {
                case 0: GenereObstacle(barque); break;
                case 1: GenereObstacle(bateau); break;
            }
        }


        private void ApparitionEau()
        {
            switch (new Random().Next(0, 1))
            {
                case 0: GenereObstacle(meduse); break;
            }
        }


        private void ApparitionFond()
        {
            switch (new Random().Next(0, 5))
            {
                case 0: GenereObstacle(caillou); break;
                case 1: GenereObstacle(corail); break;
                case 2: GenereObstacle(crabe); break;
                case 3: GenereObstacle(coquillage); break;
                case 4: GenereObstacle(encre); break;
            }
        }


        private void Affiche()
        {
            // Joueur
            Canvas.SetBottom(Joueur, pY);
            rotation = Math.Round( -90 * Math.Atanh(Math.Round(vY, 2) / ratioRotation), 1);
            Joueur.RenderTransform = new RotateTransform(rotation);
            // Score
            if (stage == 2 || stage == 3) lbDistance.Content = Math.Round(score, 0);
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
                ImageBrush img = ObtenirPNG(source);
                Rectangle visuel = new Rectangle()
                { Fill = img, Width = img.ImageSource.Width, Height = img.ImageSource.Height };
                return new Obstacle(nom, visuel, collisions);
            }
            else if (source.EndsWith(".gif"))
            {
                Image visuel = ObtenirGIF(source);
                return new Obstacle(nom, visuel, collisions);
            }
            throw new NotImplementedException();
        }


        private ImageBrush ObtenirPNG(string source)
        {
            return new ImageBrush(new BitmapImage(new Uri(dir + source)));
        }


        private Image ObtenirGIF(string source)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(dir + source);
            image.EndInit();
            Image visuel = new Image()
            { Width = image.Width, Height = image.Height, Source = image };
            ImageBehavior.SetAnimatedSource(visuel, image);
            return visuel;
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
            obst.DX = rd.Next(param[4], param[5])/10.0;
            obst.DY = rd.Next(param[6], param[7])/100.0;

            // Personnalisation spécifique
            switch (obst.Nom)
            {
                case "caillou": obst.Visuel.Fill = textureCaillou[rd.Next(0, 3)]; break;
                case "corail": obst.Visuel.Fill = textureCorail[rd.Next(0, 3)]; break;
            }
        }


        private void ObtenirTextures()
        {
            ImageBrush[,] textures = new ImageBrush[nbMondes, 2];
            // 0: Fond, 1: Sol
            for (int i = 0; i < nbMondes; i++)
            {
                textures[i, 0] = ObtenirPNG($"/img/mondes/monde{i + 1}/fond.png");
                textures[i, 1] = ObtenirPNG($"/img/mondes/monde{i + 1}/sol.png");
            }
            texturesDecor = textures;

            // Caillou
            for (int i = 0; i < 3; i++)
            {
                textureCaillou[i] = ObtenirPNG($"/img/obstacles/fond/caillou/caillou{i + 1}.png");
            }

            // Corail
            for (int i = 0; i < 3; i++)
            {
                textureCorail[i] = ObtenirPNG($"/img/obstacles/fond/corail/corail{i + 1}.png");
            }

            // Icones
            iconesMonde[0] = ObtenirPNG("/img/iconesMondes/mondeBloque.png");
            for (int i = 1; i <= nbMondes; i++)
            {
                iconesMonde[i] = ObtenirPNG($"/img/iconesMondes/monde{i}.png");
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
                    $"nbObstacles : {obstacles.Count()}\n" +
                    $"estEnCollision : {estEnCollision}\n" +
                    $"stageDeJeu : {stage}\n" +
                    $"numeroDeMonde : {monde}\n" +
                    $"transition: {positionXtransition}\n" +
                    $"score : {FormatDebug(score)}\n" +
                    $"record : {recordJoueur}\n" +
                    $"vie : {vie}\n" +
                    $"invinc : {invincible}\n" +
                    $"btnPerso : {btnUtilise} -> {btnPerso}\n" +
                    $"IPScalcul : {60 / tickParImage}"
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
            if (e.Key == Key.Escape)
            {
                Pause();
            }
            if (btnUtilise && e.Key == btnPerso)
            {
                btnAppui = true;
            }
#if DEBUG
            // Commandes
            if (e.Key == Key.NumPad0) { ApparitionFond(); }
            if (e.Key == Key.NumPad1) { ApparitionEau(); }
            if (e.Key == Key.NumPad4) { ApparitionSurface(); }
            if (e.Key == Key.NumPad7) { ApparitionAir(); }
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
            if (btnUtilise && e.Key == btnPerso)
            {
                btnAppui = false;
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
            if (stage == 0 && (options == null || options.Visibility == Visibility.Hidden)) stage = 1;
        }


        private void Transition(int sens)
        {
            // Effet de transition
            if (transition.RenderTransform is TranslateTransform translateTransform)
            {
                translateTransform.X += 20 * sens * tickParImage;
                positionXtransition = translateTransform.X;
            }
            else transition.RenderTransform = new TranslateTransform(0, 0);

            if (sens == 1)
            {
                Menu.Opacity -= 0.05 * tickParImage;

                if (positionXtransition >= 1000 && stage == 1)
                {
                    AppliqueOptions();
                    obstacles.ForEach(obstacle => obstacle.RetireObstacle(Canvas));
                    obstacles.Clear();
                    lbDistance.Visibility = Visibility.Visible;
                    vitesse = vitesseInit;
                    vie = vieMax;
                    Vie.Visibility = Visibility.Visible;
                    AfficheVie();
                    score = 0;
                    stage = 2;
                }
                else if (positionXtransition >= 3300)
                {
                    Menu.Visibility = Visibility.Hidden;
                    ProchaineApparition();
                    stage = 3;
                    Menu.Opacity = 0;
                }
            }
            else
            {
                if (positionXtransition <= 1000 && stage == 4)
                {
                    Vie.Visibility = Visibility.Hidden;
                    lbDistance.Visibility = Visibility.Hidden;
                    vitesse = vitesseInit;
                    ProchaineApparition();
                    stage = 5;
                }
                else if (positionXtransition <= 500)
                {
                    Menu.Opacity += 0.05 * tickParImage;
                    Menu.Visibility = Visibility.Visible;
                    
                    if (positionXtransition <= 0)
                    {
                        stage = 0;
                        Menu.Opacity = 1;
                    }
                }
            }
        }


        private void AppliqueOptions()
        {
            PlacerTextures();
            if (options == null) return;
            string contenu = options.BtnPerso.Content.ToString();
            if (contenu == "..." || contenu == "-")
            {
                btnUtilise = false;
            }
            else
            {
                object temp;
                btnUtilise = Enum.TryParse(typeof(Key), contenu, out temp);
                if (temp != null) btnPerso = (Key)temp;
            }
            for (int i = 0; i < paramsIPS.Length; i++)
            {
                if (paramsIPS[i].IsChecked == true)
                {
                    tickParImage = i + 1; break;
                }
            }
            options.BtnReinitScore.IsEnabled = true;
        }


        private void ProchaineApparition()
        {
            score = 0;
            Random rd = new Random();
            prochaineApparition = new double[4];
            for (int i = 0; i < 4; i++) 
            {
                prochaineApparition[i] = rd.Next(borneApparition.Item1, borneApparition.Item2) / 10.0 - 2.5;
            }
        }


        private int Obtenir(string fichier)
        {
            int sortie = 0;
            if (File.Exists(dir + fichier))
            {
                int.TryParse(File.ReadAllText(dir + fichier), out sortie);
            }
            return sortie;
        }


        private void EcrireScore(string fichier, string txt)
        {
            using (StreamWriter document = new StreamWriter(dir + fichier))
            {
                document.WriteLine(txt);
            }
        }


        private void TesterScore()
        {
            if (score > recordJoueur)
            {
                NouveauMonde();
                recordJoueur = (int)score;
                lbRecord.Content = recordJoueur;
                EcrireScore(fichierScore, ((int)Math.Round(score, 0)).ToString());
            }
        }
        private void NouveauMonde()
        {
            foreach (int palier in paliersDebloquageMonde)
            {
                if (score >= palier && recordJoueur < palier)
                {
                    lbNouvMonde.Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        private void AfficheVie()
        {
            for (int i = 0 ; i < coeurs.Length ; i++)
            {
                if (vie < vieMax-i)
                {
                    coeurs[i].Visibility = Visibility.Hidden;
                }
                else
                {
                    coeurs[i].Visibility = Visibility.Visible;
                }
            }
        }


        private void MainWindow1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double ratio = 1250.0 / 750.0; 
            double largeur = e.NewSize.Width;
            double longueur = largeur * ratio;
            Canvas.Width = largeur;
            Canvas.Height = longueur;
        }


        private void SurvolEntrée(object sender, MouseEventArgs e)
        {
            if (sender is Label btn) btn.Foreground = new SolidColorBrush(Colors.SkyBlue);
        }


        private void SurvolSortie(object sender, MouseEventArgs e)
        {
            if (sender is Label btn) btn.Foreground = new SolidColorBrush(Colors.White);
        }
        

        private void MontrerOptions(object sender, MouseButtonEventArgs e)
        {
            if (stage != 0 && !pause) return;
            if (options == null)
            {
                options = new Options();
                paramsIPS = new RadioButton[6] { options.IPS60, options.IPS30, options.IPS20, options.IPS15, options.IPS12, options.IPS10 };
                options.Show();
            }
            else if (options.Visibility == Visibility.Visible)
            {
                options.Visibility = Visibility.Hidden;
            }
            else
            {
                options.Visibility= Visibility.Visible;
            }
        }


        private void ReinitScore()
        {
            if (options != null && lbRecord.Content.ToString() != "0")
            {
                if (options.BtnReinitScore.IsEnabled == false)
                {
                    lbRecord.Content = "0";
                }
            }
        }


        private void MenuMonde(object sender, MouseButtonEventArgs e)
        {
            if (stage !=0) return;
            Menu.Visibility = Visibility.Hidden;
            menuMonde.Visibility = Visibility.Visible;
            lbNouvMonde.Visibility = Visibility.Hidden;
        }


        private void MondeSuivant(object sender, MouseButtonEventArgs e)
        {
            iconeMondeMenu++;
            if (iconeMondeMenu == nbMondes) lbMondeSuivant.Visibility = Visibility.Hidden;
            lbMondePrécédent.Visibility = Visibility.Visible;
            AffichageIconeMonde();
        }


        private void MondePrécédent(object sender, MouseButtonEventArgs e)
        {
            iconeMondeMenu--;
            if (iconeMondeMenu == 1) lbMondePrécédent.Visibility = Visibility.Hidden;
            lbMondeSuivant.Visibility = Visibility.Visible;
            AffichageIconeMonde();
        }


        private void AffichageIconeMonde()
        {
            if (MondeDebloque(iconeMondeMenu))
            {
                lbPointRequis.Content = "";
                iconeMonde = iconesMonde[iconeMondeMenu];
                lbConfirmerMonde.Visibility = Visibility.Visible;
            }
            else
            {
                lbPointRequis.Content = $"{paliersDebloquageMonde[iconeMondeMenu - 1]}pts requis";
                iconeMonde = iconesMonde[0];
                lbConfirmerMonde.Visibility = Visibility.Hidden;
            }
            IconeMonde.Background = iconeMonde;
            lbMonde.Content = $"Monde {iconeMondeMenu}";
        }


        private void ConfirmerMonde(object sender, MouseButtonEventArgs e)
        {
            monde = iconeMondeMenu;
            menuMonde.Visibility = Visibility.Hidden;
            Menu.Visibility = Visibility.Visible;
            PlacerTextures();
        }


        private bool MondeDebloque(int numMonde)
        {
            return recordJoueur >= paliersDebloquageMonde[numMonde - 1];
        }


        private void Pause()
        {
            if (stage != 3) return;
            pause = !pause;
            if (pause)
            {
                menuPause.Visibility = Visibility.Visible;
            }
            else
            {
                menuPause.Visibility = Visibility.Hidden;
            }
        }


        private void btnReprendre_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (options != null && options.Visibility == Visibility.Visible) return;
            AppliqueOptions();
            Pause();
        }


        private void btnQuitterPartie_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TesterScore();
            this.Close();
        }
    }
}
