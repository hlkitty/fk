using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fk
{
    public partial class Form1 : Form
    {
        private bool IsPause;
        private Graphics MainG;
        private Graphics SubG;
        private BufferedGraphics MainBG;
        private BufferedGraphics SubBG;
        private DateTime TickTime;
        private double Speed;
        private ColorDialog c = new ColorDialog();

        public Form1()
        {
            InitializeComponent();
            MainG = panel1.CreateGraphics();
            SubG = panel2.CreateGraphics();
            MainBG = BufferedGraphicsManager.Current.Allocate(MainG, new Rectangle(0, 0, panel1.Width, panel1.Height));
            SubBG = BufferedGraphicsManager.Current.Allocate(SubG, new Rectangle(0, 0, panel2.Width, panel2.Height));
            FkGame.MainG = MainBG.Graphics;
            FkGame.SubG = SubBG.Graphics;
            FkGame.OnScoreChanged += FkGame_OnScoreChanged;
            panel1.Paint += panel1_Paint;
            panel2.Paint += panel2_Paint;
            Task.Factory.StartNew(Ticks, TaskCreationOptions.LongRunning);
            Speed = trackBar1.Value;
            IsPause = checkBox1.Checked;
            panel3.BackColor = Color.White;
            panel4.BackColor = Color.White;
            panel5.BackColor = Color.Gray;
            InitPanel();
        }

        private void InitPanel()
        {
            FkGame.InitPanel(panel3.BackColor, panel4.BackColor, panel5.BackColor, checkBox2.Checked, checkBox3.Checked);
            RefreshPanel();
        }

        private void Ticks()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(1);
                DoTick();
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            SubBG.Render();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            MainBG.Render();
        }

        private void DoTick()
        {
            if (TickTime == DateTime.MinValue)
            {
                TickTime = DateTime.Now;
                return;
            }
            if (FkGame.IsGameStart && !this.IsPause)
            {
                var now = DateTime.Now;
                if ((now - TickTime).Ticks > 100000 * (100 - FkGame.Level - Speed))
                {
                    TickTime = now;
                    BeginInvoke((Action)DoSlowDown);
                }
            }
        }

        private void DoSlowDown()
        {
            FkGame.SlowDown();
            RefreshPanel();
        }

        private void FkGame_OnScoreChanged(object sender, EventArgs e)
        {
            ShowScore();
        }

        private void ShowScore()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)ShowScore);
                return;
            }
            label1.Text = string.Format("积分：{0}\r\n等级：{1}", FkGame.Score, FkGame.Level);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.W:
                    FkGame.Switch();
                    break;
                case Keys.Down:
                case Keys.S:
                    FkGame.QuickDown();
                    TickTime = DateTime.MinValue;
                    break;
                case Keys.Left:
                case Keys.A:
                    FkGame.Left();
                    break;
                case Keys.Right:
                case Keys.D:
                    FkGame.Right();
                    break;
                case Keys.Tab:
                    FkGame.ChangeNext();
                    break;
            }
            RefreshPanel();
            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FkGame.RestartGame();
            TickTime = DateTime.MinValue;
            Speed = trackBar1.Value;
            IsPause = checkBox1.Checked;
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            MainBG.Render();
            SubBG.Render();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            Speed = trackBar1.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            IsPause = checkBox1.Checked;
        }

        private void panel3_Click(object sender, EventArgs e)
        {
            c.Color = panel3.BackColor;
            c.ShowDialog(this);
            panel3.BackColor = c.Color;
            InitPanel();
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            c.Color = panel4.BackColor;
            c.ShowDialog(this);
            panel4.BackColor = c.Color;
            InitPanel();
        }

        private void panel5_Click(object sender, EventArgs e)
        {
            c.Color = panel5.BackColor;
            c.ShowDialog(this);
            panel5.BackColor = c.Color;
            InitPanel();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            InitPanel();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            InitPanel();
        }
    }

    public static class FkGame
    {
        private const int VCount = 20;
        private const int HCount = 10;
        private const int SCount = 4;
        private const int WidthInt = 0x3FF;
        private const int MainWidth = 15;
        private const int MainSpace = 2;
        private const int SubWidth = 10;
        private const int SubSpace = 1;
        public static Graphics MainG { get; set; }
        public static Graphics SubG { get; set; }
        private static int[] MainItems = new int[VCount];
        private static Point[] CurItem;
        private static Point[] NextItem;
        private static List<Point[]> Items = new List<Point[]>();
        private static Point[][] CommonItems = new Point[][]
        {
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0), },
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(0, 1), },
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(1, 1), },
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(2, 1), },
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(1, 0), new Point(1, 1), },
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1), },
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(0, 1), new Point(1, 1), },
        };
        private static Point[][] SuperItems = new Point[][]
        {
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(0, 2) },
                new Point[] { new Point(0, 0), new Point(0, 1) },
                new Point[] { new Point(0, 0) },
                new Point[] { new Point(0, 0), new Point(3, 0), new Point(1, 1), new Point(1, 2), new Point(2, 1), new Point(2, 2), },
        };
        private static Point[][] ExternItems = new Point[][]
        {
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2) },
                new Point[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new Point[] { new Point(2, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2) },
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(0, 1), new Point(2, 1) }            
        };
        private static SolidBrush ClearBrush;
        private static SolidBrush SpaceBrush;
        private static SolidBrush DrawBrush;
        private static TextureBrush GOBrush;
        private static Random R = new Random();
        private static int[] Scores = new[] { 0, 100, 300, 600, 1000 };
        public static int Score = 0;
        public static bool IsGameOver = false;
        public static event EventHandler OnScoreChanged;

        public static int Level
        {
            get
            {
                return Score / 10000;
            }
        }
        public static bool IsGameStart
        {
            get
            {
                return CurItem != null;
            }
        }

        public static void RestartGame()
        {
            Array.Clear(MainItems, 0, MainItems.Length);
            CurItem = null;
            NextItem = CreateItem();
            Score = 0;
            IsGameOver = false;
            TellScore();
            ReDrawMain();
            CalcNext();
        }

        public static void InitPanel(Color s, Color b, Color d, bool hasExtern, bool hasSuper)
        {
            SpaceBrush = new SolidBrush(s);
            ClearBrush = new SolidBrush(b);
            DrawBrush = new SolidBrush(d);
            Bitmap bm = new Bitmap(2, 2);
            bm.SetPixel(0, 0, Color.Black);
            bm.SetPixel(0, 1, s);
            bm.SetPixel(1, 0, b);
            bm.SetPixel(1, 1, d);
            GOBrush = new TextureBrush(bm);
            lock (Items)
            {
                Items.Clear();
                Items.AddRange(CommonItems);
                if (hasExtern)
                {
                    Items.AddRange(ExternItems);
                }
                if (hasSuper)
                {
                    Items.AddRange(SuperItems);
                }
            }
            ReDrawMain();
            ReDrawSub();
        }

        public static bool Left()
        {
            if (CurItem == null)
            {
                return false;
            }
            return MoveItem(-1, 0);
        }

        public static bool Right()
        {
            if (CurItem == null)
            {
                return false;
            }
            return MoveItem(1, 0);
        }

        public static bool SlowDown()
        {
            if (CurItem == null)
            {
                return false;
            }
            bool result = MoveItem(0, 1);
            if (!result)
            {
                return MergeItem(result);
            }
            return result;
        }

        private static bool MergeItem(bool result)
        {
            if (CurItem.Length != 4 && CurItem.Length != 5 && CurItem.Length != 1)
            {
                if (CurItem.Length == 2 || CurItem.Length == 3)
                {
                    for (var i = 0; i < CurItem.Length; i++)
                    {
                        var p = CurItem[i];
                        ClearMain(p.X, p.Y);
                    }
                }
                if (CurItem.Length == 6)
                {
                    DoBomb();
                }
            }
            else
            {
                for (var i = 0; i < CurItem.Length; i++)
                {
                    if (CurItem[i].Y < 0)
                    {
                        foreach (var p in CurItem)
                        {
                            if (p.Y >= 0)
                                MainItems[p.Y] |= 1 << p.X;
                        }
                        DoGameOver();
                        return false;
                    }
                }
                foreach (var p in CurItem)
                {
                    MainItems[p.Y] |= 1 << p.X;
                }
                var line = CalcMainItems();
                if (line > 0)
                {
                    Score += Scores[line];
                    TellScore();
                }
            }
            CalcNext();
            return result;
        }

        private static void DoBomb()
        {
            var item = CurItem;
            if (item.Length == 6)
            {
                var bound = ItemBound(item);
                for (int i = bound.X - 1; i <= bound.X + bound.Width + 1; i++)
                {
                    if (i < 0 || i >= HCount)
                    {
                        continue;
                    }
                    for (int j = bound.Y; j <= bound.Y + bound.Height + 2; j++)
                    {
                        if (j < 0 || j >= VCount)
                        {
                            continue;
                        }
                        ClearMain(i, j);
                        MainItems[j] &= WidthInt ^ (1 << i);
                    }
                }
            }
        }
        public static bool QuickDown()
        {
            if (CurItem == null)
            {
                return false;
            }
            bool result = MoveItem(0, 1);
            if (result)
            {
                //while (MoveItem(0, 1));
            }
            else
            {
                return MergeItem(result);
            }
            return result;
        }

        public static void ChangeNext()
        {
            NextItem = CreateItem();
            ReDrawSub();
        }

        private static void CalcNext()
        {
            CurItem = NextItem;
            NextItem = CreateItem();
            ReDrawSub();
            var b = ItemBound(CurItem);
            var offset = b.Height;
            bool DownOk = true;
            for (var i = 0; i < CurItem.Length; i++)
            {
                CurItem[i].Offset(3, -offset);
                var c = CurItem[i];
                if (c.Y >= 0)
                {
                    if ((MainItems[c.Y] & (1 << c.X)) != 0)
                    {
                        DownOk = false;
                    }
                    FillMain(CurItem[i].X, CurItem[i].Y);
                }
            }
            if (!DownOk)
            {
                DoGameOver();
            }
        }

        private static void TellScore()
        {
            if (OnScoreChanged != null)
            {
                OnScoreChanged(typeof(FkGame), EventArgs.Empty);
            }
        }
        public static bool Switch()
        {
            if (CurItem == null)
            {
                return false;
            }
            var item = CurItem;
            var b = ItemBound(item);
            if (item.Length != 4 && item.Length != 5)
            {
                if (item.Length == 1)
                {
                }
                if (item.Length == 2)
                {
                    for (var i = b.Y + 2; i < VCount; i++)
                    {
                        if ((MainItems[i] & (1 << b.X)) != 0)
                        {
                            MainItems[i] &= WidthInt ^ (1 << b.X);
                            ClearMain(b.X, i);
                            break;
                        }
                    }
                }
                if (item.Length == 3)
                {
                    var i = b.Y + 3;
                    for (; i < VCount; i++)
                    {
                        if ((MainItems[i] & (1 << b.X)) != 0)
                        {
                            break;
                        }
                    }
                    if (i > b.Y + 3)
                    {
                        MainItems[i - 1] |= (1 << b.X);
                        FillMain(b.X, i - 1);
                        var line = CalcMainItems();
                        if (line > 0)
                        {
                            Score += Scores[line];
                            TellScore();
                        }
                    }
                }
                if (item.Length == 6)
                {
                    DoBomb();
                    CalcNext();
                }
                return true;
            }
            if (b.X < 0 || b.X + b.Width >= HCount || b.Y + b.Height >= VCount
                || b.X + b.Height >= HCount || b.Y + b.Width >= VCount)
            {
                return false;
            }
            var newItem = SwitchItem(item);
            foreach (var p in newItem)
            {
                if (p.Y >= MainItems.Length || p.Y > 0 && (MainItems[p.Y] & (1 << (p.X))) != 0)
                {
                    return false;
                }
            }
            for (var i = 0; i < item.Length; i++)
            {
                var p = item[i];
                ClearMain(p.X, p.Y);
            }
            for (var i = 0; i < newItem.Length; i++)
            {
                var p = newItem[i];
                FillMain(p.X, p.Y);
            }
            CurItem = newItem;
            return true;
        }

        private static Point[] CreateItem()
        {
            var r = R.Next(Items.Count);
            var pick = Items[r];
            var item = new Point[pick.Length];
            Array.Copy(pick, item, pick.Length);
            r = R.Next(4);
            while (r > 0)
            {
                r--;
                item = SwitchItem(item);
            }
            return item;
        }
        private static Point[] SwitchItem(Point[] item)
        {
            if (item.Length != 4 && item.Length != 5)
            {
                return item;
            }
            var b = ItemBound(item);
            var t = b.Width;
            b.Width = b.Height;
            b.Height = t;
            var newItem = new Point[item.Length];
            var xy = b.Location;
            for (int i = 0; i < item.Length; i++)
            {
                var p = item[i];
                newItem[i] = new Point(xy.X - (p.Y - xy.Y) + b.Width, xy.Y + (p.X - xy.X));
            }
            return newItem;
        }
        private static Rectangle ItemBound(Point[] item)
        {
            var x = item[0].X;
            var y = item[0].Y;
            var x2 = x;
            var y2 = y;
            foreach (var p in item)
            {
                if (x > p.X)
                {
                    x = p.X;
                }
                if (x2 < p.X)
                {
                    x2 = p.X;
                }
                if (y > p.Y)
                {
                    y = p.Y;
                }
                if (y2 < p.Y)
                {
                    y2 = p.Y;
                }
            }
            return new Rectangle(x, y, x2 - x, y2 - y);
        }

        private static bool MoveItem(int x, int y)
        {
            var b = ItemBound(CurItem);
            b.Offset(x, y);
            if (b.X < 0 || b.X + b.Width >= HCount || b.Y + b.Height >= VCount)
            {
                return false;
            }
            if ((x == 1 && y == 0) || (x == -1 && y == 0) || (x == 0 && y == 1))
            {
                if (y == 1 && CurItem.Length == 1)
                {
                    var p = CurItem[0];
                    int i = p.Y + 1;
                    for (; i < VCount; i++)
                    {
                        if ((MainItems[i] & (1 << p.X)) == 0)
                        {
                            break;
                        }
                    }
                    if (i == VCount)
                    {
                        return false;
                    }
                    if (p.Y >= 0 && (MainItems[p.Y] & (1 << p.X)) == 0)
                    {
                        ClearMain(p.X, p.Y);
                    }
                }
                else
                {
                    foreach (var p in CurItem)
                    {
                        if (p.Y + y > 0 && (MainItems[p.Y + y] & (1 << (p.X + x))) != 0)
                        {
                            return false;
                        }
                    }
                    for (var i = 0; i < CurItem.Length; i++)
                    {
                        var p = CurItem[i];
                        if (p.Y >= 0 && (MainItems[p.Y] & (1 << p.X)) == 0)
                        {
                            ClearMain(p.X, p.Y);
                        }
                    }
                }
                for (var i = 0; i < CurItem.Length; i++)
                {
                    var p = CurItem[i];
                    p.Offset(x, y);
                    CurItem[i] = p;
                    FillMain(p.X, p.Y);
                }
                return true;
            }
            return false;
        }

        private static int CalcMainItems()
        {
            var line = 0;
            for (var j = MainItems.Length - 1; j >= 0; j--)
            {
                if (MainItems[j] == WidthInt)
                {
                    line++;
                    for (int i = 0; i < HCount; i++)
                    {
                        ClearMain(i, j);
                    }
                    MainItems[j] = 0;
                }
                else if (line > 0)
                {
                    MainItems[j + line] = MainItems[j];
                    for (int i = 0; i < HCount; i++)
                    {
                        if ((MainItems[j + line] & (1 << i)) == 0)
                        {
                            ClearMain(i, j + line);
                        }
                        else
                        {
                            FillMain(i, j + line);
                        }
                    }
                }
            }
            if (line > 0)
            {
                var l = line-1;
                while (l >= 0)
                {
                    MainItems[l] = 0;
                    for (int i = 0; i < HCount; i++)
                    {
                        ClearMain(i, l);
                    }
                    l--;
                }
            }
            return line;
        }

        private static void ClearMain(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return;
            }
            MainG.FillRectangle(ClearBrush, MainSpace + x * (MainWidth + MainSpace), MainSpace + y * (MainWidth + MainSpace), MainWidth, MainWidth);
        }
        private static void FillMain(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return;
            }
            MainG.FillRectangle(DrawBrush, MainSpace + x * (MainWidth + MainSpace), MainSpace + y * (MainWidth + MainSpace), MainWidth, MainWidth);
        }

        private static void DoGameOver()
        {
            CurItem = null;
            IsGameOver = true;
            Font font = new Font(SystemFonts.DefaultFont.FontFamily, 20, FontStyle.Bold);
            MainG.DrawString("Game Over", font, GOBrush, MainWidth, MainWidth * (VCount / 2));
        }
        public static void ReDrawMain()
        {
            MainG.Clear(ClearBrush.Color);
            var rects = new List<Rectangle>();
            for (var i = 0; i <= HCount; i++)
            {
                rects.Add(new Rectangle(i * (MainWidth + MainSpace), 0, MainSpace, VCount * (MainWidth + MainSpace) + MainSpace));
            }
            for (var i = 0; i <= VCount; i++)
            {
                rects.Add(new Rectangle(0, i * (MainWidth + MainSpace), HCount * (MainWidth + MainSpace) + MainSpace, MainSpace));
            }
            MainG.FillRectangles(SpaceBrush, rects.ToArray());
            for (var i = 0; i < MainItems.Length; i++)
            {
                for (var j = 0; j < HCount; j++)
                {
                    if ((MainItems[i] & (1 << j)) != 0)
                    {
                        MainG.FillRectangle(DrawBrush, MainSpace + j * (MainWidth + MainSpace), MainSpace + i * (MainWidth + MainSpace), MainWidth, MainWidth);
                    }
                }
            }
            if (CurItem != null)
            {
                foreach (var p in CurItem)
                {
                    MainG.FillRectangle(DrawBrush, MainSpace + p.X * (MainWidth + MainSpace), MainSpace + p.Y * (MainWidth + MainSpace), MainWidth, MainWidth);
                }
            }
            if (IsGameOver)
            {
                DoGameOver();
            }
        }
        public static void ReDrawSub()
        {
            SubG.Clear(ClearBrush.Color);
            var rects = new List<Rectangle>();
            for (var i = 0; i <= SCount; i++)
            {
                rects.Add(new Rectangle(i * (SubWidth + SubSpace), 0, SubSpace, VCount * (SubWidth + SubSpace) + SubSpace));
            }
            for (var i = 0; i <= SCount; i++)
            {
                rects.Add(new Rectangle(0, i * (SubWidth + SubSpace), HCount * (SubWidth + SubSpace) + SubSpace, SubSpace));
            }
            SubG.FillRectangles(SpaceBrush, rects.ToArray());
            if (NextItem != null)
            {
                foreach (var p in NextItem)
                {
                    SubG.FillRectangle(DrawBrush, SubSpace + p.X * (SubWidth + SubSpace), SubSpace + p.Y * (SubWidth + SubSpace), SubWidth, SubWidth);
                }
            }
        }
    }
}
