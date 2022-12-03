using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GSK2
{
    public partial class Form1 : Form
    {
        Graphics g;
        Pen DrawPen = new Pen(Color.Black, 1);
        List<Point> VertexList = new List<Point>();
        bool SplineType = false;
        bool FlagFigure = false;


        int yMin;
        int yMax;

        public Form1()
        {
            InitializeComponent();
            g = pictureBox1.CreateGraphics();
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        }

        static double Factorial(int n)
        {
            double x = 1;
            for (int i = 1; i <= n; i++)
                x *= i;
            return x;
        }

        //Кубический сплайн
        public void DrawCubeSpline(Pen DrPen, List<Point> P)
        {
            PointF[] L = new PointF[4]; // Матрица вещественных коэффициентов
            Point Pv1 = P[0];
            Point Pv2 = P[0];
            const double dt = 0.04;
            double t = 0;
            double xt, yt;
            Point Ppred = P[0], Pt = P[0];
            // Касательные векторы
            Pv1.X = 4 * (P[1].X - P[0].X);
            Pv1.Y = 4 * (P[1].Y - P[0].Y);
            Pv2.X = 4 * (P[3].X - P[2].X);
            Pv2.Y = 4 * (P[3].Y - P[2].Y);
            // Коэффициенты полинома
            L[0].X = 2 * P[0].X - 2 * P[2].X + Pv1.X + Pv2.X; // Ax
            L[0].Y = 2 * P[0].Y - 2 * P[2].Y + Pv1.Y + Pv2.Y; // Ay
            L[1].X = -3 * P[0].X + 3 * P[2].X - 2 * Pv1.X - Pv2.X; // Bx
            L[1].Y = -3 * P[0].Y + 3 * P[2].Y - 2 * Pv1.Y - Pv2.Y; // By
            L[2].X = Pv1.X; // Cx
            L[2].Y = Pv1.Y; // Cy
            L[3].X = P[0].X; // Dx
            L[3].Y = P[0].Y; // Dy
            while (t < 1 + dt / 2)
            {
                xt = ((L[0].X * t + L[1].X) * t + L[2].X) * t + L[3].X;
                yt = ((L[0].Y * t + L[1].Y) * t + L[2].Y) * t + L[3].Y;
                Pt.X = (int)Math.Round(xt);
                Pt.Y = (int)Math.Round(yt);
                g.DrawLine(DrPen, Ppred, Pt);
                Ppred = Pt;
                t += dt;
            }
        }


        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (FlagFigure)
            {
                CreateFg1(e);
                VertexList = figure.Last();
                FirstAlgoritm(e);
            }
            else if (MouseButtons == MouseButtons.Left)
            {
                VertexList.Add(new Point(e.X, e.Y));
                g.DrawEllipse(DrawPen, e.X - 2, e.Y - 2, 5, 5);
                if (VertexList.Count > 1)
                {
                    g.DrawLine(DrawPen, VertexList[VertexList.Count - 2], VertexList[VertexList.Count - 1]);
                }
            }

            else if (SplineType == true && VertexList.Count >= 4)
            {
                DrawCubeSpline(DrawPen, VertexList);
            }
            else if (MouseButtons == MouseButtons.Right)
            {
                FirstAlgoritm(e);
            }

        }

        //алгоритм закрашивание фигуры
        private void FirstAlgoritm(MouseEventArgs e)
        {
            int k;
            List<int> xb = new List<int>();
            SearchMinAndMax();
            for (int Y = yMin; Y <= yMax; Y++)
            {
                xb.Clear();
                for (int i = 0; i < VertexList.Count - 1; i++)
                {
                    if (i < VertexList.Count)
                    {
                        k = i + 1;
                    }
                    else k = 1;

                    if (VertexList[i].Y < Y && VertexList[k].Y >= Y || VertexList[i].Y >= Y && VertexList[k].Y < Y)
                    {
                        var x = -((Y * (VertexList[i].X - VertexList[k].X)) - VertexList[i].X * VertexList[k].Y + VertexList[k].X * VertexList[i].Y)
                          / (VertexList[k].Y - VertexList[i].Y);
                        xb.Add(x);
                    }

                }
                if (VertexList[VertexList.Count - 1].Y < Y && VertexList[0].Y >= Y || VertexList[VertexList.Count - 1].Y >= Y && VertexList[0].Y < Y)
                {
                    var x = -((Y * (VertexList[VertexList.Count - 1].X - VertexList[0].X)) - VertexList[VertexList.Count - 1].X * VertexList[0].Y + VertexList[0].X * VertexList[VertexList.Count - 1].Y)
                      / (VertexList[0].Y - VertexList[VertexList.Count - 1].Y);
                    xb.Add(x);
                }
                xb.Sort();
                for (int i = 0; i < xb.Count; i += 2)
                {
                    g.DrawLine(DrawPen, new Point(xb[i], Y), new Point(xb[i + 1], Y));
                }

            }
        }

        // Поиск Ymin и Ymax
        private void SearchMinAndMax()
        {
            yMin = VertexList[0].Y;
            yMax = VertexList[0].Y;

            foreach (Point p in VertexList)
            {

                if (p.Y < yMin)
                {
                    yMin = p.Y;
                }
                else if (yMax < p.Y)
                {
                    yMax = p.Y;
                }
            }
        }

        private List<List<Point>> figure = new List<List<Point>>();

        //создание Фигуры 1
        private void CreateFg1(MouseEventArgs e)
        {
            var fg = new List<Point>()
            {
                new Point(e.X - 150, e.Y + 100),
                new Point(e.X - 150, e.Y), 
                new Point(e.X - 50 , e.Y),
                new Point(e.X, e.Y-100),
                new Point(e.X+ 50, e.Y),
                new Point(e.X+150, e.Y),
                new Point(e.X + 150, e.Y + 100)
            };
            figure.Add(fg);

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox2.SelectedIndex)
            {
                case 0:
                    DrawPen.Color = Color.Black;
                    break;
                case 1:
                    DrawPen.Color = Color.Red;
                    break;
                case 2:
                    DrawPen.Color = Color.Green;
                    break;
                case 3:
                    DrawPen.Color = Color.Blue;
                    break;
            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:

                    break;


            }
            FlagFigure = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            VertexList.Clear();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SplineType = checkBox1.Checked;
        }
    }
}

