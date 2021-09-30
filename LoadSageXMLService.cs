using System;
using System.Linq;
using System.Collections.Generic;
using WooCommerceNET.WooCommerce.v3;
using System.Xml.Linq;
using System.Globalization;
using WooSage.Models;
using Serilog;

namespace WooSage.Services
{

    public class LoadSageXMLService
    {        

        public List<ProductWithVariations> LoadProducts(string filename, SageXMLConfig config, LoadOptions options)
        {
            var products = new List<ProductWithVariations>();
            IEnumerable<XElement> prodXML = null;
            try
            {
                 prodXML = LoadDetailSections(filename);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "Error loading {file}", filename);
                return products;
            }
            
            

            ProductWithVariations current = null;

            foreach (var item in prodXML)
            {
                var sku = GetElementFieldValue(config.sku_key, item);
                var unitsDec = StrToDecimal(GetElementFieldValue(config.stock_quantity_key, item));
                int? stock_quantity = unitsDec.HasValue ? (int?) unitsDec.Value : null;
                var price = StrToDecimal(GetElementFieldValue(config.price_key, item));

                if (!string.IsNullOrEmpty(sku))
                {
                    current = new ProductWithVariations
                    {
                        Product = new Product
                        {
                            sku = sku,
                            name = GetElementFieldValue(config.name_key, item),
                            price = price,                                                        
                            attributes = new List<ProductAttributeLine>()
                        }
                    };
                    if (stock_quantity.HasValue)
                        current.Product.stock_quantity = stock_quantity;

                    SetProperty(current.Product, item, config.backordered_key, (p,v) =>  p.backordered = StrToBool(v) );
                    SetProperty(current.Product, item, config.average_rating_key, (p,v) =>  p.average_rating = v );
                    SetProperty(current.Product, item, config.backorders_key, (p,v) =>  p.backorders = v );
                    SetProperty(current.Product, item, config.backorders_allowed_key, (p,v) =>  p.backorders_allowed = StrToBool(v) );
                    SetProperty(current.Product, item, config.description_key, (p,v) =>  p.description = v );
                    SetProperty(current.Product, item, config.external_url_key, (p,v) =>  p.external_url = v );
                    SetProperty(current.Product, item, config.featured_key, (p,v) =>  p.featured = StrToBool(v) );
                    SetProperty(current.Product, item, config.on_sale_key, (p,v) =>  p.on_sale = StrToBool(v) );
                    SetProperty(current.Product, item, config.permalink_key, (p,v) =>  p.permalink = v );
                    SetProperty(current.Product, item, config.price_html_key, (p,v) =>  p.price_html = v );
                    SetProperty(current.Product, item, config.purchasable_key, (p,v) =>  p.purchasable = StrToBool(v) );
                    SetProperty(current.Product, item, config.slug_key, (p,v) =>  p.slug = v );                    
                    SetProperty(current.Product, item, config.status_key, (p,v) =>  p.status = v , options.Status);
                    SetProperty(current.Product, item, config.stock_status_key, (p,v) =>  p.stock_status = v );
                    SetProperty(current.Product, item, config.type_key, (p,v) =>  p.type = v );                    
                    var categories = CheckProperty(item, config.categories_key, options.Category);
                    if (!string.IsNullOrEmpty(categories)) {
                        current.Categories = categories.Split(',').ToList();
                    }

                    products.Add(current);
                } else {
                    sku = current.Product.sku;
                    if (price == null)
                        price = current.Product.price;
                }
                FillAttributes(item, current.Product.attributes, config.GetAttributes());
                FillVariation(item, current.Variations, config.GetAttributes(), sku, stock_quantity, price);
            }

            return products;
        }

        private bool? StrToBool(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;   
            
            return str.ToLower() == "true";
        }

        private string CheckProperty(XElement item, string prop, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(prop) && string.IsNullOrEmpty(defaultValue))
                return null;            
            
            var propValue = string.IsNullOrEmpty(prop) ? null : GetElementFieldValue(prop, item);
            if (string.IsNullOrEmpty(propValue))
                propValue = defaultValue;
            return propValue;
        }


        private void SetProperty(Product product, XElement item, string prop, Action<Product,string> func, string defaultValue = null)
        {
            var propValue = CheckProperty(item, prop, defaultValue);
            if (!string.IsNullOrEmpty(propValue))
                func(product,propValue);                                        
        }

        void FillAttributes(XElement item, List<ProductAttributeLine> attributeLines, string[] attributes)
        {            
            foreach (var attrName in attributes)
            {
                var value = GetElementFieldValue(attrName, item);
                if (!string.IsNullOrEmpty(value)) {
                    var attrLine = attributeLines.FirstOrDefault(al => al.name == attrName);
                    if (attrLine  == null) {
                        attributeLines.Add(new ProductAttributeLine { 
                            name = attrName,
                            variation = true,
                            visible = true,
                            options = new List<string> { value }
                        });
                    } 
                    else if (attrLine.options.IndexOf(value) == -1) {
                        attrLine.options.Add(value);
                    }                        
                }
            }            
        }

        void FillVariation(XElement item, List<Variation> variations, string[] attributes, string sku, int? units, decimal? price)
        {   
            var vatrib = new List<VariationAttribute>();
            

            foreach (var attrName in attributes)
            {
                var value = GetElementFieldValue(attrName, item);

                if (!string.IsNullOrEmpty(value)) {
                    vatrib.Add(new VariationAttribute() {name = attrName, option = value });                   
                }
            }           
              
            if (vatrib.Count == 0)
                return;
            var optionsStr = vatrib.Select(at => at.option);
            var var1 = new Variation
            {                
                sku = string.Format("{0}_{1}", sku, string.Join('.', optionsStr)),
                price = price,
                regular_price = price,
                sale_price = price,
                attributes = vatrib,
                stock_quantity = units,
                manage_stock = true
            }; 
            variations.Add(var1);              
        }

        

        Product LoadProduct(XElement element)
        {
            var columns = element.Descendants("COLUMNS");
            var prod = new Product();


            return prod;
        }

        string GetElementFieldValue(string field, XElement element)
        {
            var columns = element.Descendants("COLUMNS").SelectMany(s => s.Descendants("COLUMN"));
            var col = columns.FirstOrDefault(item => item.Attribute("NAME").Value == field);
            return col != null ? col.Value : null;
        }

        List<string> LoadProductAttributeVariations(string attrName, XElement element)
        {
            return element.Descendants("COLUMNS").SelectMany(s => s.Descendants("COLUMN"))
                .Where(item => item.Attribute("NAME").Value == attrName)
                .Select(i => i.Value).ToList();
        }

        IEnumerable<XElement> LoadDetailSections(string filename)
        {
            var products = XElement.Load(filename);
            return products.Descendants("PAGE")
                .SelectMany(s => s.Descendants("SECTIONS"))
                .SelectMany(s => s.Descendants("SECTION"))
                .Where(item => item.Attribute("NAME").Value == "Detail");
        }

        private ProductAttributeLine GetAttribute(string name, List<string> options)
        {
            return new ProductAttributeLine()
            { name = name, variation = true, visible = true, options = options };
        }


        private decimal? StrToDecimal(string str)
        {
            decimal res;

            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out res))
            {
                return res;
            }
            return null;
        }

    }
}
