using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GSK2
{
    public partial class Form1 : Form
    {
        readonly Graphics g;
        readonly Pen DrawPen = new Pen(Color.Black, 1);
        List<MyPoint> VertexList = new List<MyPoint>();
        private readonly List<List<MyPoint>> _figures = new List<List<MyPoint>>();
        bool _splineType;
        bool _flagFigure;
        int _cornersCount;
        Point pictureBox1MousePos;
        readonly Bitmap _bitmap;
        bool checkPgn;
        private M[] _m;

        int yMin;
        int yMax;
        int xMin;
        int xMax;

        private string _nameNowFigure;

        public Form1()
        {
            InitializeComponent();
            _bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(_bitmap);
            MouseWheel += TR;
        }

        //Кубический сплайн
        private void DrawCubeSpline(Pen drPen, List<MyPoint> p)
        {
            PointF[] L = new PointF[4]; // Матрица вещественных коэффициентов
            Point Pv1 = p[0].ToPoint();
            Point Pv2 = p[0].ToPoint();
            const double dt = 0.04;
            double t = 0;
            double xt, yt;
            Point Ppred = p[0].ToPoint(), Pt = p[0].ToPoint();
            // Касательные векторы
            Pv1.X = (int) (4 * (p[1].X - p[0].X));
            Pv1.Y = (int) (4 * (p[1].Y - p[0].Y));
            Pv2.X = (int) (4 * (p[3].X - p[2].X));
            Pv2.Y = (int) (4 * (p[3].Y - p[2].Y));
            // Коэффициенты полинома
            L[0].X = 2 * p[0].X - 2 * p[2].X + Pv1.X + Pv2.X; // Ax
            L[0].Y = 2 * p[0].Y - 2 * p[2].Y + Pv1.Y + Pv2.Y; // Ay
            L[1].X = -3 * p[0].X + 3 * p[2].X - 2 * Pv1.X - Pv2.X; // Bx
            L[1].Y = -3 * p[0].Y + 3 * p[2].Y - 2 * Pv1.Y - Pv2.Y; // By
            L[2].X = Pv1.X; // Cx
            L[2].Y = Pv1.Y; // Cy
            L[3].X = p[0].X; // Dx
            L[3].Y = p[0].Y; // Dy
            VertexList.Clear();
            VertexList.Add(new MyPoint(Ppred.X, Ppred.Y) {IsFunc = true});
            while (t < 1 + dt / 2)
            {
                xt = ((L[0].X * t + L[1].X) * t + L[2].X) * t + L[3].X;
                yt = ((L[0].Y * t + L[1].Y) * t + L[2].Y) * t + L[3].Y;
                Pt.X = (int) Math.Round(xt);
                Pt.Y = (int) Math.Round(yt);
                g.DrawLine(drPen, Ppred, Pt);
                pictureBox1.Image = _bitmap;
                Ppred = Pt;
                VertexList.Add(new MyPoint(Ppred.X, Ppred.Y));
                t += dt;
            }
        }

        //обработчик нажатия
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox1MousePos = e.Location;
            try
            {
                if (ThisPgn(e.X, e.Y))
                {
                    g.DrawEllipse(new Pen(Color.Blue), e.X - 2, e.Y - 2, 5, 5);
                    checkPgn = true;
                }
                else if (_flagFigure)
                {
                    switch (_nameNowFigure)
                    {
                        case "Fg1":
                            CreateFg1(e);
                            break;
                        case "Zv":
                            CreateZv(e);
                            break;
                        default:
                            break;
                    }

                    VertexList = _figures.Last();
                    FillIn(VertexList);
                    VertexList = new List<MyPoint>();
                    _flagFigure = false;
                }

                else if (MouseButtons == MouseButtons.Left)
                {
                    VertexList.Add(new MyPoint(e.X, e.Y));
                    g.DrawEllipse(DrawPen, e.X - 2, e.Y - 2, 5, 5);
                    if (VertexList.Count > 1)
                    {
                        g.DrawLine(DrawPen, VertexList[VertexList.Count - 2].ToPoint(),
                            VertexList[VertexList.Count - 1].ToPoint());
                        pictureBox1.Image = _bitmap;
                    }
                }
                else if (_splineType && VertexList.Count >= 4)
                {
                    DrawCubeSpline(DrawPen, VertexList);
                    PaintLine(VertexList);
                    _figures.Add(VertexList.ToList());
                    VertexList.Clear();
                }
                else if (MouseButtons == MouseButtons.Right)
                {
                    FillIn(VertexList);
                    _figures.Add(VertexList.ToList());
                    VertexList.Clear();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        
        private void PaintLine(List<MyPoint> points)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                g.DrawLine(DrawPen, points[i].ToPoint(), points[i + 1].ToPoint());
            }

            pictureBox1.Image = _bitmap;
        }

        //обработчик события 
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left && operation == 3 && checkPgn)
                {
                    Moving(e, e.X - pictureBox1MousePos.X, e.Y - pictureBox1MousePos.Y);
                    g.Clear(pictureBox1.BackColor);

                    FillIn(VertexList);
                    pictureBox1.Image = _bitmap;

                    pictureBox1MousePos = e.Location;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        //алгоритм закрашивание фигуры (внутри)
        private void FillIn(List<MyPoint> points)
        {
            if (points[0].IsFunc)
            {
                PaintLine(points);
                return;
            }

            int k;
            List<int> xb = new List<int>();
            SearchMinAndMax(points);
            for (int Y = yMin; Y <= yMax; Y++)
            {
                xb.Clear();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    if (i < points.Count)
                    {
                        k = i + 1;
                    }
                    else k = 1;

                    if (points[i].Y < Y && points[k].Y >= Y || points[i].Y >= Y && points[k].Y < Y)
                    {
                        var x = -((Y * (points[i].X - points[k].X)) - points[i].X * points[k].Y +
                                  points[k].X * points[i].Y)
                                / (points[k].Y - points[i].Y);
                        xb.Add((int) x);
                    }
                }

                if (points[points.Count - 1].Y < Y && points[0].Y >= Y ||
                    points[points.Count - 1].Y >= Y && points[0].Y < Y)
                {
                    var x = -((Y * (points[points.Count - 1].X - points[0].X)) -
                              points[points.Count - 1].X * points[0].Y +
                              points[0].X * points[points.Count - 1].Y)
                            / (points[0].Y - points[points.Count - 1].Y);
                    xb.Add((int) x);
                }

                xb.Sort();
                for (int i = 0; i < xb.Count; i += 2)
                {
                    g.DrawLine(DrawPen, new Point(xb[i], Y), new Point(xb[i + 1], Y));
                }

                pictureBox1.Image = _bitmap;
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

        #region Поиск минимума и максимума фигур

        // Поиск Ymin и Ymax
        private List<int> SearchMinAndMax(List<MyPoint> VertexList)
        {
            yMin = (int) VertexList[0].Y;
            yMax = (int) VertexList[0].Y;

            foreach (MyPoint p in VertexList)
            {
                if (p.Y < yMin)
                {
                    yMin = (int) p.Y;
                }
                else if (yMax < p.Y)
                {
                    yMax = (int) p.Y;
                }
            }

            yMin = yMin < 0 ? 0 : yMin;
            yMax = yMax < pictureBox1.Height ? yMax : pictureBox1.Height;
            return new List<int> {yMin, yMax};
        }

        //Поиск Xmin и Xmax
        private List<int> SearchXMinAndMax(List<MyPoint> VertexList)
        {
            xMin = (int) VertexList[0].Y;
            xMax = (int) VertexList[0].Y;

            foreach (MyPoint p in VertexList)
            {
                if (p.Y < xMin)
                {
                    xMin = (int) p.Y;
                }
                else if (xMax < p.Y)
                {
                    xMax = (int) p.Y;
                }
            }

            xMin = xMin < 0 ? 0 : xMin;
            xMax = xMax < pictureBox1.Height ? xMax : pictureBox1.Height;
            return new List<int> {xMin, xMax};
        }

        #endregion

        #region Создание Фигур

        //Создание Фигуры 1
        private void CreateFg1(MouseEventArgs e)
        {
            var fg = new List<MyPoint>()
            {
                new MyPoint(e.X - 150, e.Y + 100),
                new MyPoint(e.X - 150, e.Y),
                new MyPoint(e.X - 50, e.Y),
                new MyPoint(e.X, e.Y - 100),
                new MyPoint(e.X + 50, e.Y),
                new MyPoint(e.X + 150, e.Y),
                new MyPoint(e.X + 150, e.Y + 100)
            };
            _figures.Add(fg);
        }

        // Создание фигуры Звезда 
        private void CreateZv(MouseEventArgs e)
        {
            const double R = 25;
            const double r = 50;
            const double d = 0;
            double a = d, da = Math.PI / _cornersCount, l;
            var star = new List<MyPoint>();
            for (var k = 0; k < 2 * _cornersCount + 1; k++)
            {
                l = k % 2 == 0 ? r : R;
                star.Add(new MyPoint() {X = ((float) (e.X + l * Math.Cos(a))), Y = ((float) (e.Y + l * Math.Sin(a)))});
                a += da;
            }

            _figures.Add(star);
        }

        #endregion

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
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    _nameNowFigure = "Fg1";
                    break;
                case 1:
                    _nameNowFigure = "Zv";
                    break;
            }

            _flagFigure = true;
        }

        // Кнопка очистки
        private void button1_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            VertexList.Clear();
            pictureBox1.Image = _bitmap;
            _figures.Clear();
            MessageBox.Show("Очистка выполнена");
        }

        // Выбор сплайна
        private void checkBox1_CheckedChanged(object sender, EventArgs e) => _splineType = checkBox1.Checked;

        // Колличество углов
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) =>
            _cornersCount = comboBox3.SelectedIndex + 5;

       
        #region ТМО

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
            var s1Figure = SearchMinAndMax(_figures[0]);
            var s2Figure = SearchMinAndMax(_figures[1]);
            var yMin = s1Figure[0] < s2Figure[0] ? s1Figure[0] : s2Figure[0];
            var yMax = s1Figure[1] < s2Figure[1] ? s2Figure[1] : s1Figure[1];
            for (int Y = yMin; Y < yMax; Y++)
            {
                var oneFigure = CalculationXlAndXr(_figures[0], Y);
                Xal = oneFigure[0];
                Xar = oneFigure[1];
                var secondFigure = CalculationXlAndXr(_figures[1], Y);
                Xbl = secondFigure[0];
                Xbr = secondFigure[1];
                if (Xal.Count == 0 && Xbl.Count == 0)
                {
                    continue;
                }

                int n = Xal.Count;
                int nM;
                _m = new M[Xal.Count + Xar.Count + Xbl.Count + Xbr.Count];
                for (int i = 0; i < n; i++)
                {
                    _m[i] = new M(Xal[i], 2);
                }

                nM = n;
                n = Xar.Count;
                for (int i = 0; i < n; i++)
                {
                    _m[i + nM] = new M(Xar[i], -2);
                }

                nM = nM + n;
                n = Xbl.Count;
                for (int i = 0; i < n; i++)
                {
                    _m[nM + i] = new M(Xbl[i], 1);
                }

                nM = nM + n;
                n = Xbr.Count;
                for (int i = 0; i < n; i++)
                {
                    _m[nM + i] = new M(Xbr[i], -1);
                }

                nM += n;

                SortArrayM();

                int k = 1;
                int m1 = 1;
                int q = 0;

                xrl.Clear();
                xrr.Clear();

                if (_m[0].x >= 0 && _m[0].dQ < 0)
                {
                    xrl.Add(0);
                    q = -_m[0].dQ;
                }

                for (int i = 0; i < nM; i++)
                {
                    int x = _m[i].x;
                    int Qnew = q + _m[i].dQ;
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

                try
                {
                    for (int i = 0; i < xrr.Count; i++)
                    {
                        g.DrawLine(DrawPen, new Point(xrr[i], Y), new Point(xrl[i], Y));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Наименование: " + ex.Message, "Ошибка", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }
        }

        //сортировка массива пузырьковым методом
        private void SortArrayM()
        {
            for (var i = 0; i < _m.Length; i++)
            {
                for (var j = 0; j < _m.Length - 1; j++)
                {
                    if (_m[j].x > _m[j + 1].x)
                    {
                        var buffSort = new M(_m[j + 1].x, _m[j + 1].dQ);
                        _m[j + 1] = _m[j];
                        _m[j] = buffSort;
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
        private List<List<int>> CalculationXlAndXr(List<MyPoint> VertexList, int Y)
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
                        xR.Add((int) x);
                    }
                    else if (VertexList[i].Y > VertexList[k].Y)
                    {
                        xL.Add((int) x);
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
            if (_figures.Count > 1)
            {
                g.Clear(Color.White);
                Tmo();
            }

            VertexList.Clear();
            pictureBox1.Image = _bitmap;
        }

        #endregion


        #region Геометрические преобразования

        int operation;

        public void TR(object sender, MouseEventArgs e)
        {
            switch ( /*Выбор преобразования*/operation)
            {
                case 0:
                    // Вращение
                    Rotation(e);
                    break;
                case 1:
                    // Масштабирование OX
                    Zoom(e.Delta, e);
                    break;
                case 2:
                    // Масштабирование OY
                    ZoomY(e.Delta, e);
                    break;
                default:
                    break;
            }

            g.Clear(Color.White);
            FillIn(_figures[_figures.Count - 1]);
        }


        // Вращение
        private void Rotation(MouseEventArgs e)
        {
            var buffer = _figures[_figures.Count - 1];
            var center = new MyPoint {X = e.X, Y = e.Y};
            RotationCenter(center, true, buffer);
            const double alpha = 0.575;

            //вращение
            var matrixR30 = new[,]
            {
                {(float) Math.Cos(alpha), (float) Math.Sin(alpha), 0},
                {(float) -Math.Sin(alpha), (float) Math.Cos(alpha), 0},
                {0, 0, 1}
            };
            
            // изменяем координату вершины фигуры
            for (var i = 0; i < buffer.Count; i++) 
                buffer[i] = СalculatinTheMatrix(matrixR30, buffer[i]);

            RotationCenter(center, false, buffer);
        }


        //Плоскопараллельное перемещение
        private void Moving(MouseEventArgs e, int dx, int dy)
        {
            for (int i = 0; i <= VertexList.Count - 1; i++)
            {
                MyPoint fP = new MyPoint();
                fP.X = (VertexList[i].X + dx);
                fP.Y = (VertexList[i].Y + dy);
                VertexList[i] = fP;
            }
        }

        #region Масштабирование

        //Масштабирование по оси X относительно центра фигуры
        private void Zoom(float zoom, MouseEventArgs eMouse)
        {
            if (zoom <= 0) zoom = -0.1f;
            else zoom = 0.1f;

            var sxt = operation == 1 ? 1 + zoom : 1;
            var syt = 1;
            float[,] z =
            {
                {sxt, 0, 0},
                {0, syt, 0},
                {0, 0, 1}
            };
            var e = CenterFigure();
            var figure = _figures[_figures.Count - 1];
            RotationCenter(e, true, figure);

            for (int i = 0; i < VertexList.Count; i++)
            {
                VertexList[i] = СalculatinTheMatrix(z, VertexList[i]);
            }

            RotationCenter(e, false, figure);
        }

        //Масштабирование по оси Y относительно центра фигуры
        private void ZoomY(float zoomY, MouseEventArgs eMouseY)
        {
            if (zoomY <= 0) zoomY = -0.1f;
            else zoomY = 0.1f;
            var sxt = operation == 2 ? 1 : zoomY + 1;
            var syt = 1 + zoomY;
            float[,] z =
            {
                {sxt, 0, 0},
                {0, syt, 0},
                {0, 0, 1}
            };
            var e = CenterFigure();
            var figure = _figures[_figures.Count - 1];
            RotationCenter(e, true, figure);

            for (int i = 0; i < VertexList.Count; i++)
            {
                VertexList[i] = СalculatinTheMatrix(z, VertexList[i]);
            }

            RotationCenter(e, false, figure);
        }

        #endregion

        //центр фигуры
        private MyPoint CenterFigure()
        {
            SearchMinAndMax(_figures[_figures.Count - 1]);
            var xCenter = xMax - xMin / 2 + xMin;
            var yCenter = yMax - yMin / 2 + yMin;

            return new MyPoint() {X = xCenter, Y = yCenter};
        }

        // Перемещение относительно центра с координатами
        private void RotationCenter(MyPoint center, bool start, List<MyPoint> points)
        {
            if (start)
            {
                //массив для перемищения в начало координат
                float[,] toCenter =
                {
                    {1, 0, 0},
                    {0, 1, 0},
                    {-center.X, -center.Y, 1}
                };
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = СalculatinTheMatrix(toCenter, points[i]);
                }
            }
            else
            {
                //Из начала координат
                float[,] fromCenter =
                {
                    {1, 0, 0},
                    {0, 1, 0},
                    {center.X, center.Y, 1}
                };
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = СalculatinTheMatrix(fromCenter, points[i]);
                }
            }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e) => operation = comboBox6.SelectedIndex;

        //Метод вычисления матрицы
        public MyPoint СalculatinTheMatrix(float[,] matrixR30, MyPoint pointGt) => new MyPoint()
        {
            X = pointGt.X * matrixR30[0, 0] + pointGt.Y * matrixR30[1, 0] + pointGt.Z * matrixR30[2, 0],
            Y = pointGt.X * matrixR30[0, 1] + pointGt.Y * matrixR30[1, 1] + pointGt.Z * matrixR30[2, 1],
            Z = pointGt.X * matrixR30[0, 2] + pointGt.Y * matrixR30[1, 2] + pointGt.Z * matrixR30[2, 2],
            IsFunc = pointGt.IsFunc
        };

        // выделение многоугольника
        public bool ThisPgn(int mX, int mY)
        {
            int n = VertexList.Count() - 1, k = 0, m = 0;
            MyPoint Pi, Pk;
            double x;
            bool check = false;
            for (int i = 0; i <= n; i++)
            {
                if (i < n) k = i + 1;
                else k = 0;
                Pi = VertexList[i];
                Pk = VertexList[k];
                if ((Pi.Y < mY) && (Pk.Y >= mY) || (Pi.Y >= mY) && (Pk.Y < mY))
                {
                    if ((mY - Pi.Y) * (Pk.X - Pi.X) / (Pk.Y - Pi.Y) + Pi.X < mX)
                        m++;
                }
            }

            if (m % 2 == 1) check = true;
            return check;
        }


        // структура описывающая точку в трехмерном пространстве
        public class MyPoint
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public bool IsFunc { get; set; }

            public MyPoint(float x = 0.0f, float y = 0.0f, float z = 1.0f)
            {
                X = x;
                Y = y;
                Z = z;
                IsFunc = false;
            }

            public Point ToPoint()
            {
                return new Point((int) X, (int) Y);
            }
        }

        #endregion
    }
}