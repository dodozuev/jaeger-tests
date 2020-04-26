using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace JaegerTestServer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class FormatController : ControllerBase
	{
		private readonly ITracer _tracer;

		public FormatController(ITracer tracer)
		{
			_tracer = tracer;
		}

		[HttpGet("{helloTo}", Name = "GetFormat")]
		public string Get(string helloTo)
		{
			using var scope = _tracer.BuildSpan("format-controller").StartActive();
			var formattedHelloString = $"Hello, {helloTo}!";
			scope.Span.Log(new Dictionary<string, object>
			{
				[LogFields.Event] = "string-format",
				["value"] = formattedHelloString
			});
			return formattedHelloString;
		}

		public static IScope StartServerSpan(ITracer tracer, IDictionary<string, string> headers, string operationName)
		{
			ISpanBuilder spanBuilder;
			try
			{
				ISpanContext parentSpanCtx =
					tracer.Extract(BuiltinFormats.HttpHeaders, new TextMapExtractAdapter(headers));

				spanBuilder = tracer.BuildSpan(operationName);
				if (parentSpanCtx != null)
				{
					spanBuilder = spanBuilder.AsChildOf(parentSpanCtx);
				}
			}
			catch (Exception)
			{
				spanBuilder = tracer.BuildSpan(operationName);
			}

			// could add more tags like http.url
			return spanBuilder.WithTag(Tags.SpanKind, Tags.SpanKindServer).StartActive(true);
		}
	}
}