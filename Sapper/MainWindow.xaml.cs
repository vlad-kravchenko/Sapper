using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sapper
{
    public partial class MainWindow : Window
    {
        class Cell
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public bool Checked { get; set; }
            public Cell(int row, int col, bool ch = false)
            {
                Row = row;
                Col = col;
                Checked = ch;
            }
        }

        enum ImageType { NUMBER, EMPTY, EXPLOSION, BOMB, FLAG, UNKNOWN }

        Random rand = new Random();
        bool cheat = false;
        int rows, cols, bombs;
        List<Cell> emptyGroup = new List<Cell>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckInput())
            {
                MessageBox.Show("Please enter rows, cols and level!");
                return;
            }
            rows = Convert.ToInt32(Rows.Text);
            cols = Convert.ToInt32(Cols.Text);
            bombs = Convert.ToInt32(Level.Text);

            if (bombs < 5 || rows > 20 || cols > 20 || rows < 5 || cols < 5) return;

            RestartGame();
        }

        private bool CheckInput()
        {
            if (string.IsNullOrEmpty(Rows.Text) || string.IsNullOrEmpty(Cols.Text) || string.IsNullOrEmpty(Level.Text))
            {
                return false;
            }
            int res = 0;
            if (!int.TryParse(Rows.Text, out res)) return false;
            if (!int.TryParse(Cols.Text, out res)) return false;
            if (!int.TryParse(Level.Text, out res)) return false;
            return true;
        }

        private void RestartGame()
        {
            Map.Children.Clear();
            Map.RowDefinitions.Clear();
            Map.ColumnDefinitions.Clear();
            SetupMap();
            PlaceBombs();
            cheat = false;
            emptyGroup.Clear();
        }

        private void SetupMap()
        {
            Map.ShowGridLines = true;
            for (int i = 0; i < rows; i++)
            {
                Map.RowDefinitions.Add(new RowDefinition());
            }
            for (int i = 0; i < cols; i++)
            {
                Map.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    PlaceImage(row, col, 10, Resources["Unknown"] as BitmapImage, ImageType.UNKNOWN);
                }
            }
        }

        private void PlaceImage(int row, int col, int z, BitmapImage image, ImageType tag)
        {
            var unknown = new Image
            {
                Stretch = Stretch.Fill,
                Source = image,
                Tag = tag
            };
            Map.Children.Add(unknown);
            Panel.SetZIndex(unknown, z);
            Grid.SetRow(unknown, row);
            Grid.SetColumn(unknown, col);
        }

        private void PlaceBombs()
        {
            int factBombs = 0;
            while (factBombs < bombs)
            {
                int row = rand.Next(0, rows);
                int col = rand.Next(0, cols);

                if (EmptyCell(row, col))
                {
                    factBombs++;
                    PlaceImage(row, col, 1, Resources["Bomb"] as BitmapImage, ImageType.BOMB);
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Map.RowDefinitions.Count == 0) return;

            int row = 0;
            int col = 0;
            GetClickCoordinates(out row, out col);
            if (NotInRange(row, col)) return;

            if (e.RightButton == MouseButtonState.Pressed)
            {
                var cell = ImagesInCell(row, col);
                var flags = cell.Where(i => (ImageType)i.Tag == ImageType.FLAG).ToList();
                if (flags.Count != 0)
                {
                    Map.Children.Remove(flags.First());
                }
                else
                {
                    PlaceImage(row, col, 30, Resources["Flag"] as BitmapImage, ImageType.FLAG);                    
                }
                CheckIfVictory();
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (ImagesInCell(row, col).Any(i => (ImageType)i.Tag == ImageType.BOMB))
                {
                    ShowAllBombs();
                    PlaceImage(row, col, 200, Resources["Explosion"] as BitmapImage, ImageType.EXPLOSION);
                    if (MessageBox.Show("You died", "Endgame", MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        RestartGame();
                        return;
                    }
                }
                var closest = GetNeighbours(row, col);
                int k = closest.Count(c => c is Image && (ImageType)(c as Image).Tag == ImageType.BOMB);
                if (k == 0)
                {
                    FillEmpty(row, col);

                    foreach(var empty in emptyGroup)
                    {
                        closest = GetNeighbours(empty.Row, empty.Col);
                        foreach(var close in closest)
                        {
                            var t = GetNeighbours(Grid.GetRow(close), Grid.GetColumn(close));
                            k = t.Count(c => c is Image && (ImageType)(c as Image).Tag == ImageType.BOMB);
                            if (k > 0)
                            {
                                PlaceImage(Grid.GetRow(close), Grid.GetColumn(close), 30, Resources[k.ToString()] as BitmapImage, ImageType.NUMBER);
                            }
                        }
                    }
                    emptyGroup.Clear();
                    return;
                }
                else
                {
                    PlaceImage(row, col, 30, Resources[k.ToString()] as BitmapImage, ImageType.NUMBER);
                }
            }
        }

        private void CheckIfVictory()
        {
            int points = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var flagsList = ImagesInCell(row, col).Where(i => (ImageType)i.Tag == ImageType.FLAG).ToList();
                    var bombsList = ImagesInCell(row, col).Where(i => (ImageType)i.Tag == ImageType.BOMB).ToList();
                    if (flagsList.Count > 0 && bombsList.Count > 0)
                    {
                        points++;
                        if (points == bombs)
                        {
                            MessageBox.Show("Victory!");
                        }
                    }
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                int z = cheat ? 1 : 100;
                cheat = !cheat;
                foreach (var bomb in Map.Children.Cast<Image>().Where(t => (ImageType)t.Tag == ImageType.BOMB))
                {
                    Panel.SetZIndex(bomb, z);
                }
            }
        }

        private void FillEmpty(int row, int col)
        {
            if (NotInRange(row, col)) return;

            TryPlaceEmpty(row, col);
            TryAddToEmptyGroup(new Cell(row, col));
            List<Cell> candidates = new List<Cell>();
            candidates = GetEmptyCells(row, col, candidates);
            foreach(var cell in candidates)
            {
                TryPlaceEmpty(cell.Row, cell.Col);
                FillEmpty(cell.Row, cell.Col);
            }
        }

        private void TryAddToEmptyGroup(Cell cell)
        {
            if (NotInRange(cell.Row, cell.Col)) return;
            if (emptyGroup.Any(e => e.Col == cell.Col && e.Row == cell.Row)) return;
            emptyGroup.Add(cell);
        }

        private List<Cell> GetEmptyCells(int row, int col, List<Cell> candidates)
        {
            List<Cell> nearEmpty = GetNearEmptyCells(row, col);
            foreach(var near in nearEmpty)
            {
                var closest = GetNeighbours(near.Row, near.Col);
                int k = closest.Count(c => c is Image && (ImageType)(c as Image).Tag == ImageType.BOMB);
                if (k == 0)
                {
                    near.Checked = true;
                    candidates.Add(near);
                }
            }
            return candidates;
        }

        private void TryPlaceEmpty(int row, int col)
        {
            if (ImagesInCell(row, col).Any(i => (ImageType)i.Tag == ImageType.EMPTY)) return;

            var empty = new Image
            {
                Stretch = Stretch.Fill,
                Source = Resources["Empty"] as BitmapImage,
                Tag = ImageType.EMPTY
            };
            Map.Children.Add(empty);
            Panel.SetZIndex(empty, 30);
            Grid.SetRow(empty, row);
            Grid.SetColumn(empty, col);
        }

        private void ShowAllBombs()
        {
            foreach (var child in Map.Children.Cast<Image>().Where(t => (ImageType)t.Tag == ImageType.BOMB))
            {
                Panel.SetZIndex(child, bombs);
            }
        }

        private List<UIElement> GetNeighbours(int row, int col)
        {
            List<UIElement> neighbours = new List<UIElement>();
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row - 1 && Grid.GetColumn(c) == col - 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row - 1 && Grid.GetColumn(c) == col).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row - 1 && Grid.GetColumn(c) == col + 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row && Grid.GetColumn(c) == col - 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row && Grid.GetColumn(c) == col + 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row + 1 && Grid.GetColumn(c) == col - 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row + 1 && Grid.GetColumn(c) == col).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == row + 1 && Grid.GetColumn(c) == col + 1).ToList());
            return neighbours;
        }

        private List<Cell> GetNearEmptyCells(int row, int col)
        {
            List<Cell> cells = new List<Cell>();
            cells.Add(new Cell(row - 1, col - 1));
            cells.Add(new Cell(row - 1, col));
            cells.Add(new Cell(row - 1, col + 1));
            cells.Add(new Cell(row, col - 1));
            cells.Add(new Cell(row, col + 1));
            cells.Add(new Cell(row + 1, col - 1));
            cells.Add(new Cell(row + 1, col));
            cells.Add(new Cell(row + 1, col + 1));
            cells = cells.Where(c => !NotInRange(c.Row, c.Col)).ToList();
            List<Cell> result = new List<Cell>();
            foreach(var cell in cells)
            {
                var children = Map.Children.Cast<Image>().Where(c => Grid.GetColumn(c) == cell.Col && Grid.GetRow(c) == cell.Row);
                if (children.Any(c => (ImageType)c.Tag == ImageType.BOMB || (ImageType)c.Tag == ImageType.EMPTY)) { }
                else
                {
                    result.Add(cell);
                }
            }
            return result;
        }
        
        private bool NotInRange(int row, int col)
        {
            return row < 0 || row > rows - 1 || col < 0 || col > cols - 1;
        }

        private bool EmptyCell(int row, int col)
        {
            var children = ImagesInCell(row, col);
            children = children.Where(c => (ImageType)(c as Image).Tag == ImageType.BOMB).ToList();
            if (children.Count == 0) return true;
            return false;
        }

        private void Rows_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Start_Click(null, null);
        }

        private List<Image> ImagesInCell(int row, int col)
        {
            return Map.Children.Cast<UIElement>().Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col && i is Image).Cast<Image>().ToList();
        }

        private void GetClickCoordinates(out int row, out int col)
        {
            col = row = 0;
            var point = Mouse.GetPosition(Map);
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;
            foreach (var rowDefinition in Map.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }
            foreach (var columnDefinition in Map.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }
        }
    }
}