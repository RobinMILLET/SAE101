using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SAE101
{
    internal class Obstacle
    {
        // Champs

        private Rectangle visuel;
        private Image animation;
        private bool anime = false;
        private Rect[] collisions = new Rect[0];
        private double dX = 0;
        private double dY = 0;
        private double pX = 0;
        private double pY = 0;
        private string nom = "";
#if DEBUG
        private Rectangle[] visuelCollisions = new Rectangle[0];
#endif

        // Constructeurs

        public Obstacle(string nom, Rectangle visuel, Rect[] collisions, double dX = 0, double dY = 0)
        {
            this.Nom = nom;
            this.Visuel = visuel;
            this.Anime = false;
            this.Collisions = collisions;
            this.DX = dX;
            this.DY = dY;
        }

        public Obstacle(string nom, Image animation, Rect[] collisions, double dX = 0, double dY = 0)
        {
            this.Nom = nom;
            this.Animation = animation;
            this.Anime = true;
            this.Collisions = collisions;
            this.DX = dX;
            this.DY = dY;
        }

        // Propriétés

        public string Nom
        {
            get
            {
                return nom;
            }

            set
            {
                nom = value;
            }
        }

        public Rectangle Visuel
        {
            get
            {
                return visuel;
            }

            set
            {
                visuel = value;
            }
        }

        public Image Animation
        {
            get
            {
                return animation;
            }

            set
            {
                animation = value;
            }
        }


        public bool Anime
        {
            get
            {
                return anime;
            }

            set
            {
                anime = value;
            }
        }


        public Rect[] Collisions
        {
            get
            {
                return collisions;
            }

            set
            {
                collisions = value;
            }
        }


        public double DX
        {
            get
            {
                return dX;
            }

            set
            {
                dX = value;
            }
        }


        public double DY
        {
            get
            {
                return this.dY;
            }

            set
            {
                this.dY = value;
            }
        }


#if DEBUG
        public Rectangle[] VisuelCollisions
        {
            get
            {
                return this.visuelCollisions;
            }

            set
            {
                this.visuelCollisions = value;
            }
        }
#endif


        public double PX
        {
            get
            {
                return pX;
            }

            set
            {
                pX = value;
            }
        }


        public double PY
        {
            get
            {
                return this.pY;
            }

            set
            {
                this.pY = value;
            }
        }

        // Méthodes

        public Obstacle GenereObstacle()
        {
            Obstacle obst;
            if (!anime)
            {
                obst = new Obstacle(Nom, Visuel, Collisions);
            }
            else
            {
                obst = new Obstacle(Nom, Animation, Collisions);
            }
            return obst;
        }


        public void Place(double x, double y)
        {
            PX = x;
            PY = y;
        }


        public void PlaceCollisions()
        {
            // Création des nouvelles collisions pour permettre le placement
            Rect[] vieu = this.Collisions;
            Rect[] nouv = new Rect[vieu.Length];
            for (int i = 0; i < vieu.Length; i++)
            {
                nouv[i] = new Rect(vieu[i].X + PX, vieu[i].Y + PY, vieu[i].Width, vieu[i].Height);
            }
            collisions = nouv;
        }


        public void Mouvement(double x)
        {
            double ratio = x / -10.0;
            pX += x + this.DX * ratio;
            pY += this.DY * ratio;
            for (int i = 0; i < Collisions.Length; i++)
            {
                Collisions[i].X += x + this.DX * ratio;
                Collisions[i].Y += this.DY * ratio;
            }
        }


        public void AfficheObstacle()
        {
            if (!anime)
            {
                Canvas.SetLeft(visuel, pX);
                Canvas.SetBottom(visuel, pY);
            }
            else
            {
                Canvas.SetLeft(animation, pX);
                Canvas.SetBottom(animation, pY);
            }
#if DEBUG
            for (int i = 0; i < VisuelCollisions.Length; i++)
            {
                Canvas.SetLeft(VisuelCollisions[i], collisions[i].X);
                Canvas.SetBottom(VisuelCollisions[i], collisions[i].Y);
            }
#endif
        }


#if DEBUG
        public void AfficheCollisions(Canvas canvas)
        {
            visuelCollisions = new Rectangle[collisions.Length];
            for (int i = 0; i < this.Collisions.Length; i++)
            {
                Rectangle r = AfficheCollisions(Collisions[i]);
                canvas.Children.Add(r);
                visuelCollisions[i] = r;
            }
        }


        public void CacheCollisions(Canvas canvas)
        {
            foreach (Rectangle r in visuelCollisions)
            {
                canvas.Children.Remove(r);
            }
            VisuelCollisions = new Rectangle[0];
        }


        public static Rectangle AfficheCollisions(Rect rect)
        {
            Rectangle r = new Rectangle();
            Canvas.SetBottom(r, rect.Bottom - rect.Height);
            Canvas.SetLeft(r, rect.Left);
            r.Width = rect.Width;
            r.Height = rect.Height;
            r.Stroke = new SolidColorBrush(Colors.Red);
            r.Fill = null;
            Canvas.SetZIndex(r, 999);
            return r;
        }
#endif

        // Avec borne
        public bool EstEnCollision(Rect rect, (double, double) borne)
        {
            if (borne.Item1 < pX && pX < borne.Item2)
            {
                return EstEnCollision(rect);
            }
            return false;
        }

        public bool EstEnCollision(double X, double Y, (double, double) borne)
        {
            if (borne.Item1 < pX && pX < borne.Item2)
            {
                return EstEnCollision(X, Y);
            }
            return false;
        }

        public bool EstEnCollision(Point point, (double, double) borne)
        {
            if (borne.Item1 < pX && pX < borne.Item2)
            {
                return EstEnCollision(point);
            }
            return false;
        }

        // Sans borne
        public bool EstEnCollision(Rect rect)
        {
            return collisions.Any(x => x.IntersectsWith(rect));
        }

        public bool EstEnCollision(double  X, double Y)
        {
            return collisions.Any(x => x.Contains(X, Y));
        }

        public bool EstEnCollision(Point point)
        {
            return collisions.Any(x => x.Contains(point));
        }


        // Uniforme
        public void ChangeTaille(double xy)
        {
            ChangeTaille(xy, xy);
        }


        public void ChangeTaille(double x, double y)
        {
            // A executer AVANT AfficheCollisions() et PlaceCollisions()
            if (!anime)
            {
                visuel.Width *= x;
                visuel.Height *= y;
            }
            else
            {
                ScaleTransform t = new ScaleTransform() {ScaleX = x, ScaleY = y, CenterX = pX, CenterY = pY};
                animation.RenderTransformOrigin = new Point(0,1);
                animation.RenderTransform = t;
            }
            
            for (int i = 0; i < collisions.Count(); i++)
            {
                collisions[i].Width *= x;
                collisions[i].Height *= y;
                collisions[i].X *= x;
                collisions[i].Y *= y;
            }
        }


        public bool Sorti(Canvas canvas, int limite = 0)
        {
            double tailleX;
            if (!anime) { tailleX = visuel.Width; }
            else tailleX = animation.RenderSize.Width;
            
            if (PX < canvas.Margin.Left + limite - tailleX) return true;
            return false;
        }


        public void RetireObstacle(Canvas canvas)
        {
            if (!Anime)
            {
                canvas.Children.Remove(Visuel);
            }
            else
            {
                canvas.Children.Remove(Animation);
            }
#if DEBUG
            CacheCollisions(canvas);
#endif
        }
    }
}
