using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using MqttWebSocket.Configuration;
using MqttWebSocket.Logging;

namespace MqttWebSocket.Mqtt
{
    public class CustomMqttFactory
    {
        public readonly MqttFactory _mqttFactory;

        public CustomMqttFactory(MqttSettingsModel settings, ILogger<MqttServer> logger)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            // It is important to avoid injecting the logger wrapper to ensure that no
            // unused log messages are generated by the MQTTnet library. Debug logging
            // has a huge performance impact.
            if (settings.EnableDebugLogging)
            {
                var mqttNetLogger = new MqttNetLoggerWrapper(logger);
                _mqttFactory = new MqttFactory(mqttNetLogger);

                logger.LogWarning("Debug logging is enabled. Performance of MQTTnet Server is decreased!");
            }
            else
            {
                _mqttFactory = new MqttFactory();
            }

            Logger = _mqttFactory.DefaultLogger;
        }

        public IMqttNetLogger Logger { get; }

        public IMqttServer CreateMqttServer(List<IMqttServerAdapter> adapters)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));

            return _mqttFactory.CreateMqttServer(adapters);
        }
    }
}