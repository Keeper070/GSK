using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
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
        List<PointGeoTransform> VertexList = new List<PointGeoTransform>();
        bool SplineType = false;
        bool FlagFigure = false;
        int cornersCount;
        Bitmap bitmap;
        public M[] m;

        int yMin;
        int yMax;

        public Form1()
        {
            InitializeComponent();
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(bitmap);
        }

        static double Factorial(int n)
        {
            double x = 1;
            for (int i = 1; i <= n; i++)
                x *= i;
            return x;
        }

        //Кубический сплайн
        public void DrawCubeSpline(Pen DrPen, List<PointGeoTransform> P)
        {
            PointF[] L = new PointF[4]; // Матрица вещественных коэффициентов
            PointGeoTransform Pv1 = P[0];
            PointGeoTransform Pv2 = P[0];
            const double dt = 0.04;
            double t = 0;
            double xt, yt;
            PointGeoTransform Ppred = P[0], Pt = P[0];
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
                g.DrawLine(DrPen, Ppred.ToPoint(), Pt.ToPoint());
                pictureBox1.Image = bitmap;
                Ppred = Pt;
                t += dt;

            }
        }

        //обработчик события 
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
                VertexList.Add(new PointGeoTransform() { X = e.X, Y = e.Y });
                g.DrawEllipse(DrawPen, e.X - 2, e.Y - 2, 5, 5);
                if (VertexList.Count > 1)
                {
                    g.DrawLine(DrawPen, VertexList[VertexList.Count - 2].ToPoint(), VertexList[VertexList.Count - 1].ToPoint());
                    pictureBox1.Image = bitmap;
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
            SearchMinAndMax(VertexList);
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
                        xb.Add((int)x);
                    }
                }

                if (VertexList[VertexList.Count - 1].Y < Y && VertexList[0].Y >= Y ||
                    VertexList[VertexList.Count - 1].Y >= Y && VertexList[0].Y < Y)
                {
                    var x = -((Y * (VertexList[VertexList.Count - 1].X - VertexList[0].X)) -
                              VertexList[VertexList.Count - 1].X * VertexList[0].Y +
                              VertexList[0].X * VertexList[VertexList.Count - 1].Y)
                            / (VertexList[0].Y - VertexList[VertexList.Count - 1].Y);
                    xb.Add((int)x);
                }

                xb.Sort();
                for (int i = 0; i < xb.Count; i += 2)
                {
                    g.DrawLine(DrawPen, new Point(xb[i], Y), new Point(xb[i + 1], Y));
                }

                pictureBox1.Image = bitmap;
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
        private List<int> SearchMinAndMax(List<PointGeoTransform> VertexList)
        {
            yMin = (int)VertexList[0].Y;
            yMax = (int)VertexList[0].Y;

            foreach (PointGeoTransform p in VertexList)
            {
                if (p.Y < yMin)
                {
                    yMin = (int)p.Y;
                }
                else if (yMax < p.Y)
                {
                    yMax = (int)p.Y;
                }

            }

            yMin = yMin < 0 ? 0 : yMin;
            yMax = yMax < pictureBox1.Height ? yMax : pictureBox1.Height;
            return new List<int> { yMin, yMax };
        }

        private List<List<PointGeoTransform>> figure = new List<List<PointGeoTransform>>();

        //Создание Фигуры 1
        private void CreateFg1(MouseEventArgs e)
        {
            var fg = new List<PointGeoTransform>()
            {
                new PointGeoTransform(){X=e.X - 150, Y=e.Y + 100},
                new PointGeoTransform(){X=e.X - 150,Y= e.Y},
                new PointGeoTransform(){X=e.X - 50, Y= e.Y},
                new PointGeoTransform(){X=e.X, Y=e.Y - 100},
                new PointGeoTransform(){X=e.X + 50,Y= e.Y},
                new PointGeoTransform(){X=e.X + 150,Y= e.Y},
                new PointGeoTransform(){X= e.X + 150,Y= e.Y + 100 }
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
            var star = new List<PointGeoTransform>();
            for (var k = 0; k < 2 * cornersCount + 1; k++)
            {
                l = k % 2 == 0 ? r : R;
                star.Add(new PointGeoTransform() { X = ((float)(e.X + l * Math.Cos(a))), Y = ((float)(e.Y + l * Math.Sin(a))) });
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
            pictureBox1.Image = bitmap;
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
                default:
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

        private void Tmo()
        {
            List<int> Xal = new List<int>();
            List<int> Xar = new List<int>();
            List<int> Xbl = new List<int>();
            List<int> Xbr = new List<int>();
            List<int> xrl = new List<int>();
            List<int> xrr = new List<int>();
            var s1Figure = SearchMinAndMax(figure[0]);
            var s2Figure = SearchMinAndMax(figure[1]);
            var yMin = s1Figure[0] < s2Figure[0] ? s1Figure[0] : s2Figure[0];
            var yMax = s1Figure[1] < s2Figure[1] ? s2Figure[1] : s1Figure[1];
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

                nM += n;

                SortArrayM();

                int k = 1;
                int m1 = 1;
                int q = 0;

                xrl.Clear();
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
        private List<List<int>> CalculationXlAndXr(List<PointGeoTransform> VertexList, int Y)
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
                        xR.Add((int)x);
                    }
                    else if (VertexList[i].Y > VertexList[k].Y)
                    {
                        xL.Add((int)x);
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
                g.Clear(Color.White);
                Tmo();
            }

            VertexList.Clear();
            pictureBox1.Image = bitmap;
        }

        // геометрические преобразования
        private void GeometricTransformations(object sender, MouseEventArgs e)
        {

            float[,] matrixR30 = new[,]
            {
                { (float) Math.Cos(30),     (float) Math.Sin(30),   0},
                { (float)(-Math.Sin(30)),  (float)Math.Cos(30),   0},
                { 0, 1, 2}
            };
            // изменяем координату вершины фигуры
            for (int i = 0; i < VertexList.Count; i++)
            {
                VertexList[i] = СalculatinTheMatrix(matrixR30, VertexList[i]);

            }

        }

        //вращение относительно центра с координатами
        private void RotationToTheCenter(PointGeoTransform pointGt, bool start)
        {
            if (start)
            {
                //массив начала координат
                float[,] fromCenterOfOrigin =

                {
                    {1,0,0},
                    {0,1,1},
                    {-pointGt.X,-pointGt.Y,1}
                };
                for (int i = 0; i < VertexList.Count; i++)
                {
                    VertexList[i] = СalculatinTheMatrix(fromCenterOfOrigin, VertexList[i]);

                }
            }
            else
            {
                //в центр
                float[,] fromCenter =

                {
                    {1,0,0},
                    {0,1,0},
                    {pointGt.X,pointGt.Y,1}
                };
                for (int i = 0; i < VertexList.Count; i++)
                {
                    VertexList[i] = СalculatinTheMatrix(fromCenter, VertexList[i]);

                }
            }

        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedIndex == 0)
            {

            }
        }

        //Метод вычисления матрицы
        public static PointGeoTransform СalculatinTheMatrix(float[,] matrixR30, PointGeoTransform pointGt)
        {

            return new PointGeoTransform
            {
                X = pointGt.X * matrixR30[0, 0] + pointGt.Y * matrixR30[1, 0] + pointGt.Z * matrixR30[2, 0],
                Y = pointGt.X * matrixR30[0, 1] + pointGt.Y * matrixR30[1, 1] + pointGt.Z * matrixR30[2, 1],
                Z = pointGt.X * matrixR30[0, 2] + pointGt.Y * matrixR30[1, 2] + pointGt.Z * matrixR30[2, 2]
            };

        }
    }

    // структура описывающая точку в трехмерном пространстве
    public struct PointGeoTransform
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Point ToPoint()
        {

            return new Point((int)X, (int)Y);
        }
    }

}
