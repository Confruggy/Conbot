using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Disqord.Bot.Hosting;

using HandlebarsDotNet;
using HandlebarsDotNet.Collections;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Runtime;

using static Disqord.Markdown;

namespace Conbot
{
    public class HandlebarsService : DiscordBotService, IHandlebars
    {
        private readonly IHandlebars _handlebars = Handlebars.Create();

        public HandlebarsConfiguration Configuration => _handlebars.Configuration;

        public HandlebarsTemplate<TextWriter, object, object> Compile(TextReader template)
            => _handlebars.Compile(template);

        public HandlebarsTemplate<object, object> Compile(string template)
            => _handlebars.Compile(template);

        public HandlebarsTemplate<object, object> CompileView(string templatePath)
            => _handlebars.CompileView(templatePath);

        public HandlebarsTemplate<TextWriter, object, object> CompileView(string templatePath,
            ViewReaderFactory readerFactoryFactory)
            => _handlebars.CompileView(templatePath, readerFactoryFactory);

        public DisposableContainer Configure()
            => _handlebars.Configure();

        public IIndexed<string, IHelperDescriptor<BlockHelperOptions>> GetBlockHelpers()
            => _handlebars.GetBlockHelpers();

        public IIndexed<string, IHelperDescriptor<HelperOptions>> GetHelpers()
            => _handlebars.GetHelpers();

        public void RegisterDecorator(string helperName, HandlebarsBlockDecorator helperFunction)
            => _handlebars.RegisterDecorator(helperName, helperFunction);

        public void RegisterDecorator(string helperName, HandlebarsDecorator helperFunction)
            => _handlebars.RegisterDecorator(helperName, helperFunction);

        public void RegisterDecorator(string helperName, HandlebarsBlockDecoratorVoid helperFunction)
            => _handlebars.RegisterDecorator(helperName, helperFunction);

        public void RegisterDecorator(string helperName, HandlebarsDecoratorVoid helperFunction)
            => _handlebars.RegisterDecorator(helperName, helperFunction);

        public void RegisterTemplate(string templateName, HandlebarsTemplate<TextWriter, object, object> template)
            => _handlebars.RegisterTemplate(templateName, template);

        public void RegisterTemplate(string templateName, string template)
            => _handlebars.RegisterTemplate(templateName, template);

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _handlebars.Configuration.TextEncoder = new DummyTextEncoder();

            RegisterDefaultHelpers();

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void RegisterDefaultHelpers()
        {
            this.RegisterHelper("formatDate", (writer, context, args) =>
            {
                long unixTimestamp = args[0] as long? ?? 0;

                var format = ((args[1] as string)?.ToLowerInvariant() ?? string.Empty) switch
                {
                    "longdate" => TimestampFormat.LongDate,
                    "longdatetime" => TimestampFormat.LongDateTime,
                    "longtime" => TimestampFormat.LongTime,
                    "relativetime" => TimestampFormat.RelativeTime,
                    "shortdate" => TimestampFormat.ShortDate,
                    "shortdatetime" => TimestampFormat.ShortDateTime,
                    "shorttime" => TimestampFormat.ShortTime,
                    _ => TimestampFormat.LongDate,
                };

                writer.WriteSafeString(Timestamp(unixTimestamp, format));
            });
        }
    }
}
