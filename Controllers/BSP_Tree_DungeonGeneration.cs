using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApp.Models;
using System.Xml.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


class BSP_Tree_DungeonGeneration : Model
{
    private const int MAX_LEAF_SIZE = 10;
    private List<Leaf> leafs;

    private Random rand = new System.Random();

    // 画像にする用
    private List<int[]> tiles; // 生成に用いる画像データを入れる
    private int[] bitmap; // 画像のビットマップファイルを保存する。
    private int tilesize;
    private string name = "Village";
    List<string> tilenames;
    public int[] bitmapData { get; set; }
    public String VillageMoodName { get; set; }
    public int[] observedSub { get; set; }

    public bool[] bitmapCheck { get; set; }

    public BSP_Tree_DungeonGeneration(string subsetName,int mapWidth, int mapHeight, Heuristic heuristic, string VillageMoodName, string VillageFrameName) : base(mapWidth, mapHeight, 1, true, heuristic)
    {
        this.VillageMoodName = VillageMoodName;
        leafs = new List<Leaf>();
        tiles = new List<int[]>();

        bitmapCheck = new bool[MX * MY];

        Leaf root = new Leaf(0, 0, mapWidth, mapHeight);
        leafs.Add(root);
        bool didSplit = true;

        /*
         * 
         * 
         * SimpleTiledModelを参照
         * タイルデータを取得するコード
         * 
         * 
        */


        XElement xroot = XDocument.Load(HttpContext.Current.Server.MapPath($"../Models/tilesets/{name}.xml")).Root;
        // uniqueがtrueならtrueを違うならfalseを代入
        // 現状、summerのみに適用されている
        bool unique = xroot.Get("unique", false);
        List<string> subset = null; // subset : 部分集合

        // subsetName : CrossLess(交差無し), TurnLess, Dense(密集), Fabric(織物), Standard(通常), No Solid(固体なし), Large(大きい), C, CE, CL, T, TE, TL
        if (subsetName != null)
        {
            XElement xsubset = xroot.Element("subsets")
                .Elements("subset")
                .FirstOrDefault(x => x.Get<string>("name") == subsetName);
            if (xsubset == null) Console.WriteLine($"ERROR: subset {subsetName} is not found");
            else subset = xsubset.Elements("tile").Select(x => x.Get<string>("name")).ToList();
        }

        int[] tile(Func<int, int, int> f, int size)
        {
            // 1次元配列resultを定義。サイズは、size×size
            int[] result = new int[size * size];

            // resultに
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) result[x + y * size] = f(x, y);
            return result;
        };
        int[] rotate(int[] array, int size) => tile((x, y) => array[size - 1 - y + x * size], size); // 回転処理になるようにtile関数に値を渡している。
        int[] reflect(int[] array, int size) => tile((x, y) => array[size - 1 - x + y * size], size); // 反射処理

        tiles = new List<int[]>(); // タイル用のint型のリスト変数
        tilenames = new List<string>(); // タイルの名前用のstring型のリスト変数
        var weightList = new List<double>(); // 重さの用のdouble型のリスト変数

        var action = new List<int[]>(); // ? actionに何を入れるのか不明
        var firstOccurrence = new Dictionary<string, int>(); // string型とint型を格納できる辞書変数. タイルの名前(string)とactionの数(int)を代入する。

