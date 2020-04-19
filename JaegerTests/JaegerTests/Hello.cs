using System;
using System.Collections.Generic;
using System.Diagnostics;
using Jaeger;
using Jaeger.Samplers;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Util;

namespace JaegerTests
{
	internal class Hello
	{
		private readonly ITracer _tracer;
		private readonly ILogger<Hello> _logger;

		public Hello(ITracer tracer, ILogger<Hello> logger)
		{
			_tracer = tracer;
			_logger = logger;
		}

		public void SayHello(string helloTo)
		{
			var span = _tracer.BuildSpan("say-hello").Start();
			var helloString = $"Hello, {helloTo}!";
			span.Log(new Dictionary<string, object>
				{
					[LogFields.Event] = "string.Format",
					["value"] = helloString
				}
			);
			_logger.LogInformation(helloString);
			span.Log("WriteLine");
			span.Finish();
		}

		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				throw new ArgumentException("Expecting one argument");
			}

			Console.WriteLine("test");
			using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
			using (var tracer = InitTracer(nameof(Hello), loggerFactory))
			{
				var helloTo = args[0];
				new Hello(tracer, loggerFactory.CreateLogger<Hello>()).SayHello(helloTo);
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