using System;
using System.Collections.Generic;
using System.Xml.Linq;
using WooCommerceNET.WooCommerce.v3;

namespace WooSage.Models
{

    public class ProductWithVariations
    {
        public Product Product { get; set; }
        public List<Variation> Variations { get; set; }

        public List<string> Categories { get; set; }

        public	 ProductWithVariations()
        {
            Variations = new List<Variation>();
        }
       
    }
}


