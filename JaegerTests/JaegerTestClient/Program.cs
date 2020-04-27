using System;
using System.Collections.Generic;
using System.Net;
using Jaeger;
using Jaeger.Samplers;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace JaegerTestClient
{
	internal class HelloActive
	{
		private readonly ITracer _tracer;
		private readonly ILogger<HelloActive> _logger;
		private readonly WebClient _webClient = new WebClient();

		public HelloActive(ITracer tracer, ILogger<HelloActive> logger)
		{
			_tracer = tracer;
			_logger = logger;
		}

		public void SayHello(string helloTo, string greeting)
		{
			using var scope = _tracer.BuildSpan("say-hello").StartActive(true);
			scope.Span.SetTag("hello-to", helloTo);
			scope.Span.SetBaggageItem("greeting", greeting);
			var helloString = FormatString(helloTo);
			PrintHello(helloString);
		}

		private string FormatString(string helloTo)
		{
			using var scope = _tracer.BuildSpan("format-string").StartActive(true);
			var url = $"http://localhost:8081/api/format/{helloTo}";

			var span = scope.Span
				.SetTag(Tags.SpanKind, Tags.SpanKindClient)
				.SetTag(Tags.HttpMethod, "GET")
				.SetTag(Tags.HttpUrl, url);

			var dictionary = new Dictionary<string, string>();
			_tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(dictionary));

			foreach (var entry in dictionary)
				_webClient.Headers.Add(entry.Key, entry.Value);

			var helloString = _webClient.DownloadString(url);

			scope.Span.Log(new Dictionary<string, object>
			{
				[LogFields.Event] = "string.Format",
				["value"] = helloString
			});

			return helloString;
		}

		private void PrintHello(string helloString)
		{
			using var scope = _tracer.BuildSpan("print-hello").StartActive(true);

			_logger.LogInformation(helloString);
			scope.Span.Log(new Dictionary<string, object>
			{
				[LogFields.Event] = "WriteLine"
			});
		}

		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				throw new ArgumentException("Expecting one argument");
			}

			Console.WriteLine("test");
			using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
			using (var tracer = InitTracer(nameof(HelloActive), loggerFactory))
			{
				var helloTo = args[0];
				var greeting = args[1];
				new HelloActive(tracer, loggerFactory.CreateLogger<HelloActive>()).SayHello(helloTo, greeting);
			}
		}

		private static Tracer InitTracer(string serviceName, ILoggerFactory loggerFactory)
		{
			var samplerConfiguration = new Configuration.SamplerConfiguration(loggerFactory)
				.WithType(ConstSampler.Type)
				.WithParam(1);

			var reporterConfiguration = new Configuration.ReporterConfiguration(loggerFactory)
				.WithLogSpans(true);

			return (Tracer) new Configuration(serviceName, loggerFactory)
				.WithSampler(samplerConfiguration)
				.WithReporter(reporterConfiguration)
				.GetTracer();
		}
	}
}