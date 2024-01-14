﻿using System;
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

        public Obstacle(Rectangle visuel, Rect[] collisions, double dX = 0, double dY = 0)
        {
            this.Visuel = visuel;
            this.Collisions = collisions;
            this.DX = dX;
            this.DY = dY;
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

        public Obstacle GenereObstacle()
        {
            Obstacle obst = new Obstacle(Visuel, Collisions);
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


        public void ChangeTaille(double x, double y)
        {
            // A executer AVANT AfficheCollisions() et PlaceCollisions()
            visuel.Width *= x;
            visuel.Height *= y;
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
            if (PX < canvas.Margin.Left + limite - visuel.Width)
            {
#if DEBUG
                this.CacheCollisions(canvas);
#endif
                return true;
            }
            return false;
        }
    }
}
