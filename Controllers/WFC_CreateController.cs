using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using WebApp.Models;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;



public class WFC_CreateController : Controller
{
    // データベース機能を取得
    private db_OutputsEntities db = new db_OutputsEntities();
        
    // GET: WFC_Create
    public ActionResult Index()
    {
        // Viewで項目にしたがって処理を実行する。
        // ViewBagには、アウトプットした画像を格納しているパスを渡す。

        return View(db.Table.ToList());
    }

    public ActionResult Delete(int? id)
    {
        if (id == null)
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        Table table = db.Table.Find(id);
        if (table == null)
        {
            return HttpNotFound();
        }
        return View(table);
    }

    // POST: WFC_Create/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)
    {
        Table table = db.Table.Find(id);
        db.Table.Remove(table);
        db.SaveChanges();
        return RedirectToAction("Index");
    }


    public ActionResult Create_Setting()
    {
        return View();
    }

    public ActionResult Create()
    {
        Random random = new Random(); // ランダム生成
        XDocument xdoc = XDocument.Load(Server.MapPath("../Models/samples.xml")); // XML読みこみ

        string[] Create_Names = new string[] {
            "Village",
            "Cave"
        };

        string[] CaveColor_Names = new string[] {
            "Gray",
            "Brown"
        };

        string[] VillageMood_Names = new string[] {
            "ForestVillage",
            "CastleTown"
        };

        string[] VillageFrame_Names = new string[] {
            "GrassFrame",
            "RockFrame"
        };

        foreach (string Create_name in Create_Names)
        {
            // チェックボックスにチェックされていた時
            if (Request.Form["Radio"] == Create_name)
            {
                // "Elements"メソッド：指定した名前の子要素を取得する
                // 各子要素について処理を行う
                foreach (XElement xelem in xdoc.Root.Elements("BSPtiled", "Cellulartiled"))
                {

                    string name1 = xelem.Get<string>("name");

                    // Create_name(生成する名前)と取得しているoverlappingまたはsimpletiled要素の名前が等しいとき
                    if (Create_name == name1)
                    {
                        Model model; // モデルの宣言
                        Model bspTree;
                        Model cellularModel;
                        bool isBSPtiled = xelem.Name == "BSPtiled";
                        bool isCellulartiled = xelem.Name == "Cellulartiled";

                        string heuristicString = xelem.Get<string>("heuristic"); // "heuristic"のAttributeを調べる
                                                                                 // heuristicが"Scanline"の場合："Model.Heuristic.Scanline"にアクセスする
                                                                                 // heuristicが"MRV"の場合："Model.Heuristic.MRV"にアクセスする
                                                                                 // その他："Model.Heuristic.Entropy"にアクセスする
                        var heuristic = heuristicString == "Scanline" ? Model.Heuristic.Scanline : (heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy);

                        bspTree = null;
                        cellularModel = null;

                        

                        if (isBSPtiled)
                        {
                            string sendVillageMoodName = null;
                            string sendVillageFrameName = null;
                            string subset = xelem.Get<string>("subset"); // CrossLess, TurnLess, Dense(密集), Fabric(織物)
                            foreach (string VillageMoodName in VillageMood_Names)
                            {
                                if (Request.Form["VillageMood"] == VillageMoodName)
                                {
                                    sendVillageMoodName = VillageMoodName;
                                    break;
                                }
                            }
                            foreach (string VillageFrame_Name in VillageFrame_Names)
                            {
                                if (Request.Form["VillageFrame"] == VillageFrame_Name)
                                {
                                    sendVillageFrameName = VillageFrame_Name;
                                    break;
                                }
                            }

                            bspTree = new BSP_Tree_DungeonGeneration(subset, 40, 40, heuristic, sendVillageMoodName, sendVillageFrameName);
                        }

                        if (isCellulartiled)
                        {
                            string sendCaveColorName = null;
                            string subset = xelem.Get<string>("subset"); // CrossLess, TurnLess, Dense(密集), Fabric(織物)
                            foreach (string CaveColor_Name in CaveColor_Names)
                            {
                                if (Request.Form["CaveColor"] == CaveColor_Name)
                                {
                                    sendCaveColorName = CaveColor_Name;
                                    break;
                                }
                            }
                            cellularModel = new CellularAutomata(subset, 30, 30, heuristic, sendCaveColorName);
                        }

                        // 画像生成開始
                        bool success; // 実行可能かどうかを調べる
                        int seed = random.Next();
                        
                        if (bspTree != null)
                        {
                            for (int k = 0; k < 20; k++)
                            {
                                success = bspTree.Run(seed, -1);
                                if (success)
                                {
                                    bspTree.Save($"{Create_name} {seed}_bsp", db);
                                    break;
                                }
                            }
                        }

                        if (cellularModel != null)
                        {
                            for (int k = 0; k < 20; k++)
                            {
                                success = cellularModel.Run(seed, -1); ;
                                if (success)
                                {
                                    cellularModel.Save($"{Create_name} {seed}_CellularAutomata", db);
                                    break;
                                }
                            }    
                        }
                    }
                }
            }
        
        }
        return RedirectToAction("Index");
    }

    unsafe public static void SaveBitmap(int[] data, int width, int height, string filename, db_OutputsEntities db)
    {
        
        // fixedステートメントで配列dataをポインタpDataに固定する。安全なコンテキストの外でポインターを使用することが可能。
        fixed (int* pData = data)
        {
            var image = Image.WrapMemory<Bgra32>(pData, width, height); // ポインタpDataを介して直接メモリ上のピクセルデータにアクセスするためのimageオブジェクトを作成
                                                                        // Image<Bgra32> save_image = image;
            byte[] byte_image = ConvertImageToBytes(image);
            Table table = new Table();

            try
            {
                table.item = byte_image;
                table.Date = DateTime.Now;
                table.itemName = filename;
                db.Table.Add(table);
                db.SaveChanges();
            }
            
            finally
            {
                image.Dispose();
            }
        }
    }

    public static byte[] ConvertImageToBytes(Image image)
    {
        using (var outputStream = new MemoryStream())
        {
            image.Save(outputStream, new PngEncoder());
            return outputStream.ToArray();
        }
    }
}