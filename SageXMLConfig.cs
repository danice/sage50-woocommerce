namespace WooSage
{
    
    public class SageXMLConfig {
        string[] _attributeList;

        public string sku_key { get; set; }
        public string name_key { get; set; }
        public string stock_quantity_key { get; set; }        
        public string price_key { get; set; }        
        public string stock_status_key { get; set; } 
        public string average_rating_key { get; set; }      
        public string backordered_key { get; set; }
        public string backorders_key { get; set; }
        public string backorders_allowed_key { get; set; }
        public string categories_key { get; set; }
        public string description_key { get; set; }
        public string external_url_key { get; set; }
        public string featured_key { get; set; }     
        public string on_sale_key { get; set; }      
        public string permalink_key { get; set; }      
        public string price_html_key { get; set; }      
        public string purchasable_key { get; set; }        
        public string slug_key { get; set; }        
        public string status_key { get; set; }        
        public string type_key { get; set;}
        public string Attributes { 
            get 
            {
                if (_attributeList == null)
                    return null;
                return string.Join(',', _attributeList);
            } 
            set 
            {
                _attributeList = value.Split(',');
            } 
        }

        public string[] GetAttributes()
        {
            return this._attributeList;
        }
            
    }
}