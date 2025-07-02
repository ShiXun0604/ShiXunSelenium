using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiXunSeleniumTools
{
    /// <summary>
    /// 這個function由ChatGPT生成,完美的展示OOP的多型使用方式,以後必須好好研究和了解
    /// </summary>
    internal class StepCommandConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(StepCommand).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string action = jo["action"]?.ToString();

            StepCommand instance;
            switch (action)
            {
                case "FindElement":
                    instance = new FindElement();
                    break;
                case "WaitUntil":
                    instance = new WaitUntil();
                    break;
                case "FetchEntirePage":
                    instance = new FetchEntirePage();
                    break;
                case "SwitchToIframe":
                    instance = new SwitchToIframe();
                    break;
                case "IsElementSatisfyCondition":
                    instance = new IsElementSatisfyCondition();
                    break;
                case "GoToUrl":
                    instance = new GoToUrl();
                    break;
                case "FindSelect":
                    instance = new FindSelect();
                    break;
                case "FindCheckbox":
                    instance = new FindCheckbox();
                    break;
                case "ScrollWindow":
                    instance = new ScrollWindow();
                    break;
                case "MoveToElement":
                    instance = new MoveToElement();
                    break;
                case "ScrollOverflowDiv":
                    instance = new ScrollOverflowDiv();
                    break;
                case "If":
                    instance = new If();
                    break;
                case "Else":
                    instance = new Else();
                    break;
                case "EndIf":
                    instance = new EndIf();
                    break;
                case "ForLoop":
                    instance = new ForLoop();
                    break;
                case "EndForLoop":
                    instance = new EndForLoop();
                    break;
                case "WhileLoop":
                    instance = new WhileLoop();
                    break;
                case "EndWhileLoop":
                    instance = new EndWhileLoop();
                    break;
                case "TakeScreenshot":
                    instance = new TakeScreenshot();
                    break;
                default:
                    throw new InvalidActionValueException($"Unknown action type: \"{action}\", please check your json file");
            }
            serializer.Populate(jo.CreateReader(), instance);
            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
