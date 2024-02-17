using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApp.Models;
using System.Xml.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


class CellularAutomata : Model
{
    // 画像にする用
    private List<int[]> tiles; // 生成に用いる画像データを入れる
    private int[] bitmap; // 画像のビットマップファイルを保存する。
    private int tilesize;
    private string name = "Cave";
    List<string> tilenames;
    public int[] bitmapData;
    public int[] observedSub;
    bool[] mapPosition;
    string CaveColorName;


    public CellularAutomata(string subsetName, int mapWidth, int mapHeight, Heuristic heuristic, string CaveColorName) : base(mapWidth, mapHeight, 1, true, heuristic)
    {
        tiles = new List<int[]>();
        this.CaveColorName = CaveColorName;

        bool[] mapFilter = new bool[mapWidth * mapHeight];

        // 初期化
        for (int i = 0; i < mapWidth * mapHeight; i++)
            mapFilter[i] = false;

        mapPosition = Generate(mapFilter, mapWidth, mapHeight);

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

        // XMLファイルのtilesタグ中のtiles要素について順にxtileに格納していく
        foreach (XElement xtile in xroot.Element("tiles").Elements("tile"))
        {
            string tilename = xtile.Get<string>("name"); // タイル名を保存
            if (subset != null && !subset.Contains(tilename)) continue;

            Func<int, int> a, b;
            int cardinality; // cardinality : 集合の濃度

            char sym = xtile.Get("symmetry", 'X'); // symmetry要素を取得. T, L, X, I, \\, F.
            if (sym == 'L')
            {
                cardinality = 30;
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
                cardinality = 2;
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



    }
    public static bool[] Generate(bool[] mapFilter, int width, int height, int iterations = 4, int percentAreWalls = 35)
    {
        var map = new bool[width * height];

        RandomFill(mapFilter, map, width, height, percentAreWalls);

        for (var i = 0; i < iterations; i++)
            map = Step(map, width, height);

        return map;
    }

    // default値
    // percentAreWalls = 40
    private static void RandomFill(bool[] mapFilter, bool[] map, int width, int height, int percentAreWalls = 35)
    {
        var random = new Random();
        var randomColumn = random.Next(4, width - 4);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mapFilter[x + y * width])
                    map[x + y * width] = true;

                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    map[x + y * width] = true;
                else if (x != randomColumn && random.Next(100) < percentAreWalls)
                    map[x + y * width] = true;
                
            }
        }
    }

    private static bool[] Step(bool[] map, int width, int height)
    {
        var newMap = new bool[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1 || x == 1 || y == 1 || x == width - 2 || y == height - 2)
                    newMap[x + y * width] = true;
                else
                {
                    /*
                     * default
                    */
                    newMap[x + y * width] = PlaceWallLogic(map, width, height, x, y);

                    
                    /*
                     * 制限追加用
                    */
                    //for (int i = 0; i < 4; i++)
                    //{
                    //    newMap[x + y * width] = PlaceWallLogic(map, width, height, x, y);
                    //}

                    //for (int i = 0; i < 3; i++)
                    //{
                    //    newMap[x + y * width] = PlaceWallLogic01(map, width, height, x, y);
                    //}

                    //for (int i = 0; i < 0; i++)
                    //{
                    //    newMap[x + y * width] = PlaceWallLogic02(map, width, height, x, y);
                    //}
                }

            }
        }

        /*
         * 孤島を消す処理
        */
        newMap = MeasureRoomSize(newMap, width, height);

                
        return newMap;
    }

    private static bool[] MeasureRoomSize(bool[] map, int width, int height)
    {
        int[] mapSizes = new int[map.Length];

        int largestConnectedAreaSize = 0;

        // 各タイルについて、falseがつながった形のサイズを計算
        for (int t = 0; t < map.Length; t++)
        {
            mapSizes[t] = GetConnectedAreaSize(map, width, height, t);

            // 最大の形を特定
            if (mapSizes[t] > largestConnectedAreaSize)
            {
                largestConnectedAreaSize = mapSizes[t];
            }
        }

        // 最大の形以外をすべてtrueに変換
        for (int t = 0; t < map.Length; t++)
        {
            if (mapSizes[t] < largestConnectedAreaSize)
            {
                map[t] = true;  // trueに変換
       
            }
        }

        return map;
    }

    // 与えられた2Dタイル配列におけるfalseがつながった形のサイズを計算するメソッド
    private static int GetConnectedAreaSize(bool[] map, int width, int height, int position)
    {
        bool[] visited = new bool[map.Length];
        int maxSize = 0;

        if (map[position] == false && !visited[position])
        {
            int size = DFS(map, width, height, position, visited);
            maxSize = Math.Max(maxSize, size);
        }

        return maxSize;
    }

    // 深さ優先探索 (DFS) を用いてfalseがつながった形のサイズを計算
    private static int DFS(bool[] map, int width, int height, int position, bool[] visited)
    {
        if (position < 0 || position >= map.Length || visited[position] || map[position] == true)
        {
            return 0;
        }

        visited[position] = true;
        int size = 1;

        // 上下左右に対して再帰的に探索
        size += DFS(map, width, height, position - 1, visited);  // 左
        size += DFS(map, width, height, position + 1, visited);  // 右
        size += DFS(map, width, height, position - width, visited);  // 上
        size += DFS(map, width, height, position + width, visited);  // 下

        return size;
    }

    private static bool PlaceWallLogic(bool[] map, int width, int height, int x, int y) =>
        CountAdjacentWalls(map, width, height, x, y) >= 5 ||
        CountNearbyWalls(map, width, height, x, y) <= 2;

    private static bool PlaceWallLogic01(bool[] map, int width, int height, int x, int y) =>
        CountAdjacentWalls(map, width, height, x, y) >= 5;

    private static bool PlaceWallLogic02(bool[] map, int width, int height, int x, int y) =>
        CountNearbyWalls(map, width, height, x, y) <= 2;

    // 3 × 3の範囲の壁の数を返す
    private static int CountAdjacentWalls(bool[] map, int width, int height, int x, int y)
    {
        var walls = 0;

        for (var mapX = x - 1; mapX <= x + 1; mapX++)
        {
            for (var mapY = y - 1; mapY <= y + 1; mapY++)
            {
                if (map[mapX + mapY * width])
                    walls++;
            }
        }

        return walls;
    }


    // 5 × 5の範囲の壁の数を返す
    private static int CountNearbyWalls(bool[] map, int width, int height, int x, int y)
    {
        var walls = 0;

        for (var mapX = x - 2; mapX <= x + 2; mapX++)
        {
            for (var mapY = y - 2; mapY <= y + 2; mapY++)
            {
                if (Math.Abs(mapX - x) == 2 && Math.Abs(mapY - y) == 2)
                    continue;

                if (mapX < 0 || mapY < 0 || mapX >= width || mapY >= height)
                    continue;

                if (map[mapX + mapY * width])
                    walls++;
            }
        }

        return walls;
    }

    public override void Save(string filename, db_OutputsEntities db)
    {
        int[] tiled = new int[tilesize * tilesize];
        int imageWallDefference;
        int imageRoadDefference;
        //Array.Copy(observed, observedSub, observed.Length);

        // 画像になるint型のデータ
        bitmapData = new int[MX * MY * tilesize * tilesize];

        if (CaveColorName == "Gray")
        {
            imageWallDefference = 0;
            imageRoadDefference = 0;
        }
        else if (CaveColorName == "Brown"){
            imageWallDefference = 15;
            imageRoadDefference = 1;
        }
        else
        {
            imageWallDefference = 0;
            imageRoadDefference = 0;
        }

        // 背景を黒一色にする
        tiled = tiles[32];
        for (int x = 0; x < MX; x++) for (int y = 0; y < MY; y++)
        {
            for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                    bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = tiled[dx + dy * tilesize];
        }


        // タイルを設置する
        // 9種類のタイルを使用する
        // まずは、CellAutomataで生成したtrueの場所を示す変数が必要
        // mapPosition[]
        bool up, down, left, right, upLeft, upRight, downLeft, downRight;

        // 地面のタイルを決定する
        for (int i = 0; i < 2; i++)
        {
            for (int x = 1; x < MX - 1; x++) for (int y = 1; y < MY - 1; y++)
                {
                    if (mapPosition[x + y * MY])
                    {
                        up = !mapPosition[x + (y - 1) * MY];
                        down = !mapPosition[x + (y + 1) * MY];
                        left = !mapPosition[(x - 1) + y * MY];
                        right = !mapPosition[(x + 1) + y * MY];

                        if ((up && down)
                                || (left && right))
                        {
                            mapPosition[x + y * MY] = false;
                            tiled = tiles[0 + imageRoadDefference];
                        }
                        else
                        {
                            tiled = tiles[32];
                        }
                    }
                    else
                    {
                        tiled = tiles[0 + imageRoadDefference];
                    }

                    for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                            bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = tiled[dx + dy * tilesize];
                }
        }


        // 壁の決定
        for (int x = 1; x < MX - 1; x++) for (int y = 1; y < MY - 1; y++)
        {
            if (mapPosition[x + y * MY])
            {
                up = !mapPosition[x + (y - 1) * MY];
                down = !mapPosition[x + (y + 1) * MY];
                left = !mapPosition[(x - 1) + y * MY];
                right =  !mapPosition[(x + 1) +  y * MY];
                upLeft = !mapPosition[(x - 1) + (y - 1) * MY];
                upRight = !mapPosition[(x + 1) + (y - 1) * MY];
                downLeft = !mapPosition[(x - 1) + (y + 1) * MY];
                downRight = !mapPosition[(x + 1) + (y + 1) * MY];

                    if (!up && !down && !left && right) tiled = tiles[2 + imageWallDefference];
                    else if (up && !down && !left && right) tiled = tiles[12 + imageWallDefference];
                    else if (up && !down && !left && !right) tiled = tiles[5 + imageWallDefference];
                    else if (up && !down && left && !right) tiled = tiles[13 + imageWallDefference];
                    else if (!up && !down && left && !right) tiled = tiles[3 + imageWallDefference];
                    else if (!up && down && left && right && !upLeft && !upRight && downLeft && downRight) tiled = tiles[4 + imageWallDefference];
                    else if (!up && down && left && !right && !upRight && downLeft && !downRight) tiled = tiles[11 + imageWallDefference];
                    else if (!up && !down && !left && !right && !upLeft && !upRight && downLeft && !downRight) tiled = tiles[6 + imageWallDefference];
                    else if (!up && !down && !left && !right && !upLeft && !upRight && !downLeft && downRight) tiled = tiles[7 + imageWallDefference];
                    else if (!up && down && !left && !right && !upLeft && !upRight && !downLeft && !downRight) tiled = tiles[14 + imageWallDefference];
                    else if (!up && down && !left && !right && !upLeft && !upRight && downLeft && !downRight) tiled = tiles[9 + imageWallDefference];
                    else if (!up && down && !left && !right && !upLeft && !upRight && downLeft && downRight) tiled = tiles[4 + imageWallDefference];
                    else if (!up && down && left && !right && !upRight && downLeft && downRight) tiled = tiles[15 + imageWallDefference];
                    else if (!up && down && !left && !right && !upLeft && !upRight && !downLeft && downRight) tiled = tiles[8 + imageWallDefference];
                    else if (!up && down && !left && right && !upLeft && !upRight && !downLeft && downRight) tiled = tiles[10 + imageWallDefference];
                    else if (!up && down && !left && right && !upLeft && downLeft && downRight) tiled = tiles[16 + imageWallDefference];
                    else if (!up && down && !left && right && !upLeft && upRight && !downLeft && downRight) tiled = tiles[10 + imageWallDefference];
                    else tiled = tiles[32];


                for (int dy = 0; dy < tilesize; dy++) for (int dx = 0; dx < tilesize; dx++)
                    bitmapData[x * tilesize + dx + (y * tilesize + dy) * MX * tilesize] = tiled[dx + dy * tilesize];
            }
        }

        
        WFC_CreateController.SaveBitmap(bitmapData, MX * tilesize, MY * tilesize, filename, db);
    }

}