        // XMLファイルのtilesタグ中のtile要素について順にxtileに格納していく
        foreach (XElement xtile in xroot.Element("tiles").Elements("tile"))
        {
            string tilename = xtile.Get<string>("name"); // タイル名を保存
            if (subset != null && !subset.Contains(tilename)) continue;

            Func<int, int> a, b;
            int cardinality; // cardinality : 集合の濃度

            char sym = xtile.Get("symmetry", 'X'); // symmetry要素を取得. T, L, X, I, \\, F.
            if (sym == 'L')
            {
                cardinality = 5;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i + 1 : i - 1;
            }
            else if (sym == 'T')
            {
                cardinality = 2;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i : 4 - i;
            }
            else if (sym == 'I')
            {
                cardinality = 9;
                a = i => 1 - i;
                b = i => i;
            }
            else if (sym == '\\')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => 1 - i;
            }
            else if (sym == 'F')
            {
                cardinality = 8;
                a = i => i < 4 ? (i + 1) % 4 : 4 + (i - 1) % 4;
                b = i => i < 4 ? i + 4 : i - 4;
            }
            else
            {
                cardinality = 1;
                a = i => i;
                b = i => i;
            }

            T = action.Count;
            firstOccurrence.Add(tilename, T);
            // 2次配列map. 1次のサイズは、cardinalityの値.
            // cardinalityの値は、symの内容によって決まる.
            int[][] map = new int[cardinality][];

            // cardinalityの値分,繰り返す
            for (int t = 0; t < cardinality; t++)
            {
                // 2次配列mapの2次元目にサイズ8用意
                map[t] = new int[8];

                // a, bはFunc<int, int>
                map[t][0] = t;
                map[t][1] = a(t);
                map[t][2] = a(a(t));
                map[t][3] = a(a(a(t)));
                map[t][4] = b(t);
                map[t][5] = b(a(t));
                map[t][6] = b(a(a(t)));
                map[t][7] = b(a(a(a(t))));

                for (int s = 0; s < 8; s++) map[t][s] += T; // それぞれにTを足す。

                // actionにmapの値を代入する。
                // actionの中身：tの値、tの値をa, bの関数へ渡した時の値の配列(サイズ8)
                action.Add(map[t]);
            }

            // uniqueがtrueの時(現状、summerのみ)
            if (unique)
            {
                // cardinalityの分だけ繰り返す。cardinality : symの内容によって決まる.
                // sym : T, L, X, I, \\, F.
                // cardinality : 用意している画像番号の上限→symmetryの値によって決まる。
                for (int t = 0; t < cardinality; t++)
                {
                    int[] bitmap;
                    // bitmap : 画像サイズ分の配列、tilesize : 幅、tilesize : 高さ
                    (bitmap, tilesize, tilesize) = BitmapHelper.LoadBitmap(HttpContext.Current.Server.MapPath($"../Models/tilesets/{name}/{tilename} {t}.png"));
                    tiles.Add(bitmap);
                    tilenames.Add($"{tilename} {t}");
                }
            }
            else
            {
                int[] bitmap;
                // bitmap : 画像サイズ分の配列、tilesize : 幅、tilesize : 高さ
                (bitmap, tilesize, tilesize) = BitmapHelper.LoadBitmap(HttpContext.Current.Server.MapPath($"../Models/tilesets/{name}/{tilename}.png"));
                tiles.Add(bitmap);
                tilenames.Add($"{tilename} 0"); // tilenamesに名前を追加

                for (int t = 1; t < cardinality; t++)
                {
                    if (t <= 3) tiles.Add(rotate(tiles[T + t - 1], tilesize));
                    if (t >= 4) tiles.Add(reflect(tiles[T + t - 4], tilesize));
                    tilenames.Add($"{tilename} {t}");
                }
            }

