using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using UnityEngine;

namespace NHNFramework
{
    [Serializable]
    public class ProtoBufUtil
    {
        private static ProtoBufUtil instance;
        public static ProtoBufUtil Instance => instance ?? (instance = new ProtoBufUtil());

        [SerializeField] public string SDescriptorPropertyName = "Descriptor";
        [SerializeField] public string SParserPropertyName = "Parser";
        [SerializeField] public string SParseFromMethodName = "ParseFrom";

        [SerializeField] public string SParseDelimitedFromMethodName = "ParseDelimitedFrom";
        //  public string ParseJsonMethodName = "ParseJson";

        public ProtoBufUtil()
        {
            if (instance == null)
                instance = this;
        }

        public ProtoBufUtil(string _descPName = "Descriptor"
            , string _parserPName = "Parser"
            , string _parseFromMName = "ParseFrom"
            , string _parseDelMName = "ParseDelimitedFrom")
        {
            if (instance == null)
                instance = this;

            SetConfig(_descPName, _parserPName, _parseFromMName, _parseDelMName);
        }

        public static void SetDefaultConfig(string _descPName = "Descriptor"
            , string _parserPName = "Parser"
            , string _parseFromMName = "ParseFrom"
            , string _parseDelMName = "ParseDelimitedFrom")
        {
            Instance.SetConfig(_descPName, _parserPName, _parseFromMName, _parseDelMName);
        }

        public void SetConfig(string _descPName = "Descriptor"
            , string _parserPName = "Parser"
            , string _parseFromMName = "ParseFrom"
            , string _parseDelMName = "ParseDelimitedFrom")
        {
            SDescriptorPropertyName = _descPName;
            SParserPropertyName = _parserPName;
            SParseFromMethodName = _parseFromMName;
            SParseDelimitedFromMethodName = _parseDelMName;
        }


        public static string GetClassName()
        {
            return "ProtoBufViewer-v001";
        }

        public static void PrintMessageReflection(IMessage message)
        {
            var descriptor = message.Descriptor;
            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                Debug.Log($"Field {field.FieldNumber} ({field.Name}): {field.Accessor.GetValue(message)}");
            }
        }

        public static IEnumerable<Type> GetAllListIMessageTypeExcludeWellKnown()
        {
            return GetAllListIMessageType(new List<string>
            {
                "Google.Protobuf.WellKnownTypes"
            });
        }

        public static IEnumerable<Type> GetAllListIMessageType(List<string> excludeNameSpaces = null)
        {
            return GetAllPublicSubclassOf(typeof(IMessage), excludeNameSpaces);
        }

        public static IEnumerable<Type> GetAllPublicSubclassOf(Type typeSearch, List<string> excludeNameSpaces = null)
        {
            // var type = typeof(IMessage);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(typeSearch.IsAssignableFrom)
                .Where(p => p.IsPublic).Where(p => p.IsClass);

            if (excludeNameSpaces != null)
            {
                foreach (var nameSpace in excludeNameSpaces)
                {
                    types = types.Where(p => !p.Namespace.Contains(nameSpace));
                }
            }

            return types;
        }

        public MessageDescriptor GetMessageDescriptor(Type IMessageClassType)
        {
            var obj = Activator.CreateInstance(IMessageClassType);
            PropertyInfo inf = IMessageClassType.GetProperty(SDescriptorPropertyName,
                BindingFlags.Static | BindingFlags.Public);
            var descObj = inf.GetValue(obj);
            if (descObj == null)
            {
                Debug.LogError($"[{GetClassName()}][GetMessageDescriptor] Descriptor Of {IMessageClassType} is NULL!");
                return null;
            }

            return (MessageDescriptor) descObj;
        }

        public static object GetParser(Type IMessageClassType, string parserPropertyName)
        {
            try
            {
                var obj = Activator.CreateInstance(IMessageClassType);
                PropertyInfo inf = IMessageClassType.GetProperty(parserPropertyName,
                    BindingFlags.Static | BindingFlags.Public);

                if (inf == null)
                {
                    Debug.LogError(
                        $"[{GetClassName()}][GetParser] Can not get Property {parserPropertyName} of type {IMessageClassType}");
                    return null;
                }

                var parser = inf.GetValue(obj);
                if (parser == null)
                {
                    Debug.LogError(
                        $"[{GetClassName()}][GetParser] Can not get parser {parserPropertyName} of type {IMessageClassType}");
                    return null;
                }

                return parser;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetClassName()}][GetParser] Get parser {parserPropertyName}" +
                               $" of type {IMessageClassType}" +
                               $" Has Exception:{ex}");
                return null;
            }
        }


        public static object ParseFrom(Type IMessageClassType, string propertyName, string methodName, object[] objs)
        {
            try
            {
                var parser = GetParser(IMessageClassType, propertyName);
                if (parser == null) return null;

                Type[] types = new Type[objs.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    types[i] = objs[i].GetType();
                }

                var parseFromMethod = parser.GetType()
                    .GetMethod(methodName, types);

                if (parseFromMethod == null)
                {
                    Debug.LogError($"[{GetClassName()}][ParseFrom] parseFromMethod {methodName} Not Found:");
                    return null;
                }

                return parseFromMethod.Invoke(parser, objs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetClassName()}][ParseFrom] Has Exception:{ex}");
                return null;
            }
        }

        public object ParseFrom(Type IMessageClassType, byte[] data)
        {
            return ParseFrom(IMessageClassType, SParserPropertyName, SParseFromMethodName, new[] {data});
        }

        public object ParseFrom(Type IMessageClassType, ByteString data)
        {
            return ParseFrom(IMessageClassType, SParserPropertyName, SParseFromMethodName, new[] {data});
        }

        public object ParseFrom(Type IMessageClassType, byte[] data, int offset, int length)
        {
            return ParseFrom(IMessageClassType, SParserPropertyName, SParseFromMethodName,
                new object[] {data, offset, length});
        }

        public object ParseFrom(Type IMessageClassType, Stream input)
        {
            return ParseFrom(IMessageClassType, SParserPropertyName, SParseFromMethodName, new[] {input});
        }

        public object ParseFrom(Type IMessageClassType, CodedInputStream input)
        {
            return ParseFrom(IMessageClassType, SParserPropertyName, SParseFromMethodName, new[] {input});
        }

        public object ParseDelimitedFrom(Type IMessageClassType, Stream input)
        {
            return ParseFrom(IMessageClassType, SParserPropertyName, SParseDelimitedFromMethodName, new[] {input});
        }

        public object ParseJson(Type IMessageClassType, string input)
        {
            return JsonParser.Default.Parse(input, GetMessageDescriptor(IMessageClassType));
            // return ParseFrom(IMessageClassType, ParserPropertyName, ParseJsonMethodName, new []{input});
        }

        public static string FromBinaryToJson(Type IMessageClassType, byte[] binaryData)
        {
            var obj = Instance.ParseFrom(IMessageClassType, binaryData);
            return JsonFormatter.Default.Format((IMessage) obj);
        }

        public static string FromBinaryToJson(Type IMessageClassType, string jsonData)
        {
            var obj = Instance.ParseJson(IMessageClassType, jsonData);
            return JsonFormatter.Default.Format((IMessage) obj);
        }

        public static string FromBinaryToJson(Type IMessageClassType, object messageData)
        {
            return JsonFormatter.Default.Format((IMessage) messageData);
        }

        public static byte[] FromJsonToBinary(Type IMessageClassType, string jsonData)
        {
            var obj = Instance.ParseJson(IMessageClassType, jsonData);
            return ((IMessage) obj).ToByteArray();
        }
    }
}
