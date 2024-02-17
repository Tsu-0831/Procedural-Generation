using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;


class Leaf
{
    // default
    // private const int MIN_LEAF_SIZE = 6;

    // 変更用
    private const int MIN_LEAF_SIZE = 8;

    // 乱数生成用
    //private Random rand = new System.Random();
    //private int seed = Environment.TickCount;

    public int x, y, width, height; // 葉の位置とサイズ
    public Leaf leftChild;
    public Leaf rightChild;
    public Rectangle room;
    public List<Rectangle> halls;

    public Leaf(int X, int Y, int Width, int Height)
    {
        // 葉の初期化
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    // 線を引いて分割しているだけ
    // 部屋の作成はまだ
    public bool Split(Random rand)
    {
        // 葉を二つの子に分け始める
        if (leftChild != null || rightChild != null)
            return false; // すでに分裂していたため、中止する。

        // 分割の方向を決める
        // weightが25%、heightより大きかったら、垂直に分割される。
        // heightが25%、weightより大きかったら、水平に分割される。
        // それ以外の場合はランダムに分割する。
        // 0.0 ～ 1.0
        //bool splitH = new Random(seed++).NextDouble() > 0.5;
        bool splitH = rand.NextDouble() > 0.5;

        if (width > height && (width / height) >= 1.25)
            splitH = false;
        else if (height > width && (height / width) >= 1.25)
            splitH = true;

        int max = splitH ? height : width;
        max -= MIN_LEAF_SIZE;

        if (max <= MIN_LEAF_SIZE)
            return false;

        //int split = new Random(seed++).Next(MIN_LEAF_SIZE, max);
        int split = rand.Next(MIN_LEAF_SIZE, max);

        if (splitH)
        {
            leftChild = new Leaf(x, y, width, split);
            rightChild = new Leaf(x, y + split, width, height - split);
        }
        else
        {
            leftChild = new Leaf(x, y, split, height);
            rightChild = new Leaf(x + split, y, width - split, height);
        }

        return true;
    }

    public void CreateRooms(BSP_Tree_DungeonGeneration classInstance, List<int[]> tiles, int MX, int MY, int tilesize, Random rand)
    {
        int [] bitmapData;
        
        // This function generates all the rooms and hallways for this Leaf and all of its children.
        if (leftChild != null || rightChild != null)
        {
            // This leaf has been split, so go into the children leafs.
            if (leftChild != null)
            {
                leftChild.CreateRooms(classInstance, tiles, MX, MY, tilesize, rand);
            }
            if (rightChild != null)
            {
                rightChild.CreateRooms(classInstance, tiles, MX, MY, tilesize, rand);
            }
            // このリーフに左と右の両方の子がいる場合は、それらの間に廊下を作成します
            if (leftChild != null && rightChild != null)
            {
                //CreateHall(leftChild.GetRoom(), rightChild.GetRoom());
            }
        }
        else
        {
            // このリーフは部屋を作る準備ができている。
            Point roomSize;
            Point roomPos;

            // 部屋は 3x3 タイルから葉のサイズ - 2 までの間になる。
            //roomSize = new Point(rand.Next(MIN_LEAF_SIZE / 2, width - 2), rand.Next(MIN_LEAF_SIZE / 2, height - 2));
            roomSize = new Point( 5, 5);


            // 部屋をリーフ内に配置するが、リーフの側面にぴったりと配置しないこと　(部屋が結合されてしまう)。
            // 部屋のポジション決め(ランダム)
            // 変更用
            roomPos = new Point(rand.Next(1, width - roomSize.X), rand.Next(1, height - roomSize.Y));


            room = new Rectangle(x + roomPos.X, y + roomPos.Y, roomSize.X, roomSize.Y);


            for (int i = x + roomPos.X; i < x + roomPos.X + roomSize.X; i++)
            {
                for (int j = y + roomPos.Y; j < y + roomPos.Y + roomSize.Y; j++)
                {
                    classInstance.bitmapCheck[i + j * MY] = true;
                }
            }

            // 指定した図形の形にタイルを置く
            int[] tile;
            //for (int i = x + roomPos.X; i < x + roomPos.X + roomSize.X; i++) for (int j = y + roomPos.Y; j < y + roomPos.Y + roomSize.Y; j++)
            if (classInstance.VillageMoodName == "ForestVillage") tile = tiles[0];
            else if (classInstance.VillageMoodName == "CastleTown") tile = tiles[1];
            else tile = tiles[0];

            int pixelData;
            int tiles_tilesize = (int)System.Math.Sqrt(tile.Length);
            for (int i = (x + roomPos.X) * tilesize; i < (x + roomPos.X + roomSize.X) * tilesize; i++) for (int j = (y + roomPos.Y) * tilesize; j < (y + roomPos.Y + roomSize.Y) * tilesize; j++)
            {
                pixelData = tile[
                            (int)(((float)(tiles_tilesize - 1)
                            * ((float)(i - (x + roomPos.X) * tilesize)
                            / (((x + roomPos.X + roomSize.X) * tilesize) - (x + roomPos.X) * tilesize)))
                            + (int)((float)(tiles_tilesize - 1)
                            * (float)((float)(j - (y + roomPos.Y) * tilesize)
                            / (((y + roomPos.Y + roomSize.Y) * tilesize) - (y + roomPos.Y) * tilesize)))
                            * tiles_tilesize)
                            ];

                if (pixelData != 16777215)
                {
                    classInstance.bitmapData[i + j * MY * tilesize] = pixelData;
                }
            }
        }
        //for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
        //    {
        //        classInstance.bitmapData[i * tilesize + dx + (j * tilesize + dy) * MX * tilesize] = tile[dx + dy * tilesize];
        //    }
        // classInstance.bitmapData[i * tilesize + dx + (j * tilesize + dy) * MY * tilesize] = tile[(int)((dx * i / (x + roomPos.X + roomSize.X)) + (dy * tilesize * j / (y + roomPos.Y + roomSize.Y)))];

    }

    public Rectangle GetRoom(Random rand)
    {
        // Iterate all the way through these leafs to find a room, if one exists.
        if (room != null)
            return room;
        else
        {
            Rectangle lRoom = Rectangle.Empty;
            Rectangle rRoom = Rectangle.Empty;

            if (leftChild != null)
            {
                lRoom = leftChild.GetRoom(rand);
            }
            if (rightChild != null)
            {
                rRoom = rightChild.GetRoom(rand);
            }

            if (lRoom == Rectangle.Empty && rRoom == Rectangle.Empty)
                return Rectangle.Empty;
            else if (rRoom == null)
                return lRoom;
            else if (lRoom == null)
                return rRoom;
            //else if (new Random(seed++).Next() > 0.5)
            else if (rand.Next() > 0.5)
                        return lRoom;
            else
                return rRoom;
        }
    }

    public void CreateHall(Rectangle l, Rectangle r)
    {
        // 二つの部屋を廊下で接続する。
        // これはかなり複雑に見えますが、どの点がどこにあるかを把握し、直線を引くか、直角をなす 2 本の線を引いて接続するだけです。
        // 必要に応じて、追加のロジックを追加してホールをより曲がりくねらせたり、より高度な処理を行うこともできます。

        List<Rectangle> halls = new List<Rectangle>();

        Point point1 = new Point(new Random().Next(l.Left + 1, l.Right - 2), new Random().Next(l.Top + 1, l.Bottom - 2));
        Point point2 = new Point(new Random().Next(r.Left + 1, r.Right - 2), new Random().Next(r.Top + 1, r.Bottom - 2));

        int w = point2.X - point1.X;
        int h = point2.Y - point1.Y;

        if (w < 0)
        {
            if (h < 0)
            {
                if (new Random().Next() < 0.5)
                {
                    halls.Add(new Rectangle(point2.X, point1.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point2.X, point2.Y, 1, Math.Abs(h)));
                }
                else
                {
                    halls.Add(new Rectangle(point2.X, point2.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point1.X, point2.Y, 1, Math.Abs(h)));
                }
            }
            else if (h > 0)
            {
                if (new Random().Next() < 0.5)
                {
                    halls.Add(new Rectangle(point2.X, point1.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point2.X, point1.Y, 1, Math.Abs(h)));
                }
                else
                {
                    halls.Add(new Rectangle(point2.X, point2.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point1.X, point1.Y, 1, Math.Abs(h)));
                }
            }
            else // if (h == 0)
            {
                halls.Add(new Rectangle(point2.X, point2.Y, Math.Abs(w), 1));
            }
        }
        else if (w > 0)
        {
            if (h < 0)
            {
                if (new Random().Next() < 0.5)
                {
                    halls.Add(new Rectangle(point1.X, point2.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point1.X, point2.Y, 1, Math.Abs(h)));
                }
                else
                {
                    halls.Add(new Rectangle(point1.X, point1.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point2.X, point2.Y, 1, Math.Abs(h)));
                }
            }
            else if (h > 0)
            {
                if (new Random().Next() < 0.5)
                {
                    halls.Add(new Rectangle(point1.X, point1.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point2.X, point1.Y, 1, Math.Abs(h)));
                }
                else
                {
                    halls.Add(new Rectangle(point1.X, point2.Y, Math.Abs(w), 1));
                    halls.Add(new Rectangle(point1.X, point1.Y, 1, Math.Abs(h)));
                }
            }
            else // if (h == 0)
            {
                halls.Add(new Rectangle(point1.X, point1.Y, Math.Abs(w), 1));
            }
        }
        else // if (w == 0)
        {
            if (h < 0)
            {
                halls.Add(new Rectangle(point2.X, point2.Y, 1, Math.Abs(h)));
            }
            else if (h > 0)
            {
                halls.Add(new Rectangle(point1.X, point1.Y, 1, Math.Abs(h)));
            }
        }
    }

    private int[] getbitmapData(BSP_Tree_DungeonGeneration classInstance)
    {
        return classInstance.bitmapData;
    }
}