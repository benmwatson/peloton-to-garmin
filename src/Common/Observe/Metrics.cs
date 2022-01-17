﻿using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using System;
using PromMetrics = Prometheus.Metrics;

namespace Common.Observe
{
    public static class Metrics
    {
        public static IMetricServer EnableMetricsServer(Prometheus config)
        {
            IMetricServer metricsServer = null;
            if (config.Enabled)
            {
                var port = config.Port ?? 4000;
                metricsServer = new KestrelMetricServer(port: port);
                metricsServer.Start();

                Log.Information("Metrics Server started and listening on: http://localhost:{0}/metrics", port);
            }

            return metricsServer;
        }

        public static void ValidateConfig(Observability config)
        {
            if (!config.Prometheus.Enabled) return;

            if (config.Prometheus.Port.HasValue && config.Prometheus.Port <= 0)
            {
                Log.Error("Prometheus Port must be a valid port: {@ConfigSection}.{@ConfigProperty}.", nameof(config), nameof(config.Prometheus.Port));
                throw new ArgumentException("Prometheus port must be greater than 0.", nameof(config.Prometheus.Port));
            }
        }

        public static IDisposable EnableCollector(Prometheus config)
        {
            if (config.Enabled)
                return DotNetRuntimeStatsBuilder
                    .Customize()
                    .WithContentionStats()
                    .WithJitStats()
                    .WithThreadPoolStats()
                    .WithGcStats()
                    .WithExceptionStats()
                    //.WithDebuggingMetrics(true)
                    .WithErrorHandler(ex => Log.Error(ex, "Unexpected exception occurred in prometheus-net.DotNetRuntime"))
                    .StartCollecting();

            return null;
        }

        public static class Label
        {
            public static string HttpMethod = "http_method";
            public static string HttpHost = "http_host";
            public static string HttpRequestPath = "http_request_path";
            public static string HttpRequestQuery = "http_request_query";
            public static string HttpStatusCode = "http_status_code";
            public static string HttpMessage = "http_message";

            public static string DbMethod = "db_method";
            public static string DbQuery = "db_query";

            public static string FileType = "file_type";

            public static string Count = "count";

            public static string Os = "os";
            public static string OsVersion = "os_version";
            public static string Version = "version";
            public static string DotNetRuntime = "dotnet_runtime";

            public static string ReflectionMethod = "reflection_method";
        }

        public static class HealthStatus
        {
            public static int Healthy = 2;
            public static int UnHealthy = 1;
            public static int Dead = 0;
        }
    }

    public static class DbMetrics
    {
        public static readonly Histogram DbActionDuration = PromMetrics.CreateHistogram(TagValue.P2G + "_db_duration_seconds", "Counter of db actions.", new HistogramConfiguration()
        {
            LabelNames = new[] { Metrics.Label.DbMethod, Metrics.Label.DbQuery }
        });
    }
}
