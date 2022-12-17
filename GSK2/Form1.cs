using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GSK2
{
    public partial class Form1 : Form
    {
        private readonly Graphics _g;
        private readonly Pen _drawPen = new Pen(Color.Black, 1);
        private List<MyPoint> _vertexList = new List<MyPoint>();
        private readonly List<List<MyPoint>> _figures = new List<List<MyPoint>>();
        private bool _splineType;
        private bool _flagFigure;
        private int _cornersCount;
        private Point _pictureBox1MousePos;
        private readonly Bitmap _bitmap;
        private bool _checkPgn;
        private M[] _m;

        private int _yMin;
        private int _yMax;
        private int _xMin;
        private int _xMax;


        // ТМО
        private readonly int[] _setQ = new int[2];

        private string _nameNowFigure;

        // Выбор операции
        private int _operation;

        public Form1()
        {
            InitializeComponent();
            _bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            _g = Graphics.FromImage(_bitmap);
            MouseWheel += TransformationOperation;
        }

        //Кубический сплайн
        private void DrawCubeSpline(Pen drPen, List<MyPoint> p)
        {
            var l = new PointF[4]; // Матрица вещественных коэффициентов
            var pv1 = p[0].ToPoint();
            var pv2 = p[0].ToPoint();
            const double dt = 0.04;
            double t = 0;
            double xt, yt;
            Point ppred = p[0].ToPoint(), pt = p[0].ToPoint();
            // Касательные векторы
            pv1.X = (int) (4 * (p[1].X - p[0].X));
            pv1.Y = (int) (4 * (p[1].Y - p[0].Y));
            pv2.X = (int) (4 * (p[3].X - p[2].X));
            pv2.Y = (int) (4 * (p[3].Y - p[2].Y));
            // Коэффициенты полинома
            l[0].X = 2 * p[0].X - 2 * p[2].X + pv1.X + pv2.X; // Ax
            l[0].Y = 2 * p[0].Y - 2 * p[2].Y + pv1.Y + pv2.Y; // Ay
            l[1].X = -3 * p[0].X + 3 * p[2].X - 2 * pv1.X - pv2.X; // Bx
            l[1].Y = -3 * p[0].Y + 3 * p[2].Y - 2 * pv1.Y - pv2.Y; // By
            l[2].X = pv1.X; // Cx
            l[2].Y = pv1.Y; // Cy
            l[3].X = p[0].X; // Dx
            l[3].Y = p[0].Y; // Dy
            _vertexList.Clear();
            _vertexList.Add(new MyPoint(ppred.X, ppred.Y) {IsFunc = true});
            while (t < 1 + dt / 2)
            {
                xt = ((l[0].X * t + l[1].X) * t + l[2].X) * t + l[3].X;
                yt = ((l[0].Y * t + l[1].Y) * t + l[2].Y) * t + l[3].Y;
                pt.X = (int) Math.Round(xt);
                pt.Y = (int) Math.Round(yt);
                _g.DrawLine(drPen, ppred, pt);
                pictureBox1.Image = _bitmap;
                ppred = pt;
                _vertexList.Add(new MyPoint(ppred.X, ppred.Y));
                t += dt;
            }
        }

        //обработчик нажатия
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            _pictureBox1MousePos = e.Location;
            try
            {
                if (ThisPgn(e.X, e.Y))
                {
                    _g.DrawEllipse(new Pen(Color.Blue), e.X - 2, e.Y - 2, 5, 5);
                    _checkPgn = true;
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
                    }

                    _vertexList = _figures.Last();
                    FillIn(_vertexList);
                    _vertexList = new List<MyPoint>();
                    _flagFigure = false;
                }

                else if (MouseButtons == MouseButtons.Left)
                {
                    _vertexList.Add(new MyPoint(e.X, e.Y));
                    _g.DrawEllipse(_drawPen, e.X - 2, e.Y - 2, 5, 5);
                    if (_vertexList.Count > 1)
                    {
                        _g.DrawLine(_drawPen, _vertexList[_vertexList.Count - 2].ToPoint(),
                            _vertexList[_vertexList.Count - 1].ToPoint());
                        pictureBox1.Image = _bitmap;
                    }
                }
                else if (_splineType && _vertexList.Count >= 4)
                {
                    DrawCubeSpline(_drawPen, _vertexList);
                    PaintLine(_vertexList);
                    _figures.Add(_vertexList.ToList());
                    _vertexList.Clear();
                }
                else if (MouseButtons == MouseButtons.Right)
                {
                    FillIn(_vertexList);
                    _figures.Add(_vertexList.ToList());
                    _vertexList.Clear();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PaintLine(List<MyPoint> points)
        {
            for (var i = 0; i < points.Count - 1; i++)
                _g.DrawLine(_drawPen, points[i].ToPoint(), points[i + 1].ToPoint());

            pictureBox1.Image = _bitmap;
        }

        //обработчик события 
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left && _operation == 3 && _checkPgn)
                {
                    Moving(e.X - _pictureBox1MousePos.X, e.Y - _pictureBox1MousePos.Y);
                    _g.Clear(pictureBox1.BackColor);

                    FillIn(_vertexList);
                    pictureBox1.Image = _bitmap;

                    _pictureBox1MousePos = e.Location;
                }
            }
            catch (Exception)
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

            var xb = new List<int>();
            SearchMinAndMax(points);
            for (var y = _yMin; y <= _yMax; y++)
            {
                xb.Clear();
                for (var i = 0; i < points.Count - 1; i++)
                {
                    var k = i < points.Count ? i + 1 : 1; // Изменил на итераный оператор

                    if (points[i].Y < y && points[k].Y >= y || points[i].Y >= y && points[k].Y < y)
                    {
                        var x = -((y * (points[i].X - points[k].X)) - points[i].X * points[k].Y +
                                  points[k].X * points[i].Y)
                                / (points[k].Y - points[i].Y);
                        xb.Add((int) x);
                    }
                }

                if (points[points.Count - 1].Y < y && points[0].Y >= y ||
                    points[points.Count - 1].Y >= y && points[0].Y < y)
                {
                    var x = -(y * (points[points.Count - 1].X - points[0].X) -
                              points[points.Count - 1].X * points[0].Y +
                              points[0].X * points[points.Count - 1].Y)
                            / (points[0].Y - points[points.Count - 1].Y);
                    xb.Add((int) x);
                }

                xb.Sort();
                for (var i = 0; i < xb.Count; i += 2)
                    _g.DrawLine(_drawPen, new Point(xb[i], y), new Point(xb[i + 1], y));

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
        private List<int> SearchMinAndMax(List<MyPoint> vertexList)
        {
            _yMin = (int) vertexList[0].Y;
            _yMax = (int) vertexList[0].Y;

            foreach (var p in vertexList)
            {
                if (p.Y < _yMin)
                    _yMin = (int) p.Y;
                else if (_yMax < p.Y)
                    _yMax = (int) p.Y;
            }

            _yMin = _yMin < 0 ? 0 : _yMin;
            _yMax = _yMax < pictureBox1.Height ? _yMax : pictureBox1.Height;
            return new List<int> {_yMin, _yMax};
        }

        //Поиск Xmin и Xmax
        private List<int> SearchXMinAndMax(List<MyPoint> vertexList)
        {
            _xMin = (int) vertexList[0].Y;
            _xMax = (int) vertexList[0].Y;

            foreach (var p in vertexList)
            {
                if (p.Y < _xMin)
                    _xMin = (int) p.Y;
                else if (_xMax < p.Y)
                    _xMax = (int) p.Y;
            }

            _xMin = _xMin < 0 ? 0 : _xMin;
            _xMax = _xMax < pictureBox1.Height ? _xMax : pictureBox1.Height;
            return new List<int> {_xMin, _xMax};
        }

        #endregion

        #region Создание Фигур

        //Создание Фигуры 1
        private void CreateFg1(MouseEventArgs e)
        {
            var fg = new List<MyPoint>
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
            const double rR = 25;
            const double r = 50;
            const double d = 0;
            double a = d, da = Math.PI / _cornersCount;
            var star = new List<MyPoint>();
            for (var k = 0; k < 2 * _cornersCount + 1; k++)
            {
                var l = k % 2 == 0 ? r : rR;
                star.Add(new MyPoint {X = (float) (e.X + l * Math.Cos(a)), Y = (float) (e.Y + l * Math.Sin(a))});
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
                    _drawPen.Color = Color.Black;
                    break;
                case 1:
                    _drawPen.Color = Color.Red;
                    break;
                case 2:
                    _drawPen.Color = Color.Green;
                    break;
                case 3:
                    _drawPen.Color = Color.Blue;
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
            _g.Clear(Color.White);
            _vertexList.Clear();
            pictureBox1.Image = _bitmap;
            _figures.Clear();
            MessageBox.Show("Очистка выполнена");
        }

        // Выбор сплайна
        private void checkBox1_CheckedChanged(object sender, EventArgs e) => _splineType = checkBox1.Checked;

        // Колличество углов
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) =>
            _cornersCount = comboBox3.SelectedIndex + 5;

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

        #region ТМО

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox4.SelectedIndex)
            {
                case 0:
                    _setQ[0] = 1;
                    _setQ[1] = 2;
                    break;
                case 1:
                    _setQ[0] = 3;
                    _setQ[1] = 3;
                    break;
            }
        }

        private void Tmo()
        {
            var xrl = new List<int>();
            var xrr = new List<int>();

            // Изменяем значение поля HaveTmo на true, потому что над фигурами производится ТМО
            _figures[_figures.Count - 2][0].HaveTmo = true;
            _figures[_figures.Count - 1][0].HaveTmo = true;

            var s1Figure = SearchMinAndMax(_figures[_figures.Count - 2]);
            var s2Figure = SearchMinAndMax(_figures[_figures.Count - 1]);
            var yMin = s1Figure[0] < s2Figure[0] ? s1Figure[0] : s2Figure[0];
            var yMax = s1Figure[1] < s2Figure[1] ? s2Figure[1] : s1Figure[1];
            for (var y = yMin; y < yMax; y++)
            {
                var oneFigure = CalculationXlAndXr(_figures[0], y);
                var xal = oneFigure[0];
                var xar = oneFigure[1];
                var secondFigure = CalculationXlAndXr(_figures[1], y);
                var xbl = secondFigure[0];
                var xbr = secondFigure[1];
                if (xal.Count == 0 && xbl.Count == 0)
                    continue;

                var n = xal.Count;
                _m = new M[xal.Count + xar.Count + xbl.Count + xbr.Count];
                for (var i = 0; i < n; i++)
                    _m[i] = new M(xal[i], 2);

                var nM = n;
                n = xar.Count;
                for (int i = 0; i < n; i++)
                    _m[i + nM] = new M(xar[i], -2);

                nM = nM + n;
                n = xbl.Count;
                for (int i = 0; i < n; i++)
                    _m[nM + i] = new M(xbl[i], 1);

                nM = nM + n;
                n = xbr.Count;
                for (int i = 0; i < n; i++)
                    _m[nM + i] = new M(xbr[i], -1);

                nM += n;

                SortArrayM();

                var q = 0;
                xrl.Clear();
                xrr.Clear();

                if (_m[0].X >= 0 && _m[0].Dq < 0)
                {
                    xrl.Add(0);
                    q = -_m[0].Dq;
                }

                for (int i = 0; i < nM; i++)
                {
                    int x = _m[i].X;
                    int Qnew = q + _m[i].Dq;
                    if (!(_setQ[0] <= q && q <= _setQ[1]) && _setQ[0] <= Qnew && Qnew <= _setQ[1])
                        xrl.Add(x);
                    else if (_setQ[0] <= q && q <= _setQ[1] && !(_setQ[0] <= Qnew && Qnew <= _setQ[1]))
                        xrr.Add(x);

                    q = Qnew;
                }

                if (_setQ[0] <= q && q <= _setQ[1])
                    xrr.Add(pictureBox1.Height);

                try
                {
                    for (int i = 0; i < xrr.Count; i++)
                        _g.DrawLine(_drawPen, new Point(xrr[i], y), new Point(xrl[i], y));
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
            for (var j = 0; j < _m.Length - 1; j++)
                if (_m[j].X > _m[j + 1].X)
                {
                    var buffSort = new M(_m[j + 1].X, _m[j + 1].Dq);
                    _m[j + 1] = _m[j];
                    _m[j] = buffSort;
                }
        }


        // Для обработки исходных границ сегментов
        public class M
        {
            public int X { get; }
            public int Dq { get; }

            public M(int x, int dQ)
            {
                X = x;
                Dq = dQ;
            }
        }


        // Нахождение X левой и правой границы
        private List<List<int>> CalculationXlAndXr(List<MyPoint> vertexList, int y)
        {
            var xR = new List<int>();
            var xL = new List<int>();

            for (int i = 0; i < vertexList.Count - 1; i++)
            {
                int k;
                if (i < vertexList.Count)
                {
                    k = i + 1;
                }
                else k = 1;

                if (vertexList[i].Y < y && vertexList[k].Y >= y || vertexList[i].Y >= y && vertexList[k].Y < y)
                {
                    var x = -((y * (vertexList[i].X - vertexList[k].X)) - vertexList[i].X * vertexList[k].Y +
                              vertexList[k].X * vertexList[i].Y)
                            / (vertexList[k].Y - vertexList[i].Y);

                    if (vertexList[i].Y < vertexList[k].Y)
                        xR.Add((int) x);
                    else if (vertexList[i].Y > vertexList[k].Y)
                        xL.Add((int) x);
                }
            }

            if (vertexList[vertexList.Count - 1].Y < y && vertexList[0].Y >= y ||
                vertexList[vertexList.Count - 1].Y >= y && vertexList[0].Y < y)
            {
                var x = -((y * (vertexList[vertexList.Count - 1].X - vertexList[0].X)) -
                          vertexList[vertexList.Count - 1].X * vertexList[0].Y +
                          vertexList[0].X * vertexList[vertexList.Count - 1].Y)
                        / (vertexList[0].Y - vertexList[vertexList.Count - 1].Y);
                if (vertexList[vertexList.Count - 1].Y < vertexList[0].Y)
                    xR.Add((int) x);
                else if (vertexList[vertexList.Count - 1].Y > vertexList[0].Y)
                    xL.Add((int) x);
            }

            return new List<List<int>>
            {
                xL,
                xR
            };
        }

        //кнопка для применения тмо
        private void button2_Click(object sender, EventArgs e)
        {
            if (_figures.Count > 1)
            {
                _g.Clear(Color.White);
                Tmo();
            }

            _vertexList.Clear();
            pictureBox1.Image = _bitmap;
        }

        #endregion


        #region Геометрические преобразования

        private void TransformationOperation(object sender, MouseEventArgs e)
        {
            var figureBuff = _figures[_figures.Count - 1];
            if (figureBuff[0].HaveTmo) // Проверяем, было ли сделано ТМО над фигурой
            {
                TR(e, figureBuff);
                TR(e, _figures[_figures.Count - 2]);
                _g.Clear(Color.White);
                Tmo();
                pictureBox1.Image = _bitmap;
            }
            else
                TR(e, figureBuff);
        }

        private void TR(MouseEventArgs e, List<MyPoint> points) // Нужно прокидывать через метод фигуру,
            // над которой мы делаем преобразование, поэтому передаю список точек.
            // Далее прокидываем этот списко точек в каждый метод геометр преобразования
        {
            switch (_operation)
            {
                case 0:
                    // Вращение
                    Rotation(e, points);
                    break;
                case 1:
                    // Масштабирование OX
                    Zoom(e.Delta, points);
                    break;
                case 2:
                    // Масштабирование OY
                    ZoomY(e.Delta, points);
                    break;
            }

            _g.Clear(Color.White);
            FillIn(points);
        }


        // Вращение
        private void Rotation(MouseEventArgs e, List<MyPoint> points)
        {
            var center = new MyPoint {X = e.X, Y = e.Y};
            RotationCenter(center, true, points);
            const double alpha = 0.575;

            //вращение
            var matrixR30 = new[,]
            {
                {(float) Math.Cos(alpha), (float) Math.Sin(alpha), 0},
                {(float) -Math.Sin(alpha), (float) Math.Cos(alpha), 0},
                {0, 0, 1}
            };

            // изменяем координату вершины фигуры
            for (var i = 0; i < points.Count; i++)
                points[i] = СalculationMatrix(matrixR30, points[i]);

            RotationCenter(center, false, points);
        }


        //Плоскопараллельное перемещение
        private void Moving(int dx, int dy)
        {
            for (var i = 0; i <= _vertexList.Count - 1; i++)
            {
                var fP = new MyPoint
                {
                    X = _vertexList[i].X + dx,
                    Y = _vertexList[i].Y + dy
                };
                _vertexList[i] = fP;
            }
        }

        #region Масштабирование

        //Масштабирование по оси X относительно центра фигуры
        private void Zoom(float zoom, List<MyPoint> points)
        {
            if (zoom <= 0) zoom = -0.1f;
            else zoom = 0.1f;

            var sxt = _operation == 1 ? 1 + zoom : 1;
            const int syt = 1;
            float[,] z =
            {
                {sxt, 0, 0},
                {0, syt, 0},
                {0, 0, 1}
            };
            var e = CenterFigure();
            RotationCenter(e, true, points);

            for (var i = 0; i < points.Count; i++)
                points[i] = СalculationMatrix(z, points[i]);

            RotationCenter(e, false, points);
        }

        //Масштабирование по оси Y относительно центра фигуры
        private void ZoomY(float zoomY, List<MyPoint> points)
        {
            if (zoomY <= 0) zoomY = -0.1f;
            else zoomY = 0.1f;
            var sxt = _operation == 2 ? 1 : zoomY + 1;
            var syt = 1 + zoomY;
            float[,] z =
            {
                {sxt, 0, 0},
                {0, syt, 0},
                {0, 0, 1}
            };
            var e = CenterFigure();
            var figure = points[points.Count - 1];
            RotationCenter(e, true, points);

            for (var i = 0; i < points.Count; i++)
                points[i] = СalculationMatrix(z, points[i]);

            RotationCenter(e, false, points);
        }

        #endregion

        //центр фигуры
        private MyPoint CenterFigure()
        {
            SearchMinAndMax(_figures[_figures.Count - 1]);
            var xCenter = _xMax - _xMin / 2 + _xMin;
            var yCenter = _yMax - _yMin / 2 + _yMin;

            return new MyPoint {X = xCenter, Y = yCenter};
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
                for (var i = 0; i < points.Count; i++)
                    points[i] = СalculationMatrix(toCenter, points[i]);
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
                for (var i = 0; i < points.Count; i++)
                    points[i] = СalculationMatrix(fromCenter, points[i]);
            }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e) => _operation = comboBox6.SelectedIndex;

        //Метод вычисления матрицы
        private MyPoint СalculationMatrix(float[,] matrixR30, MyPoint pointGt) => new MyPoint
        {
            X = pointGt.X * matrixR30[0, 0] + pointGt.Y * matrixR30[1, 0] + pointGt.Z * matrixR30[2, 0],
            Y = pointGt.X * matrixR30[0, 1] + pointGt.Y * matrixR30[1, 1] + pointGt.Z * matrixR30[2, 1],
            Z = pointGt.X * matrixR30[0, 2] + pointGt.Y * matrixR30[1, 2] + pointGt.Z * matrixR30[2, 2],
            IsFunc = pointGt.IsFunc
        };

        // выделение многоугольника
        private bool ThisPgn(int mX, int mY)
        {
            var n = _vertexList.Count - 1;
            var m = 0;
            for (var i = 0; i <= n; i++)
            {
                int k;
                if (i < n) k = i + 1;
                else k = 0;
                var pi = _vertexList[i];
                var pk = _vertexList[k];
                if ((pi.Y < mY && pk.Y >= mY || pi.Y >= mY && pk.Y < mY)
                    && (mY - pi.Y) * (pk.X - pi.X) / (pk.Y - pi.Y) + pi.X < mX)
                    m++;
            }

            return m % 2 == 1;
        }

        // Класс описывающий точку в двухмерном пространстве
        public class MyPoint
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public bool IsFunc { get; set; }
            public bool HaveTmo { get; set; } // Если над фигурой будет произведено, то ТМО => true

            public MyPoint(float x = 0.0f, float y = 0.0f, float z = 1.0f)
            {
                X = x;
                Y = y;
                Z = z;
                IsFunc = false;
                HaveTmo = false;
            }

            public Point ToPoint() => new Point((int) X, (int) Y);
        }

        #endregion
    }
}