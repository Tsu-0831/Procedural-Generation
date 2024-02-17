using System;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WebApp.Models;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


static class Helper
{

    public static int Random(this double[] weights, double r)
    {
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += weights[i];
        double threshold = r * sum;

        double partialSum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            partialSum += weights[i];
            if (partialSum >= threshold) return i;
        }
        return 0;
    }

    public static long ToPower(this int a, int n)
    {
        long product = 1;
        for (int i = 0; i < n; i++) product *= a;
        return product;
    }

    // xlem.Get()で呼び出せる
    // 第2引数：attributeは要素の名前("size"や"name"等)
    // 第3引数：defaultTはデフォルト値
    public static T Get<T>(this XElement xelem, string attribute, T defaultT = default)
    {
        XAttribute a = xelem.Attribute(attribute); // "attribute"を要素として持つXAttributeを返す

        // XAttribute aが存在しない場合とする場合で分ける
        // 存在する場合："T"のデータ型に変換して返す
        // 存在しない場合：デフォルト値を返す
        return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
    }

    public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names) => xelement.Elements().Where(e => names.Any(n => n == e.Name));
}

static class BitmapHelper
{
    // 返り値：幅×高さの大きさのint型の配列、幅の値、高さの値
    public static (int[], int, int) LoadBitmap(string filename)
    {
        int width, height; // 幅と高さ
        int[] result;
        var image = Image.Load<Bgra32>(filename); // 指定された画像(filename)を読み込む

        try
        {
            width = image.Width; // 幅
            height = image.Height; // 高さ
            result = new int[width * height]; // 幅と高さ分の配列を用意
            // CopyPixelDataToメソッド：ピクセルデータを、指定されたメモリの位置にコピーする
            // MemoryMarshal.Cast：int型要素を持つBgra32型のデータに変換
            image.CopyPixelDataTo(MemoryMarshal.Cast<int, Bgra32>(result)); // filenameのピクセルデータをresultにコピーする
        }
        finally
        {
            image.Dispose();
        }

        return (result, width, height);
    }

    unsafe public static void SaveBitmap(int[] data, int width, int height, string filename)
    {
        // fixedステートメントで配列dataをポインタpDataに固定する。安全なコンテキストの外でポインターを使用することが可能。
        fixed (int* pData = data)
        {
            //using var image = Image.WrapMemory<Bgra32>(pData, width, height);
            var image = Image.WrapMemory<Bgra32>(pData, width, height); // ポインタpDataを介して直接メモリ上のピクセルデータにアクセスするためのimageオブジェクトを作成
            // Image<Bgra32> save_image = image;

            try
            {
                image.SaveAsPng(filename); // pngフォーマットで指定された名前(filename)で保存する
            }
            finally
            {
                image.Dispose();
            }

        }
    }

    public static MvcHtmlString DisplayImage(this HtmlHelper htmlHelper, byte[] imageBytes)
    {
        if (imageBytes != null)
        {
            var base64Image = Convert.ToBase64String(imageBytes);
            var imgSrc = string.Format("data:image/png;base64,{0}", base64Image);
            var imgTag = new TagBuilder("img");
            imgTag.MergeAttribute("src", imgSrc);
            return MvcHtmlString.Create(imgTag.ToString(TagRenderMode.SelfClosing));
        }

        return MvcHtmlString.Empty;
    }
}


