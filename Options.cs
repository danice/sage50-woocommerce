using CommandLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WooSage
{
  

    public class BaseOptions 
    {
      
        

    }

    [Verb("list", HelpText = "list products")]
    public class ListOptions : BaseOptions
    {
        [Option('a', "attributes", Required = false, HelpText = "list attributes", Default = false)]
        public bool Attributes { get; set; }
     

    }

    [Verb("get-product", HelpText = "get product data")]
    public class GetProductOptions : BaseOptions
    {
        [Value(0)]
        public string sku { get; set; }     

    }

    

    [Verb("load", HelpText = "load products")]
    public class LoadOptions : BaseOptions
    {
         [Value(0)]
        public string FileName { get; set; }

        [Option('c', "category", Required = false, HelpText = "Product category", Default = null)]
        public string Category { get; set; }
        
        [Option('s', "status", Required = false, HelpText = "status: private, pending", Default = null)]
        public string Status { get; set; }

     

    }

    [Verb("attributes", HelpText = "load attributes")]
    public class AttributesOptions : BaseOptions
    {
         [Value(0)]
        public string FileName { get; set; }
     

    }
}
    