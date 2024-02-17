using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System.Web.Mvc
{
    public static class HtmlHelperPlus
    {
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
}