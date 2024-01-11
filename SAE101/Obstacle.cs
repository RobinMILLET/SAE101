using System;
using System.Collections.Generic;
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
        private Rect[] collisions;
        private double dX;
        private double dY;
        private Rectangle[] visuelCollisions;

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
            this.DY = 0;
        }

        public Obstacle(Rectangle visuel, Rect[] collisions)
        {
            this.Visuel = visuel;
            this.Collisions = collisions;
            this.DX = 0;
            this.DY = 0;
        }

        public Obstacle(Rectangle visuel)
        {
            this.Visuel = visuel;
            this.Collisions = new Rect[0];
            this.DX = 0;
            this.DY = 0;
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

        // Méthodes

        private void Avance(int n)
        {
            double x = Canvas.GetLeft(visuel);
            Canvas.SetLeft(visuel, x + n);
            Avance();
        }

        private void Avance()
        {
            double x = Canvas.GetLeft(visuel);
            Canvas.SetLeft(visuel, x + this.DX);
        }
    }
}
