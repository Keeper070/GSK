using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace GSK2
{
    public partial class Form1 : Form
    {
        Graphics g;
        Pen DrawPen = new Pen(Color.Black, 1);
        List<Point> VertexList = new List<Point>();
        bool SplineType = false;
        bool FlagFigure = false;
        int cornersCount;
        Bitmap buff;

        int yMin;
        int yMax;

        public Form1()
        {
            InitializeComponent();
            buff = new Bitmap(pictureBox1.Width, pictureBox1.Height); // для понимания кода название перменной bitmap считаю лучше, buff часто где используешь и можешь запутаться
            g = Graphics.FromImage(buff);
            // g = pictureBox1.CreateGraphics(); -- не нужно создавать, ты уже bitmap туда закинул
            // g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; -- зачем?
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
                Pt.X = (int) Math.Round(xt);
                Pt.Y = (int) Math.Round(yt);
                g.DrawLine(DrPen, Ppred, Pt);
                Ppred = Pt;
                t += dt;
            }
        }


        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (FlagFigure && SplineType == false)
            {
                CreateFg1(e);
                VertexList = figure.Last();
                FirstAlgoritm(e);
            }
            else if (!FlagFigure && SplineType == false)
            {
                CreateZv(e);
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

        //алгоритм закрашивание фигуры (внутри)
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
                        var x = -((Y * (VertexList[i].X - VertexList[k].X)) - VertexList[i].X * VertexList[k].Y +
                                  VertexList[k].X * VertexList[i].Y)
                                / (VertexList[k].Y - VertexList[i].Y);
                        xb.Add(x);
                    }
                }

                if (VertexList[VertexList.Count - 1].Y < Y && VertexList[0].Y >= Y ||
                    VertexList[VertexList.Count - 1].Y >= Y && VertexList[0].Y < Y)
                {
                    var x = -((Y * (VertexList[VertexList.Count - 1].X - VertexList[0].X)) -
                              VertexList[VertexList.Count - 1].X * VertexList[0].Y +
                              VertexList[0].X * VertexList[VertexList.Count - 1].Y)
                            / (VertexList[0].Y - VertexList[VertexList.Count - 1].Y);
                    xb.Add(x);
                }

                xb.Sort();
                for (int i = 0; i < xb.Count; i += 2)
                {
                    g.DrawLine(DrawPen, new Point(xb[i], Y), new Point(xb[i + 1], Y));
                }

                pictureBox1.Image = buff;  // после отрисовки нужно возвращать, что нарисовал
            }
        }

        /*//Второй алгоритм закрашивание (вне фигуры)
        private void SecondAlgoritm(MouseEventArgs e)
        {
            bool CW = false;
            SearchMinAndMax();
            for (int i = 0; i < VertexList.Count; i++)
            {
                 
            }
        }

        private void Matrix(int previous, int current, int next)
        {
           var s= 0.5 * ((VertexList[previous].X * VertexList[current].Y)
            + (VertexList[previous].Y * VertexList[next].X)
            + (VertexList[current].X * VertexList[next].Y)
                   - (VertexList[current].Y * VertexList[next].X)
                   - (VertexList[previous].Y * VertexList[current].X)
                   - (VertexList[previous].X * VertexList[next].Y)) < 0;
        }*/

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

            yMin = yMin < 0 ? 0 : yMin;
            yMax = yMax < pictureBox1.Height ? yMax : pictureBox1.Height;
        }

        private List<List<Point>> figure = new List<List<Point>>();

        //Создание Фигуры 1
        private void CreateFg1(MouseEventArgs e)
        {
            var fg = new List<Point>()
            {
                new Point(e.X - 150, e.Y + 100),
                new Point(e.X - 150, e.Y),
                new Point(e.X - 50, e.Y),
                new Point(e.X, e.Y - 100),
                new Point(e.X + 50, e.Y),
                new Point(e.X + 150, e.Y),
                new Point(e.X + 150, e.Y + 100)
            };
            figure.Add(fg);
        }

        // Создание фигуры Звезда 
        private void CreateZv(MouseEventArgs e)
        {
            const double R = 25;
            const double r = 50;
            const double d = 0;
            double a = d, da = Math.PI / cornersCount, l;
            var star = new List<Point>();
            for (var k = 0; k < 2 * cornersCount + 1; k++)
            {
                l = k % 2 == 0 ? r : R;
                star.Add(new Point((int) (e.X + l * Math.Cos(a)), (int) (e.Y + l * Math.Sin(a))));
                a += da;
            }

            figure.Add(star);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        //Выбор цвета
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

        // Выбор фигур
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
            if (index == 0)
            {
                FlagFigure = true;
            }
            else if (index == 1)
            {
                FlagFigure = false;
            }
        }

        // Кнопка очистки
        private void button1_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            VertexList.Clear();
            pictureBox1.Image = buff;
            figure.Clear();
        }

        // Выбор сплайна
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SplineType = checkBox1.Checked;
        }

        // Колличество углов
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            cornersCount = comboBox3.SelectedIndex + 5;
        }

        // Фигуры
        public enum Figures
        {
            Fg1,
            Zv
        }

        // Метод создания фигуры 
        /* public void FigureAdd(MouseEventArgs e)
         {
             switch ()
             {
                 case 0:
                     CreateFg1(e);
                     FirstAlgoritm(e);
                     break;
                 case 1:
                     CreateZv(e);
                     FirstAlgoritm(e);
                     break;
             }
         }*/


        //ТМО
        int[] setQ = new int[2];

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox4.SelectedIndex)
            {
                case 0:
                    setQ[0] = 1;
                    setQ[1] = 2;
                    break;
                case 1:
                    setQ[0] = 3;
                    setQ[1] = 3;
                    break;
            }
        }

        public M[] m;

        private void Tmo()
        {
            List<int> Xal = new List<int>();
            List<int> Xar = new List<int>();
            List<int> Xbl = new List<int>();
            List<int> Xbr = new List<int>();
            List<int> xrl = new List<int>();
            List<int> xrr = new List<int>();
            SearchMinAndMax(); // находишь y только у одной фигуры, а нужно у обеих, из-за этого неправильно рисуется; оставил так, чтобы сам подумал, как исправить
            for (int Y = yMin; Y < yMax; Y++)
            {
                var oneFigure = CalculationXlAndXr(figure[0], Y);
                Xal = oneFigure[0];
                Xar = oneFigure[1];
                var secondFigure = CalculationXlAndXr(figure[1], Y);
                Xbl = secondFigure[0];
                Xbr = secondFigure[1];
                if (Xal.Count == 0 && Xbl.Count == 0)
                {
                    continue;
                }

                int n = Xal.Count;
                int nM;
                m = new M[Xal.Count + Xar.Count + Xbl.Count + Xbr.Count];
                for (int i = 0; i < n; i++)
                {
                    m[i] = new M(Xal[i], 2);
                }

                nM = n;
                n = Xar.Count;
                for (int i = 0; i < n; i++)
                {
                    m[i + nM] = new M(Xar[i], -2);
                }

                nM = nM + n;
                n = Xbl.Count;
                for (int i = 0; i < n; i++)
                {
                    m[nM + i] = new M(Xbl[i], 1);
                }

                nM = nM + n;
                n = Xbr.Count;
                for (int i = 0; i < n; i++)
                {
                    m[nM + i] = new M(Xbr[i], -1);
                }

                nM = nM + n; // лучше: nM += n;
                SortArrayM();
                int k = 1; // можно объявить в другом пространстве имен
                int m1 = 1; // тоже самое
                int q = 0;
                xrl.Clear(); // было XBl -- а это другая переменная
                xrr.Clear();
                if (m[0].x >= 0 && m[0].dQ < 0)
                {
                    xrl.Add(0);
                    q = -m[0].dQ;
                }

                for (int i = 0; i < nM; i++)
                {
                    int x = m[i].x;
                    int Qnew = q + m[i].dQ;
                    if (!(setQ[0] <= q && q <= setQ[1]) && setQ[0] <= Qnew && Qnew <= setQ[1])
                    {
                        xrl.Add(x);
                        k += 1;
                    }
                    else if (setQ[0] <= q && q <= setQ[1] && !(setQ[0] <= Qnew && Qnew <= setQ[1]))
                    {
                        xrr.Add(x);
                        m1 += 1;
                    }

                    q = Qnew;
                }

                if (setQ[0] <= q && q <= setQ[1])
                {
                    xrr.Add(pictureBox1.Height);
                }

                for (int i = 0; i < xrr.Count; i++)
                {
                    g.DrawLine(DrawPen, new Point(xrr[i], Y), new Point(xrl[i], Y));
                }
            }
        }

        //сортировка массива пузырьковым методом
        private void SortArrayM()
        {
            for (var i = 0; i < m.Length; i++)
            {
                for (var j = 0; j < m.Length - 1; j++)
                {
                    if (m[j].x > m[j + 1].x)
                    {
                        var buffSort = new M(m[j + 1].x, m[j + 1].dQ);
                        m[j + 1] = m[j];
                        m[j] = buffSort;
                    }
                }
            }
        }

        // Для обработки исходных границ сегментов
        public class M
        {
            public int x { get; }
            public int dQ { get; }

            public M(int x, int dQ)
            {
                this.x = x;
                this.dQ = dQ;
            }
        }


        // Нахождение X левой и правой границы
        private List<List<int>> CalculationXlAndXr(List<Point> VertexList, int Y)
        {
            var k = 0;
            List<int> xR = new List<int>();
            List<int> xL = new List<int>();


            for (int i = 0; i < VertexList.Count - 1; i++)
            {
                if (i < VertexList.Count)
                {
                    k = i + 1;
                }
                else k = 1;

                if (VertexList[i].Y < Y && VertexList[k].Y >= Y || VertexList[i].Y >= Y && VertexList[k].Y < Y)
                {
                    var x = -((Y * (VertexList[i].X - VertexList[k].X)) - VertexList[i].X * VertexList[k].Y +
                              VertexList[k].X * VertexList[i].Y)
                            / (VertexList[k].Y - VertexList[i].Y);

                    if (VertexList[i].Y < VertexList[k].Y)
                    {
                        xR.Add(x);
                    }
                    else if (VertexList[i].Y > VertexList[k].Y)
                    {
                        xL.Add(x);
                    }
                }
            }

            if (VertexList[VertexList.Count - 1].Y < Y && VertexList[0].Y >= Y ||
                VertexList[VertexList.Count - 1].Y >= Y && VertexList[0].Y < Y)
            {
                var x = -((Y * (VertexList[VertexList.Count - 1].X - VertexList[0].X)) -
                          VertexList[VertexList.Count - 1].X * VertexList[0].Y +
                          VertexList[0].X * VertexList[VertexList.Count - 1].Y)
                        / (VertexList[0].Y - VertexList[VertexList.Count - 1].Y);
            }

            List<List<int>> arr = new List<List<int>>();
            arr.Add(xL);
            arr.Add(xR);

            return arr;
        }

        //кнопка для применения тмо
        private void button2_Click(object sender, EventArgs e)
        {
            if (figure.Count > 1)
            {
                g.Clear(Color.White); // когда заново рисуешь, нужно стирать сначала все, а потом уже рисовать
                Tmo();
            }

            // VertexList.Clear(); // закоментил, чтобы можно было посмотреть как работает с другими ТМО
            pictureBox1.Image = buff;
        }
    }
}