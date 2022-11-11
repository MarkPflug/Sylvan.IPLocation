using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace IPLocationWeb;

public class JsonDataOutputFormatter : OutputFormatter
{
    public JsonDataOutputFormatter()
    {
        SupportedMediaTypes.Add("application/json");
        SupportedMediaTypes.Add("text/json");
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        return
            context.ContentType == "application/json" ||
            context.ContentType == "text/json";
    }

    protected override bool CanWriteType(Type type)
    {
        return
            typeof(DbDataReader).IsAssignableFrom(type);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var data = (DbDataReader)context.Object;
        await data.WriteJsonAsync(context.HttpContext.Response.Body);
    }
}
