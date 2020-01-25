using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Archer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool targeted;
        private double result;
        private bool runFlag;
        private int counter;
        private double bestResult;
        private string path = "BestResult.txt";
        private double maxDistance;
        private TranslateTransform animatedTranslateTransform;

        private double V0;

        private double currentAngle;
        private TimeSpan shotTime;
        private Rect r2;

        public int Counter
        {
            get
            {
                return counter;
            }
            set
            {
                counter ++;
                Shots.Content = $"Remaining shots : {3 - counter}";
                if (counter > 2)
                {

                    this.StartActions(this.shotTime);

                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists(path))
            {
                File.Create(path);
            }

            double.TryParse(File.ReadAllText(path), out this.bestResult);
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            var resizeValue =  SystemParameters.PrimaryScreenHeight/ this.Height;
            this.ResizeAllElements(resizeValue);
            Blood.Visibility = Visibility.Hidden;
            NameScope.SetNameScope(this, new NameScope());
            this.animatedTranslateTransform = new TranslateTransform();
            this.RegisterName("AnimatedTranslateTransform", animatedTranslateTransform);
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (!this.runFlag) { return; }

            var left = Canvas.GetLeft(Arrow);
            var top = Canvas.GetTop(Arrow);
            var rotateTransform = this.CalculateAngle(e, left, top);
            Bow.RenderTransform = rotateTransform;
            Arrow.RenderTransform = rotateTransform;
     
        }

        private RotateTransform CalculateAngle(MouseEventArgs e, double x, double y)
        { 
            Point point = e.GetPosition(this);
            double dy = (point.Y - y);
            double dx = (point.X - x);
            double theta = Math.Atan2(dy, dx);
            double angle = (theta * 180) / Math.PI;
            return new RotateTransform(angle);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.runFlag) { return; }
            this.V0 = Power.Value;
            if (this.V0 == 0)
            {
                MessageBox.Show("You need to set power before shot! Check instruction!");
                return;
            }

            Point point = e.GetPosition(this);
            var arrowTop = Canvas.GetTop(Arrow);
            var arrowLeft = Canvas.GetLeft(Arrow);

            Arrow.RenderTransform = animatedTranslateTransform;
            
            this.currentAngle = Math.Atan2(point.Y - arrowTop, point.X - arrowLeft);
            if (this.currentAngle > 1.57 || this.currentAngle < -1.57) return;

            var vy = this.V0 * Math.Sin(this.currentAngle);
            var vx = this.V0 * Math.Cos(this.currentAngle);

            var h = Math.Pow(vy, 2) / 2;
            var z = Math.Pow(V0, 2) * Math.Sin(2 * this.currentAngle);

            this.shotTime = TimeSpan.FromSeconds(Math.Abs(2 * this.V0 * Math.Sin(this.currentAngle) / 10));

            var enemyLeft = Canvas.GetLeft(Enemy);
            var y1 = arrowTop - this.CalculateY(-enemyLeft);
            Rect r1 = new Rect(enemyLeft, y1, Arrow.ActualWidth, Arrow.ActualHeight);

            if (r1.IntersectsWith(r2))
            {
                this.targeted = true;
            }

            List<double> listOfFunctionPoints = new List<double>();
            double step = z / 10;
            double nextX = step;
            while (z * 1.2 < nextX)
            {
                if (-nextX > this.ActualWidth * 1.5)
                {
                    break;
                }
                listOfFunctionPoints.Add(nextX);
                nextX += step;
            }

            if (listOfFunctionPoints.Count >= 11)
            {
                listOfFunctionPoints.RemoveAt(10);
            }

            if (targeted)
            {
                var firstPointXAfterEnemy = listOfFunctionPoints.FirstOrDefault(x => x < -enemyLeft);
                if (firstPointXAfterEnemy != 0)
                {
                    var index = listOfFunctionPoints.IndexOf(firstPointXAfterEnemy);
                    listOfFunctionPoints = listOfFunctionPoints.Take(index).ToList();
                }

                listOfFunctionPoints.Add(-enemyLeft);          
            }

            this.shotTime = TimeSpan.FromSeconds(listOfFunctionPoints.Max() / z * this.shotTime.TotalSeconds);
            PathGeometry animationPath = new PathGeometry();
            PathFigure pFigure = new PathFigure();
            pFigure.StartPoint = new Point(0, 0);
            PolyBezierSegment pBezierSegment = new PolyBezierSegment();
            foreach (double pointX in listOfFunctionPoints)
            {
                pBezierSegment.Points.Add(new Point(-pointX, -this.CalculateY(pointX)));    
            }

            //// Debug - show calculated values in window.
            Dane.Visibility = Visibility.Hidden;
            //Dane.Visibility = Visibility.Visible;
            //Dane.Content = $"Enemy left : {enemyLeft}, Hmax = {h}, Zmax={z}, time: {this.shotTime.TotalMilliseconds}, pointX={point.X}, pointY={point.Y}";
            //foreach (var item in pBezierSegment.Points)
            //{
            //    Dane.Content += Environment.NewLine + $"Point : {item.X}, {item.Y}";
            //}

            pFigure.Segments.Add(pBezierSegment);
            animationPath.Figures.Add(pFigure);
            animationPath.Freeze();

            this.ArrowAnimation(animationPath, this.shotTime);

            if (!this.targeted)
            {
                Result.Content = "Last shot result : Miss!";
            }
            else
            {
                Result.Content = "Last shot result : Hit!";
                result += 100 - this.Power.Value;
            }
        }
        
        private double CalculateY(double x)
        {
            var y = (x * Math.Tan(this.currentAngle)) - Math.Pow(x, 2) / ((2 * Math.Pow(this.V0, 2) * Math.Pow(Math.Cos(this.currentAngle),2)));
            return y;
        }

    }
}