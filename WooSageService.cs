using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;
using WooCommerceNET.WooCommerce.v3.Extension;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using WooSage.Models;
using System.Text.Json;
using Serilog;
using System.Text;

namespace WooSage.Services
{

    public class WooSageService
    {
        List<Product> products = null;
        List<ProductCategory> categories = null;

        RestAPI rest;
        public WCObject wc;
        public void Connect(string url, string key, string secret)
        {
            rest = new RestAPI(url, key, secret, true);
            wc = new WCObject(rest);
        }

        public async Task<List<Product>> GetAllProducts()
        {
            //Get all products
            if (products == null)
            {   
                products = new List<Product>();
                int page = 1, perPage = 20;
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "per_page", perPage.ToString() }
                };
                bool hasResults = true;
                while (hasResults)
                {
                    parameters["page"] = (page++).ToString();
                    var prods = await wc.Product.GetAll(parameters);
                    products.AddRange(prods);
                    hasResults = prods.Count == perPage;
                }                
            }
            return products;
        }

        public async Task<string> GetProductAsJson(GetProductOptions options)
        {
            var product = await GetProductBySKU(options.sku);

            if (product == null)
            {
                return null;
            }
            var exisitingVariations = await wc.Product.Variations.GetAll(product.id);

            string jsonString = JsonSerializer.Serialize(new ProductWithVariations
            {
                Product = product,
                Variations = exisitingVariations
            });

            return jsonString;
        }

        public async Task<List<ProductCategory>> GetAllCategories()
        {
            //Get all products
            if (categories == null)
            {
                categories = new List<ProductCategory>();
                int page = 1, perPage = 20;
                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "per_page", perPage.ToString() }
                };
                bool hasResults = true;
                while (hasResults)
                {
                    parameters["page"] = (page++).ToString();
                    var prods = await wc.Category.GetAll(parameters);
                    categories.AddRange(prods);
                    hasResults = prods.Count == perPage;
                }
            }
            return categories;
        }

        public async Task List(ListOptions options)
        {
            var products = await GetAllProducts();
            Log.Information("listing {num} products", products.Count);
            foreach (var item in products)
            {
                var categoryStr = item.categories != null ? " [" + string.Join(',', item.categories.Select(c => c.name)) + "]" : null;
                if (item.variations.Count > 0)
                {
                    var variationsStr = string.Join(',', item.variations.Select(v => v.ToString()).ToArray());
                    Log.Information("{sku}: {name}{category} variations: {variations}", item.sku, item.name, categoryStr, variationsStr);
                }
                else
                {
                    Log.Information("{sku}: {name}{category}", item.sku, item.name, categoryStr);
                }
                if (options.Attributes)
                {
                    foreach (var attr in item.attributes)
                    {
                        Log.Information("  name:{name}  options: {options}", attr.name, string.Join(',', attr.options));
                    }
                }

            }
        }

        public async Task<Product> GetProductBySKU(string sku)
        {
            var prods = await GetAllProducts();
            return prods.FirstOrDefault(p => p.sku == sku);
        }

        public async Task<ProductCategory> GetCategory(string name, bool add = false)
        {
            var cats = await GetAllCategories();
            var category = cats.FirstOrDefault(p => p.name == name);
            if (add && category == null)
            {
                category = await wc.Category.Add(new ProductCategory
                {
                    name = name
                });
                cats.Add(category);
                return category;
            }

            return category;
        }


        public async Task LoadProducts(List<ProductWithVariations> products)
        {
            Log.Information("importing {num} products", products.Count);
            foreach (var imported in products)
            {
                try
                {
                    await LoadProduct(imported);
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex, "error importing product {0}: {1}", imported.Product.sku, ex.Message);
                }
            }
            Log.Information("import finished");
        }

        private async Task LoadProduct(ProductWithVariations imported)
        {
            if (string.IsNullOrEmpty(imported.Product.sku))
            {
                Log.Warning("item with no sku : {name}", imported.Product.name);
                return;
            }
            var existing = await GetProductBySKU(imported.Product.sku);
            if (existing == null)
            {
                var categoryStr = imported.Categories != null ? " [" + string.Join(',', imported.Categories) + "]" : null;
                Log.Information("adding item: {sku}{category} stock:{quantity}  price:{price}", imported.Product.sku, categoryStr, imported.Product.stock_quantity, imported.Product.price);
                if (string.IsNullOrEmpty(imported.Product.status))
                    imported.Product.status = "private";
                await CreateNewProduct(imported);
                await UpdateVariations(imported);
            }
            else
            {
                imported.Product.id = existing.id;
                Log.Information("updating item: {sku} stock: {quantity0}->{quantity1}  price: {price0}->{price1} ",
                    imported.Product.sku, existing.stock_quantity, imported.Product.stock_quantity, existing.price, imported.Product.price);
                existing.type = imported.Variations.Count > 0 ? "variable" : "simple";
                UpdateProductFields(existing, imported.Product);

                //TestJson(existing);
                try
                {
                     await wc.Product.Update(existing.id.Value, existing);
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex, "error updating product {0}: {1}", existing.sku, ex.Message);                
                }
                
                await UpdateVariations(imported);
            }
        }

        private async Task CreateNewProduct(ProductWithVariations data)
        {
            if (data.Variations.Count > 0)
            {
                data.Product.type = "variable";
            }
            if (data.Categories != null)
            {
                foreach (var categoryName in data.Categories)
                {
                    var cat = await GetCategory(categoryName, add: true);
                    if (data.Product.categories == null)
                        data.Product.categories = new List<ProductCategoryLine>();
                    data.Product.categories.Add(new ProductCategoryLine
                    {
                        id = cat.id,
                        name = cat.name
                    });
                }
            }
            var created = await wc.Product.Add(data.Product);
            data.Product.id = created.id;
            this.products.Add(created);
        }

        void TestJson(object item)
        {

            StringBuilder json = new StringBuilder();
            json.Append("{");
            foreach (var prop in item.GetType().GetProperties())
            {
                try
                {
                    if (prop.GetValue(item).ToString() == "")
                        json.Append($"\"{prop.Name}\": \"\", ");
                    else
                        json.Append($"\"{prop.Name}\": \"{prop.GetValue(item)}\", ");
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex, "error en " + prop);
                }

            }

            if (json.Length > 1)
                json.Remove(json.Length - 2, 1);

            json.Append("}");
        }

        private async Task UpdateVariations(ProductWithVariations data)
        {
            var exisitingVariations = await wc.Product.Variations.GetAll(data.Product.id);
            foreach (var item in data.Variations)
            {
                var attributes = item.attributes.Select(at => at.option);
                var existing = exisitingVariations.FirstOrDefault(v => v.sku == item.sku);
                if (existing == null)
                {
                    Log.Information("Adding variation: {sku}  stock: {stock} price: {price}", item.sku, item.stock_quantity, item.price);
                    await wc.Product.Variations.Add(item, data.Product.id.Value);
                }
                else if (existing.stock_quantity != item.stock_quantity 
                    || existing.attributes.Count != item.attributes.Count
                    || existing.price != item.price)
                {
                    Log.Information("Updating variation: {sku}. stock: {stock0}->{stock1}  price: {price0}->{price1}  ",
                      item.sku, existing.stock_quantity, item.stock_quantity, existing.price, item.price);                    
                    existing.sku = item.sku;
                    existing.stock_quantity = item.stock_quantity;
                    existing.price = item.price;
                    existing.attributes = item.attributes;                    
                    existing.regular_price = existing.regular_price.HasValue ? existing.regular_price : item.price;
                    existing.sale_price = existing.sale_price.HasValue ? existing.sale_price : item.price;
                    existing.date_on_sale_from = existing.date_on_sale_from ?? DateTime.Now;
                    existing.date_on_sale_from_gmt = existing.date_on_sale_from_gmt ?? DateTime.Now;
                    // existing.date_on_sale_to = existing.date_on_sale_to ?? DateTime.Now.AddYears(15);
                    // existing.date_on_sale_to_gmt = existing.date_on_sale_to_gmt ?? DateTime.Now.AddYears(15);
                    existing.weight = existing.weight ?? 0;
                    // existing.image = existing.image ?? new VariationImage { 
                    //     id = 5
                    // };
                    // existing.attributes = new List<VariationAttribute>();                    
                    existing.dimensions = new VariationDimension {
                        length = "",
                        height = "",
                        width = ""
                    };                    
                    existing.downloads = new List<VariationDownload>();
                    // if (item.price.HasValue) {
                    //     existing.on_sale = true;
                    //     existing.purchasable = true;
                    // }
                    try
                    {
                        //TestJson(existing);                        
                         await wc.Product.Variations.Update(existing.id.Value, existing, data.Product.id.Value);                    
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex, "error updating variation {0}: {1}", existing.sku, ex.Message);
                    }

                }
            }
        }

        private bool SameAttributes(Variation v1, Variation v2)
        {
            if (v1.attributes.Count != v2.attributes.Count)
                return false;

            foreach (var v1attr in v1.attributes)
            {
                var v2attr = v2.attributes.FirstOrDefault(a => a.name == v1attr.name);
                if (v2attr.option != v1attr.option)
                    return false;
            }
            return true;
        }

        private ProductAttributeLine GetAttribute(string name, List<string> options)
        {
            return new ProductAttributeLine()
            { name = name, variation = true, visible = true, options = options };
        }

        private Product LoadFromStr(string item)
        {
            var fields = item.Split(",");
            return new Product
            {
                sku = fields[0],
                name = StrField(fields[1]),
                short_description = StrField(fields[2]),
                description = StrField(fields[3]),
                price = StrToDecimal(fields[4]),
                stock_quantity = StrToInt(fields[5]),
            };
        }

        private void UpdateProductFields(Product existing, Product updated)
        {
            existing.sku = updated.sku;
            existing.name = updated.name;
            existing.short_description = updated.short_description;
            existing.description = string.IsNullOrEmpty(updated.description) ? "" : updated.description;
            existing.short_description = string.IsNullOrEmpty(updated.short_description) ? "" : updated.short_description;
            existing.enable_html_description = updated.enable_html_description ?? false;
            existing.enable_html_short_description = string.IsNullOrEmpty(updated.enable_html_short_description) ? "" : updated.enable_html_short_description;
            if (updated.price.HasValue) {
                existing.price = updated.price;                
            }                            
            existing.regular_price = updated.regular_price.HasValue ? updated.regular_price : updated.price;
            existing.sale_price = updated.sale_price.HasValue ? updated.sale_price : updated.price;
            existing.date_on_sale_from = existing.date_on_sale_from ?? DateTime.Now;
            existing.date_on_sale_from_gmt = existing.date_on_sale_from_gmt ?? DateTime.Now;
            existing.stock_quantity = updated.stock_quantity;            
            existing.manage_stock = true;
        }


        private string StrField(string v)
        {
            return v.Trim('\"');
        }

        private int? StrToInt(string v)
        {
            int res;
            if (int.TryParse(v, out res))
            {
                return res;
            }
            return null;
        }

        private uint StrToUInt(string str)
        {
            return uint.Parse(str);
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