            for (int t = 0; t < cardinality; t++) weightList.Add(xtile.Get("weight", 1.0));
        }

        T = action.Count; // actionの数
        // weightListの中身：tile要素の数×cardinalityの値分、存在する。tile要素のweightの値が格納されている。
        weights = weightList.ToArray(); // weightListのコピー

        // 新しく変数宣言
        propagator = new int[4][][];
        var densePropagator = new bool[4][][];

        for (int d = 0; d < 4; d++)
        {
            // T(actionの数)分サイズを用意する
            densePropagator[d] = new bool[T][];
            propagator[d] = new int[T][];
            // T分繰り返す
            for (int t = 0; t < T; t++) densePropagator[d][t] = new bool[T];
        }

        // neighborについて
        foreach (XElement xneighbor in xroot.Element("neighbors").Elements("neighbor"))
        {
            // left, right要素についてそれぞれ取得
            // 右と左にくる画像を示しているもの。
            string[] left = xneighbor.Get<string>("left").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] right = xneighbor.Get<string>("right").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (subset != null && (!subset.Contains(left[0]) || !subset.Contains(right[0]))) continue;

            // int.Parse : 文字列を整数に
            int L = action[firstOccurrence[left[0]]][left.Length == 1 ? 0 : int.Parse(left[1])], D = action[L][1];
            int R = action[firstOccurrence[right[0]]][right.Length == 1 ? 0 : int.Parse(right[1])], U = action[R][1];

            // 左右を配置可能へ(初期化)
            // dense : 密集
            densePropagator[0][R][L] = true;
            densePropagator[0][action[R][6]][action[L][6]] = true;
            densePropagator[0][action[L][4]][action[R][4]] = true;
            densePropagator[0][action[L][2]][action[R][2]] = true;

            // 上下を配置可能へ(初期化)
            // dense : 密集
            densePropagator[1][U][D] = true;
            densePropagator[1][action[D][6]][action[U][6]] = true;
            densePropagator[1][action[U][4]][action[D][4]] = true;
            densePropagator[1][action[D][2]][action[U][2]] = true;
        }

        // action.Count × action.Count分繰り返す
        for (int t2 = 0; t2 < T; t2++) for (int t1 = 0; t1 < T; t1++)
            {
                densePropagator[2][t2][t1] = densePropagator[0][t1][t2];
                densePropagator[3][t2][t1] = densePropagator[1][t1][t2];
            }

        List<int>[][] sparsePropagator = new List<int>[4][];

        // 4回
        // Listを作成
        for (int d = 0; d < 4; d++)
        {
            sparsePropagator[d] = new List<int>[T];
            for (int t = 0; t < T; t++) sparsePropagator[d][t] = new List<int>();
        }


        for (int d = 0; d < 4; d++) for (int t1 = 0; t1 < T; t1++)
            {
                List<int> sp = sparsePropagator[d][t1];
                bool[] tp = densePropagator[d][t1];

                for (int t2 = 0; t2 < T; t2++) if (tp[t2]) sp.Add(t2);

                int ST = sp.Count;
                if (ST == 0) Console.WriteLine($"ERROR: tile {tilenames[t1]} has no neighbors in direction {d}");
                propagator[d][t1] = new int[ST];
                for (int st = 0; st < ST; st++) propagator[d][t1][st] = sp[st];
            }
    

        while (didSplit)
        {
            didSplit = false;
            for (int i = 0; i < leafs.Count; i++)
            {
                Leaf leaf = leafs[i];

                if (leaf.leftChild == null && leaf.rightChild == null)
                {
                    // leafの幅または高さが大きすぎる場合もしくは、75%の確率で実行
                    //if (leaf.width > MAX_LEAF_SIZE || leaf.height > MAX_LEAF_SIZE || new Random().NextDouble() > 0.25)
                    if (leaf.width > MAX_LEAF_SIZE || leaf.height > MAX_LEAF_SIZE || rand.NextDouble() > 0.25)
                    {
                    if (leaf.Split(rand))
                    {
                        leafs.Add(leaf.leftChild);
                        leafs.Add(leaf.rightChild);
                        didSplit = true;
                    }
                    }
                }
            }
        }

        

        int[] tiled;
        // 背景
        if (VillageMoodName == "ForestVillage") tiled = tiles[5];
        else if (VillageMoodName == "CastleTown") tiled = tiles[6];
        else tiled = tiles[5];

        tilesize = (int)System.Math.Sqrt(tiled.Length);

        // 画像になるint型のデータ
        bitmapData = new int[MX * MY * tilesize * tilesize];


        for (int x = 0; x < MX; x++) for (int y = 0; y < MY; y++)
        {
            for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = tiled[dx + dy * tilesize];
        }

        // 囲い
        if (VillageFrameName == "GrassFrame")
        {
            tiled = tiles[7];
            int pixelData;
            int tiled_tilesize = (int)System.Math.Sqrt(tiled.Length);


            //tilesize = (int)System.Math.Sqrt(tiled.Length);
            for (int x = 0; x < MX; x++) for (int y = 0; y < MY; y++)
                {
                    if (x == 0 || y == 0 || x == MX - 1 || y == MY - 1)
                    {
                        for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                            {
                                pixelData = tiled[dx + dy * tilesize];
                                if (pixelData != 16777215)
                                    bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = pixelData;
                            }

                        bitmapCheck[x + y * MY] = true;
                    }
                }
        }
        else if (VillageFrameName == "RockFrame")
        {
            int[] leftTiled = tiles[8];
            int[] upRightTiled = tiles[9];
            int[] upLeftTiled = tiles[10];
            int[] downRightTiled = tiles[11];
            int[] downLeftTiled = tiles[12];
            int[] rightTiled = tiles[13];
            int[] downTiled = tiles[14];
            int[] upTiled = tiles[15];

            int pixelData;
            int tiled_tilesize = (int)System.Math.Sqrt(tiled.Length);


            //tilesize = (int)System.Math.Sqrt(tiled.Length);
            for (int x = 0; x < MX; x++) for (int y = 0; y < MY; y++)
                {
                    if (x == 0 || y == 0 || x == MX - 1 || y == MY - 1)
                    {
                        if (x == 0 && y == 0) tiled = upLeftTiled;
                        else if (x == 0 && y == MY - 1) tiled = downLeftTiled;
                        else if (x == MX - 1 && y == 0) tiled = upRightTiled;
                        else if (x == MX - 1 && y == MY - 1) tiled = downRightTiled;
                        else if (x == 0) tiled = leftTiled;
                        else if (y == 0) tiled = upTiled;
                        else if (y == MY - 1) tiled = downTiled;
                        else tiled = rightTiled;

                        for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                            {
                                pixelData = tiled[dx + dy * tilesize];
                                if (pixelData != 16777215)
                                    bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = pixelData;
                            }

                        bitmapCheck[x + y * MY] = true;
                    }
                }
        }
        else
        {
            tiled = tiles[7];
            int pixelData;
            int tiled_tilesize = (int)System.Math.Sqrt(tiled.Length);


            //tilesize = (int)System.Math.Sqrt(tiled.Length);
            for (int x = 0; x < MX; x++) for (int y = 0; y < MY; y++)
                {
                    if (x == 0 || y == 0 || x == MX - 1 || y == MY - 1)
                    {
                        for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                            {
                                pixelData = tiled[dx + dy * tilesize];
                                if (pixelData != 16777215)
                                    bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = pixelData;
                            }

                        bitmapCheck[x + y * MY] = true;
                    }
                }
        }

    }
    

    public override void Save(string filename, db_OutputsEntities db)
    {
        // bitmapdataに部屋の情報を入れる
        leafs[0].CreateRooms(this, tiles, MX, MY, tilesize, rand);

        // 道を作る
        

        int[] tiled;

        if (VillageMoodName == "ForestVillage")
        {

            tiled = tiles[16];
            bool[] road = CellularAutomata.Generate(bitmapCheck, MX, MY);
            for (int x = 0; x < MX; x++) for (int y = 0; y < MY; y++)
                {
                    if (!road[x + y * MY])
                    {
                        for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                                bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = tiled[dx + dy * tilesize];
                    }
                }
        }

        WFC_CreateController.SaveBitmap(bitmapData, MX * tilesize, MY * tilesize, filename, db);
    }
}
