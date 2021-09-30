using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using WooSage.Services;

namespace WooSage
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false);


            IConfiguration config = builder.Build();

            var wooApiConfig = config.GetSection("WooAPI").Get<WooAPIConfig>();        
            var sageXMLConfig = config.GetSection("SageXML").Get<SageXMLConfig>();        
            var logsAppConfig = config.GetSection("Logs").Get<LogsConfig>();        

            var logsFolder = (logsAppConfig != null && !string.IsNullOrEmpty(logsAppConfig.Folder)) 
                ? logsAppConfig.Folder
                : Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "logs");
            
            var logsFile = Path.Combine(logsFolder, "sagexml2woo-"+DateTime.Today.ToString("yyyy-MM-dd")+".log");            

            var loggerConfig = new LoggerConfiguration();
            loggerConfig.WriteTo.Console(
                theme: SystemConsoleTheme.Grayscale,
                outputTemplate: "{Message:lj}{NewLine}{Exception}"
            );
            loggerConfig.WriteTo.File(logsFile);            
                
            Log.Logger = loggerConfig.CreateLogger();       

            Log.Information("starting  sagexml2woo...");
            Log.Information("log file: "+logsFile);     
                
            var sageXML = new LoadSageXMLService();
            var woo = new WooSageService();
            woo.Connect(wooApiConfig.ServiceUrl, wooApiConfig.Key, wooApiConfig.Secret);

            var parser = new Parser(config => config.HelpWriter = Console.Out);
            var options = parser.ParseArguments<ListOptions, LoadOptions,GetProductOptions>(args)
                .WithParsed<ListOptions>(async options => await woo.List(options))
                .WithParsed<GetProductOptions>(async options => {
                    var str = await woo.GetProductAsJson(options);
                    Log.Information(str);
                })
                .WithParsed<LoadOptions>(async options => { 
                    var products = sageXML.LoadProducts(options.FileName, sageXMLConfig, options);
                    await woo.LoadProducts(products);
                })                
                .WithNotParsed(errors => { }); 
            Console.ReadLine();                            
        }

       

    }


}
