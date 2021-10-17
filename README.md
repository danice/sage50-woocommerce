# sagexml2woo


## Description
Command line utility to export and update a list of products exported from Sage 50 in XML format to [woocommerce](https://woocommerce.com/).


---
**DISCLAIMER**: This tool is provided as is and you are using them at your own risk. I am not responsible for any damage or lost data.

---


**SPECIAL THANKS**: many thanks to [XiaoFaye / WooCommerce.NET](https://github.com/XiaoFaye/WooCommerce.NET), a great library to use WooCommerce API from .NET applications.


## Build (skip this if you are familiar with .NET)

The project uses netcoreapp3.1 framework. I recommend you to use VS Code to work with it, but only .net core 3.1 SDK is required.  Follow this:

1) install [dot.net core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)

2) open a cmd, move to your <projects> folder and execute:

```
> git clone https://github.com/danice/sagexml2woo.git

(or download project from https://github.com/danice/sagexml2woo/archive/refs/heads/main.zip)

> cd sagexml2woo
> dotnet build
```

Copy the compiled files from <projects>\sagetxml2woo\src\bin\Debug\netcoreapp3.1
to some folder.

## WooCommerce REST API access configuration
The application uses [WooCommerce REST API DOCS](https://woocommerce.github.io/woocommerce-rest-api-docs/). To access this API you should generate a client key and secret following [this instructions](https://docs.woocommerce.com/document/woocommerce-rest-api/). 

Introduce the url, client key and secret in the config.json file in sagexml2woo application folder.


## Attributes and language configuration
In this config.json file you should also introduce list of all possible product attributes.

For example the application defines "talla,color" attributes.

You should also check the fields that contains different values in sage 50 xml export.
As my test version is in Spanish, they are configured as

```
"SageXML": {
      "attributes": "talla,color",
      "sku_key": "articulo",
      "name_key": "nombre",
      "stock_quantity_key": "unidades",
      "price_key": "precio"      
},
```

The sage product id ("articulo") will be used to define woocommerce sku. In the SageXMLConfig.cs you can find the list of all possible properties to export.

## Commands
### list
Lists all the products, if executed with -a parameter in will include product variations.

```
sagexml2woo list [-a]
```


### get-product
Generates a json description of the product and its variations.

```
sagexml2woo get-product <sku>
```


### load
Reads the indicated xml file and exports it to WooCommerce. It tries to find existing products with the same sku. 

```
sagexml2woo load [-c <some cathegory>] <products.xml>
```

**Important**: this will create and update your WooCommerce products data. I recommend testing it will small imports first. Again I should warn you that ?'m not responsible for any damage, or data lost that could be ocassionated using this tool.

The expected format of the xml file is like:

```
<?xml version="1.0"?>
<PAGES xmlns:dt="urn:schemas-microsoft.com:datatypes">
	<PAGE>	
		<SECTIONS>
			<SECTION NAME="Page Header">
				<COLUMNS>
					<COLUMN NAME="Titulo">VALORACIÓN DE STOCKS</COLUMN>
					<COLUMN NAME="Empresa">my shop</COLUMN>
					<COLUMN NAME="Ejercicio">2021</COLUMN>	
				</COLUMNS>
			</SECTION>
			<SECTION NAME="Detail">
				<COLUMNS>
					<COLUMN NAME="articulo">1</COLUMN>
					<COLUMN NAME="nombre">Very nice T-SHIRT</COLUMN>
					<COLUMN NAME="talla"></COLUMN>
					<COLUMN NAME="color"></COLUMN>
					<COLUMN NAME="unidades">15</COLUMN>
					<COLUMN NAME="precio">42</COLUMN>
					<COLUMN NAME="total" />
				</COLUMNS>
			</SECTION>
            ....
```

If -c parameter is used it will assigne **new created products** the indicated cathegory. All new created products are create as "private" status.



