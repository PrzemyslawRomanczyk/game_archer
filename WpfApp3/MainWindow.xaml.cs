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

        public double V0;

        public double currentAngle;
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
                counter++;
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
            var resizeValue = SystemParameters.PrimaryScreenHeight / this.Height;
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
            //Dane.Visibility = Visibility.Hidden;
           // Dane.Visibility = Visibility.Visible;
            //Dane.Content = $"Enemy left : {enemyLeft}, Hmax = {h}, Zmax={z}, time: {this.shotTime.TotalMilliseconds}, pointX={point.X}, pointY={point.Y}";
            //foreach (var item in pBezierSegment.Points)
            //{
             //   Dane.Content += Environment.NewLine + $"Point : {item.X}, {item.Y}";
            //}

            pFigure.Segments.Add(pBezierSegment);
            animationPath.Figures.Add(pFigure);
            animationPath.Freeze();

            this.ArrowAnimation(animationPath, this.shotTime);
            //Obliczanie rezultatu 
            if (!this.targeted)
            {
                Result.Content = "Last shot result : Miss!";
                //Jeśli nie trafimy to nie dodajemy żadnego punktów do naszego wyniku
            }
            else
            {
                //Jeśli trafimy to dodajemy 100 punktów - siła naszego strzału
                Result.Content = "Last shot result : Hit!";
                result += 100 - this.Power.Value;
            }
        }

        public double CalculateY(double x)
        {
            var y = (x * Math.Tan(this.currentAngle)) - Math.Pow(x, 2) / ((2 * Math.Pow(this.V0, 2) * Math.Pow(Math.Cos(this.currentAngle), 2)));
            return y;
        }
        private void AddEnemy()
        {
            Blood.Visibility = Visibility.Hidden;
            Enemy.Visibility = Visibility.Hidden;
            Random random = new Random();
            var x = random.Next((int)(this.ActualWidth * 0.6), (int)(this.ActualWidth * 0.85));
            var y = random.Next(0, (int)(this.ActualHeight * 0.7));
            Canvas.SetLeft(Enemy, x);
            Canvas.SetTop(Enemy, y);
            Canvas.SetLeft(Blood, x);
            Canvas.SetTop(Blood, y);
            this.r2 = new Rect(x, y, Enemy.ActualWidth, Enemy.ActualHeight);
            Enemy.Visibility = Visibility.Visible;
        }

        async Task AddEnemyAsync(TimeSpan time)
        {
            await new TaskFactory().StartNew(() => Thread.Sleep(time));
            this.AddEnemy();
        }

        private void StartActions(TimeSpan time)
        {
            this.runFlag = false;
            StartButton.Visibility = Visibility.Visible;
            FinalResult.Content = $"Your result: {(int)this.result}.";
            FinalResult.Visibility = Visibility.Visible;
            counter = 0;

            if (result > bestResult)
            {
                File.WriteAllText(path, $"{this.result}");
                this.bestResult = result;
            }

            BestResult.Content = $"Best result : {(int)this.bestResult}";
            BestResult.Visibility = Visibility.Visible;
            this.result = 0;
        }

        private void MyCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!this.runFlag) { return; }
            if (e.Delta > 0)
            {
                Power.Value += 7;
            }
            else if (Power.Value > 0)
            {
                Power.Value -= 7;
            }
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            this.runFlag = true;
            this.AddEnemy();
            FinalResult.Visibility = Visibility.Hidden;
            StartButton.Visibility = Visibility.Hidden;
            BestResult.Visibility = Visibility.Hidden;
            Result.Visibility = Visibility.Hidden;
            Shots.Content = $"Remaining shots : 3";
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Height == 0) { return; }
            var resizeValue = e.NewSize.Height / e.PreviousSize.Height;
            this.ResizeAllElements(resizeValue);
        }

        private void ResizeAllElements(double resizeValue)
        {
            foreach (var x in myCanvas.Children)
            {
                this.ResizeElement((FrameworkElement)x, resizeValue);
            }
        }

        private void ResizeElement(FrameworkElement frameworkElement, double resizeValue)
        {
            frameworkElement.Height = resizeValue * frameworkElement.Height;
            frameworkElement.Width = resizeValue * frameworkElement.Width;
            var a = Canvas.GetTop(frameworkElement);
            var b = Canvas.GetLeft(frameworkElement);
            Canvas.SetTop(frameworkElement, Canvas.GetTop(frameworkElement) * resizeValue);
            Canvas.SetLeft(frameworkElement, Canvas.GetLeft(frameworkElement) * resizeValue);
        }

        private void Instructions(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Targeting: Click left button on Enemy." + Environment.NewLine +
                "Shot power: Use mouse wheel to increase/decrease power of shot.");
        }

        public void ArrowAnimation(PathGeometry animationPath, TimeSpan time)
        {
            DoubleAnimationUsingPath translateXAnimation = new DoubleAnimationUsingPath
            {
                PathGeometry = animationPath,
                Duration = time,
                Source = PathAnimationSource.X
            };

            Storyboard.SetTargetName(translateXAnimation, "AnimatedTranslateTransform");
            Storyboard.SetTargetProperty(translateXAnimation, new PropertyPath(TranslateTransform.XProperty));

            DoubleAnimationUsingPath translateYAnimation = new DoubleAnimationUsingPath
            {
                PathGeometry = animationPath,
                Duration = time,

                Source = PathAnimationSource.Y
            };

            Storyboard.SetTargetName(translateYAnimation, "AnimatedTranslateTransform");
            Storyboard.SetTargetProperty(translateYAnimation, new PropertyPath(TranslateTransform.YProperty));

            Storyboard pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.Completed += PathAnimationStoryboard_Completed;
            pathAnimationStoryboard.Children.Add(translateXAnimation);
            pathAnimationStoryboard.Children.Add(translateYAnimation);
            this.runFlag = false;
            pathAnimationStoryboard.Begin(this);
        }

        private void PathAnimationStoryboard_Completed(object sender, EventArgs e)
        {
            this.runFlag = true;
            if (this.targeted)
            {
                Blood.Visibility = Visibility.Visible;
                if (this.Counter < 2) this.AddEnemyAsync(TimeSpan.FromMilliseconds(300));
                this.targeted = false;
            }

            this.Counter++;
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}