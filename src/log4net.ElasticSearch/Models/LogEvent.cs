using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using log4net.ElasticSearch.Infrastructure;

namespace log4net.ElasticSearch.Models
{
    /// <summary>
    /// Primary object which will get serialized into a json object to pass to ES. Deviating from CamelCase
    /// class members so that we can stick with the build in serializer and not take a dependency on another lib. ES
    /// exepects fields to start with lowercase letters.
    /// </summary>
    public class logEvent
    {
        public logEvent()
        {
            properties = new Dictionary<string, string>();
        }

        public object messageObject { get; set; }

        public static IEnumerable<logEvent> CreateMany(IEnumerable<LoggingEvent> loggingEvents)
        {
            return loggingEvents.Select(@event => Create(@event)).ToArray();
        }

        static logEvent Create(LoggingEvent loggingEvent)
        {
            // Added special handling of the MessageObject since it may be an exception. 
            // Exception Types require specialized serialization to prevent serialization exceptions.
            if (loggingEvent.MessageObject != null && loggingEvent.MessageObject.GetType() != typeof(string))
            {
                if (loggingEvent.MessageObject is Exception)
                {
                    logEvent.messageObject = JsonSerializableException.Create((Exception)loggingEvent.MessageObject);
                }
                else
                {
                    logEvent.messageObject = loggingEvent.MessageObject;
                }
            }
            else
            {
                logEvent.messageObject = new object();
            }

            AddProperties(loggingEvent, logEvent);

            return logEvent;
        }

        static void AddProperties(LoggingEvent loggingEvent, logEvent logEvent)
        {
            loggingEvent.Properties().Union(AppenderPropertiesFor(loggingEvent)).
                         Do(pair => logEvent.properties.Add(pair));
        }

        static IEnumerable<KeyValuePair<string, string>> AppenderPropertiesFor(LoggingEvent loggingEvent)
        {
            yield return Pair.For("@timestamp", loggingEvent.TimeStamp.ToUniversalTime().ToString("O"));
        }
    }
}
