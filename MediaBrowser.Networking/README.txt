ServiceStack services should be available under '/api' path. If it's a brand new MVC project 
install NuGet Package: ServiceStack.Host.Mvc. The package prepares ServiceStack default services. Make sure 
that you added ignore for MVC routes:

	routes.IgnoreRoute("api/{*pathInfo}"); 

If it's MVC4 project, then don't forget to disable WebAPI:

	//WebApiConfig.Register(GlobalConfiguration.Configuration);
 
Enable Swagger plugin in AppHost.cs with:

    public override void Configure(Container container)
    {
		...

        Plugins.Add(new SwaggerFeature());
		// uncomment CORS feature if it's has to be available from external sites 
        //Plugins.Add(new CorsFeature()); 
		...

    }

Compile it. Now you can access swagger UI with:

http://localost:port/swagger-ui/index.html

or

http://yoursite/swagger-ui/index.html


For more info about ServiceStack please visit: http://www.servicestack.net

Feel free to ask questions about ServiceStack on:
http://stackoverflow.com/

or on the mailing Group at:
http://groups.google.com/group/servicestack

Enjoy!