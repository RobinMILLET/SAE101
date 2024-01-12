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
        private Rect[] collisions = new Rect[0];
        private double dX = 0;
        private double dY = 0;
        private Rectangle[] visuelCollisions = new Rectangle[0];
        private double pX = 0;
        private double pY = 0;

        // Constructeurs

        public Obstacle(Rectangle visuel, Rect[] collisions, double dX, double dY)
        {
            this.Visuel = visuel;
            this.Collisions = collisions;
            this.DX = dX;
            this.DY = dY;
        }

        public Obstacle(Rectangle visuel, Rect[] collisions, double dX)
        {
            this.Visuel = visuel;
            this.Collisions = collisions;
            this.DX = dX;
        }

        // Propriétés

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

        public Obstacle GenereObstacle(double x, double y)
        {
            Obstacle obst = new Obstacle(Visuel, Collisions, dX, dY);
            obst.pX = x;
            obst.pY = y;
            return obst;
        }


        public void PlaceCollisions()
        {
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
            pX += x + this.DX;
            for (int i = 0; i < Collisions.Length; i++)
            {
                Collisions[i].X += x + this.DX;
            }
        }

        public void AfficheObstacle()
        {
            Canvas.SetLeft(visuel, pX);
            Canvas.SetBottom(visuel, pY);
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


        public bool EstEnCollision(Rect rect)
        {
            return collisions.Any(x => x.Contains(rect));
        }

        public bool EstEnCollision(double  X, double Y)
        {
            return collisions.Any(x => x.Contains(X, Y));
        }

        public bool EstEnCollision(Point point)
        {
            return collisions.Any(x => x.Contains(point));
        }
    }
}
