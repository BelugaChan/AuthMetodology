using Asp.Versioning;

namespace AuthMetodology.API.Extensions
{
    public static class VersionExtensions
    {
        public static void AddVersioning(this IServiceCollection services) 
        {
            services.AddApiVersioning(options =>
            {
                //Версия API по умолчанию
                options.DefaultApiVersion = new ApiVersion(1);

                //Cпециальные HTTP-заголовки, в которых перечислены актуальные и устаревшие версии API
                options.ReportApiVersions = true;

                //Используем версию API по умолчанию, если клиент явно не указал нужную ему
                options.AssumeDefaultVersionWhenUnspecified = true;

                //Определяем, что будем ожидать нужную версию API в самой строке запроса или в URL-сегменте
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),//site.com/v2/getdata
                    new QueryStringApiVersionReader("version")); //site.com/getdata?version=2
            })
                .AddMvc()
                //Данный метод исправляет конечные маршруты и подставляет нужную версию API через параметр в маршруте.
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                });
        }
    }
}
