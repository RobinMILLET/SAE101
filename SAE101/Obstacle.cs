using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SAE101
{
    internal class Obstacle
    {
        private double pX;
        private double pY;
        private double vX;
        private double vY;
        private int indexeGraphique;
        private int[][] collisions;
        private bool fixe;

        // Constructeurs

        public Obstacle(double pX, double pY, double vX, double vY, int indexeGraphique, int[][] collisions, bool fixe)
        {
            this.PX = pX;
            this.PY = pY;
            this.VX = vX;
            this.VY = vY;
            this.IndexeGraphique = indexeGraphique;
            this.Collisions = collisions;
            this.Fixe = fixe;
        }

        public Obstacle(double pX, double pY, double vX, double vY, int indexeGraphique, int[][] collisions)
        {
            this.PX = pX;
            this.PY = pY;
            this.VX = vX;
            this.VY = vY;
            this.IndexeGraphique = indexeGraphique;
            this.Collisions = collisions;
            this.Fixe = false;
        }

        public Obstacle(double pX, double pY, double vX, double vY, int indexeGraphique, bool fixe)
        {
            this.PX = pX;
            this.PY = pY;
            this.VX = vX;
            this.VY = vY;
            this.IndexeGraphique = indexeGraphique;
            this.Fixe = fixe;
        }

        // Champs

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
                return pY;
            }

            set
            {
                pY = value;
            }
        }

        public double VX
        {
            get
            {
                return vX;
            }

            set
            {
                vX = value;
            }
        }

        public double VY
        {
            get
            {
                return vY;
            }

            set
            {
                vY = value;
            }
        }

        public int IndexeGraphique
        {
            get
            {
                return indexeGraphique;
            }

            set
            {
                indexeGraphique = value;
            }
        }

        public int[][] Collisions
        {
            get
            {
                return collisions;
            }

            set
            {
                if (collisions.GetLength(1) != 4) { throw new ArgumentException("Format des collisions : [x, y, dx, dy]"); }
                collisions = value;
            }
        }

        public bool Fixe
        {
            get
            {
                return fixe;
            }

            set
            {
                fixe = value;
            }
        }

        // Méthodes

        private void Avance(double x)
        {
            pX += x;
            Avance();
        }

        private void Avance()
        {
            if (vX != 0) pX += vX;
        }

        private void Approche(double joueurY)
        {
            if (vY != 0)
            {
                if (vY < joueurY - vY) { pY += vY; }
                else if (vY > joueurY + vY) { pY -= vY; }
            }
        }

        private void CollisionObstacle()
        {
            Obstacle[] o = new Obstacle[collisions.Length]; 
            for (int i = 0; i < collisions.Length; i++)
            {
                    o[i] = CollisionObstacle(i);
            }
        }

        private Obstacle CollisionObstacle(int i)
        {
            int[] col = collisions[i];
            Obstacle o = new Obstacle(pX + col[0], pX + col[1], vX, vY, 0, fixe);
            return o;
        }

        private void CollisionRectangle()
        {
            Rectangle[] r = new Rectangle[collisions.Length];
            for (int i = 0; i < collisions.Length; i++)
            {
                r[i] = CollisionRectangle(i);
            }
        }

        private Rectangle CollisionRectangle(int i)
        {
            int[] col = collisions[i];
            Rectangle r = new Rectangle();
            Canvas.SetLeft(r, pX + col[0]);
            Canvas.SetBottom(r, pY + col[1]);
            r.Width = col[2];
            r.Height = col[3];
            r.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            return r;
        }
    }
}
